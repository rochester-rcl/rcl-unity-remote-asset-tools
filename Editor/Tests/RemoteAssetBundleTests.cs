using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    }

    public class RemoteAssetBundleUtilsTests {

        [SetUp]
        public void Setup() {
            
        }

        [TearDown]
        public static void TearDown() {

        }
    }

    public class AssetBundleInfoTests {

        [SetUp]
        public void Setup() {
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

        [Test]
        public static void VerifyAssetBundleInfo() {
            AssetBundleInfo info = new AssetBundleInfo(TestConstants.TEST_BUNDLE_NAME, TestConstants.TEST_BUNDLE_PATH);
            Assert.IsTrue(info.Exists());
            // Attempt to load the AssetBundle
            AssetBundle bundle = info.Load();
            GameObject cube = bundle.LoadAsset<GameObject>(TestConstants.SAMPLE_PREFAB);
            Assert.AreNotEqual(cube, null);

            // Test async loading
            bundle = info.LoadAsync().GetAwaiter().GetResult();
            cube = bundle.LoadAsset<GameObject>(TestConstants.SAMPLE_PREFAB);
            Assert.AreNotEqual(cube, null);
        }

        [TearDown]
        public static void TearDown() {
            Directory.Delete(TestConstants.TEST_BUNDLE_DIR, true);
        }
    }
}
