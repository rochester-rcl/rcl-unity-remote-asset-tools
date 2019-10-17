using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.Linq;
using System;
namespace RemoteAssetBundleTools
{
    [RequireComponent(typeof(SimpleProgressBar))]
    public class RemoteAssetBundleMapper : MonoBehaviour
    {
        [System.Serializable]
        public class RemoteAssetBundleMap
        {
            public string assetBundleKey;
            public string appName;
            public string displayName;
            public RemoteAssetBundleManifest Manifest { get; set; }
            public List<AssetBundle> Bundles { get; set; }
            public bool IsReady()
            {
                try
                {
                    return Manifest.bundles.Length == Bundles.Count;
                }
                catch (System.Exception ex)
                {
                    return false;
                }

            }
        }

        public string remoteAssetBundleEndpoint;
        [Tooltip("Set to false if you would like to pull unverified bundles (i.e. for development)")]
        public bool verified = true;
        [Tooltip("The maximum number of concurrent requests for fetching RemoteAssetBundles")]
        public uint requestCount = 4;
        public RemoteAssetBundleMap[] remoteAssetBundleMaps;
        public delegate void HandleManifestsRetrieved(RemoteAssetBundleManifest[] manifests);
        public delegate void HandleAssetBundlesLoaded(string key);
        public delegate void HandleProgressUpdate(string message, float progress);
        public delegate void HandleAllAssetBundlesLoaded();
        public delegate void HandleAssetBundleLoadingError(string message);
        public event HandleManifestsRetrieved OnManifestsRetrieved;
        public event HandleAssetBundlesLoaded OnAssetBundlesLoaded;
        public event HandleAllAssetBundlesLoaded OnAllAssetBundlesLoaded;
        public event HandleAssetBundleLoadingError OnAssetBundleLoadingError;
        public event HandleProgressUpdate OnProgressUpdate;
        private bool manifestsRetrieved;
        private CoroutineQueue taskQueue;
        private uint totalAssetBundles;
        private uint curretAssetBundleCount;
        private float progress;
        private SimpleProgressBar progressBar;
        public void Start()
        {
            taskQueue = new CoroutineQueue(requestCount, StartCoroutine);
            taskQueue.OnCoroutineError += HandleError;
            progressBar = gameObject.GetComponent<SimpleProgressBar>();
            GetUpdatedContent();
        }

        public void OnDestroy()
        {
            taskQueue.OnCoroutineError -= HandleError;
        }

        public IEnumerator FetchAllManifests()
        {
            Task t = FetchAllManifestsAwaitable();
            while (!t.IsCompleted)
            {
                yield return null;
            }
            if (t.IsFaulted)
            {
                throw new Exception("Unable to Check for New Content. Is Wi-Fi or Data Turned On?");
            }
        }

        public void GetUpdatedContent()
        {
            taskQueue.Chain(FetchAllManifests(), FetchAllAssetBundles());
        }

        public IEnumerator FetchAllAssetBundles()
        {
            foreach (RemoteAssetBundleMap mapping in remoteAssetBundleMaps)
            {
                yield return FetchAssetBundles(mapping.assetBundleKey);
            }
            if (AllRemoteBundlesReady())
            {
                UpdateProgressBar(1.0f, "All New Content Successfully Downloaded");
                if (OnAllAssetBundlesLoaded != null)
                {
                    OnAllAssetBundlesLoaded();
                }
            }
            else
            {
                string message = "There was an Error Downloading Some of the New Content.";
                if (OnAssetBundleLoadingError != null)
                {
                    OnAssetBundleLoadingError(message);
                }
                throw new Exception(message);
            }
        }

        public bool AllRemoteBundlesReady()
        {
            return remoteAssetBundleMaps.All(mapping => mapping.IsReady());
        }

        private IEnumerator FetchAssetBundles(string key)
        {
            RemoteAssetBundleMap assetMap = System.Array.Find(
                remoteAssetBundleMaps,
                (RemoteAssetBundleMap map) => map.assetBundleKey == key
            );
            if (assetMap == null || assetMap.Manifest.bundles == null)
            {
                yield return null;
            }
            else
            {
                assetMap.Bundles = new List<AssetBundle>();
                RemoteAssetBundle bundle;
                IEnumerator[] tasks = new IEnumerator[assetMap.Manifest.bundles.Length];
                for (int i = 0; i < assetMap.Manifest.bundles.Length; i++)
                {
                    bundle = assetMap.Manifest.bundles[i];
                    tasks[i] = FetchAssetBundle(bundle, assetMap);
                }
                UpdateProgressBar(0.0f, null);
                CoroutineQueue.HandleProgressUpdate func = PrepareProgress(
                    string.Format("Downloading {0} Assets for App {1}",
                    tasks.Length,
                    assetMap.displayName
                ));
                taskQueue.OnProgressUpdate += func;
                yield return taskQueue.All(tasks);
                taskQueue.OnProgressUpdate -= func;
                if (OnAssetBundlesLoaded != null)
                {
                    OnAssetBundlesLoaded(key);
                }
                UpdateProgressBar(1.0f, "All Assets Downloaded");
            }
        }

        private CoroutineQueue.HandleProgressUpdate PrepareProgress(string message)
        {
            return (float progress) =>
            {
                if (OnProgressUpdate != null)
                {
                    OnProgressUpdate(message, progress);
                }
                UpdateProgressBar(progress, message);
            };
        }

        private void UpdateProgressBar(float progress, string message = null)
        {
            progressBar.Progress = progress;
            progressBar.Message = message;
        }

        private void HandleError(string message)
        {
            UpdateProgressBar(1.0f, message);
        }

        private IEnumerator FetchAssetBundle(RemoteAssetBundle bundle, RemoteAssetBundleMap map)
        {
            System.Action<string, AssetBundle> callback = (error, b) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(error);
                }
                else
                {
                    if (b) map.Bundles.Add(b);
                }
            };
            return RemoteAssetBundleUtils.DownloadAssetBundleAsync(remoteAssetBundleEndpoint, bundle, callback);
        }

        private async Task FetchAllManifestsAwaitable()
        {
            UpdateProgressBar(1.0f, "Checking for New Content");
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
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(RemoteAssetBundleMapper))]
    public class RemoteAssetBundleMapperEditorLayout : Editor
    {
        public SerializedProperty remoteAssetBundleEndpoint;
        public SerializedProperty verified;
        public SerializedProperty requestCount;
        public SerializedProperty remoteAssetBundleMaps;
        public SerializedProperty arraySize;
        public void OnEnable()
        {
            remoteAssetBundleEndpoint = serializedObject.FindProperty("remoteAssetBundleEndpoint");
            verified = serializedObject.FindProperty("verified");
            requestCount = serializedObject.FindProperty("requestCount");
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
                EditorGUILayout.PropertyField(
                    remoteAssetBundleMaps.GetArrayElementAtIndex(i),
                    new GUIContent(string.Format("Remote Asset Bundle Map {0}",
                    i.ToString())),
                    true
                );
            }
            EditorGUI.indentLevel -= 1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(remoteAssetBundleEndpoint);
            EditorGUILayout.PropertyField(verified);
            EditorGUILayout.PropertyField(requestCount);
            DrawArrayProperties();
            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}
