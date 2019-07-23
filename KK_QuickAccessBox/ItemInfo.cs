﻿using System;
using Studio;

namespace KK_QuickAccessBox
{
    public sealed class ItemInfo
    {
        private readonly bool _initFinished;
        private readonly string _origFullname;

        public ItemInfo(int groupNo, int categoryNo, int itemNo, Info.ItemLoadInfo item = null)
        {
            GroupNo = groupNo;
            CategoryNo = categoryNo;
            ItemNo = itemNo;

            Item = item ?? QuickAccessBox.Info.dicItemLoadInfo[groupNo][categoryNo][itemNo];
            if (Item == null) throw new ArgumentNullException(nameof(item), "Info.ItemLoadInfo is null in dicItemLoadInfo");

            if (!QuickAccessBox.Info.dicItemGroupCategory.ContainsKey(GroupNo)) throw new ArgumentException("Invalid group number " + CategoryNo);
            GroupInfo = QuickAccessBox.Info.dicItemGroupCategory[GroupNo];

            if (!GroupInfo.dicCategory.ContainsKey(CategoryNo)) throw new ArgumentException("Invalid category number " + CategoryNo);
            var origCategoryName = GroupInfo.dicCategory[CategoryNo];
            _origFullname = GroupInfo.name + "/" + origCategoryName + "/" + Item.name;

            // Get translated versions of the relevant strings
            TranslationHelper.Translate(
                GroupInfo.name, s =>
                {
                    GroupName = s;
                    if (_initFinished)
                        UpdateCompositeStrings();
                });
            TranslationHelper.Translate(
                origCategoryName, s =>
                {
                    CategoryName = s;
                    if (_initFinished)
                        UpdateCompositeStrings();
                });
            TranslationHelper.Translate(
                Item.name, s =>
                {
                    ItemName = s;
                    if (_initFinished)
                        UpdateCompositeStrings();
                });
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

        public Info.GroupInfo GroupInfo { get; }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        public string GroupName { get; private set; }

        /// <summary>
        /// Top level under add/Item menu
        /// </summary>
        public int GroupNo { get; }

        public Info.ItemLoadInfo Item { get; }

        /// <summary>
        /// Translated name, or original if not necessary/available
        /// </summary>
        public string ItemName { get; private set; }

        /// <summary>
        /// Index of the item in
        /// </summary>
        public int ItemNo { get; }

        /// <summary>
        /// String to search against
        /// </summary>
        internal string SearchStr { get; private set; }

        /// <summary>
        /// Spawn this item in studio
        /// </summary>
        public void AddItem()
        {
            Singleton<Studio.Studio>.Instance.AddItem(GroupNo, CategoryNo, ItemNo);
        }

	    private void UpdateCompositeStrings()
        {
            FullName = GroupName + "/" + CategoryName + "/" + ItemName;

            var searchStr = FullName;

            if (!_origFullname.Equals(FullName, StringComparison.OrdinalIgnoreCase))
                searchStr = $"{searchStr}\v{_origFullname}";

            if (QuickAccessBox.SearchDeveloperInfo.Value)
                searchStr = $"{searchStr}\v{Item.childRoot}\v{Item.bundlePath}\v{Item.fileName}\v{Item.manifest}\v{GroupNo}\v{CategoryNo}\v{ItemNo}";

            SearchStr = searchStr.ToLowerInvariant();
		}

	    public override int GetHashCode()
	    {
		    return _origFullname.GetHashCode();
	    }

	    public override bool Equals(object obj)
	    {
		    return obj is ItemInfo i && i._origFullname == _origFullname;
	    }
	}
}
