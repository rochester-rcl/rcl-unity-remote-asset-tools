using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using RemoteAssetBundleTools;

namespace RemoteAssetBundleToolsTests {

    public static class TestConstants {

        public const string SAMPLE_PREFAB = "Assets/RemoteAssetBundleTools/Editor/Tests/TestAsset.prefab";
        public const string TEST_BUNDLE_NAME = "abtest.unity3d";
        public const string TEST_BUNDLE_DIR = "Assets/RemoteAssetBundleTools/Editor/Tests/TestBundles";
        public static string TEST_BUNDLE_PATH = string.Format("{0}/{1}", TEST_BUNDLE_DIR, TEST_BUNDLE_NAME);
        public const string TEST_SERVER_URL = "http://localhost:3000/bundles";

    }

    public abstract class AssetBundleTests {

        [SetUp]
        public static void Setup() {
            // Build an AssetBundle
            AssetBundleBuild[] bundles = new AssetBundleBuild[1];
            bundles[0] = new AssetBundleBuild() {
                assetBundleName = TestConstants.TEST_BUNDLE_NAME,
                assetNames = new string[] {
                    TestConstants.SAMPLE_PREFAB
                }
            };
            Directory.CreateDirectory(TestConstants.TEST_BUNDLE_DIR);
            BuildPipeline.BuildAssetBundles(TestConstants.TEST_BUNDLE_DIR, bundles, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
        }

        [TearDown]
        public static void TearDown() {
            Directory.Delete(TestConstants.TEST_BUNDLE_DIR, true);
        }

    }

    public class RemoteAssetBundleUtilsTests : AssetBundleTests {
        
        [Test]
        public static void VerifyGetAssetBundleInfo() {
            Debug.Log("Testing RemoteAssetBundleUtils.GetAssetBundleInfo");
            AssetBundleInfo info = RemoteAssetBundleUtils.GetAssetBundleInfo(TestConstants.TEST_BUNDLE_NAME, TestConstants.TEST_BUNDLE_DIR);
            Assert.AreEqual(info.Exists(), true);
            Debug.Log("Passed");
        }

        [UnityTest, Order(2)]
        public static IEnumerator VerifyGetAssetBundleManifest() {
            Debug.Log("Testing RemoteAssetBundleUtils.GetAssetBundleManifest");
            Task<RemoteAssetBundleManifest> task = RemoteAssetBundleUtils.GetAssetBundleManifest(TestConstants.TEST_SERVER_URL);
            while (!task.IsCompleted) {
                yield return null;
            }
            RemoteAssetBundleManifest content = task.Result;
            // Should at the very least be an empty array
            Assert.AreNotEqual(content.Bundles, null);
            Debug.Log(content.Bundles.Length);
            foreach(var bundle in content.Bundles) {
                Debug.Log(bundle.VersionHash);
                Debug.Log(bundle.Info.Path);
            }
            Debug.Log("Passed");           
        }

        [UnityTest, Order(1)]
        public static IEnumerator VerifyUploadAssetBundle() {
            Debug.Log("Testing RemoteAssetBundleUtils.UploadAssetBundle");
            AssetBundleInfo info = new AssetBundleInfo(TestConstants.TEST_BUNDLE_NAME, TestConstants.TEST_BUNDLE_PATH);
            Task<HttpStatusCode> task = RemoteAssetBundleUtils.UploadAssetBundle(TestConstants.TEST_SERVER_URL, info, "This is a test");
            while (!task.IsCompleted) {
                yield return null;
            }
            HttpStatusCode status = task.Result;
            // TODO status code should be 201 but for now we'll check for 200
            Assert.AreEqual(status, HttpStatusCode.OK);
            Debug.Log("Passed");
        }
    }

    public class AssetBundleInfoTests : AssetBundleTests {

        [Test]
        public static void VerifyAssetBundleInfo() {
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
            // Test async loading 
            // TODO this currently causes the application to hang.
            /* bundle = Task.Run(() => info.LoadAsync()).GetAwaiter().GetResult();
            cube = bundle.LoadAsset<GameObject>(TestConstants.SAMPLE_PREFAB);
            Assert.AreNotEqual(cube, null); */
        }

    }
}
