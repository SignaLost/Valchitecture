using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valchitecture.Modules.HintBar;

/// <summary>
///     One hint bar row, cloned from a game row: a localized label and a set of key caps, grown or hidden to the
///     number of keys. Text is written only on change, and a cap can be replaced by copies of the game's own icons.
/// </summary>
internal sealed class HintRow
{
    private const string KeyCapPrefix = "key_bkg";
    private const string KeyChildName = "Key";
    private const string LabelChildName = "Text";

    /// <summary>Last unlocalized text written to each element; the element's own text is localized.</summary>
    private static readonly Dictionary<TextMeshProUGUI, string> _written = new();

    private readonly GameObject _root;
    private readonly TextMeshProUGUI _label;
    private readonly LayoutElement _labelLayout;
    private readonly List<TextMeshProUGUI> _keyCaps = new();
    private readonly List<GameObject> _keyCapRoots = new();

    /// <summary>Additional objects of an adopted cap, shown and hidden with it.</summary>
    private readonly Dictionary<int, List<GameObject>> _adoptedExtras = new();

    private HintRow(GameObject root, TextMeshProUGUI label, LayoutElement labelLayout)
    {
        _root = root;
        _label = label;
        _labelLayout = labelLayout;
    }

    internal bool IsValid => _root;

    internal static HintRow Create(GameObject template, Transform parent, string name, int keyCount)
    {
        GameObject root = Object.Instantiate(template, parent, false);
        root.name = name;
        root.SetActive(false);

        Transform labelTransform = root.transform.Find(LabelChildName);
        TextMeshProUGUI label = labelTransform ? labelTransform.GetComponent<TextMeshProUGUI>() : null;
        LayoutElement labelLayout = null;

        if (label)
        {
            // Rows have a fixed height.
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;

            if (!labelTransform.TryGetComponent(out labelLayout))
            {
                labelLayout = labelTransform.gameObject.AddComponent<LayoutElement>();
            }
        }

        HintRow row = new(root, label, labelLayout);
        row.BuildKeyCaps(keyCount);
        return row;
    }

    private void BuildKeyCaps(int keyCount)
    {
        List<Transform> existing = new();

        foreach (Transform child in _root.transform)
        {
            if (child.name.StartsWith(KeyCapPrefix))
            {
                existing.Add(child);
            }
        }

        if (existing.Count == 0)
        {
            return;
        }

        Transform capTemplate = existing[0];

        while (existing.Count < keyCount)
        {
            GameObject clone = Object.Instantiate(capTemplate.gameObject, _root.transform, false);
            clone.name = $"{KeyCapPrefix} ({existing.Count})";
            existing.Add(clone.transform);
        }

        for (int i = 0; i < existing.Count; i++)
        {
            bool used = i < keyCount;
            existing[i].gameObject.SetActive(used);

            if (!used)
            {
                continue;
            }

            Transform keyTransform = existing[i].Find(KeyChildName);
            TextMeshProUGUI keyText = keyTransform ? keyTransform.GetComponent<TextMeshProUGUI>() : null;

            if (keyText)
            {
                _keyCaps.Add(keyText);
                _keyCapRoots.Add(existing[i].gameObject);
            }
        }
    }

    /// <summary>Writes and localizes text, only on change; returns whether it was written.</summary>
    private static bool Write(TextMeshProUGUI target, string text)
    {
        if (!target || (_written.TryGetValue(target, out string last) && last == text))
        {
            return false;
        }

        Localization.instance?.RemoveTextFromCache(target);
        target.text = text;
        _written[target] = text;
        Localization.instance?.Localize(target.transform);

        return true;
    }

    internal void SetLabel(string text)
    {
        if (!Write(_label, text) || !_labelLayout)
        {
            return;
        }

        _labelLayout.preferredWidth = Mathf.Ceil(_label.GetPreferredValues(_label.text).x) + 6f;
    }

    /// <summary>Replaces a cap with copies of the given game objects.</summary>
    internal void AdoptCap(int index, List<GameObject> sources)
    {
        if (sources == null || sources.Count == 0 || index < 0 || index >= _keyCapRoots.Count)
        {
            return;
        }

        GameObject old = _keyCapRoots[index];
        int siblingIndex = old ? old.transform.GetSiblingIndex() : index;

        if (_keyCaps[index] is not null)
        {
            _written.Remove(_keyCaps[index]);
        }

        if (old)
        {
            Object.Destroy(old);
        }

        List<GameObject> clones = new();

        foreach (GameObject source in sources)
        {
            if (!source)
            {
                continue;
            }

            GameObject clone = Object.Instantiate(source, _root.transform, false);
            clone.name = $"{KeyCapPrefix} (adopted {index}.{clones.Count})";
            clone.transform.SetSiblingIndex(siblingIndex + clones.Count);
            clones.Add(clone);
        }

        if (clones.Count == 0)
        {
            return;
        }

        _keyCapRoots[index] = clones[0];
        _keyCaps[index] = null;
        clones.RemoveAt(0);
        _adoptedExtras[index] = clones;
    }

    /// <summary>Sets the caps; a null entry leaves that cap's content unchanged.</summary>
    internal void SetKeys(string[] keys)
    {
        for (int i = 0; i < _keyCapRoots.Count; i++)
        {
            bool used = i < keys.Length;

            if (_keyCapRoots[i] && _keyCapRoots[i].activeSelf != used)
            {
                _keyCapRoots[i].SetActive(used);
            }

            if (_adoptedExtras.TryGetValue(i, out List<GameObject> extras))
            {
                foreach (GameObject extra in extras)
                {
                    if (extra && extra.activeSelf != used)
                    {
                        extra.SetActive(used);
                    }
                }
            }

            if (used && keys[i] != null && _keyCaps[i] is not null)
            {
                Write(_keyCaps[i], keys[i]);
            }
        }
    }

    internal void SetSiblingIndex(int index)
    {
        if (_root)
        {
            _root.transform.SetSiblingIndex(index);
        }
    }

    internal void SetVisible(bool visible)
    {
        if (_root && _root.activeSelf != visible)
        {
            _root.SetActive(visible);
        }
    }

    internal void Destroy()
    {
        if (_label is not null)
        {
            _written.Remove(_label);
        }

        foreach (TextMeshProUGUI cap in _keyCaps)
        {
            if (cap is not null)
            {
                _written.Remove(cap);
            }
        }

        if (_root)
        {
            Object.Destroy(_root);
        }
    }
}
