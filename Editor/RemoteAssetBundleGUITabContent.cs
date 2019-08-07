using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public abstract class RemoteAssetBundleGUITabContent
{
    public string Label { get; set; }
    protected Color DefaultColor = new Color(0.9f, 0.9f, 0.9f);
    protected int TabLayoutPadding = 20;

    protected GUILayoutOption[] DefaultOptions = { GUILayout.ExpandWidth(true), GUILayout.MinWidth(300), GUILayout.MaxWidth(500) };
    public RemoteAssetBundleGUITabContent(string label)
    {
        Label = label;
    }
    public virtual void Show()
    {

    }
}

public class RemoteAssetBundleGUIConfigureTab : RemoteAssetBundleGUITabContent
{
    [SerializeField]
    public string ServerEndpoint;
    [SerializeField]
    private Object JWTFile;

    public RemoteAssetBundleGUIConfigureTab(string label) : base(label)
    {

    }

    public override void Show()
    {
        GUI.backgroundColor = DefaultColor;
        GUILayout.Space(TabLayoutPadding);
        GUILayout.Label(Label);
        ServerEndpoint = EditorGUILayout.TextField("Server URL", ServerEndpoint, DefaultOptions);
        JWTFile = EditorGUILayout.ObjectField("JWT Auth File", JWTFile, typeof(TextAsset), false, DefaultOptions);
    }
}
