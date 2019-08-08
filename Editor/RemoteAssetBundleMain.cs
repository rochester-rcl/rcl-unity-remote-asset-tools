using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RemoteAssetBundleTools;

#if UNITY_EDITOR
public class RemoteAssetBundleMain : EditorWindow
{
    public RemoteAssetBundleGUITabs Tabs { get; set; }
    public delegate void ParentDisabled();
    public static event ParentDisabled OnParentDisabled;

    private RemoteAssetBundleGUIConfigureTab GUIConfigureTab { get; set; }
    private RemoteAssetBundleGUIAddTab GUIAddTab { get; set; }

    [MenuItem("Window/General/Remote Asset Bundles")]
    static void Init()
    {
        RemoteAssetBundleMain window = (RemoteAssetBundleMain)EditorWindow.GetWindow(typeof(RemoteAssetBundleMain), false, "Remote Asset Bundles", true);
        window.Show();
    }

    void OnEnable()
    {
        GUIConfigureTab = new RemoteAssetBundleGUIConfigureTab("Configure Settings to Connect to a Remote Asset Bundle Server");
        GUIAddTab = new RemoteAssetBundleGUIAddTab("Add a Remote Asset Bundle to a Server");
        Tabs = new RemoteAssetBundleGUITabs(new List<RemoteAssetBundleGUITab>
        {
            new RemoteAssetBundleGUITab("Configure", 0, GUIConfigureTab.Show),
            new RemoteAssetBundleGUITab("Add", 1, GUIAddTab.Show),
            new RemoteAssetBundleGUITab("Edit", 2, EditTab)
        });
        GUIAddTab.OnUploadRemoteAssetBundle += OnUploadAssetBundle;
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

    // Async Server Methods
    public void OnUploadAssetBundle(AssetBundleInfo assetBundleInfo, string appName, string message)
    {
        PerformUpload(assetBundleInfo, appName, message);
    }

    public async void PerformUpload(AssetBundleInfo assetBundleInfo, string appName, string message)
    {

    }
}
#endif