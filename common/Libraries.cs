using System;
using Chestworks;
using Mimir;
using BepInEx.Bootstrap;
using BepInEx.Logging;

namespace Valchitecture.Common;

/// <summary>
///     The versions of Chestworks and Mimir this build works with. A release of either that adds to or changes its
///     API, a 0.1.0 or 1.0.0 step, gets a Valchitecture release built against it, which raises these. BepInEx holds
///     the floor; this holds the ceiling: a library one major version up may have changed what Valchitecture calls,
///     so Valchitecture then stays out of the way with a clear message rather than misbehave.
/// </summary>
internal static class Libraries
{
    internal const string ChestworksAtLeast = "1.1.1";
    internal const string MimirAtLeast = "1.1.1";

    /// <summary>Whether both libraries are within the versions this build works with; the problem is logged when not.</summary>
    internal static bool Check(ManualLogSource logger)
    {
        return Within(logger, "Chestworks", ChestworksAPI.Guid, ChestworksAtLeast) && Within(logger, "Mimir", MimirAPI.Guid, MimirAtLeast);
    }

    private static bool Within(ManualLogSource logger, string name, string guid, string atLeast)
    {
        if (!Chainloader.PluginInfos.TryGetValue(guid, out BepInEx.PluginInfo info))
        {
            logger.LogError($"{name} is not installed; Valchitecture needs {name} {atLeast} or a later {System.Version.Parse(atLeast).Major}.x release.");
            return false;
        }

        System.Version found = info.Metadata.Version;
        System.Version least = System.Version.Parse(atLeast);

        if (found.Major != least.Major || found < least)
        {
            logger.LogError($"Valchitecture {ValchitecturePlugin.PluginVersion} works with {name} {atLeast} up to the last {least.Major}.x release, but {name} {found} is installed. "
                            + $"Update {(found.Major > least.Major ? "Valchitecture" : name)}. Valchitecture is staying off until then.");
            return false;
        }

        return true;
    }
}
