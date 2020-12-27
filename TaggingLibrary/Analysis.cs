// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    using System.Collections.Immutable;

    public class AnalysisResult
    {
        public static AnalysisResult Empty = new AnalysisResult(
            ImmutableHashSet<string>.Empty,
            ImmutableHashSet<string>.Empty,
            ImmutableList<RuleResult<ImmutableHashSet<string>>>.Empty,
            ImmutableHashSet<RuleResult<string>>.Empty);

        public AnalysisResult(
            ImmutableHashSet<string> normalizedTags,
            ImmutableHashSet<string> effectiveTags,
            ImmutableList<RuleResult<ImmutableHashSet<string>>> missingTagSets,
            ImmutableHashSet<RuleResult<string>> suggestedTags)
        {
            this.NormalizedTags = normalizedTags;
            this.EffectiveTags = effectiveTags;
            this.MissingTagSets = missingTagSets;
            this.SuggestedTags = suggestedTags;
        }

        public ImmutableHashSet<string> EffectiveTags { get; }

        public ImmutableList<RuleResult<ImmutableHashSet<string>>> MissingTagSets { get; }

        public ImmutableHashSet<string> NormalizedTags { get; }

        public ImmutableHashSet<RuleResult<string>> SuggestedTags { get; }
    }
}
