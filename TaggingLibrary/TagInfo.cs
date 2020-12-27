// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace TaggingLibrary
{
    using System.Collections.Immutable;

    /// <summary>
    /// An immutable object containing information on the specified tag.
    /// </summary>
    public class TagInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagInfo"/> class.
        /// </summary>
        /// <param name="tag">The tag described by this object.</param>
        /// <param name="isAbstract">A value indicating whether or not the tag has the <see cref="TagRuleEngine.AbstractProperty"/> property.</param>
        /// <param name="aliases">The aliases for the tag.</param>
        /// <param name="properties">The properties on the tag.</param>
        /// <param name="parents">The parents of the tag.</param>
        /// <param name="children">The children of the tag.</param>
        /// <param name="ancestors">The ancestors of the tag.</param>
        /// <param name="descendants">The descendants of the tag.</param>
        public TagInfo(string tag, bool isAbstract, ImmutableHashSet<string> aliases, ImmutableList<string> properties, ImmutableHashSet<string> parents, ImmutableHashSet<string> children, ImmutableHashSet<string> ancestors, ImmutableHashSet<string> descendants)
        {
            this.Tag = tag;
            this.IsAbstract = isAbstract;
            this.Aliases = aliases;
            this.Properties = properties;
            this.Ancestors = ancestors;
            this.Descendants = descendants;
            this.Parents = parents;
            this.Children = children;
        }

        /// <summary>
        /// Gets the aliases for the tag.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <see cref="TagRuleEngine.GetTagAliases(string)"/>.
        /// </remarks>
        public ImmutableHashSet<string> Aliases { get; }

        /// <summary>
        /// Gets all ancestors of the tag.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <see cref="TagRuleEngine.GetTagAncestors(string)"/>.
        /// </remarks>
        public ImmutableHashSet<string> Ancestors { get; }

        /// <summary>
        /// Gets the children of the tag.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <see cref="TagRuleEngine.GetTagChildren(string)"/>.
        /// </remarks>
        public ImmutableHashSet<string> Children { get; }

        /// <summary>
        /// Gets all descendants of the tag.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <see cref="TagRuleEngine.GetTagDescendants(string)"/>.
        /// </remarks>
        public ImmutableHashSet<string> Descendants { get; }

        /// <summary>
        /// Gets a value indicating whether or not the tag has the <see cref="TagRuleEngine.AbstractProperty"/> property.
        /// </summary>
        public bool IsAbstract { get; }

        /// <summary>
        /// Gets the parents of the tag.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <see cref="TagRuleEngine.GetTagParents(string)"/>.
        /// </remarks>
        public ImmutableHashSet<string> Parents { get; }

        /// <summary>
        /// Gets the properties on the tag.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <see cref="TagRuleEngine.GetTagProperties(string)"/>.
        /// </remarks>
        public ImmutableList<string> Properties { get; }

        /// <summary>
        /// Gets the tag that this <see cref="TagInfo"/> describes.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// Finds related tags given the specified <see cref="HierarchyRelation"/>.
        /// </summary>
        /// <param name="relation">The hierarchy relation to look-up.</param>
        /// <returns>The set of tags requested.</returns>
        public ImmutableHashSet<string> RelatedTags(HierarchyRelation relation)
        {
            var tags = ImmutableHashSet<string>.Empty;
            switch (relation)
            {
                case HierarchyRelation.Ancestor:
                case HierarchyRelation.SelfOrAncestor:

                    tags = tags.Union(this.Ancestors);

                    if (relation == HierarchyRelation.SelfOrAncestor)
                    {
                        goto case HierarchyRelation.Self;
                    }

                    break;

                case HierarchyRelation.Descendant:
                case HierarchyRelation.SelfOrDescendant:

                    tags = tags.Union(this.Descendants);

                    if (relation == HierarchyRelation.SelfOrDescendant)
                    {
                        goto case HierarchyRelation.Self;
                    }

                    break;

                case HierarchyRelation.Self:
                    tags = tags.Add(this.Tag);
                    break;
            }

            return tags;
        }
    }
}
