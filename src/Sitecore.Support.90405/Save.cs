using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using Sitecore.Web;
using Sitecore.Zip;
using Sitecore.Pipelines.Upload;


namespace Sitecore.Support.Pipelines.Upload
{
    /// <summary>
	/// Saves the uploaded files.
	/// </summary>
	public class Save : UploadProcessor
    {
        /// <summary>
        /// Runs the processor.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <exception cref="T:System.Exception"><c>Exception</c>.</exception>
        public void Process(UploadArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            for (int i = 0; i < args.Files.Count; i++)
            {
                HttpPostedFile httpPostedFile = args.Files[i];
                if (!string.IsNullOrEmpty(httpPostedFile.FileName))
                {
                    try
                    {
                        bool flag = UploadProcessor.IsUnpack(args, httpPostedFile);
                        if (args.FileOnly)
                        {
                            if (flag)
                            {
                                Save.UnpackToFile(args, httpPostedFile);
                            }
                            else
                            {
                                string filename = this.UploadToFile(args, httpPostedFile);
                                if (i == 0)
                                {
                                    args.Properties["filename"] = FileHandle.GetFileHandle(filename);
                                }
                            }
                        }
                        else
                        {
                            MediaUploader mediaUploader = new MediaUploader
                            {
                                File = httpPostedFile,
                                Unpack = flag,
                                Folder = args.Folder,
                                Versioned = args.Versioned,
                                Language = args.Language,
                                AlternateText = args.GetFileParameter(httpPostedFile.FileName, "alt"),
                                Overwrite = args.Overwrite,
                                FileBased = (args.Destination == UploadDestination.File)
                            };
                            List<MediaUploadResult> list;
                            using (new SecurityDisabler())
                            {
                                list = mediaUploader.Upload();
                            }
                            Log.Audit(this, "Upload: {0}", new string[]
                            {
                                httpPostedFile.FileName
                            });
                            foreach (MediaUploadResult current in list)
                            {
                                #region Support.527357
                                // Zadli 14th March 2019. Made changes to fix Ticket #527357

                                // There is a bug in the system where the workflow state is not set when overwriting media items.
                                // Hence, here we start the workflow state.
                                if (mediaUploader.Overwrite)
                                {
                                    var workflowId = current.Item.Fields[Sitecore.FieldIDs.DefaultWorkflow].Value;
                                    // Make sure start the workflow when it is not empty
                                    if (!String.IsNullOrEmpty(workflowId))
                                    {
                                        var workflow = Context.ContentDatabase.WorkflowProvider.GetWorkflow(workflowId);
                                        workflow.Start(current.Item);
                                    }
                                }

                                #endregion

                                this.ProcessItem(args, current.Item, current.Path);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Could not save posted file: " + httpPostedFile.FileName, exception, this);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Processes the item.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="mediaItem">The media item.</param>
        /// <param name="path">The path.</param>
        private void ProcessItem(UploadArgs args, MediaItem mediaItem, string path)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(mediaItem, "mediaItem");
            Assert.ArgumentNotNull(path, "path");
            if (args.Destination == UploadDestination.Database)
            {
                Log.Info("Media Item has been uploaded to database: " + path, this);
            }
            else
            {
                Log.Info("Media Item has been uploaded to file system: " + path, this);
            }
            args.UploadedItems.Add(mediaItem.InnerItem);
        }

        /// <summary>
        /// Unpacks to file.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="file">The file.</param>
        private static void UnpackToFile(UploadArgs args, HttpPostedFile file)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(file, "file");
            string filename = FileUtil.MapPath(TempFolder.GetFilename("temp.zip"));
            file.SaveAs(filename);
            using (ZipReader zipReader = new ZipReader(filename))
            {
                using (IEnumerator<ZipEntry> enumerator = zipReader.Entries.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        ZipEntry entry = enumerator.Current;
                        if (Path.GetInvalidFileNameChars().Any((char ch) => entry.Name.Contains(ch)))
                        {
                            string text = string.Format("The \"{0}\" file was not uploaded because it contains malicious file: \"{1}\"", file.FileName, entry.Name);
                            Log.Warn(text, typeof(Save));
                            args.UiResponseHandlerEx.MaliciousFile(StringUtil.EscapeJavascriptString(file.FileName));
                            args.ErrorText = text;
                            args.AbortPipeline();
                            return;
                        }
                    }
                }
                foreach (ZipEntry current in zipReader.Entries)
                {
                    string text2 = FileUtil.MakePath(args.Folder, current.Name, '\\');
                    if (current.IsDirectory)
                    {
                        Directory.CreateDirectory(text2);
                    }
                    else
                    {
                        if (!args.Overwrite)
                        {
                            text2 = FileUtil.GetUniqueFilename(text2);
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(text2));
                        object fileLock = FileUtil.GetFileLock(text2);
                        lock (fileLock)
                        {
                            FileUtil.CreateFile(text2, current.GetStream(), true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Uploads to file.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="file">The file.</param>
        /// <returns>The name of the uploaded file</returns>
        private string UploadToFile(UploadArgs args, HttpPostedFile file)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(file, "file");
            string text = FileUtil.MakePath(args.Folder, Path.GetFileName(file.FileName), '\\');
            if (!args.Overwrite)
            {
                text = FileUtil.GetUniqueFilename(text);
            }
            file.SaveAs(text);
            Log.Info("File has been uploaded: " + text, this);
            return Assert.ResultNotNull<string>(text);
        }
    }
}
