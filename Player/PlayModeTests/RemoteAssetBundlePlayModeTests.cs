using System.Collections;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RemoteAssetBundleTools;
using UnityEditor;
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
            IEnumerator coroutine = TestDownloadRemoteAssetBundle((string error, AssetBundle bundle) => {
                finished = true;
                Debug.Log(bundle);
            });
            StartCoroutine(coroutine);
        }
        public IEnumerator TestDownloadRemoteAssetBundle(System.Action<string, AssetBundle> callback)
        {
            AssetBundleInfo info = new AssetBundleInfo(TestConstants.TEST_BUNDLE_NAME, TestConstants.TEST_BUNDLE_PATH);
            Task<RemoteAssetBundle> task = RemoteAssetBundleUtils.UploadAssetBundle(TestConstants.TEST_SERVER_URL, info, "This is a test");
            while (!task.IsCompleted)
            {
                yield return new WaitForFixedUpdate();
            }
            RemoteAssetBundle bundle = task.Result;
            yield return StartCoroutine(RemoteAssetBundleUtils.DownloadAssetBundleAsync(TestConstants.TEST_SERVER_URL, bundle, callback));
            // TODO Add another callback for cleaning up  
        }

    }
}
