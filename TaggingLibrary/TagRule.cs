// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    using System.Collections.Immutable;

    /// <summary>
    /// A rule for using a <see cref="TagOperator"/> on a set of tags.
    /// </summary>
    public sealed class TagRule
    {
        /// <summary>
        /// Provides a lookup from <see cref="TagOperator"/> to <see cref="string"/>.
        /// </summary>
        public static ImmutableDictionary<TagOperator, string> OperatorToStringLookup =
            ImmutableDictionary<TagOperator, string>.Empty
                .Add(TagOperator.Definition, "=>")
                .Add(TagOperator.Implication, "->")
                .Add(TagOperator.BidirectionalImplication, "<->")
                .Add(TagOperator.Suggestion, "~>")
                .Add(TagOperator.BidirectionalSuggestion, "<~>")
                .Add(TagOperator.Exclusion, "!>")
                .Add(TagOperator.MutualExclusion, "<!>")
                .Add(TagOperator.Specialization, "::");

        /// <summary>
        /// Provides a lookup from <see cref="string"/> to <see cref="TagOperator"/>.
        /// </summary>
        public static ImmutableDictionary<string, TagOperator> StringToOperatorLookup =
            ImmutableDictionary<string, TagOperator>.Empty
                .Add("=>", TagOperator.Definition)
                .Add("->", TagOperator.Implication)
                .Add("<->", TagOperator.BidirectionalImplication)
                .Add("~>", TagOperator.Suggestion)
                .Add("<~>", TagOperator.BidirectionalSuggestion)
                .Add("!>", TagOperator.Exclusion)
                .Add("<!>", TagOperator.MutualExclusion)
                .Add("::", TagOperator.Specialization);

        public TagRule(string left, TagOperator @operator, string right)
            : this(ImmutableHashSet.Create(left), @operator, ImmutableHashSet.Create(right))
        {
        }

        public TagRule(string left, TagOperator @operator, ImmutableHashSet<string> right)
            : this(ImmutableHashSet.Create(left), @operator, right)
        {
        }

        public TagRule(ImmutableHashSet<string> left, TagOperator @operator, string right)
            : this(left, @operator, ImmutableHashSet.Create(right))
        {
        }

        public TagRule(ImmutableHashSet<string> left, TagOperator @operator, ImmutableHashSet<string> right)
        {
            this.Left = left;
            this.Operator = @operator;
            this.Right = right;
        }

        public ImmutableHashSet<string> Left { get; }

        public TagOperator Operator { get; }

        public ImmutableHashSet<string> Right { get; }

        public override string ToString() =>
            this.Operator == TagOperator.Property
                ? $"{string.Join(" & ", this.Left)} [{string.Join(", ", this.Right)}]"
                : $"{string.Join(" & ", this.Left)} {OperatorToStringLookup[this.Operator]} {string.Join(" | ", this.Right)}";
    }
}
