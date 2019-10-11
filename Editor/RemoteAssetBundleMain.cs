using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Net;
using RemoteAssetBundleTools;
using System.Threading.Tasks;
#if UNITY_EDITOR
public class RemoteAssetBundleMain : EditorWindow
{
    public RemoteAssetBundleGUITabs Tabs { get; set; }
    public delegate void ParentDisabled();
    public static event ParentDisabled OnParentDisabled;

    private RemoteAssetBundleGUIConfigureTab GUIConfigureTab { get; set; }
    private RemoteAssetBundleGUIAddTab GUIAddTab { get; set; }
    private RemoteAssetBundleGUIEditTab GUIEditTab { get; set; }

    private const string uploadEndpoint = "/bundles";
    private const string messageEndpoint = "/messages";

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
        GUIAddTab.OnUploadRemoteAssetBundle += UploadAssetBundle;
        GUIEditTab.OnLoadManifestsAwaitable += OnLoadManifests;
        GUIEditTab.OnUpdateRemoteBundleVerification += VerifyRemoteAssetBundle;
        GUIEditTab.OnDeleteRemoteBundle += DeleteRemoteAssetBundle;
    }

    void OnGUI()
    {
        GUILayout.Label("Remote Asset Bundles", EditorStyles.boldLabel);
        GUIEditTab.parentPosition = position;
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
    public async void UploadAssetBundle(AssetBundleInfo assetBundleInfo, string appName, FCMMessage message)
    {
        Object jwt = GUIConfigureTab.jwtFile;
        string endpoint = FormatEndpoint(uploadEndpoint);
        if (!string.IsNullOrEmpty(endpoint))
        {
            string jwtName = jwt ? jwt.name : null;
            try
            {
                EditorUtility.DisplayProgressBar("Remote Asset Bundles", string.Format("Uploading Asset Bundle {0} from {1}", assetBundleInfo.name, appName), 1.0f);
                RemoteAssetBundle ab = await RemoteAssetBundleUtils.UploadAssetBundle(endpoint, assetBundleInfo, message, appName, jwtName);
                GUIAddTab.AddMessage(string.Format("Successfully Uploaded Asset Bundle {0}", assetBundleInfo.name), MessageStatus.Success);
                EditorUtility.ClearProgressBar();
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                GUIAddTab.AddMessage(string.Format("Unable to upload Asset Bundle {0}. \n Reason: {1}", assetBundleInfo.name, ex.Message), MessageStatus.Error);
                throw;
            }
        }
    }

    public async void UploadAssetBundle(AssetBundleInfo assetBundleInfo, string appName)
    {
        Object jwt = GUIConfigureTab.jwtFile;
        string endpoint = FormatEndpoint(uploadEndpoint);
        if (!string.IsNullOrEmpty(endpoint))
        {
            string jwtName = jwt ? jwt.name : null;
            try
            {
                EditorUtility.DisplayProgressBar("Remote Asset Bundles", string.Format("Uploading Asset Bundle {0} from {1}", assetBundleInfo.name, appName), 1.0f);
                RemoteAssetBundle ab = await RemoteAssetBundleUtils.UploadAssetBundle(endpoint, assetBundleInfo, appName, jwtName);
                GUIAddTab.AddMessage(string.Format("Successfully Uploaded Asset Bundle {0}", assetBundleInfo.name), MessageStatus.Success);
                EditorUtility.ClearProgressBar();
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                GUIAddTab.AddMessage(string.Format("Unable to upload Asset Bundle {0}. \n Reason: {1}", assetBundleInfo.name, ex.Message), MessageStatus.Error);
                throw;
            }
        }
    }

    public async void DeleteRemoteAssetBundle(RemoteAssetBundle bundle)
    {
        Object jwt = GUIConfigureTab.jwtFile;
        string endpoint = FormatEndpoint(uploadEndpoint);
        if (!string.IsNullOrEmpty(endpoint))
        {
            string jwtName = jwt ? jwt.name : null;
            try
            {
                EditorUtility.DisplayProgressBar("Remote Asset Bundles", string.Format("Deleting Asset Bundle {0} from {1}", bundle.info.name, bundle.appName), 1.0f);
                HttpStatusCode status = await RemoteAssetBundleUtils.DeleteAssetBundle(endpoint, bundle, jwtName);
                GUIEditTab.AddMessage(string.Format("Successfully Deleted Asset Bundle {0} from app {1}", bundle.info.name, bundle.appName), MessageStatus.Success);
                EditorUtility.DisplayProgressBar("Remote Asset Bundles", "The content of the manifest has changed - refreshing now ...", 1.0f);
                await OnLoadManifests();
                GUIEditTab.SelectCurrentManifest(bundle.appName);
                EditorUtility.ClearProgressBar();
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                GUIEditTab.AddMessage(string.Format("Unable to delete Asset Bundle {0} from app {1}. \n Reason: {2}", bundle.info.name, bundle.appName, ex.Message), MessageStatus.Error);
                throw;
            }
        }
    }

    public async void SendRemoteAssetBundleMessage(RemoteAssetBundle bundle)
    {
        Object jwt = GUIConfigureTab.jwtFile;
        string endpoint = FormatEndpoint(messageEndpoint);
        if (!string.IsNullOrEmpty(endpoint))
        {
            string jwtName = jwt ? jwt.name : null;
            try
            {
                EditorUtility.DisplayProgressBar("Remote Asset Bundles", string.Format("Sending Message for Asset Bundle {0} from {1}", bundle.info.name, bundle.appName), 1.0f);
                FCMMessageStatus message = await RemoteAssetBundleUtils.SendBundleMessage(endpoint, bundle, jwtName);
                if (message.sendStatus)
                {
                    GUIEditTab.AddMessage(string.Format("Successfully Sent Message {0} for Asset Bundle {1} from {2}.", message.statusMessage, bundle.info.name, bundle.appName), MessageStatus.Success);
                }
                else 
                {
                    GUIEditTab.AddMessage(string.Format("Unable to Send Message for Asset Bundle {1} from {2}. \n Reason: {1}", bundle.info.name, bundle.appName, message.statusMessage), MessageStatus.Error);
                }

                EditorUtility.ClearProgressBar();
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                GUIEditTab.AddMessage(string.Format("Unable to Send Message for Asset Bundle {1} from {2}. \n Reason: {1}", bundle.info.name, bundle.appName, ex.Message), MessageStatus.Error);
                throw;
            }
        }
    }

    public async void VerifyRemoteAssetBundle(RemoteAssetBundle bundle, bool verified)
    {
        Object jwt = GUIConfigureTab.jwtFile;
        string endpoint = FormatEndpoint(uploadEndpoint);
        if (!string.IsNullOrEmpty(endpoint))
        {
            string jwtName = jwt ? jwt.name : null;
            try
            {
                EditorUtility.DisplayProgressBar("Remote Asset Bundles", string.Format("Updating Asset Bundle {0} from {1}", bundle.info.name, bundle.appName), 1.0f);
                RemoteAssetBundle newBundle = await RemoteAssetBundleUtils.VerifyAssetBundle(endpoint, bundle, verified, jwtName);
                GUIEditTab.AddMessage(string.Format("Successfully Updated Asset Bundle {0} from app {1}", bundle.info.name, bundle.appName), MessageStatus.Success);
                EditorUtility.DisplayProgressBar("Remote Asset Bundles", "The content of the manifest has changed - refreshing now ...", 1.0f);
                await OnLoadManifests();
                GUIEditTab.SelectCurrentManifest(bundle.appName);
                EditorUtility.ClearProgressBar();
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                GUIEditTab.AddMessage(string.Format("Unable to update Asset Bundle {0} from app {1}. \n Reason: {2}", bundle.info.name, bundle.appName, ex.Message), MessageStatus.Error);
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

    public async Task OnLoadManifests()
    {
        string endpoint = FormatEndpoint(uploadEndpoint);
        if (!string.IsNullOrEmpty(endpoint))
        {
            try
            {
                EditorUtility.DisplayProgressBar("Remote Asset Bundles", "Loading All Manifests", 1.0f);
                RemoteAssetBundleManifest manifest = await RemoteAssetBundleUtils.GetAssetBundleManifest(endpoint, null, false);
                GUIEditTab.SetManifests(manifest);
                EditorUtility.ClearProgressBar();
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                GUIEditTab.AddMessage(string.Format("Unable to Load Manifests. Have you Uploaded any Asset Bundles? Info: {0}", ex.Message), MessageStatus.Error);
                throw;
            }
        }
    }
}
#endif