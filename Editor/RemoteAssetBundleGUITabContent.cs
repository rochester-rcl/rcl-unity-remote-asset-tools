using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using RemoteAssetBundleTools;
using System.Linq;
using System.Threading.Tasks;
#if UNITY_EDITOR
public enum MessageStatus { Success, Error };

public struct StatusMessage
{
    public string Content { get; set; }
    public MessageStatus Status { get; set; }

    public StatusMessage(string content, MessageStatus status)
    {
        Content = content;
        Status = status;
    }
}

public static class RemoteAssetBundleEditorPrefsKeys
{
    public const string ConfigKey = "RemoteAssetBundleConfig";
    public const string AppsKey = "RemoteAssetBundleApps";
}

/*************************** ABSTRACT CLASS ************************************************/

public abstract class RemoteAssetBundleGUITabContent
{
    public string Label { get; set; }
    public Rect parentPosition;
    protected Color DefaultColor = new Color(0.9f, 0.9f, 0.9f);
    protected int TabLayoutPadding = 20;

    protected List<StatusMessage> Messages = new List<StatusMessage>();

    protected GUILayoutOption[] DefaultTextFieldOptions = { GUILayout.ExpandWidth(true), GUILayout.MinWidth(100) };
    protected GUILayoutOption[] DefaultButtonOptions = { GUILayout.MaxWidth(85f) };
    protected GUIStyle MessageStyle = new GUIStyle(EditorStyles.label);
    protected GUIStyle ScrollViewStyle = new GUIStyle(EditorStyles.textArea);
    protected Color ErrorColor = new Color(1.0f, 0.0f, 0.0f);
    protected Color SuccessColor = new Color(0.2f, 0.2f, 0.2f);
    protected Vector2 MessageScrollPos;

    public RemoteAssetBundleGUITabContent(string label)
    {
        Label = label;
        RemoteAssetBundleMain.OnParentDisabled += OnParentDisabled;
    }
    public virtual void Show()
    {

    }

    public virtual void OnParentDisabled()
    {

    }

    public void AddMessage(string content, MessageStatus status)
    {
        StatusMessage message = new StatusMessage(content, status);
        Messages.Add(message);
    }

    public void ClearMessages()
    {
        Messages.Clear();
    }

    public void ShowMessages()
    {
        EditorGUILayout.LabelField("Status Messages");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        MessageScrollPos = GUILayout.BeginScrollView(MessageScrollPos, false, false);
        {
            foreach (StatusMessage message in Messages)
            {
                MessageStyle.normal.textColor = message.Status == MessageStatus.Success ? SuccessColor : ErrorColor;
                EditorGUILayout.LabelField(message.Content, MessageStyle);
            }
        }
        GUILayout.EndScrollView();
    }
}

/********************** CONFIGURE TAB *************************************************************************/

public class RemoteAssetBundleGUIConfigureTab : RemoteAssetBundleGUITabContent
{
    public string serverEndpoint;
    public Object jwtFile;
    public delegate void HandleCheckEndpoint(string endpoint);
    public delegate void HandleCheckJWT(string endpoint, Object jwt);
    public event HandleCheckEndpoint OnCheckEndpoint;
    public event HandleCheckJWT OnCheckJWT;

    public RemoteAssetBundleGUIConfigureTab(string label) : base(label)
    {
        string data = EditorPrefs.GetString(RemoteAssetBundleEditorPrefsKeys.ConfigKey, JsonUtility.ToJson(this, false));
        if (!string.IsNullOrEmpty(data))
        {
            JsonUtility.FromJsonOverwrite(data, this);
        }
    }

    public override void Show()
    {
        GUI.backgroundColor = DefaultColor;
        GUILayout.Space(TabLayoutPadding);
        GUILayout.Label(Label);
        GUILayout.Space(TabLayoutPadding / 2);
        serverEndpoint = EditorGUILayout.TextField("Server URL", serverEndpoint, DefaultTextFieldOptions);
        jwtFile = EditorGUILayout.ObjectField("JWT Auth File", jwtFile, typeof(TextAsset), false, DefaultTextFieldOptions);
        GUILayout.Space(TabLayoutPadding);
        CheckEndpointButton();
        GUILayout.Space(TabLayoutPadding / 4);
        CheckJWTButton();
        GUILayout.Space(TabLayoutPadding);
        ShowMessages();
    }

    public void CheckEndpoint()
    {
        if (OnCheckEndpoint != null)
        {
            OnCheckEndpoint(serverEndpoint);
        }
    }

    public void CheckJWT()
    {
        if (OnCheckJWT != null)
        {
            OnCheckJWT(serverEndpoint, jwtFile);
        }
    }

    public void CheckEndpointButton()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Check Server Connection");
            if (GUILayout.Button("Check Server", DefaultButtonOptions))
            {
                CheckEndpoint();
            }
        }
        GUILayout.EndHorizontal();
    }

    public void CheckJWTButton()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Check JWT Authentication");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Check JWT", DefaultButtonOptions))
            {
                CheckJWT();
            }

        }
        GUILayout.EndHorizontal();
    }

    public override void OnParentDisabled()
    {
        string data = JsonUtility.ToJson(this, false);
        if (!string.IsNullOrEmpty(data))
        {
            EditorPrefs.SetString(RemoteAssetBundleEditorPrefsKeys.ConfigKey, data);
        }
    }
}

