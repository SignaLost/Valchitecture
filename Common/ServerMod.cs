using Jotunn.Utils;
using UnityEngine;

namespace Valchitecture.Common;

/// <summary>Whether the current server runs the mod; always true when hosting.</summary>
internal static class ServerMod
{
    private static ZNet _checkedWith;
    private static bool _present;
    private static int _frame = -1;

    /// <summary>Cached per connection and refreshed once per frame; a false result is rechecked each frame.</summary>
    internal static bool Present
    {
        get
        {
            if (_frame == Time.frameCount)
            {
                return _present;
            }

            _frame = Time.frameCount;
            ZNet net = ZNet.instance;

            if (_checkedWith != net)
            {
                _checkedWith = net;
                _present = false;
            }

            if (!_present && net)
            {
                _present = ModCompatibility.IsModuleOnServer(ValchitecturePlugin.PluginGuid);
            }

            return _present;
        }
    }
}
