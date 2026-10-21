// Unity editor script run by build-shader-bundles.sh, not part of the mod's build.
// One bundle per platform: a bundle only carries shader variants for the graphics
// APIs of the platform it was built for.

using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildGizmoShaderBundle
{
    private const string BundleName = "valchitecture_shaders";
    private const string ShaderFolder = "Assets/Shaders";

    private static readonly BuildTarget[] Targets =
    {
        BuildTarget.StandaloneWindows64,
        BuildTarget.StandaloneLinux64
    };

    [MenuItem("Valchitecture/Build Shader Bundles")]
    public static void Build()
    {
        string[] shaders = Directory.GetFiles(ShaderFolder, "*.shader");

        if (shaders.Length == 0)
        {
            Debug.LogError($"No .shader files in {ShaderFolder}.");
            return;
        }

        AssetBundleBuild build = new() { assetBundleName = BundleName, assetNames = shaders };
        string root = Path.Combine(Directory.GetCurrentDirectory(), "ShaderBundles");

        foreach (BuildTarget target in Targets)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                Debug.LogError($"{target}: this editor has no Build Support module for it (Data/PlaybackEngines).");
                continue;
            }

            string directory = Path.Combine(root, target.ToString());
            Directory.CreateDirectory(directory);
            string produced = Path.Combine(directory, BundleName);

            try
            {
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(directory, new[] { build }, BuildAssetBundleOptions.None, target);
                Debug.Log(manifest != null && File.Exists(produced) ? $"Built {produced}" : $"{target}: BuildAssetBundles produced nothing; see the errors above.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{target}: {e}");
            }
        }
    }
}