/*********** ADD TAB *****************************************************/

public class RemoteAssetBundleGUIAddTab : RemoteAssetBundleGUITabContent
{
    public AssetBundleInfo ABInfo { get; set; }
    public FCMMessage UploadMessage;
    public string AppName;
    public delegate void UploadRemoteAssetBundle(AssetBundleInfo bundleInfo, string appName, FCMMessage message);
    public event UploadRemoteAssetBundle OnUploadRemoteAssetBundle;
    public RemoteAssetBundleGUIAddTab(string label) : base(label)
    {
        UploadMessage = new FCMMessage(null, null, null);
    }
    private const string firebaseDocsUrl = "https://firebase.google.com";
    private const string remoteABServerUrl = "https://github.com/rochester-rcl/rcl-unity-asset-server";
    public override void Show()
    {
        GUI.backgroundColor = DefaultColor;
        GUILayout.Space(TabLayoutPadding);
        GUILayout.Label(Label);
        GUILayout.Space(TabLayoutPadding);
        OpenAssetBundleButton();
        GUILayout.Space(TabLayoutPadding / 2);
        AppName = EditorGUILayout.TextField("App Name (optional)", AppName, DefaultTextFieldOptions);
        GUILayout.Space(TabLayoutPadding);
        GUILayout.Label("Firebase Cloud Messaging (optional)");
        GUILayout.Space(TabLayoutPadding);
        OpenFirebaseLinkButton();
        GUILayout.Space(TabLayoutPadding);
        UploadMessage.title = EditorGUILayout.TextField("Message Title", UploadMessage.title, DefaultTextFieldOptions);
        UploadMessage.body = EditorGUILayout.TextField("Message Body", UploadMessage.body, DefaultTextFieldOptions);
        UploadMessage.icon = EditorGUILayout.TextField("Notification Icon (Android)", UploadMessage.icon, DefaultTextFieldOptions);
        UploadMessage.sendImmediate = EditorGUILayout.Toggle("Send Immediately", UploadMessage.sendImmediate, DefaultTextFieldOptions);
        GUILayout.Space(TabLayoutPadding);
        UploadAssetBundleButton();
        GUILayout.Space(TabLayoutPadding);
        ShowMessages();
    }

    public void OpenAssetBundleButton()
    {
        GUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Select Local Asset Bundle");
            if (GUILayout.Button("Open File", DefaultButtonOptions))
            {
                OpenAssetBundleDialog();
            }
        }
        GUILayout.EndHorizontal();
    }

    public void OpenFirebaseLinkButton()
    {
        EditorGUILayout.LabelField("Requires a Firebase Project to be properly configured on the server side.");
        EditorGUILayout.LabelField("By default, the push notification is only sent once the bundle has been manually verified.");
        EditorGUILayout.LabelField("If you wish to send it immediately after the bundle has been uploaded, select \"Send Immediately\".");
        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Firebase Docs", DefaultButtonOptions))
            {
                Application.OpenURL(firebaseDocsUrl);
            }
            if (GUILayout.Button("Server Docs", DefaultButtonOptions))
            {
                Application.OpenURL(remoteABServerUrl);
            }
        }
        GUILayout.EndHorizontal();
    }

    public void OpenAssetBundleDialog()
    {
        string abPath = EditorUtility.OpenFilePanel("Select an AssetBundle", "", "unity3d");
        if (!string.IsNullOrEmpty(abPath))
        {
            ABInfo = new AssetBundleInfo(abPath);
            AddMessage(string.Format("Current AssetBundle is {0}", ABInfo.name), MessageStatus.Success);
        }
    }

    public void UploadAssetBundleButton()
    {
        if (GUILayout.Button("Save"))
        {
            UploadAssetBundle();
        }
    }

    public void UploadAssetBundle()
    {
        if (OnUploadRemoteAssetBundle != null)
        {
            OnUploadRemoteAssetBundle(ABInfo, AppName, UploadMessage);
        }
    }
}

/********** EDIT TAB *********/
public class RemoteAssetBundleGUIEditTab : RemoteAssetBundleGUITabContent
{
    protected HashSet<string> Apps = new HashSet<string>();
    protected RemoteAssetBundleManifest Manifests { get; set; }
    protected RemoteAssetBundleManifest CurrentManifest { get; set; }
    protected string CurrentAppName { get; set; }
    protected int selectedIndex;
    private Vector2 AppViewScrollPos;
    private Vector2 BundleViewScrollPos;
    private RemoteAssetBundleTreeView BundleTree;
    private RemoteAssetBundle currentBundle;
    public delegate void HandleLoadManifests();
    public delegate Task HandleLoadManifestsAwaitable();
    public delegate void HandleDeleteRemoteBundle(RemoteAssetBundle bundle);
    public delegate void HandleUpdateRemoteBundleVerification(RemoteAssetBundle bundle, bool verified);
    public delegate void HandleSendRemoteBundleMessage(RemoteAssetBundle bundle);
    public event HandleLoadManifests OnLoadManifests;
    public event HandleLoadManifestsAwaitable OnLoadManifestsAwaitable;
    public event HandleDeleteRemoteBundle OnDeleteRemoteBundle;
    public event HandleUpdateRemoteBundleVerification OnUpdateRemoteBundleVerification;
    public event HandleSendRemoteBundleMessage OnSendRemoteBundleMessage;

