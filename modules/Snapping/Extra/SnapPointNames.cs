
using Valchitecture.Common;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>Snap point names: the game's localized position words, plus this mod's own.</summary>
internal static class SnapPointNames
{
    public const string TOP = "$hud_snappoint_top";
    public const string BOTTOM = "$hud_snappoint_bottom";
    public const string CENTER = "$hud_snappoint_center";
    public const string CORNER = "$hud_snappoint_corner";
    public const string EDGE = "$hud_snappoint_edge";
    public const string INNER = "$hud_snappoint_inner";
    public const string SIDE = Tokens.SnapPointSide;
    public const string END = Tokens.SnapPointEnd;

    /// <summary>The tag the game gives snap point transforms.</summary>
    public const string TAG = "snappoint";

    /// <summary>Default name of an unnamed snap point; Unity duplicates add " (2)" and so on.</summary>
    public const string DEFAULT_NAME = "_snappoint";
}
