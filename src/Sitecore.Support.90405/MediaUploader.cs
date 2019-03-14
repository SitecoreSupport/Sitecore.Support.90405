using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Pipelines.GetMediaCreatorOptions;
using Sitecore.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace Sitecore.Support.Resources.Media
{
    /// <summary>
	/// Represents a MediaUpload.
	/// </summary>
	public class MediaUploader
    {
        private string _alternateText;

        private HttpPostedFile _file;

        private Language _language;

        private string _folder = string.Empty;

        /// <summary>
        /// Gets or sets the alternate text.
        /// </summary>
        /// <value>The alternate text.</value>
        public string AlternateText
        {
            get
            {
                return this._alternateText;
            }
            set
            {
                this._alternateText = value;
            }
        }

        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>The file.</value>
        public HttpPostedFile File
        {
            get
            {
                return this._file;
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                this._file = value;
            }
        }

        /// <summary>
        /// Gets or sets the folder.
        /// </summary>
        /// <value>The folder.</value>
        public string Folder
        {
            get
            {
                return this._folder;
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                this._folder = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Sitecore.Resources.Media.MediaUploader" /> is unpack.
        /// </summary>
        /// <value><c>true</c> if unpack; otherwise, <c>false</c>.</value>
        public bool Unpack
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Sitecore.Resources.Media.MediaUploader" /> is versioned.
        /// </summary>
        /// <value><c>true</c> if versioned; otherwise, <c>false</c>.</value>
        public bool Versioned
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        public Language Language
        {
            get
            {
                return this._language;
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                this._language = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Sitecore.Resources.Media.MediaUploader" /> is overwrite.
        /// </summary>
        /// <value><c>true</c> if overwrite; otherwise, <c>false</c>.</value>
        public bool Overwrite
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="T:Sitecore.Resources.Media.MediaUploader" /> files the based.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the <see cref="T:Sitecore.Resources.Media.MediaUploader" /> files the based; otherwise, <c>false</c>.
        /// </value>
        public bool FileBased
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the database to create media item in.
        /// </summary>
        /// <value>The database.</value>
        public Database Database
        {
            get;
            set;
        }

        /// <summary>
        /// Uploads this instance.
        /// </summary>
        public List<MediaUploadResult> Upload()
        {
            List<MediaUploadResult> list = new List<MediaUploadResult>();
            bool flag = string.Compare(Path.GetExtension(this.File.FileName), ".zip", StringComparison.InvariantCultureIgnoreCase) == 0;
            if (flag && this.Unpack)
            {
                this.UnpackToDatabase(list);
            }
            else
            {
                this.UploadToDatabase(list);
            }
            return list;
        }

        /// <summary>
        /// Uploads to database.
        /// </summary>
        private void UploadToDatabase(List<MediaUploadResult> list)
        {
            Assert.ArgumentNotNull(list, "list");
            MediaUploadResult mediaUploadResult = new MediaUploadResult();
            list.Add(mediaUploadResult);
            mediaUploadResult.Path = FileUtil.MakePath(this.Folder, Path.GetFileName(this.File.FileName), '/');
            mediaUploadResult.ValidMediaPath = Sitecore.Resources.Media.MediaPathManager.ProposeValidMediaPath(mediaUploadResult.Path);
            Sitecore.Resources.Media.MediaCreatorOptions mediaCreatorOptions = new Sitecore.Resources.Media.MediaCreatorOptions
            {
                Versioned = this.Versioned,
                Language = this.Language,
                OverwriteExisting = this.Overwrite,
                Destination = mediaUploadResult.ValidMediaPath,
                FileBased = this.FileBased,
                AlternateText = this.AlternateText,
                Database = this.Database
            };

            #region Support.527357
            // Zadli 14th March 2019. Made changes to fix Ticket #527357

            // Old code
            //mediaCreatorOptions.Build(GetMediaCreatorOptionsArgs.UploadContext);
            //mediaUploadResult.Item = MediaManager.Creator.CreateFromStream(this.File.InputStream, mediaUploadResult.Path, mediaCreatorOptions);

            Sitecore.Resources.Media.MediaCreatorOptions mediaCreatorOptions2 = mediaCreatorOptions;
            mediaCreatorOptions2.Build(GetMediaCreatorOptionsArgs.UploadContext);
            Sitecore.Support.Resources.Media.MediaCreator mediaCreator = new Sitecore.Support.Resources.Media.MediaCreator();
            mediaUploadResult.Item = mediaCreator.CreateFromStream(this.File.InputStream, mediaUploadResult.Path, mediaCreatorOptions2);
            
            #endregion
            
        }

        /// <summary>
        /// Unpacks to database.
        /// </summary>
        private void UnpackToDatabase(List<MediaUploadResult> list)
        {
            Assert.ArgumentNotNull(list, "list");
            string text = FileUtil.MapPath(TempFolder.GetFilename("temp.zip"));
            this.File.SaveAs(text);
            try
            {
                using (ZipReader zipReader = new ZipReader(text))
                {
                    foreach (ZipEntry current in zipReader.Entries)
                    {
                        if (!current.IsDirectory)
                        {
                            MediaUploadResult mediaUploadResult = new MediaUploadResult();
                            list.Add(mediaUploadResult);
                            mediaUploadResult.Path = FileUtil.MakePath(this.Folder, current.Name, '/');
                            mediaUploadResult.ValidMediaPath = Sitecore.Resources.Media.MediaPathManager.ProposeValidMediaPath(mediaUploadResult.Path);
                            Sitecore.Resources.Media.MediaCreatorOptions mediaCreatorOptions = new Sitecore.Resources.Media.MediaCreatorOptions
                            {
                                Language = this.Language,
                                Versioned = this.Versioned,
                                OverwriteExisting = this.Overwrite,
                                Destination = mediaUploadResult.ValidMediaPath,
                                FileBased = this.FileBased,
                                Database = this.Database
                            };

                            #region Support.527357
                            // Zadli 14th March 2019. Made changes to fix Ticket #527357

                            // Old code
                            //mediaCreatorOptions.Build(GetMediaCreatorOptionsArgs.UploadContext);
                            //Stream stream = current.GetStream();
                            //mediaUploadResult.Item = MediaManager.Creator.CreateFromStream(stream, mediaUploadResult.Path, mediaCreatorOptions);

                            Sitecore.Resources.Media.MediaCreatorOptions mediaCreatorOptions2 = mediaCreatorOptions;
                            mediaCreatorOptions2.Build(GetMediaCreatorOptionsArgs.UploadContext);
                            Stream stream = current.GetStream();
                            Sitecore.Support.Resources.Media.MediaCreator mediaCreator = new Sitecore.Support.Resources.Media.MediaCreator();
                            mediaUploadResult.Item = mediaCreator.CreateFromStream(stream, mediaUploadResult.Path, mediaCreatorOptions2);

                            #endregion
                        }
                    }
                }
            }
            finally
            {
                FileUtil.Delete(text);
            }
        }
    }
}
