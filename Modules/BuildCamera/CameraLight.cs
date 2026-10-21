using System.Collections.Generic;
using UnityEngine;

namespace Valchitecture.Modules.BuildCamera;

/// <summary>Moves helmet lights to the detached camera by copying them there and emptying the original's culling mask.</summary>
internal static class CameraLight
{
    private const float EquipmentInterval = 0.25f;
    private const float SettingsInterval = 0.1f;

    private sealed class Borrowed
    {
        internal Light Source;
        internal Light Copy;
        internal GameObject Holder;

        /// <summary>The source's original culling mask.</summary>
        internal int Mask;
    }

    private static readonly Dictionary<int, Borrowed> _borrowed = new();
    private static readonly List<Light> _found = new();
    private static readonly HashSet<int> _worn = new();
    private static readonly List<int> _dropped = new();

    private static float _nextEquipment;
    private static float _nextSettings;

    internal static void Update(Transform camera)
    {
        Player player = Player.m_localPlayer;

        if (!camera || !player)
        {
            Release();
            return;
        }

        float now = Time.time;

        if (now >= _nextEquipment)
        {
            _nextEquipment = now + EquipmentInterval;
            Borrow(player);
        }

        if (_borrowed.Count == 0)
        {
            return;
        }

        bool resync = now >= _nextSettings;

        if (resync)
        {
            _nextSettings = now + SettingsInterval;
        }

        foreach (Borrowed borrowed in _borrowed.Values)
        {
            if (!borrowed.Source)
            {
                _nextEquipment = 0f;
                continue;
            }

            if (resync)
            {
                Match(borrowed);
            }

            // Rebuilt equipment visuals restore the mask.
            if (borrowed.Source.cullingMask != 0)
            {
                borrowed.Mask = borrowed.Source.cullingMask;
                borrowed.Source.cullingMask = 0;
            }

            borrowed.Holder.transform.SetPositionAndRotation(camera.position, camera.rotation);
            borrowed.Copy.enabled = borrowed.Source.enabled;

            bool lit = borrowed.Source.gameObject.activeInHierarchy;

            if (borrowed.Holder.activeSelf != lit)
            {
                borrowed.Holder.SetActive(lit);
            }
        }
    }

    internal static void Release()
    {
        foreach (Borrowed borrowed in _borrowed.Values)
        {
            Return(borrowed);
        }

        _borrowed.Clear();
        _worn.Clear();
        _found.Clear();
        _dropped.Clear();
        _nextEquipment = 0f;
        _nextSettings = 0f;
    }

    private static void Borrow(Player player)
    {
        _worn.Clear();

        VisEquipment visuals = player.m_visEquipment;
        GameObject helmet = visuals ? visuals.m_helmetItemInstance : null;

        if (helmet)
        {
            _found.Clear();
            helmet.GetComponentsInChildren(true, _found);

            foreach (Light source in _found)
            {
                if (!source)
                {
                    continue;
                }

                int id = source.GetInstanceID();

                if (_worn.Add(id) && !_borrowed.ContainsKey(id))
                {
                    _borrowed.Add(id, Copy(source));
                }
            }
        }

        _dropped.Clear();

        foreach (KeyValuePair<int, Borrowed> pair in _borrowed)
        {
            if (!pair.Value.Source || !_worn.Contains(pair.Key))
            {
                _dropped.Add(pair.Key);
            }
        }

        foreach (int id in _dropped)
        {
            Return(_borrowed[id]);
            _borrowed.Remove(id);
        }
    }

    private static Borrowed Copy(Light source)
    {
        GameObject holder = new("ValchitectureCameraLight_" + source.name) { hideFlags = HideFlags.HideInHierarchy };

        Borrowed borrowed = new()
        {
            Source = source,
            Holder = holder,
            Copy = holder.AddComponent<Light>(),
            Mask = source.cullingMask
        };

        Match(borrowed);
        source.cullingMask = 0;
        return borrowed;
    }

    /// <summary>Copies the light's visual settings.</summary>
    private static void Match(Borrowed borrowed)
    {
        Light source = borrowed.Source;
        Light copy = borrowed.Copy;

        copy.type = source.type;
        copy.color = source.color;
        copy.colorTemperature = source.colorTemperature;
        copy.useColorTemperature = source.useColorTemperature;
        copy.intensity = source.intensity;
        copy.range = source.range;
        copy.spotAngle = source.spotAngle;
        copy.innerSpotAngle = source.innerSpotAngle;
        copy.cookie = source.cookie;
        copy.cookieSize = source.cookieSize;
        copy.shadows = source.shadows;
        copy.shadowStrength = source.shadowStrength;
        copy.shadowResolution = source.shadowResolution;
        copy.renderMode = source.renderMode;
        copy.bounceIntensity = source.bounceIntensity;
        copy.cullingMask = borrowed.Mask;
    }

    private static void Return(Borrowed borrowed)
    {
        if (borrowed.Source)
        {
            borrowed.Source.cullingMask = borrowed.Mask;
        }

        if (borrowed.Holder)
        {
            Object.Destroy(borrowed.Holder);
        }
    }
}
