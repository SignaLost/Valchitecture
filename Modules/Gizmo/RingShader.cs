using UnityEngine;
using Valchitecture.Common;

namespace Valchitecture.Modules.Gizmo;

/// <summary>Loads the ring shader from the embedded per-platform bundle.</summary>
internal static class RingShader
{
    private static Shader _shader;
    private static bool _resolved;

    internal static Shader Resolve()
    {
        if (_resolved)
        {
            return _shader;
        }

        _resolved = true;

        string resource = Application.platform switch
        {
            RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor => "Valchitecture.Resources.shaders_linux",
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => "Valchitecture.Resources.shaders_windows",
            _ => null
        };

        if (resource == null)
        {
            Log.LogError($"No gizmo ring shader was built for {Application.platform}; no rings will be drawn.");
            return null;
        }

        Load(resource);
        return _shader;
    }

    private static void Load(string resource)
    {
        try
        {
            byte[] data = EmbeddedResources.Read(resource);
            if (data == null)
            {
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromMemory(data);
            if (!bundle)
            {
                Log.LogError($"{resource} failed to load; it may have been built with a newer Unity version than the game uses.");
                return;
            }

            // Bundle assets are keyed by path, not shader name.
            foreach (Shader shader in bundle.LoadAllAssets<Shader>())
            {
                if (!shader)
                {
                    continue;
                }

                if (!shader.isSupported)
                {
                    Log.LogError($"{shader.name} has no usable variant for this machine's graphics API.");
                    continue;
                }

                _shader = shader;
            }

            if (!_shader)
            {
                Log.LogError($"{resource} holds no usable shader; no rings will be drawn.");
            }
        }
        catch (System.Exception e)
        {
            Log.LogError($"Failed to read {resource}: {e.Message}");
        }
    }
}
