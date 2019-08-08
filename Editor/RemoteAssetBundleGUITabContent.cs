﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using RemoteAssetBundleTools;

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

/*************************** ABSTRACT CLASS ************************************************/

public abstract class RemoteAssetBundleGUITabContent
{
    public string Label { get; set; }
    protected Color DefaultColor = new Color(0.9f, 0.9f, 0.9f);
    protected int TabLayoutPadding = 20;

    protected List<StatusMessage> Messages = new List<StatusMessage>();

    protected GUILayoutOption[] DefaultOptions = { GUILayout.ExpandWidth(true), GUILayout.MinWidth(300), GUILayout.MaxWidth(500) };
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
    public string ServerEndpoint;
    [SerializeField]
    private Object JWTFile;
    private string EditorPrefsKey = "RemoteAssetBundleConfig";

    public RemoteAssetBundleGUIConfigureTab(string label) : base(label)
    {
        string data = EditorPrefs.GetString(EditorPrefsKey, JsonUtility.ToJson(this, false));
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
        ServerEndpoint = EditorGUILayout.TextField("Server URL", ServerEndpoint, DefaultOptions);
        JWTFile = EditorGUILayout.ObjectField("JWT Auth File", JWTFile, typeof(TextAsset), false, DefaultOptions);
        GUILayout.Space(TabLayoutPadding);
        CheckEndpointButton();
        CheckJWTButton();
        GUILayout.Space(TabLayoutPadding);
        ShowMessages();
    }

    public async void CheckEndpoint()
    {
        bool status;
        if (!string.IsNullOrEmpty(ServerEndpoint))
        {
            status = await RemoteAssetBundleUtils.CheckEndpoint(ServerEndpoint);
        }
        else
        {
            status = false;
        }
        if (status)
        {
            AddMessage("Successfully Connected to Server!", MessageStatus.Success);
        }
        else
        {
            AddMessage("Unable to Connect to Server!", MessageStatus.Error);
        }
        Debug.Log(status);
    }

    public async void CheckJWT()
    {
        bool status;
        if (!string.IsNullOrEmpty(ServerEndpoint) && JWTFile)
        {
            status = await RemoteAssetBundleUtils.CheckJWT(ServerEndpoint, JWTFile.name);
            Debug.Log(status);
        }
        else
        {
            status = false;
        }
        if (status)
        {
            AddMessage(string.Format("Successfully Connected to Server with JWT {0}!", JWTFile.name), MessageStatus.Success);
        }
        else
        {
            AddMessage(string.Format("Unable to Connect to Server with JWT {0}!", JWTFile.name), MessageStatus.Error);
        }
    }

    public void CheckEndpointButton()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Check Server Connection");
            if (GUILayout.Button("Check Server", DefaultOptions))
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
            if (GUILayout.Button("Check JWT", DefaultOptions))
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
            EditorPrefs.SetString(EditorPrefsKey, data);
        }
    }
}