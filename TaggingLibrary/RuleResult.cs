// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace TaggingLibrary
{
    using System.Collections.Generic;

    internal static class RuleResult
    {
        public static RuleResult<TResult> Create<TResult>(TagRule rule, TResult result) => new RuleResult<TResult>(new[] { rule }, result);

        public static RuleResult<TResult> Create<TResult>(IEnumerable<TagRule> rules, TResult result) => new RuleResult<TResult>(rules, result);
    }
}
