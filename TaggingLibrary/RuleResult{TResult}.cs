// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace TaggingLibrary
{
    /// <summary>
    /// Provides a generic way to commuicate that a certain result is derrived by a specified rule.
    /// </summary>
    /// <typeparam name="TResult">The type of result that is implied by the rule.</typeparam>
    public struct RuleResult<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuleResult{TResult}"/> struct.
        /// </summary>
        /// <param name="rule">The rule that is responsible for the included result.</param>
        /// <param name="result">The result that derrived by the specified rule.</param>
        public RuleResult(TagRule rule, TResult result)
        {
            this.Rule = rule;
            this.Result = result;
        }

        /// <summary>
        /// Gets the result that derrived by the specified rule.
        /// </summary>
        public TResult Result { get; }

        /// <summary>
        /// Gets the rule that is responsible for the included result.
        /// </summary>
        public TagRule Rule { get; }

        /// <summary>
        /// Compares the <paramref name="left"/> instance of a class to the <paramref name="right"/> instance to determine whether they are different.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns>A boolean value indicating whether the specified instances are different.</returns>
        public static bool operator !=(RuleResult<TResult> left, RuleResult<TResult> right) => !(left == right);

        /// <summary>
        /// Compares the <paramref name="left"/> instance of a class to the <paramref name="right"/> instance to determine whether they are the same.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns>A boolean value indicating whether the specified instances are the same.</returns>
        public static bool operator ==(RuleResult<TResult> left, RuleResult<TResult> right) => left.Equals(right);

        /// <inheritdoc/>
        public override bool Equals(object obj) =>
            obj is RuleResult<TResult> other &&
            object.Equals(this.Rule, other.Rule) &&
            object.Equals(this.Result, other.Result);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = 17;
            unchecked
            {
                hash = (hash * 33) + (this.Result?.GetHashCode() ?? 0);
                hash = (hash * 33) + (this.Rule?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }
}
