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
            public List<AssetBundle> RemoteBundles { get; set; }
            public List<AssetBundle> LocalBundles { get; set; }

            public List<AssetBundle> AllBundles()
            {
                if (RemoteBundles == null && LocalBundles == null) return null;
                if (RemoteBundles != null && LocalBundles == null) return RemoteBundles;
                if (LocalBundles != null && RemoteBundles == null) return LocalBundles;
                return LocalBundles.Concat(RemoteBundles).ToList();
            }

            public void UnloadLocalBundles()
            {
                if (LocalBundles != null)
                {
                    foreach (AssetBundle b in LocalBundles)
                    {
                        b.Unload(false);
                    }
                    LocalBundles = null;
                }
            }

            public void UnloadRemoteBundles()
            {
                if (RemoteBundles != null)
                {
                    foreach (AssetBundle b in RemoteBundles)
                    {
                        b.Unload(false);
                    }
                }
                RemoteBundles = null;
            }
            public void UnloadAllBundles()
            {
                UnloadLocalBundles();
                UnloadRemoteBundles();
            }
            public void AddLoadedRemoteBundle(AssetBundle bundle)
            {
                if (RemoteBundles == null)
                {
                    RemoteBundles = new List<AssetBundle>();
                }
                RemoteBundles.Add(bundle);
            }
            public void AddLoadedLocalBundle(AssetBundle bundle)
            {
                if (LocalBundles == null)
                {
                    LocalBundles = new List<AssetBundle>();
                }
                LocalBundles.Add(bundle);
            }
            public bool AreLocalBundlesReady()
            {
                return localAssetBundles.Length == LocalBundles.Count;
            }

            public bool AreRemoteBundlesReady()
            {
                try
                {
                    if (Manifest.bundles.Length == 0) return true;
                    return Manifest.bundles.Length == RemoteBundles.Count;
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                    return false;
                }
            }
            public bool IsReady()
            {
                return AreLocalBundlesReady() && AreRemoteBundlesReady();
            }
        }

        public string remoteAssetBundleEndpoint;
        [Tooltip("Set to false if you would like to pull unverified bundles (i.e. for development)")]
        public bool verified = true;
        [Tooltip("The maximum number of concurrent requests for fetching RemoteAssetBundles")]
        public uint requestCount = 4;
        public bool loadAssetBundlesOnStart = false;
        public RemoteAssetBundleMap[] remoteAssetBundleMaps;
        public delegate void HandleManifestsRetrieved(RemoteAssetBundleManifest[] manifests);
        public delegate void HandleAssetBundlesLoaded(string key);
        public delegate void HandleProgressUpdate(string message, float progress);
        public delegate void HandleAllRemoteAssetBundlesLoaded();
        public delegate void HandleAllLocalAssetBundlesLoaded();
        public delegate void HandleGetUpdatedContentSucces();
        public delegate void HandleAssetBundleLoadingError(string message);
        public delegate void HandleManifestLoadingError(string message);
        public event HandleManifestsRetrieved OnManifestsRetrieved;
        public event HandleAssetBundlesLoaded OnAssetBundlesLoaded;
        public event HandleAllRemoteAssetBundlesLoaded OnAllRemoteAssetBundlesLoaded;
        public event HandleAllLocalAssetBundlesLoaded OnAllLocalAssetBundlesLoaded;
        public event HandleGetUpdatedContentSucces OnGetUpdatedContentSuccess;
        public event HandleAssetBundleLoadingError OnAssetBundleLoadingError;
        public event HandleManifestLoadingError OnManifestLoadingError;
        public event HandleProgressUpdate OnProgressUpdate;
        private CoroutineQueue taskQueue;
        private uint totalAssetBundles;
        private uint curretAssetBundleCount;
        private float progress;
        private SimpleProgressBar progressBar;
        private bool newContentAvailable = false;
        public void Start()
        {
            taskQueue = new CoroutineQueue(requestCount, StartCoroutine);
            taskQueue.OnCoroutineError += HandleError;
            progressBar = gameObject.GetComponent<SimpleProgressBar>();
            if (loadAssetBundlesOnStart)
            {
                GetUpdatedContent();
            }
        }

        public void ToggleProgressBar(bool val)
        {
            if (progressBar)
            {
                progressBar.progressObj.SetActive(val);
                progressBar.messageObj.SetActive(val);
            }
        }

        public void OnDestroy()
        {
            taskQueue.OnCoroutineError -= HandleError;
        }

        public void UnloadAllBundles()
        {
            foreach (RemoteAssetBundleMap map in remoteAssetBundleMaps)
            {
                map.UnloadAllBundles();
            }
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
                string message = "Unable to Check for New Content. Is Wi-Fi or Data Turned On?";
                if (OnManifestLoadingError != null)
                {
                    OnManifestLoadingError(message);
                }
                throw new Exception(message);
            }
        }

        public void GetUpdatedContent()
        {
            taskQueue.Chain(LoadAllLocalAssetBundles(), FetchAllManifests(), FetchAllAssetBundles(), AllContentUpdated());
        }

        public IEnumerator AllContentUpdated()
        {
            if (AllBundlesReady())
            {
                yield return null;
                if (OnGetUpdatedContentSuccess != null)
                {
                    OnGetUpdatedContentSuccess();
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

        public IEnumerator FetchAllAssetBundles()
        {
            if (newContentAvailable)
            {
                foreach (RemoteAssetBundleMap mapping in remoteAssetBundleMaps)
                {
                    yield return FetchAssetBundles(mapping.assetBundleKey);
                }
                if (AllRemoteBundlesReady())
                {
                    UpdateProgressBar(1.0f, "All New Content Successfully Loaded");
                    if (OnAllRemoteAssetBundlesLoaded != null)
                    {
                        OnAllRemoteAssetBundlesLoaded();
                    }
                }
            }
            else
            {
                UpdateProgressBar(1.0f, "No New Content Available to Download. Your App is up-to-date!");
                if (AllRemoteBundlesReady())
                {
                    UpdateProgressBar(1.0f, "All Local Content Successfully Loaded");
                    if (OnAllRemoteAssetBundlesLoaded != null)
                    {
                        OnAllRemoteAssetBundlesLoaded();
                    }
                }
                else
                {
                    string message = "There was an Error Loading the App's Content.";
                    if (OnAssetBundleLoadingError != null)
                    {
                        OnAssetBundleLoadingError(message);
                    }
                    throw new Exception(message);
                }
            }

        }

        public bool AllRemoteBundlesReady()
        {
            return remoteAssetBundleMaps.All(mapping => mapping.AreRemoteBundlesReady());
        }

        public bool AllLocalBundlesReady()
        {
            return remoteAssetBundleMaps.All(mapping => mapping.AreLocalBundlesReady());
        }

        public bool AllBundlesReady()
        {
            return remoteAssetBundleMaps.All(mapping => mapping.IsReady());
        }

        public IEnumerator LoadAllLocalAssetBundles()
        {
            UpdateProgressBar(0.0f, "Loading Local Assets");
            foreach (RemoteAssetBundleMap map in remoteAssetBundleMaps)
            {
                yield return LoadLocalAssetBundles(map);
            }
            if (AllLocalBundlesReady())
            {
                if (OnAllLocalAssetBundlesLoaded != null)
                {
                    OnAllLocalAssetBundlesLoaded();
                }
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
                assetMap.AddLoadedLocalBundle(b);
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
                    if (b) assetMap.AddLoadedRemoteBundle(b);
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
                if (map.Manifest.bundles != null && map.Manifest.bundles.Length > 0) newContentAvailable = true;
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
        public SerializedProperty loadAssetBundlesOnStart;
        public SerializedProperty remoteAssetBundleMaps;
        public SerializedProperty arraySize;
        public SerializedProperty localAssetBundlesArraySize;
        public SerializedProperty assetBundleKey;
        public SerializedProperty appName;
        public SerializedProperty displayName;
        private List<bool> foldouts;
        private bool showRemoteAssetBundleMaps = false;
        private int buttonWidth = 100;
        public void OnEnable()
        {
            remoteAssetBundleEndpoint = serializedObject.FindProperty("remoteAssetBundleEndpoint");
            verified = serializedObject.FindProperty("verified");
            requestCount = serializedObject.FindProperty("requestCount");
            loadAssetBundlesOnStart = serializedObject.FindProperty("loadAssetBundlesOnStart");
            remoteAssetBundleMaps = serializedObject.FindProperty("remoteAssetBundleMaps");
            arraySize = remoteAssetBundleMaps.FindPropertyRelative("Array.size");
            populateFoldouts();
        }

        public void populateFoldouts()
        {
            foldouts = new List<bool>();
            for (int i = 0; i < arraySize.intValue; i++)
            {
                foldouts.Add(false);
            }
        }
        private void DrawArrayLocalAssetBundleProperties(SerializedProperty localAssetBundles)
        {
            Rect last;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                localAssetBundlesArraySize = localAssetBundles.FindPropertyRelative("Array.size");
                EditorGUILayout.LabelField("Local Asset Bundles");
                EditorGUI.indentLevel += 1;
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUI.indentLevel * 10);
                    if (GUILayout.Button("Add", GUILayout.Width(buttonWidth)))
                    {
                        localAssetBundlesArraySize.intValue++;
                        SerializedProperty localBundle = localAssetBundles.GetArrayElementAtIndex(localAssetBundlesArraySize.intValue - 1);
                        string bundlePath = EditorUtility.OpenFilePanel("Asset Bundles", "", "");
                        localBundle.stringValue = System.IO.Path.GetFileName(bundlePath);
                    }
                }
                EditorGUILayout.EndHorizontal();
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
                        if (GUILayout.Button("Remove", GUILayout.Width(buttonWidth)))
                        {
                            localAssetBundles.DeleteArrayElementAtIndex(i);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    last = GUILayoutUtility.GetLastRect();
                    float h = last.height;
                    last.y = (last.y + h) + 2;
                    last.height = 1;
                    EditorGUI.DrawRect(last, Color.gray);
                    EditorGUILayout.Separator();
                }
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawArrayProperties()
        {
            showRemoteAssetBundleMaps = EditorGUILayout.Foldout(showRemoteAssetBundleMaps, "Remote Asset Bundle Maps");
            if (showRemoteAssetBundleMaps)
            {
                EditorGUI.indentLevel += 1;
                if (GUILayout.Button("Add", GUILayout.Width(buttonWidth)))
                {
                    arraySize.intValue++;
                    foldouts.Add(false);
                }
                for (int i = 0; i < arraySize.intValue; i++)
                {
                    SerializedProperty remoteAssetBundleMap = remoteAssetBundleMaps.GetArrayElementAtIndex(i);
                    assetBundleKey = remoteAssetBundleMap.FindPropertyRelative("assetBundleKey");
                    appName = remoteAssetBundleMap.FindPropertyRelative("appName");
                    displayName = remoteAssetBundleMap.FindPropertyRelative("displayName");
                    SerializedProperty localAssetBundles = remoteAssetBundleMap.FindPropertyRelative("localAssetBundles");
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        GUILayout.BeginHorizontal();
                        {
                            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], string.Format("Remote Asset Bundle Map {0}",
                                i.ToString()));
                            if (GUILayout.Button("Remove", GUILayout.Width(buttonWidth)))
                            {
                                remoteAssetBundleMaps.DeleteArrayElementAtIndex(i);
                                return;
                            }
                        }
                        GUILayout.EndHorizontal();
                        EditorGUILayout.Space();
                        if (foldouts[i])
                        {
                            EditorGUI.indentLevel += 1;
                            assetBundleKey.stringValue = EditorGUILayout.TextField("Asset Bundle Key", assetBundleKey.stringValue);
                            appName.stringValue = EditorGUILayout.TextField("App Name", appName.stringValue);
                            displayName.stringValue = EditorGUILayout.TextField("Display Name", displayName.stringValue);
                            DrawArrayLocalAssetBundleProperties(localAssetBundles);
                            EditorGUILayout.Space();
                            EditorGUI.indentLevel -= 1;
                        }
                    }
                    GUILayout.EndVertical();
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
            EditorGUILayout.PropertyField(loadAssetBundlesOnStart);
            DrawArrayProperties();
            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}
