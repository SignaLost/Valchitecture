using System.Collections.Generic;
using UnityEngine;
using Valchitecture.Common;

namespace Valchitecture.Modules;

/// <summary>Loads the mod's shaders, by name, from the embedded per-platform bundle.</summary>
internal static class ModShaders
{
    private const string RingName = "Valchitecture/GizmoRing";
    private const string SnapPointName = "Valchitecture/SnapPoint";

    private static readonly Dictionary<string, Shader> _shaders = new();
    private static bool _loaded;

    /// <summary>The gizmo ring shader, or null when the bundle has none usable.</summary>
    internal static Shader Ring => Find(RingName);

    /// <summary>The snap point shader, or null when the bundle has none usable, such as one built before it existed.</summary>
    internal static Shader SnapPoint => Find(SnapPointName);

    private static Shader Find(string name)
    {
        if (!_loaded)
        {
            _loaded = true;
            Load();
        }

        return _shaders.TryGetValue(name, out Shader shader) ? shader : null;
    }

    private static void Load()
    {
        string resource = Application.platform switch
        {
            RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor => "Valchitecture.Resources.shaders_linux",
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => "Valchitecture.Resources.shaders_windows",
            _ => null
        };

        if (resource == null)
        {
            Log.LogError($"No shaders were built for {Application.platform}; the gizmo rings and snap points will not be drawn.");
            return;
        }

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

                _shaders[shader.name] = shader;
            }

            if (!_shaders.ContainsKey(RingName))
            {
                Log.LogError($"{resource} holds no usable gizmo ring shader; no rings will be drawn.");
            }
        }
        catch (System.Exception e)
        {
            Log.LogError($"Failed to read {resource}: {e.Message}");
        }
    }
}
