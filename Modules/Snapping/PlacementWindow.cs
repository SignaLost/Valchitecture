using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Modules.Modes;
using Valchitecture.Common;
using Valchitecture.Modules.Snapping.Extra;

namespace Valchitecture.Modules.Snapping;
/// <summary>True only while the game is positioning its placement ghost.</summary>
internal static class PlacementWindow
{
    internal static bool GhostActive;
}
