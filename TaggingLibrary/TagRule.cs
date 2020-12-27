// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace TaggingLibrary
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TagRule"/> class.
        /// </summary>
        /// <param name="left">The single tag used as the left operand.</param>
        /// <param name="operator">The tag operator applied between the operands.</param>
        /// <param name="right">The single tag used as the right operand.</param>
        public TagRule(string left, TagOperator @operator, string right)
            : this(ImmutableHashSet.Create(left), @operator, ImmutableHashSet.Create(right))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagRule"/> class.
        /// </summary>
        /// <param name="left">The single tag used as the left operand.</param>
        /// <param name="operator">The tag operator applied between the operands.</param>
        /// <param name="right">The tags used as the right operand.</param>
        public TagRule(string left, TagOperator @operator, ImmutableHashSet<string> right)
            : this(ImmutableHashSet.Create(left), @operator, right)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagRule"/> class.
        /// </summary>
        /// <param name="left">The tags used as the left operand.</param>
        /// <param name="operator">The tag operator applied between the operands.</param>
        /// <param name="right">The single tag used as the right operand.</param>
        public TagRule(ImmutableHashSet<string> left, TagOperator @operator, string right)
            : this(left, @operator, ImmutableHashSet.Create(right))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagRule"/> class.
        /// </summary>
        /// <param name="left">The tags used as the left operand.</param>
        /// <param name="operator">The tag operator applied between the operands.</param>
        /// <param name="right">The tags used as the right operand.</param>
        public TagRule(ImmutableHashSet<string> left, TagOperator @operator, ImmutableHashSet<string> right)
        {
            this.Left = left;
            this.Operator = @operator;
            this.Right = right;
        }

        /// <summary>
        /// Gets the tags used as the left operand.
        /// </summary>
        /// <remarks>
        /// For <see cref="TagOperator.Property">properties</see>, the properties are applied to all of the left operands.
        /// For other operations, the tags are considered in conjunction (all are required).
        /// </remarks>
        public ImmutableHashSet<string> Left { get; }

        /// <summary>
        /// Gets the operator applied between the operands.
        /// </summary>
        public TagOperator Operator { get; }

        /// <summary>
        /// Gets the tags used as the right operand.
        /// </summary>
        /// <remarks>
        /// For <see cref="TagOperator.Property">properties</see>, this will be a list of string properties that are all applied.
        /// For other operations, the tags are considered in disjunction (any can apply).
        /// </remarks>
        public ImmutableHashSet<string> Right { get; }

        /// <inheritdoc/>
        public override string ToString() =>
            this.Operator == TagOperator.Property
                ? $"{string.Join(" & ", this.Left)} [{string.Join(", ", this.Right)}]"
                : $"{string.Join(" & ", this.Left)} {OperatorToStringLookup[this.Operator]} {string.Join(" | ", this.Right)}";
    }
}
