using System;
using System.Collections.Generic;
using System.Text;

namespace ItemShops.Utils
{
    /// <summary>
    /// A tag for defining information about an item.
    /// </summary>
    public struct Tag
    {
        /// <summary>
        /// The tag itself.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// Creates a <see cref="Tag"/>.
        /// </summary>
        /// <param name="name">The tag's name.</param>
        public Tag(string name)
        {
            this.name = name.ToUpper();
        }
    }
}
