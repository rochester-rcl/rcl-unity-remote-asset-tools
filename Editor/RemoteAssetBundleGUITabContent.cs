using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using RemoteAssetBundleTools;
using System.Linq;

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
    [SerializeField]
    public string serverEndpoint;
    [SerializeField]
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
    // TODO should use a delegate / event for this and bubble up to Main instead
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
    public string UploadMessage;
    public string AppName;
    public delegate void UploadRemoteAssetBundle(AssetBundleInfo bundleInfo, string appName, string message);
    public event UploadRemoteAssetBundle OnUploadRemoteAssetBundle;
    public RemoteAssetBundleGUIAddTab(string label) : base(label) { }

    public override void Show()
    {
        GUI.backgroundColor = DefaultColor;
        GUILayout.Space(TabLayoutPadding);
        GUILayout.Label(Label);
        GUILayout.Space(TabLayoutPadding);
        OpenAssetBundleButton();
        GUILayout.Space(TabLayoutPadding / 2);
        AppName = EditorGUILayout.TextField("App Name (optional)", AppName, DefaultTextFieldOptions);
        UploadMessage = EditorGUILayout.TextField("Upload Message (optional)", UploadMessage, DefaultTextFieldOptions);
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

    public void OpenAssetBundleDialog()
    {
        string abPath = EditorUtility.OpenFilePanel("Select an AssetBundle", "", "unity3d");
        if (!string.IsNullOrEmpty(abPath))
        {
            ABInfo = new AssetBundleInfo(abPath);
            AddMessage(string.Format("Current AssetBundle is {0}", ABInfo.Name), MessageStatus.Success);
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
    public delegate void HandleLoadManifests();
    public event HandleLoadManifests OnLoadManifests;

    public RemoteAssetBundleGUIEditTab(string label) : base(label) { }

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
        ShowMessages();
    }

    public void LoadManifests()
    {
        if (OnLoadManifests != null)
        {
            OnLoadManifests();
        }
    }

    public void SetManifests(RemoteAssetBundleManifest manifest)
    {
        Manifests = manifest;
        foreach (RemoteAssetBundle b in Manifests.Bundles)
        {
            Apps.Add(b.AppName);
        }
    }

    public void ManifestEditor()
    {
        if (!string.IsNullOrEmpty(CurrentAppName))
        {
            MultiColumnHeaderState headerState = RemoteAssetBundleTreeView.CreateDefaultMultiColumnHeaderState();
            TreeViewState state = new TreeViewState();
            RemoteAssetBundleTreeView tree = new RemoteAssetBundleTreeView(state, headerState, CurrentManifest, CurrentAppName);
            Rect lastRect = GUILayoutUtility.GetLastRect();
            lastRect.x = lastRect.x + 20;
            lastRect.height = 300;
            tree.OnGUI(lastRect);
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
        manifest.Bundles = Manifests.Bundles.Where(ab => ab.AppName == appName).ToArray();
        CurrentManifest = manifest;
        CurrentAppName = appName;
    }

}
#endif