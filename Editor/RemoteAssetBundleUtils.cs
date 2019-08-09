namespace RemoteAssetBundleTools
{

    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.Networking;
    using System.Net.Http;
    using System.Net;
    using System.Threading.Tasks;

    public static class RemoteAssetBundleUtils
    {
#if UNITY_EDITOR
        public static async Task<bool> CheckEndpoint(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage res = await client.GetAsync(url);
                return res.IsSuccessStatusCode;
            }

        }

        public static async Task<bool> CheckJWT(string url, string jwtName)
        {
            using (HttpClient client = new HttpClient())
            {
                string jwt = FindJWT(jwtName);
                if (string.IsNullOrEmpty(jwt)) throw new FileNotFoundException(string.Format("Could not find JWT file with name {0}", jwtName));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
                HttpResponseMessage res = await client.PostAsync(url, null);
                return res.IsSuccessStatusCode;
            }

        }
        ///<summary>Finds the JSON Web Token required to POST AssetBundles to the server
        ///<param name="name">The name of the JWT file</param>
        ///<returns>Returns the JWT as a string</returns>
        ///</summary>
        public static string FindJWT(string name)
        {

            string[] assets = AssetDatabase.FindAssets(name);
            if (assets.Length == 0)
            {
                Debug.LogWarning(string.Format("No JWT files found with name {0}", name));
                return null;
            }
            if (assets.Length > 1)
            {
                Debug.LogWarning(string.Format("{0} JWT files found with name {1}. Using the first one.", assets.Length, name));
            }
            return System.IO.File.ReadAllText(AssetDatabase.GUIDToAssetPath(assets[0]), System.Text.Encoding.UTF8);
        }

        ///<summary>Uploads an AssetBundle to RESTful service
        ///<param name="url">The absolute URL to the POST endpoint</param>
        ///<param name="info">The AssetBundleInfo struct</param>
        ///<param name="message">A message to be used on the server side</param>
        ///<param name="jwtName">Optional name of a JSON Web Token (placed somewhere in Assets) that can be used for authentication</param>
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>Returns a Task so can be used with await or ContinueWith</returns>
        ///</summary>
        public static Task<HttpResponseMessage> UploadAssetBundleAsync(string url, AssetBundleInfo info, string appName = null, string message = null, string jwtName = null)
        {
            bool useJWT = !string.IsNullOrEmpty(jwtName);
            using (HttpClient client = new HttpClient())
            {
                if (info.Exists())
                {
                    if (useJWT)
                    {
                        string jwt = FindJWT(jwtName);
                        if (string.IsNullOrEmpty(jwt)) throw new FileNotFoundException(string.Format("Could not find JWT file with name {0}", jwtName));
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
                    }
                    MultipartFormDataContent formData = new MultipartFormDataContent();
                    FileInfo f = new FileInfo(info.Path);
                    StreamContent fs = new StreamContent(f.OpenRead());
                    formData.Add(fs, "bundle", f.Name);
                    string app = !string.IsNullOrEmpty(appName) ? appName : Application.productName;
                    formData.Add(new StringContent(app), "AppName");
                    if (!string.IsNullOrEmpty(message))
                    {
                        formData.Add(new StringContent(message), "message");
                    }
                    return client.PostAsync(url, formData);
                }
                else
                {
                    throw new FileNotFoundException(string.Format("AssetBundle {0} does not exist in directory {1}", info.Name, info.Path));
                }
            }
        }

        ///<summary>Sets a RemoteAssetBundle's Verified property to true so it will be loaded on manifests that require all bundles to be verified.
        ///<para>This is enforced by default as an extra set of checks and balances when calling <see cref="GetAssetBundleManifestAsync"/> with verified = true</para> 
        ///<param name="url">The absolute URL to the PUT endpoint</param>
        ///<param name="bundle">The RemoteAssetBundle struct</param>
        ///<param name="jwtName">Optional name of a JSON Web Token (placed somewhere in Assets) that can be used for authentication</param>
        ///<returns>Returns a Task so can be used with await or ContinueWith</returns>
        ///</summary>
        public static Task<HttpResponseMessage> VerifyAssetBundleAsync(string url, RemoteAssetBundle bundle, string jwtName = null)
        {
            bool useJWT = !string.IsNullOrEmpty(jwtName);
            using (HttpClient client = new HttpClient())
            {
                if (useJWT)
                {
                    string jwt = FindJWT(jwtName);
                    if (string.IsNullOrEmpty(jwt)) throw new FileNotFoundException(string.Format("Could not find JWT file with name {0}", jwtName));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
                }
                StringContent content = new StringContent("{\"verified\":true}", Encoding.UTF8, "application/json");
                string endpoint = string.Format("{0}/{1}?versionhash={2}", url, WebUtility.UrlEncode(bundle.Info.Name), WebUtility.UrlEncode(bundle.VersionHash));
                return client.PutAsync(endpoint, content);
            }
        }

        ///<summary>Simplified version of <see cref="VerifyAssetBundleAsync"/>
        ///<para>This is enforced by default as an extra set of checks and balances when calling <see cref="GetAssetBundleManifestAsync"/> with verified = true</para> 
        ///<param name="url">The absolute URL to the PUT endpoint</param>
        ///<param name="bundle">The RemoteAssetBundle struct</param>
        ///<param name="jwtName">Optional name of a JSON Web Token (placed somewhere in Assets) that can be used for authentication</param>
        ///<returns>Returns a Task which returns a deserialized RemoteAssetBundle</returns>
        ///</summary>
        public static async Task<RemoteAssetBundle> VerifyAssetBundle(string url, RemoteAssetBundle bundle, string jwtName = null)
        {
            HttpResponseMessage response = await VerifyAssetBundleAsync(url, bundle, jwtName);
            string content = await response.Content.ReadAsStringAsync();
            return RemoteAssetBundle.Deserialize(content);
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
                string endpoint = string.Format("{0}?name={1}&versionhash={2}", url, WebUtility.UrlEncode(bundle.Info.Name), WebUtility.UrlEncode(bundle.VersionHash));
                return client.DeleteAsync(endpoint);
            }
        }

        ///<summary>Simplified version of <see cref="UploadAssetBundleAsync" />
        ///<param name="url">The absolute URL to the POST endpoint</param>
        ///<param name="info">The AssetBundleInfo struct</param>
        ///<param name="message">A message to be used on the server side</param>
        ///<param name="jwtName">Optional name of a JSON Web Token (placed somewhere in Assets) that can be used for authentication</param>
        ///<exception cref="FileNotFoundException">The AssetBundle is not found</exception>
        ///<returns>Returns a Task which returns a deserialized RemoteAssetBundle</returns>
        ///</summary>
        public static async Task<RemoteAssetBundle> UploadAssetBundle(string url, AssetBundleInfo info, string appName = null, string message = null, string jwtName = null)
        {
            HttpResponseMessage response = await UploadAssetBundleAsync(url, info, appName, message, jwtName);
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
#endif
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
        ///<summary>Retrieves a list of RemoteAssetBundles from a RESTful service
        ///<param name="url">The absolute URL to the GET endpoint</param>
        ///<param name="appName">Optional parameter to filter the manifest by product name</param>
        ///<param name="verified">Optional parameter to filter the manifest by verified (i.e) production bundles only.This is on by default</param>
        ///<returns>Returns a Task so can be used with await or ContinueWith</returns>
        ///</summary>
        public static Task<HttpResponseMessage> GetAssetBundleManifestAsync(string url, string appName = null, bool verified = true)
        {
            // TODO need to find a safer way to build these URIs

            string endpoint = string.Format("{0}?verified={1}", url, WebUtility.UrlEncode(verified.ToString()));
            if (!string.IsNullOrEmpty(appName)) endpoint += string.Format("&appname={0}", WebUtility.UrlEncode(appName));
            using (HttpClient client = new HttpClient())
            {
                return client.GetAsync(endpoint);
            }
        }

        ///<summary>Convenience method that wraps <see cref="GetAssetBundleManifestAsync"/>
        ///<param name="url">The absolute URL to the GET endpoint</param>
        ///<param name="appName">Optional parameter to filter the manifest by product name</param>
        ///<returns>Returns a Task which returns deserialized AssetBundleManifest struct</returns>
        ///</summary>
        public static async Task<RemoteAssetBundleManifest> GetAssetBundleManifest(string url, string appName = null, bool verified = true)
        {
            HttpResponseMessage response = await GetAssetBundleManifestAsync(url, appName, verified);
            string content = await response.Content.ReadAsStringAsync();
            Debug.Log(content);
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
    }
}

