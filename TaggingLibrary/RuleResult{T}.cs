// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    public struct RuleResult<TResult>
    {
        public RuleResult(TagRule rule, TResult result)
        {
            this.Rule = rule;
            this.Result = result;
        }

        public TResult Result { get; }

        public TagRule Rule { get; }

        public static bool operator !=(RuleResult<TResult> left, RuleResult<TResult> right) => !(left == right);

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
