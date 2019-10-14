namespace RemoteAssetBundleTools
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Threading.Tasks;
    using System.IO;

    /// <summary>
    /// Struct to hold basic AssetBundle Info
    ///<remarks>Uses Fields instead of Properties in order to work with <see cref="JsonUtility" /> </remarks>
    /// </summary>
    [System.Serializable]
    public struct AssetBundleInfo
    {

        ///<summary> The name of the AssetBundle </summary>
        public string name;
        ///<summary> The absolute path to the AssetBundle on disk </summary>
        public string path;

        public AssetBundleInfo(string n, string p)
        {
            name = n;
            path = p;
        }

        public AssetBundleInfo(string p)
        {
            FileInfo info = new FileInfo(p);
            name = info.Name;
            path = p;
        }

        ///<summary> Checks whether or not the AssetBundle exists on disk</summary>
        public bool Exists()
        {
            return File.Exists(path);
        }

        ///<summary>Loads the given AssetBundle located at <see cref="AssetBundleInfo.Path" />
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>The loaded AssetBundle</returns>
        ///</summary>
        public AssetBundle Load()
        {
            if (Exists())
            {
                return AssetBundle.LoadFromFile(path);
            }
            throw new FileNotFoundException(string.Format("AssetBundle {0} does not exist in directory {1}", name, path));
        }
        ///<summary>Asynchronously Loads the given AssetBundle located at <see cref="AssetBundleInfo.Path" />
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>The loaded AssetBundle</returns>
        ///<remarks>This can only be called from the main thread and needs to be called in Awake or Start</remarks>
        ///</summary>
        public async Task<AssetBundle> LoadAsync()
        {
            if (Exists())
            {
                string p = path;
                var bundleReq = Task.Run(() => AssetBundle.LoadFromFileAsync(p));
                var result = await bundleReq;
                return result.assetBundle;
            }
            throw new FileNotFoundException(string.Format("AssetBundle {0} does not exist in directory {1}", name, path));
        }
    }

    // TODO add a verified field to send only to dev build of app
    ///<summary>Struct to hold data about an asset bundle that lives on a remote server
    ///<remarks>Uses Fields instead of Properties in order to work with <see cref="JsonUtility" /> </remarks>
    ///</summary>
    [System.Serializable]
    public class RemoteAssetBundle
    {
        public string versionHash;
        public string appName;
        public bool verified;
        /// <summary>The date the bundle was uploaded. For display purposes only.</summary>
        public string date;
        /// <summary>
        /// The message content associated with the bundle from when it was uploaded. For display purposes only.
        /// </summary>
        public string messageContent;
        public AssetBundleInfo info;

        public Hash128 toHash128()
        {
            return Hash128.Parse(versionHash);
        }

        public static RemoteAssetBundle Deserialize(string val)
        {
            RemoteAssetBundle obj = JsonUtility.FromJson<RemoteAssetBundle>(val);
            return obj;
        }
    }

    /// <summary>Simple struct to represent a Firebase Cloud Message.</summary>
    [System.Serializable]
    public struct FCMMessage
    {
        public string title;
        public string body;
        public string icon;
        public bool sendImmediate;

        public string Serialize()
        {
            return JsonUtility.ToJson(this);
        }

        public FCMMessage(string bodyVal, string titleVal, string iconVal, bool immediate = false)
        {
            title = titleVal;
            body = bodyVal;
            icon = iconVal;
            sendImmediate = immediate;
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(title)) return false;
            if (string.IsNullOrEmpty(body)) return false;
            return true;
        }

        public static FCMMessage Deserialize(string val)
        {
            FCMMessage obj = JsonUtility.FromJson<FCMMessage>(val);
            return obj;
        }
    }

    public struct FCMMessageStatus
    {
        public bool sendStatus;
        public string statusMessage;

        public static FCMMessageStatus Deserialize(string val)
        {
            FCMMessageStatus obj = JsonUtility.FromJson<FCMMessageStatus>(val);
            return obj;
        }
    }
    /// <summary>Manifest of all RemoteAssetBundles living on a server </summary>
    /// <remarks>Uses Fields instead of Properties in order to work with <see cref="JsonUtility" /> </remarks>
    [System.Serializable]
    public struct RemoteAssetBundleManifest
    {
        public RemoteAssetBundle[] bundles;

        public static RemoteAssetBundleManifest Deserialize(string val)
        {
            RemoteAssetBundleManifest obj = JsonUtility.FromJson<RemoteAssetBundleManifest>(val);
            return obj;
        }

    }
}
