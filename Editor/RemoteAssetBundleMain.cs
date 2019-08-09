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
    private RemoteAssetBundleGUIEditTab GUIEditTab { get; set; }

    private const string UploadEndpoint = "/bundles/";

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
        GUIEditTab = new RemoteAssetBundleGUIEditTab("Edit Remote Asset Bundles");
        Tabs = new RemoteAssetBundleGUITabs(new List<RemoteAssetBundleGUITab>
        {
            new RemoteAssetBundleGUITab("Configure", 0, GUIConfigureTab.Show),
            new RemoteAssetBundleGUITab("Add", 1, GUIAddTab.Show),
            new RemoteAssetBundleGUITab("Edit", 2, GUIEditTab.Show)
        });
        GUIConfigureTab.OnCheckEndpoint += OnCheckEndpoint;
        GUIConfigureTab.OnCheckJWT += OnCheckJWT;
        GUIAddTab.OnUploadRemoteAssetBundle += OnUploadAssetBundle;
        GUIEditTab.OnLoadManifests += OnLoadManifests;
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

    public string FormatEndpoint(string endpoint)
    {
        if (!string.IsNullOrEmpty(GUIConfigureTab.serverEndpoint))
        {
            return string.Format("{0}{1}", GUIConfigureTab.serverEndpoint, endpoint);
        }
        else
        {
            return null;
        }
    }

    // Async Server Methods
    public async void OnUploadAssetBundle(AssetBundleInfo assetBundleInfo, string appName, string message)
    {
        Object jwt = GUIConfigureTab.jwtFile;
        string endpoint = FormatEndpoint(UploadEndpoint);
        if (!string.IsNullOrEmpty(endpoint))
        {
            string jwtName = jwt ? jwt.name : null;
            try
            {
                RemoteAssetBundle ab = await RemoteAssetBundleUtils.UploadAssetBundle(endpoint, assetBundleInfo, appName, message, jwtName);
                GUIAddTab.AddMessage(string.Format("Successfully Uploaded Asset Bundle {0}", assetBundleInfo.Name), MessageStatus.Success);
            }
            catch (System.Exception ex)
            {
                GUIAddTab.AddMessage(string.Format("Unable to upload Asset Bundle {0}. \n Reason: {1}", assetBundleInfo.Name, ex.Message), MessageStatus.Error);
                throw;
            }
        }
    }

    public async void OnCheckEndpoint(string serverEndpoint)
    {
        bool status;
        if (!string.IsNullOrEmpty(serverEndpoint))
        {
            status = await RemoteAssetBundleUtils.CheckEndpoint(serverEndpoint);
        }
        else
        {
            status = false;
        }
        if (status)
        {
            GUIConfigureTab.AddMessage("Successfully Connected to Server!", MessageStatus.Success);
        }
        else
        {
            GUIConfigureTab.AddMessage("Unable to Connect to Server!", MessageStatus.Error);
        }
    }

    public async void OnCheckJWT(string serverEndpoint, Object jwt)
    {
        bool status;
        if (!string.IsNullOrEmpty(serverEndpoint) && jwt)
        {
            status = await RemoteAssetBundleUtils.CheckJWT(serverEndpoint, jwt.name);
        }
        else
        {
            status = false;
        }
        if (status)
        {
            GUIConfigureTab.AddMessage(string.Format("Successfully Connected to Server with JWT {0}!", jwt.name), MessageStatus.Success);
        }
        else
        {
            GUIConfigureTab.AddMessage(string.Format("Unable to Connect to Server with JWT {0}!", jwt.name), MessageStatus.Error);
        }
    }

    public async void OnLoadManifests()
    {
        string endpoint = FormatEndpoint(UploadEndpoint);
        if (!string.IsNullOrEmpty(endpoint))
        {
            try
            {
                RemoteAssetBundleManifest manifest = await RemoteAssetBundleUtils.GetAssetBundleManifest(endpoint, null, false);
                GUIEditTab.SetManifests(manifest);
            }
            catch (System.Exception ex)
            {

                GUIEditTab.AddMessage(string.Format("Unable to Load Manifests. Have you Uploaded any Asset Bundles? Info: {0}", ex.Message), MessageStatus.Error);
                throw;
            }
        }
    }
}
#endif