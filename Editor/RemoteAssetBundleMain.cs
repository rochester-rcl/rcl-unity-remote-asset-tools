using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RemoteAssetBundleTools;

public class RemoteAssetBundleMain : EditorWindow
{
    public RemoteAssetBundleGUITabs Tabs { get; set; }
    public delegate void ParentDisabled();
    public static event ParentDisabled OnParentDisabled;
    
    [MenuItem("Window/General/Remote Asset Bundles")]
    static void Init()
    {
        RemoteAssetBundleMain window = (RemoteAssetBundleMain)EditorWindow.GetWindow(typeof(RemoteAssetBundleMain), false, "Remote Asset Bundles", true);
        window.Show();
    }

    void OnEnable()
    {
        RemoteAssetBundleGUIConfigureTab configureTab = new RemoteAssetBundleGUIConfigureTab("Configure Settings to Connect to a Remote Asset Bundle Server");
        Tabs = new RemoteAssetBundleGUITabs(new List<RemoteAssetBundleGUITab>
        {
            new RemoteAssetBundleGUITab("Configure", 0, configureTab.Show),
            new RemoteAssetBundleGUITab("Add", 1, AddTab),
            new RemoteAssetBundleGUITab("Edit", 2, EditTab)
        });
    }

    void AddTab()
    {

    }

    void EditTab()
    {

    }

    void OnGUI()
    {
        GUILayout.Label("Remote Asset Bundles", EditorStyles.boldLabel);
        Tabs.ShowTabs();
    }

    public void OnDisable()
    {
        if (OnParentDisabled != null)
        {
            OnParentDisabled();
        }
    }
}
