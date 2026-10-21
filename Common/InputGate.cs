using UnityEngine;

namespace Valchitecture.Common;

/// <summary>Whether the local player may take input, by the game's own <c>Player.TakeInput</c>.</summary>
internal static class InputGate
{
    private static int _frame = -1;
    private static bool _capturing;

    /// <summary>True when a game window has focus, or the player is dead, teleporting or in a cutscene; cached per frame.</summary>
    internal static bool UiIsCapturingInput()
    {
        if (_frame == Time.frameCount)
        {
            return _capturing;
        }

        Player player = Player.m_localPlayer;
        _capturing = !player || !player.TakeInput();
        _frame = Time.frameCount;
        return _capturing;
    }
}
