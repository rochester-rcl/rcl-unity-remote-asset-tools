namespace RemoteAssetBundleTools {
    
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Net.Http;
    using System.Net;
    using System.Threading.Tasks;

    public static class RemoteAssetBundleUtils {

        ///<summary>Searches a directory for a given AssetBundle
        ///<param name="name">The name of the AssetBundle</param>
        ///<param name="assetBundleDir">The absolute path to the directory AssetBundles are built to</param>
        ///<exception cref="DirectoryNotFoundException">The AssetBundle directory is not found</exception>
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>Returns an AssetBundleInfo struct</returns>
        ///</summary>
        public static AssetBundleInfo GetAssetBundleInfo(string name, string assetBundleDir) {
            if (Directory.Exists(assetBundleDir)) {
                string assetPath = Path.Combine(assetBundleDir, name);
                if (File.Exists(assetPath)) {
                    return new AssetBundleInfo(name, assetPath);
                } else {
                    throw new FileNotFoundException(string.Format("AssetBundle {0} does not exist in directory {1}", name, assetBundleDir));
                }
            } else {
                throw new DirectoryNotFoundException(string.Format("AssetBundle directory {0} does not exist!", assetBundleDir));
            }
        }

        ///<summary>Uploads an AssetBundle to RESTful service
        ///<param name="url">The absolute URL to the POST endpoint</param>
        ///<param name="info">The AssetBundleInfo struct</param>
        ///<param name="message">A message to be used on the server side</param>
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>Returns a Task so can be used with await or ContinueWith</returns>
        ///</summary>
        public static Task<HttpResponseMessage> UploadAssetBundleAsync(string url, AssetBundleInfo info, string message) {
            using (HttpClient client = new HttpClient()) {
                if (info.Exists()) {
                    MultipartFormDataContent formData = new MultipartFormDataContent();
                    FileInfo f = new FileInfo(info.Path);
                    StreamContent fs = new StreamContent(f.OpenRead());
                    formData.Add(fs, "bundle", f.Name);
                    formData.Add(new StringContent(message), "message");
                    return client.PostAsync(url, formData);
                } else {
                    throw new FileNotFoundException(string.Format("AssetBundle {0} does not exist in directory {1}", info.Name, info.Path));
                }      
            }
        }

        public static Task<HttpResponseMessage> GetAssetBundleManifestAsync(string url) {
            using (HttpClient client = new HttpClient()) {
                return client.GetAsync(url);
            }
        }

        public static async Task<RemoteAssetBundleManifest> GetAssetBundleManifest(string url) {
            HttpResponseMessage response = await GetAssetBundleManifestAsync(url);
            string content = await response.Content.ReadAsStringAsync();
            return RemoteAssetBundleManifest.Deserialize(content);
        }

        // TODO Add version hash on server side
        public static async Task<AssetBundle> DownloadAssetBundleAsync(string url, string name) {
            string endpoint = string.Format("{0}/{1}", url, name);
            using (UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(endpoint)) {
                var task = Task.Run(() => req.SendWebRequest());
                var result = await task;
                return DownloadHandlerAssetBundle.GetContent(req);
            }
        }

        ///<summary>Simplified version of <see cref="UploadAssetBundleAsync" />
        ///<param name="url">The absolute URL to the POST endpoint</param>
        ///<param name="info">The AssetBundleInfo struct</param>
        ///<param name="message">A message to be used on the server side</param>
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>Returns the status code of the <see cref="HttpResponseMessage" /></returns>
        ///</summary>
        public static async Task<HttpStatusCode> UploadAssetBundle(string url, AssetBundleInfo info, string message) {
            HttpResponseMessage response = await UploadAssetBundleAsync(url, info, message);
            return response.StatusCode;
        }

    }
}

