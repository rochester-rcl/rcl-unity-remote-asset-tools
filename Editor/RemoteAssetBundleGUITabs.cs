using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteAssetBundleTools
{
    public class RemoteAssetBundleGUITab
    {
        private Color SelectedColor = new Color(0.9f, 0.9f, 0.9f);
        private Color UnselectedColor = new Color(0.4f, 0.4f, 0.4f);

        public delegate void IsActive(int tabIndex);
        public delegate void DisplayTabContent();
        DisplayTabContent TabContent;
        public event IsActive OnActive;

        public string Name { get; set; }
        public bool Active { get; set; }
        public int TabIndex { get; set; }
        public RemoteAssetBundleGUITab(string tabName, int index, DisplayTabContent content)
        {
            Name = tabName;
            TabIndex = index;
            TabContent = content;
        }

        public void Show()
        {
            GUI.backgroundColor = Active ? SelectedColor : UnselectedColor;
            GUIStyle ButtonStyle = new GUIStyle(GUI.skin.button);
            if (GUILayout.Button(Name, ButtonStyle))
            {
                Active = true;
                if (OnActive != null)
                {
                    OnActive(TabIndex);
                }
            }
        }

        public void ShowTabContent()
        {
            if (Active) TabContent();
        }
    }

    public class RemoteAssetBundleGUITabs
    {
        public List<RemoteAssetBundleGUITab> Tabs { get; set; }
        private RemoteAssetBundleGUITab ActiveTab { get; set; }

        public RemoteAssetBundleGUITabs(List<RemoteAssetBundleGUITab> tabList)
        {
            Tabs = tabList;
            Tabs[0].Active = true;
            ActiveTab = Tabs[0];
            SetCallback();
        }

        public void AddTab(RemoteAssetBundleGUITab tab)
        {
            tab.OnActive += HandleActiveTab;
            Tabs.Add(tab);
        }

        public void RemoveTab(int index)
        {
            Tabs[index].OnActive -= HandleActiveTab;
            Tabs.RemoveAt(index);
        }

        public void ShowTabs()
        {
            GUILayout.BeginHorizontal();
            {
                foreach (RemoteAssetBundleGUITab tab in Tabs)
                {
                    tab.Show();
                }
            }
            GUILayout.EndHorizontal();
            ActiveTab.ShowTabContent();
        }

        public void HandleActiveTab(int index)
        {
            ActiveTab = Tabs[index];
            Tabs.ForEach(delegate (RemoteAssetBundleGUITab tab)
            {
                if (tab.TabIndex != index)
                {
                    tab.Active = false;
                }
            });
        }

        public void SetCallback()
        {
            foreach (RemoteAssetBundleGUITab tab in Tabs)
            {
                tab.OnActive += HandleActiveTab;
            }
        }

    }
}