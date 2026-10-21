using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace Valchitecture.Modules.UI;

/// <summary>Key cap text for this mod's keybinds.</summary>
internal static class KeyText
{
    internal static string ForKey(ConfigEntry<KeyCode> entry)
    {
        return entry == null ? string.Empty : Pretty(entry.Value);
    }

    /// <summary>One cap per key of a shortcut, modifiers first.</summary>
    internal static string[] ForShortcut(ConfigEntry<KeyboardShortcut> entry)
    {
        if (entry == null)
        {
            return new[] { string.Empty };
        }

        KeyboardShortcut shortcut = entry.Value;

        if (shortcut.MainKey == KeyCode.None)
        {
            return new[] { "-" };
        }

        List<string> caps = new();

        foreach (KeyCode modifier in shortcut.Modifiers)
        {
            caps.Add(Pretty(modifier));
        }

        caps.Add(Pretty(shortcut.MainKey));
        return caps.ToArray();
    }

    /// <summary>The key's display name.</summary>
    internal static string Pretty(KeyCode key)
    {
        return key switch
        {
            KeyCode.Mouse0 => "Mouse-1",
            KeyCode.Mouse1 => "Mouse-2",
            KeyCode.Mouse2 => "Mouse-3",
            KeyCode.LeftControl => "Left Ctrl",
            KeyCode.RightControl => "Right Ctrl",
            KeyCode.LeftShift => "Left Shift",
            KeyCode.RightShift => "Right Shift",
            KeyCode.LeftAlt => "Left Alt",
            KeyCode.RightAlt => "Right Alt",
            KeyCode.LeftArrow => "←",
            KeyCode.RightArrow => "→",
            KeyCode.UpArrow => "↑",
            KeyCode.DownArrow => "↓",
            KeyCode.Equals => "+",
            KeyCode.KeypadPlus => "+",
            KeyCode.Minus => "-",
            KeyCode.KeypadMinus => "-",
            KeyCode.Space => "Space",
            KeyCode.None => "-",
            _ => key.ToString()
        };
    }
}
