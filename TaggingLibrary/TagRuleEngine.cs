// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public sealed class TagRuleEngine
    {
        public const string AbstractProperty = "abstract";

        private readonly HashSet<string> abstractTags = new HashSet<string>();
        private readonly Dictionary<string, ImmutableHashSet<string>> aliasMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly Dictionary<string, string> renameMap = new Dictionary<string, string>();
        private readonly Dictionary<string, ImmutableHashSet<string>> specializationChildMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly Dictionary<string, ImmutableHashSet<string>> specializationChildTotalMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly Dictionary<string, ImmutableDictionary<string, TagRule>> specializationParentRuleMap = new Dictionary<string, ImmutableDictionary<string, TagRule>>();
        private readonly Dictionary<string, ImmutableHashSet<string>> specializationParentTotalMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly ILookup<TagOperator, TagRule> tagRules;

        public TagRuleEngine(IEnumerable<TagRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var renameMap = new Dictionary<string, ImmutableHashSet<string>>();
            foreach (var rule in rules)
            {
                if (rule.Operator == TagOperator.Definition)
                {
                    if (rule.Left.Count > 1 || rule.Right.Count > 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(rules), $"The operator '{rule.Operator}' requires a single tag on both the left and right hand sides in rule '{rule}'");
                    }

                    var fromTag = rule.Left.Single();
                    var toTag = rule.Right.Single();

                    AddParentToChild(fromTag, toTag, renameMap);
                }
                else if (rule.Operator == TagOperator.Specialization)
                {
                    if (rule.Right.Count > 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(rules), $"The operator '{rule.Operator}' requires a single tag on the right hand side in rule '{rule}'");
                    }
                }
            }

            foreach (var rename in renameMap)
            {
                var leaves = new HashSet<string>();
                var seen = new HashSet<string> { rename.Key };
                var queue = new Queue<string>(rename.Value);
                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();
                    if (seen.Add(item))
                    {
                        if (renameMap.TryGetValue(item, out var nextRenames))
                        {
                            foreach (var next in nextRenames)
                            {
                                queue.Enqueue(next);
                            }
                        }
                        else
                        {
                            leaves.Add(item);
                        }
                    }
                }

                var set = leaves.Count == 0 ? seen : leaves;
                var target = set.Min();

                this.renameMap[rename.Key] = target;
                AddParentToChild(target, rename.Key, this.aliasMap);
            }

            this.tagRules = this.SimplifyRules(rules).ToLookup(r => r.Operator);

            foreach (var rule in this.tagRules[TagOperator.Property])
            {
                foreach (var property in rule.Right)
                {
                    switch (property)
                    {
                        case AbstractProperty:
                            this.abstractTags.UnionWith(rule.Left);
                            break;
                    }
                }
            }

            foreach (var rule in this.tagRules[TagOperator.Specialization])
            {
                foreach (var fromTag in rule.Left)
                {
                    var toTag = rule.Right.Single();

                    AddParentToChild(fromTag, toTag, rule, this.specializationParentRuleMap);
                    AddParentToChild(toTag, fromTag, this.specializationChildMap);
                    AddParentToChildren(fromTag, toTag, this.specializationChildTotalMap, this.specializationParentTotalMap);
                    AddParentToChildren(toTag, fromTag, this.specializationParentTotalMap, this.specializationChildTotalMap);
                }
            }
        }

        public TagInfo this[string tag]
        {
            get
            {
                if (string.IsNullOrEmpty(tag))
                {
                    throw new ArgumentNullException(nameof(tag));
                }

                tag = this.Rename(tag);
                this.specializationParentRuleMap.TryGetValue(tag, out var parentsWithRules);
                this.specializationChildMap.TryGetValue(tag, out var children);
                this.specializationParentTotalMap.TryGetValue(tag, out var ancestors);
                this.specializationChildTotalMap.TryGetValue(tag, out var descendants);
                this.aliasMap.TryGetValue(tag, out var aliases);
                return new TagInfo(
                    tag: tag,
                    isAbstract: this.abstractTags.Contains(tag),
                    aliases: aliases ?? ImmutableHashSet<string>.Empty,
                    properties: ImmutableList.CreateRange(this.tagRules[TagOperator.Property].Where(t => t.Left.Contains(tag)).SelectMany(t => t.Right)),
                    parents: ImmutableHashSet.CreateRange(parentsWithRules?.Keys ?? Enumerable.Empty<string>()),
                    children: children ?? ImmutableHashSet<string>.Empty,
                    ancestors: ancestors ?? ImmutableHashSet<string>.Empty,
                    descendants: descendants ?? ImmutableHashSet<string>.Empty);
            }
        }

        public AnalysisResult Analyze(IEnumerable<string> tags)
        {
            var normalizedTags = ImmutableHashSet.CreateRange(tags.Select(this.Rename));
            if (normalizedTags.Count == 0)
            {
                return AnalysisResult.Empty;
            }

            var ruleLookup = this.tagRules;

            var effectiveTags = normalizedTags;
            foreach (var tag in effectiveTags)
            {
                if (this.specializationParentTotalMap.TryGetValue(tag, out var specializes))
                {
                    effectiveTags = effectiveTags.Union(specializes);
                }
            }

            var missingTagSets = ImmutableList<RuleResult<ImmutableHashSet<string>>>.Empty;
            var effectiveAndSingleMissingTags = new HashSet<string>(effectiveTags);
            var changed = true;
            while (changed)
            {
                changed = false;
                var groups = from rule in ruleLookup[TagOperator.Implication]
                             where effectiveAndSingleMissingTags.IsSupersetOf(rule.Left)
                             where !rule.Right.Overlaps(effectiveAndSingleMissingTags)
                             group rule by rule.Right.Count == 1 into g
                             orderby g.Key descending
                             select g;
                var firstGroup = groups.FirstOrDefault();
                if (firstGroup != null)
                {
                    foreach (var rule in firstGroup)
                    {
                        missingTagSets = missingTagSets.Add(RuleResult.Create(rule, rule.Right));
                        if (rule.Right.Count == 1)
                        {
                            var right = rule.Right.Single();
                            if (effectiveAndSingleMissingTags.Add(right))
                            {
                                if (this.specializationParentTotalMap.TryGetValue(right, out var specializes))
                                {
                                    effectiveAndSingleMissingTags.UnionWith(specializes);
                                }

                                changed = true;
                                break;
                            }
                        }
                    }
                }
            }

            var suggestedTags = ImmutableHashSet.CreateRange(missingTagSets.SelectMany(s => s.Result.Select(c => RuleResult.Create(s.Rule, c))));
            foreach (var rule in ruleLookup[TagOperator.Suggestion])
            {
                if (effectiveAndSingleMissingTags.IsSupersetOf(rule.Left) && !effectiveAndSingleMissingTags.Overlaps(rule.Right))
                {
                    suggestedTags = suggestedTags.Union(rule.Right.Select(c => RuleResult.Create(rule, c)));
                }
            }

            foreach (var tag in effectiveAndSingleMissingTags)
            {
                if (this.specializationChildTotalMap.TryGetValue(tag, out var children) &&
                    !effectiveAndSingleMissingTags.Overlaps(children))
                {
                    suggestedTags = suggestedTags.Union(
                        from child in children
                        from directParent in this.specializationParentRuleMap[child]
                        let parentTag = directParent.Key
                        where parentTag == tag || (this.specializationParentTotalMap.TryGetValue(parentTag, out var grandparents) && grandparents.Contains(tag))
                        select RuleResult.Create(directParent.Value, child));
                }
            }

            ImmutableHashSet<RuleResult<string>> ExpandAbstractTags(IEnumerable<RuleResult<string>> results) => ImmutableHashSet.CreateRange(results.SelectMany(r =>
            {
                if (this.abstractTags.Contains(r.Result))
                {
                    this.specializationChildTotalMap.TryGetValue(r.Result, out var children);
                    return children == null
                        ? Enumerable.Empty<RuleResult<string>>()
                        : children.Where(c => !this.abstractTags.Contains(c)).Select(c => RuleResult.Create(r.Rule, c));
                }
                else
                {
                    return new[] { r };
                }
            }));

            suggestedTags = ExpandAbstractTags(suggestedTags);

            return new AnalysisResult(
                normalizedTags,
                effectiveTags,
                missingTagSets,
                suggestedTags);
        }

        public IEnumerable<string> GetAllTagProperties(string tag) =>
            this.GetTagProperties(tag).Concat(this.GetInheritedTagProperties(tag));

        public IEnumerable<string> GetInheritedTagProperties(string tag)
        {
            tag = this.Rename(tag);

            var visited = new HashSet<string>() { tag };
            var queue = new Queue<string>();
            this.specializationParentRuleMap.TryGetValue(tag, out var parentsWithRules);
            if (parentsWithRules == null)
            {
                yield break;
            }

            foreach (var parent in parentsWithRules)
            {
                if (visited.Add(parent.Key))
                {
                    queue.Enqueue(parent.Key);
                }
            }

            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                foreach (var property in this.GetTagProperties(next))
                {
                    if (property != AbstractProperty)
                    {
                        yield return property;
                    }
                }

                this.specializationParentRuleMap.TryGetValue(next, out parentsWithRules);
                if (parentsWithRules != null)
                {
                    foreach (var parent in parentsWithRules)
                    {
                        if (visited.Add(parent.Key))
                        {
                            queue.Enqueue(parent.Key);
                        }
                    }
                }
            }
        }

        public IEnumerable<string> GetKnownTags() =>
            this.tagRules.SelectMany(g => g).SelectMany(r => r.Operator == TagOperator.Property ? r.Left : r.Left.Concat(r.Right)).Select(this.Rename).Distinct();

        public ImmutableHashSet<string> GetTagAliases(string tag) =>
            this.aliasMap.TryGetValue(this.Rename(tag), out var set) ? set : ImmutableHashSet<string>.Empty;

        public ImmutableHashSet<string> GetTagAncestors(string tag) =>
            this.specializationParentTotalMap.TryGetValue(this.Rename(tag), out var set) ? set : ImmutableHashSet<string>.Empty;

        public ImmutableHashSet<string> GetTagChildren(string tag) =>
            this.specializationChildMap.TryGetValue(this.Rename(tag), out var set) ? set : ImmutableHashSet<string>.Empty;

        public ImmutableHashSet<string> GetTagDescendants(string tag) =>
            this.specializationChildTotalMap.TryGetValue(this.Rename(tag), out var set) ? set : ImmutableHashSet<string>.Empty;

        public ImmutableHashSet<string> GetTagParents(string tag) =>
            this.specializationParentRuleMap.TryGetValue(this.Rename(tag), out var dict) ? ImmutableHashSet.CreateRange(dict.Keys) : ImmutableHashSet<string>.Empty;

        public IEnumerable<string> GetTagProperties(string tag)
        {
            tag = this.Rename(tag);
            return this.tagRules[TagOperator.Property].Where(t => t.Left.Contains(tag)).SelectMany(t => t.Right);
        }

        public string Rename(string tag) =>
            this.renameMap.TryGetValue(tag, out var renamed) ? renamed : tag;

        public IEnumerable<ImmutableHashSet<string>> TagSetsThatSuggest(string target) => this.tagRules[TagOperator.Suggestion].Where(r => r.Right.Contains(target)).Select(r => r.Left);

        private static void AddParentToChild(string fromTag, string toTag, TagRule rule, Dictionary<string, ImmutableDictionary<string, TagRule>> map)
        {
            if (!map.TryGetValue(fromTag, out var parents))
            {
                parents = ImmutableDictionary<string, TagRule>.Empty;
            }

            if (!parents.ContainsKey(toTag))
            {
                map[fromTag] = parents.Add(toTag, rule);
            }
        }

        private static void AddParentToChild(string fromTag, string toTag, Dictionary<string, ImmutableHashSet<string>> map)
        {
            if (!map.TryGetValue(fromTag, out var parents))
            {
                parents = ImmutableHashSet<string>.Empty;
            }

            if (!parents.Contains(toTag))
            {
                map[fromTag] = parents.Add(toTag);
            }
        }

        private static void AddParentToChildren(string parent, string child, Dictionary<string, ImmutableHashSet<string>> parentMap, Dictionary<string, ImmutableHashSet<string>> childMap)
        {
            if (!parentMap.TryGetValue(child, out var parents))
            {
                parents = ImmutableHashSet<string>.Empty;
            }

            if (parentMap.TryGetValue(parent, out var grandparents))
            {
                parents = parents.Union(grandparents);
            }

            parents = parents.Add(parent);

            var queue = new Queue<string>();
            var seen = new HashSet<string>();
            queue.Enqueue(child);
            while (queue.Count > 0)
            {
                var currentChild = queue.Dequeue();
                if (!seen.Add(currentChild))
                {
                    continue;
                }

                if (!parentMap.TryGetValue(currentChild, out var currentChildParents))
                {
                    currentChildParents = ImmutableHashSet<string>.Empty;
                }

                parentMap[currentChild] = currentChildParents.Union(parents.Remove(currentChild));

                if (childMap.TryGetValue(currentChild, out var grandchildren))
                {
                    foreach (var grandchild in grandchildren)
                    {
                        queue.Enqueue(grandchild);
                    }
                }
            }
        }

        private IEnumerable<TagRule> SimplifyRules(IEnumerable<TagRule> rules)
        {
            foreach (var r in rules)
            {
                if (r.Operator == TagOperator.Definition)
                {
                    yield return r;
                    continue;
                }

                TagRule rule;
                if (r.Left.Any(this.renameMap.ContainsKey) || (r.Operator != TagOperator.Property && r.Right.Any(this.renameMap.ContainsKey)))
                {
                    rule = new TagRule(
                        ImmutableHashSet.CreateRange(r.Left.Select(this.Rename)),
                        r.Operator,
                        r.Operator != TagOperator.Property ? ImmutableHashSet.CreateRange(r.Right.Select(this.Rename)) : r.Right);
                }
                else
                {
                    rule = r;
                }

                if (rule.Operator == TagOperator.BidirectionalImplication ||
                    rule.Operator == TagOperator.BidirectionalSuggestion)
                {
                    var singleDirection = (TagOperator)((int)r.Operator + 1);
                    yield return new TagRule(r.Left, singleDirection, r.Right);
                    foreach (var newLeft in rule.Right)
                    {
                        foreach (var newRight in rule.Left)
                        {
                            yield return new TagRule(newLeft, singleDirection, newRight);
                        }
                    }
                }
                else
                {
                    yield return rule;
                }
            }
        }
    }
}
