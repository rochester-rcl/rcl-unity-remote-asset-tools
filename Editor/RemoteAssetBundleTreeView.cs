﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RemoteAssetBundleTools
{
#if UNITY_EDITOR
    using UnityEditor.IMGUI.Controls;
    public class RemoteAssetBundleTreeViewItem : TreeViewItem
    {

        public bool verified;
        public string date;
        public string messageContent;

        public RemoteAssetBundleTreeViewItem(string name, int depth, int id, bool isVerified, string uploadDate, string message) : base(id, depth, name)
        {
            verified = isVerified;
            date = uploadDate;
            messageContent = message;
        }
    }

    public class RemoteAssetBundleTreeView : TreeView
    {
        public RemoteAssetBundleManifest Manifest { get; set; }
        public string AppName { get; set; }
        public delegate void SelectBundleToEdit(RemoteAssetBundle bundle);
        public static event SelectBundleToEdit OnSelectBundleToEdit;
        internal static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColumns());
        }

        private static MultiColumnHeaderState.Column[] GetColumns()
        {
            MultiColumnHeaderState.Column[] cols = new MultiColumnHeaderState.Column[] {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
            };

            cols[0].headerContent = new GUIContent("Name", "The Name of the Asset Bundle");
            cols[0].canSort = false;
            cols[0].minWidth = 50;
            cols[0].width = 150;
            cols[0].maxWidth = 300;
            cols[0].headerTextAlignment = TextAlignment.Left;
            cols[0].autoResize = true;

            cols[1].headerContent = new GUIContent("Date", "The Date the Bundle was Uploaded");
            cols[1].canSort = false;
            cols[1].minWidth = 50;
            cols[1].width = 100;
            cols[1].maxWidth = 300;
            cols[1].headerTextAlignment = TextAlignment.Left;
            cols[1].autoResize = true;

            cols[2].headerContent = new GUIContent("Verified", "Whether or not the Bundle has been Verified");
            cols[2].canSort = false;
            cols[2].minWidth = 50;
            cols[2].width = 100;
            cols[2].maxWidth = 300;
            cols[2].headerTextAlignment = TextAlignment.Left;
            cols[2].autoResize = true;

            cols[3].headerContent = new GUIContent("Message Content", "The Push Notification Uploaded with the Bundle");
            cols[3].canSort = false;
            cols[3].minWidth = 100;
            cols[3].width = 150;
            cols[3].maxWidth = 500;
            cols[3].headerTextAlignment = TextAlignment.Left;
            cols[3].autoResize = true;

            return cols;
        }

        public RemoteAssetBundleTreeView(TreeViewState treeViewState, MultiColumnHeaderState header, RemoteAssetBundleManifest manifest, string AppName) : base(treeViewState, new MultiColumnHeader(header))
        {
            Manifest = manifest;
            rowHeight = 20;
            columnIndexForTreeFoldouts = 2;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            Reload();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), args.item as RemoteAssetBundleTreeViewItem, args.GetColumn(i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, RemoteAssetBundleTreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case 0:
                    {
                        DefaultGUI.Label(cellRect, item.displayName, args.selected, args.focused);
                        break;
                    }

                case 1:
                    {
                        DefaultGUI.Label(cellRect, item.date, args.selected, args.focused);
                        break;
                    }

                case 2:
                    {
                        DefaultGUI.Label(cellRect, item.verified.ToString(), args.selected, args.focused);
                        break;
                    }
                case 3:
                    {
                        DefaultGUI.Label(cellRect, item.messageContent, args.selected, args.focused);
                        break;
                    }
            }
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        protected override void SingleClickedItem(int id)
        {
            if (OnSelectBundleToEdit != null)
            {
                OnSelectBundleToEdit(Manifest.bundles[id]);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            int id = 0;
            TreeViewItem root = new TreeViewItem { id = id, depth = -1, displayName = AppName };
            foreach (RemoteAssetBundle bundle in Manifest.bundles)
            {
                var item = new RemoteAssetBundleTreeViewItem(bundle.info.name, 1, id++, bundle.verified, bundle.date, bundle.messageContent);
                root.AddChild(item);
            }
            return root;
        }
    }
#endif
}
