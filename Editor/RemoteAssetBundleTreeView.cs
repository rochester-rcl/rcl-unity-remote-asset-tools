using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace RemoteAssetBundleTools
{
    public class RemoteAssetBundleTreeViewItem : TreeViewItem
    {
        [SerializeField]
        public bool verified;
        [SerializeField]
        public string date;

        public RemoteAssetBundleTreeViewItem(string name, int depth, int id, bool isVerified, string uploadDate) : base(depth, id, name)
        {
            verified = isVerified;
            date = uploadDate;
        }
    }

    public class RemoteAssetBundleTreeView : TreeView
    {
        public RemoteAssetBundleManifest Manifest { get; set; }
        public string AppName { get; set; }
        internal static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColumns());
        }

        private static MultiColumnHeaderState.Column[] GetColumns()
        {
            MultiColumnHeaderState.Column[] cols = new MultiColumnHeaderState.Column[] {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
            };

            cols[0].headerContent = new GUIContent("Name", "The Name of the Asset Bundle");
            cols[0].canSort = false;
            cols[0].minWidth = 50;
            cols[0].width = 100;
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
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), args.item as RemoteAssetBundleTreeViewItem, args.GetColumn(i), ref args);
            }
        }
        // TODO switch to enum
        private void CellGUI(Rect cellRect, RemoteAssetBundleTreeViewItem item, int column, ref RowGUIArgs args)
        {
            switch (column)
            {
                case 0:
                    {
                        if (args.selected)
                        {
                            Debug.Log("SELECTEZD");
                        }
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

        protected override void ContextClickedItem(int id)
        {
            Debug.Log(id);
        }

        protected override void DoubleClickedItem(int id)
        {
            Debug.Log(id);
        }

        protected override TreeViewItem BuildRoot()
        {
            int id = 0;
            TreeViewItem root = new TreeViewItem { id = id, depth = -1, displayName = AppName };
            foreach (RemoteAssetBundle bundle in Manifest.Bundles)
            {
                var item = new RemoteAssetBundleTreeViewItem(bundle.Info.Name, 1, id, bundle.Verified, "A Date");
                root.AddChild(item);
            }
            return root;
        }
    }

}
