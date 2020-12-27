// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    /// <summary>
    /// Describes a relationship between a tag and its relatives.
    /// </summary>
    public enum HierarchyRelation
    {
        /// <summary>
        /// No relationship.
        /// </summary>
        /// <remarks>
        /// This is not supported as a semantic relationship.  Instead, it is treated as uninitialized data.
        /// </remarks>
        None = 0,

        /// <summary>
        /// An ancestor of the specified tag.
        /// </summary>
        Ancestor = 1,

        /// <summary>
        /// The specified tag.
        /// </summary>
        Self = 2,

        /// <summary>
        /// The specified tag or an ancestor.
        /// </summary>
        SelfOrAncestor = 3,

        /// <summary>
        /// A descendant of the specified tag.
        /// </summary>
        Descendant = 4,

        /// <summary>
        /// The specified tag or a descendant.
        /// </summary>
        SelfOrDescendant = 6,
    }
}
