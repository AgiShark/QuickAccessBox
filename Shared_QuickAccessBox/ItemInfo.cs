﻿using System;
using KK_QuickAccessBox.Thumbs;
using MessagePack;
using Studio;
using UnityEngine;

namespace KK_QuickAccessBox
{
    [MessagePackObject]
    public sealed class ItemInfo
    {
        private readonly bool _initFinished;

        public ItemInfo(int groupNo, int categoryNo, int itemNo, Info.ItemLoadInfo item = null)
        {
            GroupNo = groupNo;
            CategoryNo = categoryNo;
            ItemNo = itemNo;

            if (item == null) item = Info.Instance.dicItemLoadInfo[groupNo][categoryNo][itemNo];

            if (item == null) throw new ArgumentNullException(nameof(item), "Info.ItemLoadInfo is null in dicItemLoadInfo");

#if KK || KKS
            DeveloperSearchString = $"{item.childRoot}\v{item.bundlePath}\v{item.fileName}\v{item.manifest}\v{GroupNo}\v{CategoryNo}\v{ItemNo}";
#elif AI || HS2
            DeveloperSearchString = $"{item.bundlePath}\v{item.fileName}\v{item.manifest}\v{GroupNo}\v{CategoryNo}\v{ItemNo}";
#endif
            if (ItemInfoLoader.ZipmodCache.TryGetValue(itemNo, out var cachedGuid) && cachedGuid.Key != null)
            {
                GUID = cachedGuid.Key;
                DeveloperSearchString += "\v" + GUID;
                if (cachedGuid.Value != null)
                {
                    FileName = cachedGuid.Value;
                    DeveloperSearchString += "\v" + FileName;
                }
            }

            CacheId = MakeCacheId(groupNo, categoryNo, item);

            if (!Info.Instance.dicItemGroupCategory.ContainsKey(GroupNo)) throw new ArgumentException("Invalid group number");
            var groupInfo = Info.Instance.dicItemGroupCategory[GroupNo];

            if (!groupInfo.dicCategory.ContainsKey(CategoryNo)) throw new ArgumentException("Invalid category number");
#if KK || KKS
            var origCategoryName = groupInfo.dicCategory[CategoryNo];
#elif AI || HS2
            var origCategoryName = groupInfo.dicCategory[CategoryNo].name;
#endif
            OriginalItemName = groupInfo.name + "/" + origCategoryName + "/" + item.name;

            ItemInfoLoader.TranslationCache.TryGetValue(CacheId, out var cachedTranslations);
            if (cachedTranslations != null)
            {
                CategoryName = cachedTranslations.CategoryName;
                GroupName = cachedTranslations.GroupName;
                ItemName = cachedTranslations.ItemName;
            }
            else
            {
                // Get translated versions of the relevant strings
                Translate(groupInfo.name, s =>
                {
                    GroupName = s;
                    if (_initFinished)
                        UpdateCompositeStrings();
                });
                Translate(origCategoryName, s =>
                {
                    CategoryName = s;
                    if (_initFinished)
                        UpdateCompositeStrings();
                });
                Translate(item.name, s =>
                {
                    ItemName = s;
                    if (_initFinished)
                        UpdateCompositeStrings();
                });
            }

            UpdateCompositeStrings();
            _initFinished = true;
        }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        public string CategoryName { get; private set; }

        /// <summary>
        /// Under add/Item/Group
        /// </summary>
        public int CategoryNo { get; }

        /// <summary>
        /// Full translated (or original if not necessary/available) path of the item in the item tree
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        public string GroupName { get; private set; }

        /// <summary>
        /// Top level under add/Item menu
        /// </summary>
        public int GroupNo { get; }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        public string ItemName { get; private set; }

        private string OriginalItemName { get; }

        /// <summary>
        /// Index of the item in
        /// </summary>
        public int ItemNo { get; }

        /// <summary>
        /// String to search against
        /// </summary>
        internal string SearchString { get; private set; }

        /// <summary>
        /// String with developer info, used to build SearchString
        /// </summary>
        internal string DeveloperSearchString { get; }

        public Sprite Thumbnail => ThumbnailLoader.GetThumbnail(this);

        /// <summary>
        /// Item is a sound effect and should get the SFX thumbnail
        /// </summary>
        public bool IsSFX =>
#if KK || KKS
            GroupNo == 00000011; // stock 3d sfx
#elif AI || HS2
            GroupNo == 00000009 || // stock 3d sfx
            GroupNo == 2171; // dirty's 3dsfx
#endif

        public string CacheId { get; }

        /// <summary>
        /// If this item is from a zipmod, GUID of the zipmod. Otherwise null.
        /// </summary>
        public string GUID { get; }

        /// <summary>
        /// If this item is from a zipmod, name of the .zipmod file. Otherwise null.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Spawn this item in studio
        /// </summary>
        public void AddItem()
        {
            try
            {
                Studio.Studio.Instance.AddItem(GroupNo, CategoryNo, ItemNo);
            }
            catch (NullReferenceException)
            {
                // Some modded items crash in Studio.OCIItem.UpdateColor()
            }
        }

        private void UpdateCompositeStrings()
        {
            FullName = GroupName + "/" + CategoryName + "/" + ItemName;

            var searchStr = FullName;

            if (!OriginalItemName.Equals(FullName, StringComparison.OrdinalIgnoreCase))
                searchStr = $"{searchStr}\v{OriginalItemName}";

            if (QuickAccessBox.SearchDeveloperInfo.Value)
                searchStr = $"{searchStr}\v{DeveloperSearchString}";

            SearchString = searchStr.ToLowerInvariant();
        }

        public static string MakeCacheId(int groupNo, int categoryNo, Info.ItemLoadInfo item)
        {
            // Can't use itemNo because it can change with sideloader
            // todo change into Sideloader.AutoResolver.UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)categoryNo, 1).Slot ?
            return $"{groupNo:D8}-{categoryNo:D8}-{Utils.MakeValidFileName(item.name)}";
            // old - return $"{groupNo:D8}-{categoryNo:D8}-{item.name.GetHashCode():D32}";
        }

        public override int GetHashCode()
        {
            if (OriginalItemName == null) return 0;
            return OriginalItemName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ItemInfo i && i.OriginalItemName == OriginalItemName;
        }

        private static void Translate(string input, Action<string> updateAction)
        {
            if (KKAPI.Utilities.TranslationHelper.AutoTranslatorInstalled)
            {
                var didFire = false;
                KKAPI.Utilities.TranslationHelper.TranslateAsync(input, s =>
                {
                    updateAction(s);
                    didFire = true;
                    ItemInfoLoader.TriggerCacheSave();
                });
                if (didFire) return;
            }

            // Make sure there's a valid value set
            updateAction(input);
            ItemInfoLoader.TriggerCacheSave();
        }
    }
}
