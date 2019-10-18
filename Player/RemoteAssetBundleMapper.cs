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
            public string[] localAssetBundles;
            public RemoteAssetBundleManifest Manifest { get; set; }
            public List<AssetBundle> Bundles { get; set; }
            public void AddLoadedBundle(AssetBundle bundle)
            {
                if (Bundles == null)
                {
                    Bundles = new List<AssetBundle>();
                }
                Bundles.Add(bundle);
            }
            public bool IsReady()
            {
                try
                {
                    return (Manifest.bundles.Length + localAssetBundles.Length) == Bundles.Count;
                }
                catch
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
            taskQueue.Chain(LoadAllLocalAssetBundles(), FetchAllManifests(), FetchAllAssetBundles());
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

        public IEnumerator LoadAllLocalAssetBundles()
        {
            foreach (RemoteAssetBundleMap map in remoteAssetBundleMaps)
            {
                yield return LoadLocalAssetBundles(map);
            }
        }

        private IEnumerator LoadLocalAssetBundles(RemoteAssetBundleMap assetMap)
        {
            IEnumerator[] tasks = new IEnumerator[assetMap.localAssetBundles.Length];
            for (int i = 0; i < assetMap.localAssetBundles.Length; i++)
            {
                tasks[i] = LoadLocalAssetBundle(assetMap.localAssetBundles[i], assetMap);
            }
            yield return taskQueue.All(tasks);
        }

        private IEnumerator LoadLocalAssetBundle(string path, RemoteAssetBundleMap assetMap)
        {
            string absPath = System.IO.Path.Combine(Application.streamingAssetsPath, path);
            var bundleRequest = AssetBundle.LoadFromFileAsync(absPath);
            yield return bundleRequest;
            AssetBundle b = bundleRequest.assetBundle;
            if (b)
            {
                assetMap.AddLoadedBundle(b);
            }
            else
            {
                throw new Exception("Unable to Load Local Asset Bundle");
            }
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

        private void UpdateProgressBar(float progress, string message = null, bool error = false)
        {
            progressBar.Progress = progress;
            progressBar.Message = message;
            progressBar.ErrorState = error;
        }

        private void HandleError(string message)
        {
            UpdateProgressBar(1.0f, message, true);
        }

        private IEnumerator FetchAssetBundle(RemoteAssetBundle bundle, RemoteAssetBundleMap assetMap)
        {
            System.Action<string, AssetBundle> callback = (error, b) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(error);
                }
                else
                {
                    if (b) assetMap.AddLoadedBundle(b);
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
        public SerializedProperty localAssetBundlesArraySize;
        public SerializedProperty assetBundleKey;
        public SerializedProperty appName;
        public SerializedProperty displayName;
        private bool[] foldouts;
        private bool showRemoteAssetBundleMaps = false;
        public void OnEnable()
        {
            remoteAssetBundleEndpoint = serializedObject.FindProperty("remoteAssetBundleEndpoint");
            verified = serializedObject.FindProperty("verified");
            requestCount = serializedObject.FindProperty("requestCount");
            remoteAssetBundleMaps = serializedObject.FindProperty("remoteAssetBundleMaps");
            arraySize = remoteAssetBundleMaps.FindPropertyRelative("Array.size");
            foldouts = new bool[arraySize.intValue];
        }
        private void DrawArrayLocalAssetBundleProperties(SerializedProperty localAssetBundles)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            localAssetBundlesArraySize = localAssetBundles.FindPropertyRelative("Array.size");
            EditorGUILayout.LabelField("Local Asset Bundles");
            EditorGUI.indentLevel += 1;

            if (GUILayout.Button("Add"))
            {
                localAssetBundlesArraySize.intValue++;
                SerializedProperty localBundle = localAssetBundles.GetArrayElementAtIndex(localAssetBundlesArraySize.intValue - 1);
                if (GUILayout.Button("Add"))
                {
                    string bundlePath = EditorUtility.OpenFilePanel("Asset Bundles", "", "");
                    localBundle.stringValue = System.IO.Path.GetFileName(bundlePath);
                }
            }
            for (int i = 0; i < localAssetBundlesArraySize.intValue; i++)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    string assetBundleName = localAssetBundles.GetArrayElementAtIndex(i).stringValue;
                    if (string.IsNullOrEmpty(assetBundleName))
                    {
                        assetBundleName = "Add a Local Asset Bundle";
                    }
                    EditorGUILayout.LabelField(assetBundleName);
                    if (GUILayout.Button("Remove"))
                    {
                        localAssetBundles.DeleteArrayElementAtIndex(i);
                        localAssetBundlesArraySize.intValue--;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }

        private void DrawArrayProperties()
        {
            showRemoteAssetBundleMaps = EditorGUILayout.Foldout(showRemoteAssetBundleMaps, "Remote Asset Bundle Maps");
            if (showRemoteAssetBundleMaps)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(arraySize, new GUIContent("Mappings Count"), true);
                for (int i = 0; i < arraySize.intValue; i++)
                {
                    SerializedProperty remoteAssetBundleMap = remoteAssetBundleMaps.GetArrayElementAtIndex(i);
                    assetBundleKey = remoteAssetBundleMap.FindPropertyRelative("assetBundleKey");
                    appName = remoteAssetBundleMap.FindPropertyRelative("appName");
                    displayName = remoteAssetBundleMap.FindPropertyRelative("displayName");
                    SerializedProperty localAssetBundles = remoteAssetBundleMap.FindPropertyRelative("localAssetBundles");
                    foldouts[i] = EditorGUILayout.Foldout(foldouts[i], string.Format("Remote Asset Bundle Map {0}",
                        i.ToString()));
                    if (foldouts[i])
                    {
                        EditorGUI.indentLevel += 1;
                        assetBundleKey.stringValue = EditorGUILayout.TextField("Asset Bundle Key", assetBundleKey.stringValue);
                        appName.stringValue = EditorGUILayout.TextField("App Name", appName.stringValue);
                        displayName.stringValue = EditorGUILayout.TextField("Display Name", displayName.stringValue);
                        DrawArrayLocalAssetBundleProperties(localAssetBundles);
                        EditorGUI.indentLevel -= 1;
                    }
                }
                EditorGUI.indentLevel -= 1;
            }
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
