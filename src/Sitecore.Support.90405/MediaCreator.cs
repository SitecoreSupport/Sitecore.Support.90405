using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using System;

namespace Sitecore.Support.Resources.Media
{
    public class MediaCreator : Sitecore.Resources.Media.MediaCreator
    {
        protected override Item CreateItem(string itemPath, string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNullOrEmpty(itemPath, "itemPath");
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            Item item2;
            using (new SecurityDisabler())
            {
                Database database = this.GetDatabase(options);
                Item item = options.OverwriteExisting ? database.GetItem(itemPath, options.Language) : null;
                Item parentFolder = this.GetParentFolder(itemPath, options);
                string itemName = this.GetItemName(itemPath);
                if (item != null && !item.HasChildren && item.TemplateID != TemplateIDs.MediaFolder)
                {
                    item2 = item;
                    item2.Database.WorkflowProvider.GetWorkflow(item2);
                    item2 = item2.Database.GetItem(item2.ID, item2.Language, Data.Version.Latest);
                    item2.Versions.RemoveAll(true);
                    item2 = item2.Versions.AddVersion();
                    Assert.IsNotNull(item2, "item");
                    item2.Editing.BeginEdit();
                    foreach (Field field in item2.Fields)
                    {
                        #region Support.527357

                        // Zadli 14th March 2019. Made changes to fix Ticket #527357
                        // Basically we do not reset the workflow state and workflow when we upload and overwrite the files.

                        if (field.Name != "__Workflow state" && field.Name != "__Workflow")
                        {
                            field.Reset();
                        }

                        #endregion
                    }
                    item2.Editing.EndEdit();
                    item2.Editing.BeginEdit();
                    item2.Name = itemName;
                    item2.TemplateID = this.GetItemTemplate(filePath, options).ID;
                    item2.Editing.EndEdit();
                }
                else
                {
                    item2 = parentFolder.Add(itemName, this.GetItemTemplate(filePath, options));
                }
                Assert.IsNotNull(item2, typeof(Item), "Could not create media item: '{0}'.", new object[]
                {
                    itemPath
                });
                Language[] array;
                if (!options.Versioned)
                {
                    array = item2.Database.Languages;
                }
                else
                {
                    array = new Language[]
                    {
                        item2.Language
                    };
                }
                Language[] array2 = array;
                string extension = FileUtil.GetExtension(filePath);
                Language[] array3 = array2;
                for (int i = 0; i < array3.Length; i++)
                {
                    Language language = array3[i];
                    MediaItem mediaItem = item2.Database.GetItem(item2.ID, language, Data.Version.Latest);
                    if (mediaItem != null)
                    {
                        using (new EditContext(mediaItem, SecurityCheck.Disable))
                        {
                            mediaItem.Extension = StringUtil.GetString(new string[]
                            {
                                mediaItem.Extension,
                                extension
                            });
                            mediaItem.FilePath = this.GetFullFilePath(item2.ID, filePath, itemPath, options);
                            mediaItem.Alt = StringUtil.GetString(new string[]
                            {
                                mediaItem.Alt,
                                options.AlternateText
                            });
                            mediaItem.InnerItem.Statistics.UpdateRevision();
                        }
                    }
                }
            }
            item2.Reload();
            return item2;
        }

        private TemplateItem GetItemTemplate(string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(filePath, "filePath");
            Assert.ArgumentNotNull(options, "options");
            string extension = FileUtil.GetExtension(filePath);
            string template = MediaManager.Config.GetTemplate(extension, options.Versioned);
            Assert.IsNotNullOrEmpty(template, "Could not find template for extension '{0}' (versioned: {1}).", new object[]
            {
                extension,
                options.Versioned
            });
            TemplateItem templateItem = this.GetDatabase(options).Templates[template];
            Assert.IsNotNull(templateItem, typeof(TemplateItem), "Could not find item template for media. Template: '{0}'", new object[]
            {
                template
            });
            return templateItem;
        }

        private Database GetDatabase(MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(options, "options");
            Database arg_23_0;
            if ((arg_23_0 = options.Database) == null)
            {
                arg_23_0 = (Context.ContentDatabase ?? Context.Database);
            }
            return Assert.ResultNotNull<Database>(arg_23_0);
        }

        private Item GetParentFolder(string itemPath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull(itemPath, "itemPath");
            Assert.ArgumentNotNull(options, "options");
            string[] array = StringUtil.Divide(itemPath, '/', true);
            return this.CreateFolder((array.Length > 1) ? array[0] : "/sitecore/media library", options);
        }

        private string GetItemName(string itemPath)
        {
            Assert.ArgumentNotNull(itemPath, "itemPath");
            string lastPart = StringUtil.GetLastPart(itemPath, '/', string.Empty);
            if (!string.IsNullOrEmpty(lastPart))
            {
                return lastPart;
            }
            if (Settings.Media.IncludeExtensionsInItemNames)
            {
                throw new InvalidOperationException("Invalid item path for media item: " + itemPath);
            }
            return "unnamed";
        }
    }
}
