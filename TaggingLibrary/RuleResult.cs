// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    internal static class RuleResult
    {
        public static RuleResult<TResult> Create<TResult>(TagRule rule, TResult result) => new RuleResult<TResult>(rule, result);
    }
}
