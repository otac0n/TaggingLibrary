// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace TaggingLibrary.Tests
{
    using System;
    using System.Linq;
    using Xunit;

    public class TagRulesParserTests
    {
        public static readonly object[][] InfixOperators =
        {
            new[] { "->" },
            new[] { "<->" },
            new[] { "~>" },
            new[] { "<~>" },
            new[] { "!>" },
            new[] { "<!>" },
            new[] { "::" },
            new[] { "=>" },
        };

        public static readonly string[] InvalidTags =
        {
            " ok",
            "o k",
            "ok ",
            "-ok",
            ".ok",
        };

        public static readonly string[] ValidTags =
        {
            "ok",
            "OK",
            "0k",
            "o-kay",
            "O.Kay",
            "ök",
            "ok-",
            "ok.",
        };

        public static object[][] TagSyntaxValidity =>
            Enumerable.Concat(
                ValidTags.Select(t => new object[] { true, t }),
                InvalidTags.Select(t => new object[] { false, t })).ToArray();

        [Fact]
        public void Parse_GivenACompositeRule_ReturnsTheExpectedRules()
        {
            var parser = new TagRulesParser();

            var results = parser.Parse("a :: b [c]");

            var specialization = results.Single(r => r.Operator == TagOperator.Specialization);
            var properties = results.Single(r => r.Operator == TagOperator.Property);
            Assert.Equal(new[] { "a" }, specialization.Left);
            Assert.Equal(new[] { "b" }, specialization.Right);
            Assert.Equal(new[] { "a" }, properties.Left);
            Assert.Equal(new[] { "c" }, properties.Right);
        }

        [Theory]
        [MemberData(nameof(InfixOperators))]
        public void Parse_GivenASimpleWellFormedRule_ReturnsTheExpectedRule(string @operator)
        {
            var parser = new TagRulesParser();

            var result = parser.Parse($"a {@operator} b").Single();

            Assert.Equal(new[] { "a" }, result.Left);
            Assert.Equal(new[] { "b" }, result.Right);
            Assert.Equal(TagRule.StringToOperatorLookup[@operator], result.Operator);
        }

        [Theory]
        [MemberData(nameof(TagSyntaxValidity))]
        public void Parse_GivenATagOfAKnowValidity_ParsesOrFailsToParseAsExpected(bool valid, string tag)
        {
            var parser = new TagRulesParser();

            void test()
            {
                var result = parser.Parse($"{tag} [ok]").Single();
                Assert.Equal(tag, result.Left.Single());
            }

            if (valid)
            {
                test();
            }
            else
            {
                Assert.ThrowsAny<Exception>(test);
            }
        }
    }
}
