namespace Valchitecture.Common;

/// <summary>Localization helpers.</summary>
internal static class Names
{
    internal static string Of(Piece piece)
    {
        string name = piece ? piece.m_name : null;
        return string.IsNullOrEmpty(name) ? "piece" : Localize(name);
    }

    /// <summary>Localizes a token, filling <c>$1</c>, <c>$2</c> from <paramref name="words" />.</summary>
    internal static string Localize(string token, params string[] words)
    {
        return Localization.instance != null ? Localization.instance.Localize(token, words) : token;
    }
}
