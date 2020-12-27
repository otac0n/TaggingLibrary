// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    using System.Collections.Immutable;

    public class TagInfo
    {
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

        public ImmutableHashSet<string> Aliases { get; }

        public ImmutableHashSet<string> Ancestors { get; }

        public ImmutableHashSet<string> Children { get; }

        public ImmutableHashSet<string> Descendants { get; }

        public bool IsAbstract { get; }

        public ImmutableHashSet<string> Parents { get; }

        public ImmutableList<string> Properties { get; }

        public string Tag { get; }

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
