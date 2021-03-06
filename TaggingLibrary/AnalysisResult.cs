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
        public static readonly AnalysisResult Empty = new AnalysisResult(
            ImmutableHashSet<string>.Empty,
            ImmutableHashSet<string>.Empty,
            ImmutableHashSet<string>.Empty,
            ImmutableList<TagRule>.Empty,
            ImmutableList<RuleResult<ImmutableHashSet<string>>>.Empty,
            ImmutableList<RuleResult<string>>.Empty);

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalysisResult"/> class.
        /// </summary>
        /// <param name="normalizedTags">The set of normalized tags from the analysis.</param>
        /// <param name="effectiveTags">The effective tags from the analysis.</param>
        /// <param name="existingRejectedTags">The set of normalized tags from the analysis that are also rejected.</param>
        /// <param name="violatedExclusions">The set of exclusion rules voilated by the effective tags.</param>
        /// <param name="missingTagSets">The sets of missing tags from the analysis.</param>
        /// <param name="suggestedTags">The suggested tags from the analysis.</param>
        public AnalysisResult(
            ImmutableHashSet<string> normalizedTags,
            ImmutableHashSet<string> effectiveTags,
            ImmutableHashSet<string> existingRejectedTags,
            ImmutableList<TagRule> violatedExclusions,
            ImmutableList<RuleResult<ImmutableHashSet<string>>> missingTagSets,
            ImmutableList<RuleResult<string>> suggestedTags)
        {
            this.NormalizedTags = normalizedTags;
            this.EffectiveTags = effectiveTags;
            this.ExistingRejectedTags = existingRejectedTags;
            this.ViolatedExclusions = violatedExclusions;
            this.MissingTagSets = missingTagSets;
            this.SuggestedTags = suggestedTags;
        }

        /// <summary>
        /// Gets the effective tags from the analysis.
        /// </summary>
        public ImmutableHashSet<string> EffectiveTags { get; }

        /// <summary>
        /// Gets the set of normalized tags from the analysis that are also rejected.
        /// </summary>
        public ImmutableHashSet<string> ExistingRejectedTags { get; }

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
        public ImmutableList<RuleResult<string>> SuggestedTags { get; }

        /// <summary>
        /// Gets the set of exclusion rules voilated by the effective tags.
        /// </summary>
        public ImmutableList<TagRule> ViolatedExclusions { get; }
    }
}
