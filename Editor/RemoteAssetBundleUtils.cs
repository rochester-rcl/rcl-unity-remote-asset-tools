namespace RemoteAssetBundleTools
{

    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Net.Http;
    using System.Net;
    using System.Threading.Tasks;

    public static class RemoteAssetBundleUtils
    {

        ///<summary>Searches a directory for a given AssetBundle
        ///<param name="name">The name of the AssetBundle</param>
        ///<param name="assetBundleDir">The absolute path to the directory AssetBundles are built to</param>
        ///<exception cref="DirectoryNotFoundException">The AssetBundle directory is not found</exception>
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>Returns an AssetBundleInfo struct</returns>
        ///</summary>
        public static AssetBundleInfo GetAssetBundleInfo(string name, string assetBundleDir)
        {
            if (Directory.Exists(assetBundleDir))
            {
                string assetPath = Path.Combine(assetBundleDir, name);
                if (File.Exists(assetPath))
                {
                    return new AssetBundleInfo(name, assetPath);
                }
                else
                {
                    throw new FileNotFoundException(string.Format("AssetBundle {0} does not exist in directory {1}", name, assetBundleDir));
                }
            }
            else
            {
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
        public static Task<HttpResponseMessage> UploadAssetBundleAsync(string url, AssetBundleInfo info, string message)
        {
            using (HttpClient client = new HttpClient())
            {
                if (info.Exists())
                {
                    MultipartFormDataContent formData = new MultipartFormDataContent();
                    FileInfo f = new FileInfo(info.Path);
                    StreamContent fs = new StreamContent(f.OpenRead());
                    formData.Add(fs, "bundle", f.Name);
                    formData.Add(new StringContent(message), "message");
                    return client.PostAsync(url, formData);
                }
                else
                {
                    throw new FileNotFoundException(string.Format("AssetBundle {0} does not exist in directory {1}", info.Name, info.Path));
                }
            }
        }

        ///<summary>Deletes an AssetBundle from a RESTful service
        ///<param name="url">The absolute URL to the POST endpoint</param>
        ///<param name="bundle">The RemoteAssetBundle struct</param>
        ///<returns>Returns a Task so can be used with await or ContinueWith</returns>
        ///</summary>
        public static Task<HttpResponseMessage> DeleteAssetBundleAsync(string url, RemoteAssetBundle bundle)
        {
            using (HttpClient client = new HttpClient())
            {
                string endpoint = string.Format("{0}?name={1}&versionhash={2}", url, bundle.Info.Name, bundle.VersionHash);
                return client.DeleteAsync(endpoint);
            }
        }

        ///<summary>Retrieves a list of RemoteAssetBundles from a RESTful service
        ///<param name="url">The absolute URL to the GET endpoint</param>
        ///<returns>Returns a Task so can be used with await or ContinueWith</returns>
        ///</summary>
        public static Task<HttpResponseMessage> GetAssetBundleManifestAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                return client.GetAsync(url);
            }
        }

        ///<summary>Convenience method that wraps <see cref="GetAssetBundleManifestAsync"/>
        ///<param name="url">The absolute URL to the GET endpoint</param>
        ///<returns>Returns a Task which returns deserialized AssetBundleManifest struct</returns>
        ///</summary>
        public static async Task<RemoteAssetBundleManifest> GetAssetBundleManifest(string url)
        {
            HttpResponseMessage response = await GetAssetBundleManifestAsync(url);
            string content = await response.Content.ReadAsStringAsync();
            return RemoteAssetBundleManifest.Deserialize(content);
        }

        ///<summary>Retrieves an AssetBundle from a remote server. Wraps <see cref="UnityWebRequestAssetBundle.GetAssetBundle"/> and uses the default AssetBundle cache
        ///<param name="url">The absolute URL to the GET endpoint</param>
        ///<param name="bundle">The RemoteAssetBundle struct</param>
        ///<param name="callback">An Action with signature (string, AssetBundle) Note - string will be null if there's no error</param>
        ///<returns>Returns an enumerator. The AssetBundle is available via the callback</returns>
        ///</summary>
        public static IEnumerator DownloadAssetBundleAsync(string url, RemoteAssetBundle bundle, System.Action<string, AssetBundle> callback)
        {
            string endpoint = string.Format("{0}/{1}?versionhash={2}", url, bundle.Info.Name, bundle.VersionHash);
            CachedAssetBundle cachedBundle = new CachedAssetBundle();
            cachedBundle.hash = bundle.toHash128();
            cachedBundle.name = bundle.Info.Name;
            using (UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(endpoint, cachedBundle, 0))
            {
                yield return req.SendWebRequest();
                if (req.isNetworkError || req.isHttpError)
                {
                    // TODO add some sort of error handling here - should pass error as first param of callback
                    callback(req.error, null);
                }
                callback(null, DownloadHandlerAssetBundle.GetContent(req));
            }
        }

        ///<summary>Simplified version of <see cref="UploadAssetBundleAsync" />
        ///<param name="url">The absolute URL to the POST endpoint</param>
        ///<param name="info">The AssetBundleInfo struct</param>
        ///<param name="message">A message to be used on the server side</param>
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>Returns a Task which returns a deserialized RemoteAssetBundle</returns>
        ///</summary>
        public static async Task<RemoteAssetBundle> UploadAssetBundle(string url, AssetBundleInfo info, string message)
        {
            HttpResponseMessage response = await UploadAssetBundleAsync(url, info, message);
            string content = await response.Content.ReadAsStringAsync();
            return RemoteAssetBundle.Deserialize(content);
        }

        ///<summary>Simplified version of <see cref="DeleteAssetBundleAsync" />
        ///<param name="url">The absolute URL to the POST endpoint</param>
        ///<param name="bundle">The RemoteAssetBundle struct</param>
        ///<returns>Returns the status code of the <see cref="HttpResponseMessage" /></returns>
        ///</summary>
        public static async Task<HttpStatusCode> DeleteAssetBundle(string url, RemoteAssetBundle bundle)
        {
            HttpResponseMessage response = await DeleteAssetBundleAsync(url, bundle);
            return response.StatusCode;
        }

    }
}

