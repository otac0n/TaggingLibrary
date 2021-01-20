// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace TaggingLibrary.Tests
{
    using TaggingLibrary.Tests.Properties;
    using Xunit;

    public class TagRuleEngineTests
    {
        [Fact]
        public void Analyze_WithADescendantTag_DoesNotIncludeTheParentTagInMissingTagSets()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "cat", "fur" });

            Assert.DoesNotContain(results.MissingTagSets, s => s.Result.Contains("hair"));
        }

        [Fact]
        public void Analyze_WithADescendantTag_InlcudesAncestorsOfTheTagInEffectiveTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "cat" });

            Assert.Contains(results.EffectiveTags, t => t == "mammal");
            Assert.Contains(results.EffectiveTags, t => t == "animal");
            Assert.Contains(results.EffectiveTags, t => t == "object");
        }

        [Fact]
        public void Analyze_WithAFullyDescribedScenario_DoesNotSuggestExtraTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "cat", "tail", "fur", "whiskers" });

            Assert.Empty(results.MissingTagSets);
            Assert.Empty(results.SuggestedTags);
        }

        [Fact]
        public void Analyze_WithAMissingTag_DoesNotIncludesRootAbstractDescdendantTagsInSuggestedTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "tail" });

            Assert.Contains(results.SuggestedTags, s => s.Result == "animal");
        }

        [Fact]
        public void Analyze_WithAMissingTag_InlcudesTheFirstConcreteTagInSuggestedTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "tail" });

            Assert.Contains(results.SuggestedTags, s => s.Result == "mammal");
            Assert.DoesNotContain(results.SuggestedTags, s => s.Result == "cat");
        }

        [Fact]
        public void Analyze_WithAMissingTag_InlcudesTheTagInMissingTagSets()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "cat" });

            Assert.Contains(results.MissingTagSets, s => s.Result.Contains("tail"));
        }

        [Fact]
        public void Analyze_WithAMissingTag_InlcudesTheTagInSuggestedTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "cat" });

            Assert.Contains(results.SuggestedTags, t => t.Result == "tail");
        }

        [Fact]
        public void Analyze_WithAMoreSpecificImplication_IncludesBothTagsInMissingTagSets()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "dog" });

            Assert.Contains(results.MissingTagSets, s => s.Result.Contains("hair"));
            Assert.Contains(results.MissingTagSets, s => s.Result.Contains("fur"));
        }

        [Fact]
        public void Analyze_WithAMoreSpecificImplication_IncludesBothTagsInSuggestedTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "dog" });

            Assert.Contains(results.SuggestedTags, s => s.Result == "hair");
            Assert.Contains(results.SuggestedTags, s => s.Result == "fur");
        }

        [Fact]
        public void Analyze_WithAMoreSpecificSuggestion_IncludesBothTagsInSuggestedTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "whale" });

            Assert.Contains(results.SuggestedTags, s => s.Result == "hair");
            Assert.Contains(results.SuggestedTags, s => s.Result == "whiskers");
        }

        [Fact]
        public void Analyze_WithAnExcludedTag_DoesNotIncludeTheExcludedTagInMissingTagSets()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Music);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "rock", "instrumental" });

            Assert.DoesNotContain(results.MissingTagSets, s => s.Result.Contains("vocals"));
        }

        [Fact]
        public void Analyze_WithAnExcludedTag_DoesNotIncludeTheExcludedTagInSuggestedTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Music);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "rock", "instrumental" });

            Assert.DoesNotContain(results.SuggestedTags, s => s.Result == "vocals");
        }

        [Fact]
        public void Analyze_WithARejectedExistingTag_IncludesTheExistingTagInTheExistingRejectedTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "cat" }, new[] { "mammal" });

            Assert.Contains(results.ExistingRejectedTags, t => t == "cat");
        }

        [Fact]
        public void Analyze_WithARejectedMissingTag_DoesNotIncludeTheRejectedTagInMissingTagSets()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "cat" }, new[] { "hair" });

            Assert.DoesNotContain(results.MissingTagSets, s => s.Result.Contains("hair"));
        }

        [Fact]
        public void Analyze_WithARejecteSuggestedTag_DoesNotIncludeTheRejectedTagInSuggestedTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "whale" }, new[] { "hair" });

            Assert.DoesNotContain(results.SuggestedTags, s => s.Result == "whiskers");
        }

        [Fact]
        public void Analyze_WithATagAlias_ReplacesTheAliasWithTheCanonicalTagInEffectiveTags()
        {
            var parser = new TagRulesParser();
            var rules = parser.Parse(Resources.Animals);
            var engine = new TagRuleEngine(rules);

            var results = engine.Analyze(new[] { "feline" });

            Assert.Contains(results.EffectiveTags, t => t == "cat");
            Assert.DoesNotContain(results.EffectiveTags, t => t == "feline");
        }
    }
}
