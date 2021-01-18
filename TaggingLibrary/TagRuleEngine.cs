// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace TaggingLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    /// <summary>
    /// Provides lookup and analysis for a set of <see cref="TagRule">TagRules</see>.
    /// </summary>
    public sealed class TagRuleEngine
    {
        /// <summary>
        /// Gets a string property that represents an abstract tag.
        /// </summary>
        public const string AbstractProperty = "abstract";

        private readonly Dictionary<string, TagRule> abstractTags = new Dictionary<string, TagRule>();
        private readonly Dictionary<string, ImmutableHashSet<string>> aliasMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly Dictionary<string, string> renameMap = new Dictionary<string, string>();
        private readonly Dictionary<string, ImmutableHashSet<string>> specializationChildMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly Dictionary<string, ImmutableHashSet<string>> specializationChildTotalMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly Dictionary<string, ImmutableDictionary<string, TagRule>> specializationParentRuleMap = new Dictionary<string, ImmutableDictionary<string, TagRule>>();
        private readonly Dictionary<string, ImmutableHashSet<string>> specializationParentTotalMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly ILookup<TagOperator, TagRule> tagRules;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagRuleEngine"/> class.
        /// </summary>
        /// <param name="rules">The set of rules that the engine will use.</param>
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

            this.tagRules = TagRuleEngine.SimplifyRules(this.NormalizeRules(rules)).ToLookup(r => r.Operator);

            foreach (var rule in this.tagRules[TagOperator.Property])
            {
                foreach (var property in rule.Right)
                {
                    switch (property)
                    {
                        case AbstractProperty:
                            foreach (var left in rule.Left)
                            {
                                this.abstractTags[left] = rule;
                            }

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

        /// <summary>
        /// Gets info for the specified tag.
        /// </summary>
        /// <param name="tag">The tag for which information will be retrieved.</param>
        /// <returns>A <see cref="TagInfo"/> object that contains info on the specified tag.</returns>
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
                    isAbstract: this.abstractTags.ContainsKey(tag),
                    aliases: aliases ?? ImmutableHashSet<string>.Empty,
                    properties: ImmutableList.CreateRange(this.tagRules[TagOperator.Property].Where(t => t.Left.Contains(tag)).SelectMany(t => t.Right)),
                    parents: ImmutableHashSet.CreateRange(parentsWithRules?.Keys ?? Enumerable.Empty<string>()),
                    children: children ?? ImmutableHashSet<string>.Empty,
                    ancestors: ancestors ?? ImmutableHashSet<string>.Empty,
                    descendants: descendants ?? ImmutableHashSet<string>.Empty);
            }
        }

        /// <summary>
        /// Simplifies the specified set of rules by expanding bidirectional rules.
        /// </summary>
        /// <param name="rules">The set of rules to simplify.</param>
        /// <returns>The simplified set of rules, with bidirectional rules expanded.</returns>
        public static IEnumerable<TagRule> SimplifyRules(IEnumerable<TagRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            foreach (var rule in rules)
            {
                if (rule.Operator == TagOperator.BidirectionalImplication ||
                    rule.Operator == TagOperator.BidirectionalSuggestion ||
                    rule.Operator == TagOperator.MutualExclusion)
                {
                    var singleDirection = (TagOperator)((int)rule.Operator + 1);
                    yield return new TagRule(rule.Left, singleDirection, rule.Right);
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

        /// <summary>
        /// Analyzes the specified set of tags and produces an <see cref="AnalysisResult"/>.
        /// </summary>
        /// <param name="tags">The set of tags to analyze.</param>
        /// <param name="rejected">The set of rejected tags to consider.</param>
        /// <returns>The result of the analysis.</returns>
        public AnalysisResult Analyze(IEnumerable<string> tags, IEnumerable<string> rejected = null)
        {
            var normalizedTags = ImmutableHashSet.CreateRange(tags.Select(this.Rename));
            var effectiveTags = this.GetTagsAndAncestors(normalizedTags);
            if (effectiveTags.Count == 0)
            {
                return AnalysisResult.Empty;
            }

            var normalizedRejected = ImmutableHashSet.CreateRange((rejected ?? Enumerable.Empty<string>()).Select(this.Rename));
            var effectiveRejected = this.GetTagsAndDescendants(normalizedRejected);
            var existingRejectedTags = normalizedTags.Intersect(effectiveRejected);

            var violatedExclusions = ImmutableList<TagRule>.Empty;
            var singleExcluded = ImmutableHashSet.CreateBuilder<string>();
            foreach (var rule in this.tagRules[TagOperator.Exclusion])
            {
                if (effectiveTags.IsSupersetOf(rule.Left))
                {
                    if (rule.Right.Count == 1)
                    {
                        singleExcluded.Add(rule.Right.Single());
                    }

                    var exceptEffective = rule.Right.Except(effectiveTags);
                    if (exceptEffective.Count == 0)
                    {
                        violatedExclusions = violatedExclusions.Add(rule);
                    }
                    else if (exceptEffective.Count == 1)
                    {
                        singleExcluded.Add(exceptEffective.Single());
                    }
                }
            }

            var effectiveExcluded = this.GetTagsAndDescendants(singleExcluded).Union(effectiveRejected);

            var missingTagSets = ImmutableList<RuleResult<ImmutableHashSet<string>>>.Empty;
            var singleMissingTags = new Dictionary<string, TagRule>();
            var effectiveAndSingleMissingTags = new HashSet<string>(effectiveTags);
            var changed = true;
            while (changed)
            {
                changed = false;
                var groups = from rule in this.tagRules[TagOperator.Implication]
                             where effectiveAndSingleMissingTags.IsSupersetOf(rule.Left)
                             where !rule.Right.Overlaps(effectiveAndSingleMissingTags)
                             let effectiveRight = rule.Right.Except(effectiveExcluded)
                             where effectiveRight.Count > 0
                             group new { Rule = rule, Right = effectiveRight } by effectiveRight.Count == 1 into g
                             orderby g.Key descending
                             select g;
                var firstGroup = groups.FirstOrDefault();
                if (firstGroup != null)
                {
                    foreach (var pair in firstGroup)
                    {
                        var rule = pair.Rule;
                        var effectiveRight = pair.Right;
                        missingTagSets = missingTagSets.Add(RuleResult.Create(rule, effectiveRight));
                        if (effectiveRight.Count == 1)
                        {
                            var right = effectiveRight.Single();
                            singleMissingTags[right] = rule;
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

            var suggestedTags = ImmutableList.CreateRange(missingTagSets.SelectMany(s => s.Result.Select(c => RuleResult.Create(s.Rules, c))));
            foreach (var rule in this.tagRules[TagOperator.Suggestion])
            {
                if (effectiveAndSingleMissingTags.IsSupersetOf(rule.Left) && !effectiveAndSingleMissingTags.Overlaps(rule.Right))
                {
                    var effectiveRight = rule.Right.Except(effectiveExcluded);
                    suggestedTags = suggestedTags.AddRange(effectiveRight.Select(c => RuleResult.Create(rule, c)));
                }
            }

            foreach (var tag in effectiveAndSingleMissingTags.Where(t => !this.abstractTags.ContainsKey(t)))
            {
                var isSingleMissing = singleMissingTags.TryGetValue(tag, out var sourceRule);
                if (this.specializationChildTotalMap.TryGetValue(tag, out var children) &&
                    !effectiveAndSingleMissingTags.Overlaps(children))
                {
                    suggestedTags = suggestedTags.AddRange(
                        from child in children
                        where !this.abstractTags.ContainsKey(child) && !effectiveExcluded.Contains(child)
                        from directParent in this.specializationParentRuleMap[child]
                        let parentTag = directParent.Key
                        where parentTag == tag || (this.specializationParentTotalMap.TryGetValue(parentTag, out var grandparents) && grandparents.Contains(tag))
                        select isSingleMissing
                            ? RuleResult.Create(new[] { sourceRule, directParent.Value }, child)
                            : RuleResult.Create(directParent.Value, child));
                }
            }

            IEnumerable<RuleResult<string>> ExpandAbtractTags(RuleResult<string> result)
            {
                if (this.abstractTags.TryGetValue(result.Result, out var abstractRule))
                {
                    var sharedRules = result.Rules;

                    if (singleMissingTags.TryGetValue(result.Result, out var sourceRule))
                    {
                        sharedRules.Add(sourceRule);
                    }

                    sharedRules.Add(abstractRule);

                    var visited = new HashSet<string>();
                    var toVisit = new Queue<string>();
                    toVisit.Enqueue(result.Result);
                    while (toVisit.Count > 0)
                    {
                        var tag = toVisit.Dequeue();
                        if (visited.Add(tag) && this.specializationChildMap.TryGetValue(tag, out var children))
                        {
                            foreach (var child in children)
                            {
                                if (!effectiveExcluded.Contains(child))
                                {
                                    if (!this.abstractTags.ContainsKey(child))
                                    {
                                        yield return RuleResult.Create(sharedRules, child);
                                    }
                                }

                                toVisit.Enqueue(child);
                            }
                        }
                    }
                }
                else
                {
                    yield return result;
                }
            }

            suggestedTags = ImmutableList.CreateRange(suggestedTags.SelectMany(ExpandAbtractTags));

            return new AnalysisResult(
                normalizedTags,
                effectiveTags,
                existingRejectedTags,
                violatedExclusions,
                missingTagSets,
                suggestedTags);
        }

        /// <summary>
        /// Enumerates the properties and inherited properties of the specified tag.
        /// </summary>
        /// <param name="tag">The tag for which to enumerate properties.</param>
        /// <returns>An enumerable collection of string properties.</returns>
        /// <remarks>As this enumerates properties from multiple rules, it may contain duplicates.</remarks>
        public IEnumerable<string> GetAllTagProperties(string tag) =>
            this.GetTagProperties(tag).Concat(this.GetInheritedTagProperties(tag));

        /// <summary>
        /// Enumerates the inherited properties of the specified tag.
        /// </summary>
        /// <param name="tag">The tag for which to enumerate properties.</param>
        /// <returns>An enumerable collection of string properties.</returns>
        /// <remarks>As this enumerates properties from multiple rules, it may contain duplicates.</remarks>
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

        /// <summary>
        /// Gets an enumerable collection of known tags.
        /// </summary>
        /// <param name="canonicalOnly"><c>true</c>, to only return canonical forms. <c>false</c> to return all forms of the tags.</param>
        /// <returns>A distinct, enumerable collection of known tags.</returns>
        public IEnumerable<string> GetKnownTags(bool canonicalOnly = false)
        {
            var tags = this.tagRules.SelectMany(g => g).SelectMany(r => r.Operator == TagOperator.Property ? r.Left : r.Left.Concat(r.Right));

            if (canonicalOnly)
            {
                tags = tags.Select(this.Rename);
            }

            return tags.Distinct();
        }

        /// <summary>
        /// Gets the aliases for the specified tag.
        /// </summary>
        /// <param name="tag">The tag for which to fetch aliases.</param>
        /// <returns>The aliases of the specified tag.</returns>
        public ImmutableHashSet<string> GetTagAliases(string tag) =>
            this.aliasMap.TryGetValue(this.Rename(tag), out var set) ? set : ImmutableHashSet<string>.Empty;

        /// <summary>
        /// Gets all ancestors of the specified tag.
        /// </summary>
        /// <param name="tag">The tag for which to fetch ancestors.</param>
        /// <returns>The ancestors of the specified tag.</returns>
        public ImmutableHashSet<string> GetTagAncestors(string tag) =>
            this.specializationParentTotalMap.TryGetValue(this.Rename(tag), out var set) ? set : ImmutableHashSet<string>.Empty;

        /// <summary>
        /// Gets the children of the specified tag.
        /// </summary>
        /// <param name="tag">The tag for which to fetch children.</param>
        /// <returns>The children of the specified tag.</returns>
        public ImmutableHashSet<string> GetTagChildren(string tag) =>
            this.specializationChildMap.TryGetValue(this.Rename(tag), out var set) ? set : ImmutableHashSet<string>.Empty;

        /// <summary>
        /// Gets all descendants of the specified tag.
        /// </summary>
        /// <param name="tag">The tag for which to fetch descendants.</param>
        /// <returns>The descendants of the specified tag.</returns>
        public ImmutableHashSet<string> GetTagDescendants(string tag) =>
            this.specializationChildTotalMap.TryGetValue(this.Rename(tag), out var set) ? set : ImmutableHashSet<string>.Empty;

        /// <summary>
        /// Gets the parents of the specified tag.
        /// </summary>
        /// <param name="tag">The tag for which to fetch parents.</param>
        /// <returns>The parents of the specified tag.</returns>
        public ImmutableHashSet<string> GetTagParents(string tag) =>
            this.specializationParentRuleMap.TryGetValue(this.Rename(tag), out var dict) ? ImmutableHashSet.CreateRange(dict.Keys) : ImmutableHashSet<string>.Empty;

        /// <summary>
        /// Enumerates the properties of the specified tag.
        /// </summary>
        /// <param name="tag">The tag for which to enumerate properties.</param>
        /// <returns>An enumerable collection of string properties.</returns>
        /// <remarks>
        /// <para>To enumerate inherited properties, use <see cref="GetInheritedTagProperties(string)"/> or <see cref="GetAllTagProperties(string)"/>.</para>
        /// <para>As this enumerates properties from multiple rules, it may contain duplicates.</para>
        /// </remarks>
        public IEnumerable<string> GetTagProperties(string tag)
        {
            tag = this.Rename(tag);
            return this.tagRules[TagOperator.Property].Where(t => t.Left.Contains(tag)).SelectMany(t => t.Right);
        }

        /// <summary>
        /// Renames rules according the canonical form for the configured rules.
        /// </summary>
        /// <param name="rules">The set of rules to normalize.</param>
        /// <returns>The normalized set of rules, with all rules renamed to their canoical form.</returns>
        public IEnumerable<TagRule> NormalizeRules(IEnumerable<TagRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            foreach (var rule in rules)
            {
                if (rule.Operator != TagOperator.Definition)
                {
                    var replaceLeft = rule.Left.Any(this.renameMap.ContainsKey);
                    var replaceRight = rule.Operator != TagOperator.Property && rule.Right.Any(this.renameMap.ContainsKey);
                    if (replaceLeft || replaceRight)
                    {
                        yield return new TagRule(
                            replaceLeft ? ImmutableHashSet.CreateRange(rule.Left.Select(this.Rename)) : rule.Left,
                            rule.Operator,
                            replaceRight ? ImmutableHashSet.CreateRange(rule.Right.Select(this.Rename)) : rule.Right);
                        continue;
                    }
                }

                yield return rule;
            }
        }

        /// <summary>
        /// Applies rename rules to the specified tag.
        /// </summary>
        /// <param name="tag">The tag that may be renamed.</param>
        /// <returns>The canonical form of the specified tag.</returns>
        public string Rename(string tag) =>
            this.renameMap.TryGetValue(tag, out var renamed) ? renamed : tag;

        /// <summary>
        /// Find all sets of tags that suggest the specified tag.
        /// </summary>
        /// <param name="target">The tag that is being suggested.</param>
        /// <returns>An enumerable collecion of tag sets that suggest the specified tag.</returns>
        public IEnumerable<ImmutableHashSet<string>> TagSetsThatSuggest(string target) =>
            this.tagRules[TagOperator.Suggestion].Where(r => r.Right.Contains(target)).Select(r => r.Left);

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

        private ImmutableHashSet<string> GetTagsAndAncestors(IEnumerable<string> tags)
        {
            var tagsAndAncestors = ImmutableHashSet.CreateBuilder<string>();

            foreach (var tag in tags)
            {
                tagsAndAncestors.Add(tag);
                if (this.specializationParentTotalMap.TryGetValue(tag, out var specializes))
                {
                    tagsAndAncestors.UnionWith(specializes);
                }
            }

            return tagsAndAncestors.ToImmutable();
        }

        private ImmutableHashSet<string> GetTagsAndDescendants(IEnumerable<string> tags)
        {
            var tagsAndDescendants = ImmutableHashSet.CreateBuilder<string>();

            foreach (var tag in tags)
            {
                tagsAndDescendants.Add(tag);
                if (this.specializationChildTotalMap.TryGetValue(tag, out var children))
                {
                    tagsAndDescendants.UnionWith(children);
                }
            }

            return tagsAndDescendants.ToImmutable();
        }
    }
}
