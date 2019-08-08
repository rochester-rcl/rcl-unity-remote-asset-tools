using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using RemoteAssetBundleTools;

namespace RemoteAssetBundleToolsTests
{

    public static class TestConstants
    {

        public const string SAMPLE_PREFAB = "Assets/RemoteAssetBundleTools/Editor/Tests/TestAsset.prefab";
        public const string TEST_BUNDLE_NAME = "abtest.unity3d";
        public const string TEST_BUNDLE_DIR = "Assets/RemoteAssetBundleTools/Editor/Tests/TestBundles";
        public static string TEST_BUNDLE_PATH = string.Format("{0}/{1}", TEST_BUNDLE_DIR, TEST_BUNDLE_NAME);
        public const string TEST_SERVER_URL = "http://localhost:3000/bundles";
        public const string TEST_SERVER_ENDPOINT_CHECK = "http://localhost:3000/";
        public const string JWT_TOKEN_NAME = "remote-asset-token";

    }

    public abstract class AssetBundleTests : IPrebuildSetup
    {

        public void Setup()
        {
            // Build an AssetBundle
            AssetBundleBuild[] bundles = new AssetBundleBuild[1];
            bundles[0] = new AssetBundleBuild()
            {
                assetBundleName = TestConstants.TEST_BUNDLE_NAME,
                assetNames = new string[] {
                    TestConstants.SAMPLE_PREFAB
                }
            };
            Directory.CreateDirectory(TestConstants.TEST_BUNDLE_DIR);
            BuildPipeline.BuildAssetBundles(TestConstants.TEST_BUNDLE_DIR, bundles, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
        }

        public void TearDown()
        {
            Directory.Delete(TestConstants.TEST_BUNDLE_DIR, true);
        }

    }

    public class RemoteAssetBundleUtilsTests : AssetBundleTests
    {
        RemoteAssetBundle bundle;

        [UnityTest, Order(1)]
        public static IEnumerator VerifyEndpoint()
        {
            Debug.Log("Testing RemoteAssetBundleUtils.CheckEndpoint");
            Task<bool> task = RemoteAssetBundleUtils.CheckEndpoint(TestConstants.TEST_SERVER_ENDPOINT_CHECK);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            bool status = task.Result;
            Assert.IsTrue(status);

            // Now check JWT authentication
            Task<bool> t = RemoteAssetBundleUtils.CheckJWT(TestConstants.TEST_SERVER_ENDPOINT_CHECK, TestConstants.JWT_TOKEN_NAME);
            while (!t.IsCompleted)
            {
                yield return null;
            }
            bool jwtStatus = t.Result;
            Assert.IsTrue(jwtStatus);
            Debug.Log("Passed");
        }

        [UnityTest, Order(2)]
        public static IEnumerator VerifyGetAssetBundleInfo()
        {
            Debug.Log("Testing RemoteAssetBundleUtils.GetAssetBundleInfo");
            AssetBundleInfo info = RemoteAssetBundleUtils.GetAssetBundleInfo(TestConstants.TEST_BUNDLE_NAME, TestConstants.TEST_BUNDLE_DIR);
            Assert.AreEqual(info.Exists(), true);
            Debug.Log("Passed");
            yield return null;
        }

        [UnityTest, Order(3)]
        public IEnumerator VerifyUploadAssetBundle()
        {
            Debug.Log("Testing RemoteAssetBundleUtils.UploadAssetBundle");
            AssetBundleInfo info = new AssetBundleInfo(TestConstants.TEST_BUNDLE_NAME, TestConstants.TEST_BUNDLE_PATH);
            Task<RemoteAssetBundle> task = RemoteAssetBundleUtils.UploadAssetBundle(TestConstants.TEST_SERVER_URL, info, "This is a test", TestConstants.JWT_TOKEN_NAME);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            bundle = task.Result;
            Assert.IsTrue(bundle.toHash128().isValid);
            Assert.AreEqual(Application.productName, bundle.AppName);
        }

        [UnityTest, Order(4)]
        public IEnumerator VerifyGetAssetBundleManifestWithUnverifiedBundle()
        {
            Debug.Log("Testing RemoteAssetBundleUtils.GetAssetBundleManifest");
            Task<RemoteAssetBundleManifest> task = RemoteAssetBundleUtils.GetAssetBundleManifest(TestConstants.TEST_SERVER_URL, Application.productName);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            RemoteAssetBundleManifest content = task.Result;
            // Should at the very least be an empty array
            Assert.AreNotEqual(content.Bundles, null);
            Assert.AreEqual(content.Bundles.Length, 0);
            Debug.Log("Passed");
        }


        [UnityTest, Order(5)]
        public IEnumerator VerifyUpdateAssetBundle()
        {
            Debug.Log("Testing RemoteAssetBundleUtils.VerifyAssetBundle");
            Task<RemoteAssetBundle> task = RemoteAssetBundleUtils.VerifyAssetBundle(TestConstants.TEST_SERVER_URL, bundle, TestConstants.JWT_TOKEN_NAME);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            bundle = task.Result;
            Assert.IsTrue(bundle.toHash128().isValid);
            Assert.IsTrue(bundle.Verified);
        }

        [UnityTest, Order(6)]
        public IEnumerator VerifyGetAssetBundleManifest()
        {
            Debug.Log("Testing RemoteAssetBundleUtils.GetAssetBundleManifest");
            Task<RemoteAssetBundleManifest> task = RemoteAssetBundleUtils.GetAssetBundleManifest(TestConstants.TEST_SERVER_URL, Application.productName);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            RemoteAssetBundleManifest content = task.Result;
            // Should at the very least be an empty array
            Assert.AreNotEqual(content.Bundles, null);
            Assert.AreEqual(content.Bundles.Length, 1);
            foreach (var _bundle in content.Bundles)
            {
                Assert.IsTrue(_bundle.toHash128().isValid);
                Assert.AreEqual(_bundle.AppName, Application.productName);
            }
            Debug.Log("Passed");
        }

        [UnityTest, Order(7)]
        public IEnumerator VerifyDeleteAssetBundle()
        {
            Debug.Log("Testing RemoteAssetBundleUtils.DeleteAssetBundle");
            Task<HttpStatusCode> task = RemoteAssetBundleUtils.DeleteAssetBundle(TestConstants.TEST_SERVER_URL, bundle);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            HttpStatusCode status = task.Result;
            Assert.AreEqual(status, HttpStatusCode.OK);
            Debug.Log("Passed");
        }
    }

    public class AssetBundleInfoTests : AssetBundleTests
    {

        [Test]
        public static void VerifyAssetBundleInfo()
        {
            Debug.Log("Testing AssetBundleInfo.Path");
            AssetBundleInfo info = new AssetBundleInfo(TestConstants.TEST_BUNDLE_NAME, TestConstants.TEST_BUNDLE_PATH);
            Assert.IsTrue(info.Exists());
            Debug.Log("Passed");
            // Attempt to load the AssetBundle
            Debug.Log("Testing AssetBundleInfo.Load");
            AssetBundle bundle = info.Load();
            GameObject cube = bundle.LoadAsset<GameObject>(TestConstants.SAMPLE_PREFAB);
            Assert.AreNotEqual(cube, null);
            Debug.Log("Passed");
        }
    }
}