    public RemoteAssetBundleGUIEditTab(string label) : base(label)
    {
        RemoteAssetBundleTreeView.OnSelectBundleToEdit += SelectBundleToEdit;
    }

    public override void Show()
    {
        GUI.backgroundColor = DefaultColor;
        GUILayout.Space(TabLayoutPadding);
        GUILayout.Label(Label);
        GUILayout.Space(TabLayoutPadding);
        GUILayout.BeginHorizontal();
        {
            AppSelector();
            GUILayout.FlexibleSpace();
            ManifestEditor();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(TabLayoutPadding);
        ShowMessages();
    }

    public void LoadManifests()
    {
        if (OnLoadManifests != null)
        {
            OnLoadManifests();
        }

        if (OnLoadManifestsAwaitable != null)
        {
            OnLoadManifestsAwaitable();
        }
    }

    public void SelectBundleToEdit(RemoteAssetBundle bundle)
    {
        currentBundle = bundle;
    }

    public void SetManifests(RemoteAssetBundleManifest manifest)
    {
        Manifests = manifest;
        currentBundle = null;
        Apps = new HashSet<string>();
        foreach (RemoteAssetBundle b in Manifests.bundles)
        {
            Apps.Add(b.appName);
        }
    }

    public void ManifestEditor()
    {
        if (!string.IsNullOrEmpty(CurrentAppName))
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            lastRect.x = lastRect.x + TabLayoutPadding;
            lastRect.height = (parentPosition.height / 2) - TabLayoutPadding;
            BundleTree.OnGUI(lastRect);
            GUILayout.Space(TabLayoutPadding);
            GUILayout.BeginVertical();
            {
                RemoteAssetBundleEditor();
            }
            GUILayout.EndVertical();
        }
        else
        {
            GUILayout.Label("Click \"Load\" to Load All Apps and Select an App to Begin Editing Remote Asset Bundles");
        }
    }

    public void RemoteAssetBundleEditor()
    {
        if (currentBundle != null)
        {
            GUILayout.Label("Update Remote Asset Bundle");
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label(currentBundle.info.name);
            string verifyLabel = currentBundle.verified ? "Revoke" : "Verify";
            GUILayout.Space(TabLayoutPadding);
            // TODO hook all of these buttons up with their appropriate functions
            if (GUILayout.Button(verifyLabel, DefaultButtonOptions))
            {
                if (OnUpdateRemoteBundleVerification != null)
                {
                    OnUpdateRemoteBundleVerification(currentBundle, !currentBundle.verified);
                }
            }
            if (GUILayout.Button("Delete", DefaultButtonOptions))
            {
                if (OnDeleteRemoteBundle != null)
                {
                    OnDeleteRemoteBundle(currentBundle);
                }
            }
            GUILayout.Space(TabLayoutPadding);
            GUILayout.Label("Send Push Notification");
            GUILayout.Label("(Note: Push Notifications are Sent after Verification by Default, but can be Re-Sent Here.)");
            if (GUILayout.Button("Send", DefaultButtonOptions))
            {
                if (OnSendRemoteBundleMessage != null)
                {
                    OnSendRemoteBundleMessage(currentBundle);
                }
            }
        }
    }

    public void AppSelector()
    {
        GUILayout.BeginVertical();
        {
            EditorGUILayout.LabelField("Select App To View Manifest");
            if (GUILayout.Button("Load", DefaultButtonOptions))
            {
                LoadManifests();
            }
            AppViewScrollPos = GUILayout.BeginScrollView(AppViewScrollPos, ScrollViewStyle);
            {
                foreach (string app in Apps)
                {
                    if (GUILayout.Button(app))
                    {
                        SelectCurrentManifest(app);
                    }
                }
            }
            GUILayout.EndScrollView();
        }
        GUILayout.EndVertical();
    }

    public void SelectCurrentManifest(string appName)
    {
        RemoteAssetBundleManifest manifest = new RemoteAssetBundleManifest();
        manifest.bundles = Manifests.bundles.Where(ab => ab.appName == appName).ToArray();
        if (manifest.bundles.Length == 0)
        {
            CurrentAppName = null;
            return;
        }
        CurrentManifest = manifest;
        CurrentAppName = appName;
        MultiColumnHeaderState headerState = RemoteAssetBundleTreeView.CreateDefaultMultiColumnHeaderState();
        TreeViewState state = new TreeViewState();
        BundleTree = new RemoteAssetBundleTreeView(state, headerState, CurrentManifest, CurrentAppName);
    }

}
#endif