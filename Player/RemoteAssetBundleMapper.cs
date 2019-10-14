using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

namespace RemoteAssetBundleTools
{

    public class RemoteAssetBundleMapper : MonoBehaviour
    {
        [System.Serializable]
        public class RemoteAssetBundleMap
        {
            public string assetBundleKey;
            public string appName;
            public RemoteAssetBundleManifest Manifest { get; set; }
            public AssetBundle[] Bundles { get; set; }
        }

        public string remoteAssetBundleEndpoint;
        [Tooltip("Set to false if you would like to pull unverified bundles (i.e. for development)")]
        public bool verified = true;
        public RemoteAssetBundleMap[] remoteAssetBundleMaps;
        public delegate void HandleManifestsRetrieved(RemoteAssetBundleManifest[] manifests);
        public event HandleManifestsRetrieved OnManifestsRetrieved;
        private bool manifestsRetrieved;
        public void Start()
        {
            StartCoroutine(FetchAllManifests());
        }

        public IEnumerator FetchAllManifests()
        {
            Task t = FetchAllManifestsAwaitable();
            while (!t.IsCompleted)
            {
                yield return null;
            }
            // try to download the bundle - won't actually do it here though 
            StartCoroutine(FetchAssetBundles(remoteAssetBundleMaps[0].assetBundleKey));
        }

        private IEnumerator FetchAssetBundles(string key)
        {
            RemoteAssetBundleMap assetMap = System.Array.Find(remoteAssetBundleMaps, (RemoteAssetBundleMap map) => map.assetBundleKey == key);
            assetMap.Bundles = new AssetBundle[assetMap.Manifest.bundles.Length];
            RemoteAssetBundle bundle;
            for (int i = 0; i < assetMap.Manifest.bundles.Length; i++)
            {
                bundle = assetMap.Manifest.bundles[i];
                yield return FetchAssetBundle(bundle, assetMap, i);
            }
            // On Asset Bundles Loaded 
           
        }

        private Coroutine FetchAssetBundle(RemoteAssetBundle bundle, RemoteAssetBundleMap map, int index)
        {
            System.Action<string, AssetBundle> callback = (error, b) => {
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(error);
                }
                else 
                {
                    map.Bundles[index] = b;
                }
            };
            return StartCoroutine(RemoteAssetBundleUtils.DownloadAssetBundleAsync(remoteAssetBundleEndpoint, bundle, callback));
        }

        private async Task FetchAllManifestsAwaitable()
        {
            try
            {
                RemoteAssetBundleManifest[] manifests = new RemoteAssetBundleManifest[remoteAssetBundleMaps.Length];
                RemoteAssetBundleMap map;
                for (int i = 0; i < remoteAssetBundleMaps.Length; i++)
                {
                    map = remoteAssetBundleMaps[i];
                    map.Manifest = await RemoteAssetBundleUtils.GetAssetBundleManifest(remoteAssetBundleEndpoint, map.appName, verified);
                    manifests[i] = map.Manifest;
                }
                manifestsRetrieved = true;
                if (OnManifestsRetrieved != null)
                {
                    OnManifestsRetrieved(manifests);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(RemoteAssetBundleMapper))]
    public class RemoteAssetBundleMapperEditorLayout : Editor
    {
        public SerializedProperty remoteAssetBundleEndpoint;
        public SerializedProperty verified;
        public SerializedProperty remoteAssetBundleMaps;
        public SerializedProperty arraySize;
        public void OnEnable()
        {
            remoteAssetBundleEndpoint = serializedObject.FindProperty("remoteAssetBundleEndpoint");
            verified = serializedObject.FindProperty("verified");
            remoteAssetBundleMaps = serializedObject.FindProperty("remoteAssetBundleMaps");
            arraySize = remoteAssetBundleMaps.FindPropertyRelative("Array.size");
        }

        private void DrawArrayProperties()
        {
            EditorGUILayout.PropertyField(remoteAssetBundleMaps);
            EditorGUI.indentLevel += 1;
            EditorGUILayout.PropertyField(arraySize, new GUIContent("Mappings Count"), true);
            for (int i = 0; i < arraySize.intValue; i++)
            {
                EditorGUILayout.PropertyField(remoteAssetBundleMaps.GetArrayElementAtIndex(i), new GUIContent(string.Format("Remote Asset Bundle Map {0}", i.ToString())), true);
            }
            EditorGUI.indentLevel -= 1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(remoteAssetBundleEndpoint);
            EditorGUILayout.PropertyField(verified);
            DrawArrayProperties();
            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}
