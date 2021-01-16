TaggingLibrary
=======

[![MIT Licensed](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](license.md)
[![Get it on NuGet](https://img.shields.io/nuget/v/TaggingLibrary.svg?style=flat-square)](http://nuget.org/packages/TaggingLibrary)

[![Appveyor Build](https://img.shields.io/appveyor/ci/otac0n/TaggingLibrary.svg?style=flat-square)](https://ci.appveyor.com/project/otac0n/TaggingLibrary)
[![Test Coverage](https://img.shields.io/codecov/c/github/otac0n/TaggingLibrary.svg?style=flat-square)](https://codecov.io/gh/otac0n/TaggingLibrary)
[![Pre-release packages available](https://img.shields.io/nuget/vpre/TaggingLibrary.svg?style=flat-square)](http://nuget.org/packages/TaggingLibrary)

A library for providing tagging suggestions for documents, music, photos, etc.

Getting Started
---------------

    PM> Install-Package TaggingLibrary

Create a set of tag rules (as a text file resource, for example):

    animal [abstract]
    mammal :: animal
    dog & cat :: mammal
    mammal -> hair
    fur :: hair

Parse the tag rules and index the results:

    using TaggingLibrary;
    ...

    var parser = new TagRulesParser();
    var rules = parser.Parse(Resources.TagRules);
    var engine = new TagRuleEngine(rules);

Analyze a set of tags:

    var currentTags = new[] { "cat" }; // If you are offended by cats, you can change this to "dog".
    var analysis = engine.Analyze(currentTags);

    Console.WriteLine($"Current Tags: {string.Join(", ", currentTags)}");
    foreach (var suggestion in analysis.SuggestedTags)
    {
        Console.WriteLine($"    Suggestion: {suggestion.Result} (via {string.Join("; ", suggestion.Rules)})");
    }

The output in this example is something like:

>     Current Tags: cat
>         Suggestion: fur (via fur :: hair)
>         Suggestion: hair (via mammal -> hair)

Examples
--------

For examples of tag sets, see:
* [Animals.txt](TaggingLibrary.Tests/Resources/Animals.txt) - An example used for displaying simple behaviours in our tests.
* [Music.txt](TaggingLibrary.Tests/Resources/Music.txt) - A more detailed example used for stress-testing.

Tag Operators
-------------

* `::` (Specialization) The tags on the left are a specialization of the tag on the right. Multiple and circular inheritance are supported.
* `[]` (Property Assignment) The tags on the left are all given each property on the right (comma separated, between the brackets).
* `->` (Implication) The tags on the left together imply some tag on the right.
* `<->` (Bidirectional Implication) A bidirectional form of implication. See below. All tags on the right also imply each tag on the left.
* `~>` (Suggestion) The tags on the left together suggest some tag on the right.
* `<~>` (Bidirectional Suggestion) A bidirectional form of suggestion. See below. All tags on the right also suggest each tag on the left.
* `!>` (Exclusion) The tags on the left together exclude some tag on the right.
* `<!>` (Mutual Exclusion) A mutual form of exclusion. See below. All tags on the right also exclude each tag on the left.
* `=>` (Definition) Adds an alias. No bidirectional form is provided. If a circular reference is discovered, the tie is broken lexicographically.

When it makes sense, the left side of operators is allowed to be expressed as a conjunction and the right side is allowed to be expressed as a disjunction. For example:

    a & b :: c
    b -> x | y | z
    a & b -> q | r

When a bidirectional operator includes conjunctions or disjunctions, the rule is simplified accordingly. For example:

    a & b <!> x | y | z

becomes:

    a & b !> x | y | z
    x !> a
    y !> a
    z !> a
    x !> b
    y !> b
    z !> b

A property assignment rule and a specialization rule for the same tags can be combined into a single rule. The rule is expanded into two rules at parse-time. For example:

    q & r :: c [abstract]

becomes:

    q & r :: c
    q & r [abstract]

API
---

* `TagRule` - A single rule using the above operators.
* `TagRulesParser` - Deserializes rules from text representation to `TagRule` objects.
* `TagRuleEngine` - Provides analysis for a set of `TagRule` objects.
    * `.Analyze(tags, rejected)` - Analyze a set of tags and (optionally) rejected tags for missing, suggested, or incorrect tags.
    * `[tag]` - Get information about a single tag as a `TagInfo` object. Can be used to traverse the entire set of tags.
    * `GetTagChildren` / `GetTagParents` / `GetTagDescendants` / `GetTagAncestors` - Provides sets of tags allowing the traversal of the tag specialization map.
    * `GetTagProperties` / `GetInheritedTagProperties` / `GetAllTagProperties` - Enumerates the properties for the specified tag.
    * `GetKnownTags` - Enumerates all known tags.
    * `NormalizeRules` - Renames all tags according to defninition rules (`=>`). The `TagRuleEngine` will call this method automatically when constructed.
    * `SimplifyRules` - Performs the above mentioned rule expansion steps to remove bidirectional rules (`<->`, `<~>`, `<!>`) from the set rules. The `TagRuleEngine` will call this method automatically when constructed.
* `TagInfo` - Describes the indexed state of a single tag.
    * `Children` / `Parents` / `Descendants` / `Ancestors` - Provides sets of tags allowing the traversal of the tag specialization map.
    * `Properties` - Enumerates all properties for the tag.
