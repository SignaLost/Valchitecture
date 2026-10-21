using UnityEngine;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>A point's role, for the labeler where position alone is ambiguous.</summary>
internal enum SnapRole
{
    None,

    /// <summary>The middle of a face or grid cell.</summary>
    Center,

    /// <summary>On the upward face of a thin piece.</summary>
    Top,

    /// <summary>On the downward face of a thin piece.</summary>
    Bottom,

    /// <summary>On a face of a thin piece that is neither up nor down.</summary>
    Side,

    /// <summary>The end of a thin piece lying on its side.</summary>
    End
}

/// <summary>A snap point to add: position and role.</summary>
internal sealed class SnapPointSpec
{
    public Vector3 LocalPosition { get; }
    public SnapRole Role { get; }

    public SnapPointSpec(Vector3 localPosition, SnapRole role = SnapRole.None)
    {
        LocalPosition = localPosition;
        Role = role;
    }

    public SnapPointSpec(float x, float y, float z, SnapRole role = SnapRole.None)
        : this(new Vector3(x, y, z), role)
    {
    }

    public override string ToString()
    {
        return $"{LocalPosition} [{Role}]";
    }
}
