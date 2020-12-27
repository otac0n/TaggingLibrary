// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace TaggingLibrary
{
    using System.Collections.Immutable;

    /// <summary>
    /// An immutable representation of tag analysis results.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// Gets an empty set of analysis results.
        /// </summary>
        public static AnalysisResult Empty = new AnalysisResult(
            ImmutableHashSet<string>.Empty,
            ImmutableHashSet<string>.Empty,
            ImmutableList<RuleResult<ImmutableHashSet<string>>>.Empty,
            ImmutableHashSet<RuleResult<string>>.Empty);

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalysisResult"/> class.
        /// </summary>
        /// <param name="normalizedTags">The set of normalized tags from the analysis.</param>
        /// <param name="effectiveTags">The effective tags from the analysis.</param>
        /// <param name="missingTagSets">The sets of missing tags from the analysis.</param>
        /// <param name="suggestedTags">The suggested tags from the analysis.</param>
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

        /// <summary>
        /// Gets the effective tags from the analysis.
        /// </summary>
        public ImmutableHashSet<string> EffectiveTags { get; }

        /// <summary>
        /// Gets the sets of missing tags from the analysis.
        /// </summary>
        public ImmutableList<RuleResult<ImmutableHashSet<string>>> MissingTagSets { get; }

        /// <summary>
        /// Gets the set of normalized tags from the analysis.
        /// </summary>
        public ImmutableHashSet<string> NormalizedTags { get; }

        /// <summary>
        /// Gets the suggested tags from the analysis.
        /// </summary>
        public ImmutableHashSet<RuleResult<string>> SuggestedTags { get; }
    }
}
