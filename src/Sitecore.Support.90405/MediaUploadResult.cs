using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System;

namespace Sitecore.Support.Resources.Media
{
    /// <summary>
    /// Represents a MediaUploadResult.
    /// </summary>
    public class MediaUploadResult
    {
        private Item _item;

        private string _path;

        private string _validMediaPath;

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public Item Item
        {
            get
            {
                return this._item;
            }
            internal set
            {
                Assert.ArgumentNotNull(value, "value");
                this._item = value;
            }
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path
        {
            get
            {
                return this._path;
            }
            internal set
            {
                Assert.ArgumentNotNull(value, "value");
                this._path = value;
            }
        }

        /// <summary>
        /// Gets or sets the valid media path.
        /// </summary>
        /// <value>The valid media path.</value>
        public string ValidMediaPath
        {
            get
            {
                return this._validMediaPath;
            }
            internal set
            {
                Assert.ArgumentNotNull(value, "value");
                this._validMediaPath = value;
            }
        }
    }
}