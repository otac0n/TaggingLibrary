// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace TaggingLibrary
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    /// <summary>
    /// Provides a generic way to commuicate that a certain result is derrived by a specified rule.
    /// </summary>
    /// <typeparam name="TResult">The type of result that is implied by the rule.</typeparam>
    public class RuleResult<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuleResult{TResult}"/> class.
        /// </summary>
        /// <param name="rules">The rules that are responsible for the included result.</param>
        /// <param name="result">The result that derrived by the specified rule.</param>
        public RuleResult(IEnumerable<TagRule> rules, TResult result)
        {
            this.Rules = ImmutableList.CreateRange(rules);
            this.Result = result;
        }

        /// <summary>
        /// Gets the result that derrived by the specified rule.
        /// </summary>
        public TResult Result { get; }

        /// <summary>
        /// Gets the rule that is responsible for the included result.
        /// </summary>
        public ImmutableList<TagRule> Rules { get; }
    }
}
