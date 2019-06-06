

namespace RemoteAssetBundleTools {
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
    public struct AssetBundleInfo {

        ///<summary> The name of the AssetBundle </summary>
        public string Name; 
        ///<summary> The absolute path to the AssetBundle on disk </summary>
        public string Path;

        public AssetBundleInfo(string name, string path) {
            Name = name;
            Path = path;
        }

        ///<summary> Checks whether or not the AssetBundle exists on disk</summary>
        public bool Exists() {
            return File.Exists(Path);
        }

        ///<summary>Loads the given AssetBundle located at <see cref="AssetBundleInfo.Path" />
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>The loaded AssetBundle</returns>
        ///</summary>
        public AssetBundle Load() {
            if (Exists()) {
                return AssetBundle.LoadFromFile(Path);
            }
            throw new FileNotFoundException(string.Format("AssetBundle {0} does not exist in directory {1}", Name, Path));
        }
        ///<summary>Asynchronously Loads the given AssetBundle located at <see cref="AssetBundleInfo.Path" />
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>The loaded AssetBundle</returns>
        ///<remarks>This can only be called from the main thread and needs to be called in Awake or Start</remarks>
        ///</summary>
        public async Task<AssetBundle> LoadAsync() {
            if (Exists()) {
                string path = Path;
                var bundleReq = Task.Run(() => AssetBundle.LoadFromFileAsync(path));
                var result = await bundleReq;
                return result.assetBundle;
            }
            throw new FileNotFoundException(string.Format("AssetBundle {0} does not exist in directory {1}", Name, Path));
        }
    }

    // TODO what else do we need here?
    ///<summary>Struct to hold data about an asset bundle that lives on a remote server
    ///<remarks>Uses Fields instead of Properties in order to work with <see cref="JsonUtility" /> </remarks>
    ///</summary>
    [System.Serializable]
    public struct RemoteAssetBundle {
        public Hash128 VersionHash;
        public AssetBundleInfo Info;

        /* public static RemoteAssetBundle Deserialize(string val) {

        } */
    }

    ///<summary>Manifest of all RemoteAssetBundles living on a server
    ///<remarks>Uses Fields instead of Properties in order to work with <see cref="JsonUtility" /> </remarks>
    ///</summary>
    [System.Serializable]
    public struct RemoteAssetBundleManifest {
        public RemoteAssetBundle[] Bundles;

        public static RemoteAssetBundleManifest Deserialize(string val) {
            RemoteAssetBundleManifest obj = JsonUtility.FromJson<RemoteAssetBundleManifest>(val);
            return obj;
        }

    } 
}
