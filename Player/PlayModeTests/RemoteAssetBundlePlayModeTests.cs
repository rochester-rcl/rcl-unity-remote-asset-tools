using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RemoteAssetBundleTools;
namespace RemoteAssetBundleToolsTests
{
    public class RemoteAssetBundlePlayModeTests : AssetBundleTests
    {
        [UnityTest]
        public IEnumerator VerifyAsyncDownloadRemoteAssetBundle()
        {
            yield return new MonoBehaviourTest<RemoteAssetBundleMonobehaviourTest>();
        }
    }

    public class RemoteAssetBundleMonobehaviourTest : MonoBehaviour, IMonoBehaviourTest
    {
        public bool finished = false;
        public bool IsTestFinished { get { return finished; } }

        public void Start()
        {
            IEnumerator coroutine = TestDownloadRemoteAssetBundle((string error, AssetBundle bundle) =>
            {
                // try to read from the bundle 
                Assert.IsTrue(bundle.Contains(TestConstants.SAMPLE_PREFAB));
                GameObject go = bundle.LoadAsset<GameObject>(TestConstants.SAMPLE_PREFAB);
                Assert.IsNotNull(go.GetInstanceID());
                AssetBundle.UnloadAllAssetBundles(false);
            });
            StartCoroutine(coroutine);
        }

        public IEnumerator TestDownloadRemoteAssetBundle(System.Action<string, AssetBundle> callback)
        {
            Debug.Log("Testing RemoteAssetBundleUtils.DownloadAssetBundle");
            AssetBundleInfo info = new AssetBundleInfo(TestConstants.TEST_BUNDLE_NAME, TestConstants.TEST_BUNDLE_PATH);
            FCMMessage message = new FCMMessage("Test Upload", "This is a test", null);
            Task<RemoteAssetBundle> task = RemoteAssetBundleUtils.UploadAssetBundle(TestConstants.TEST_SERVER_URL, info, message, null, TestConstants.JWT_TOKEN_NAME);
            while (!task.IsCompleted)
            {
                yield return new WaitForFixedUpdate();
            }
            RemoteAssetBundle bundle = task.Result;
            Assert.AreEqual(bundle.toHash128().isValid, true);
            yield return StartCoroutine(RemoteAssetBundleUtils.DownloadAssetBundleAsync(TestConstants.TEST_SERVER_URL, bundle, callback));

            // Try to download again and check the cache
            System.Action<string, AssetBundle> cb = TestDownloadCachedAsset;
            yield return StartCoroutine(RemoteAssetBundleUtils.DownloadAssetBundleAsync(TestConstants.TEST_SERVER_URL, bundle, cb));

            // Now try to delete it 
            Task<HttpStatusCode> t = RemoteAssetBundleUtils.DeleteAssetBundle(TestConstants.TEST_SERVER_URL, bundle);
            while (!t.IsCompleted)
            {
                yield return new WaitForFixedUpdate();
            }
            HttpStatusCode status = t.Result;
            Assert.AreEqual(status, HttpStatusCode.OK);
            Debug.Log("Passed");
            finished = true;
        }

        public void TestDownloadCachedAsset(string error, AssetBundle bundle)
        {
            List<Hash128> cachedVersions = new List<Hash128>();
            Caching.GetCachedVersions(bundle.name, cachedVersions);
            Assert.IsTrue(bundle.Contains(TestConstants.SAMPLE_PREFAB));
            Assert.AreEqual(cachedVersions.Count, 1);
            AssetBundle.UnloadAllAssetBundles(false);
            bool cleared = Caching.defaultCache.ClearCache();
        }

    }
}
