using Mimir;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;

namespace Valchitecture.Modules;
/// <summary>
///     Piece health and support loss from the builder's skill, active with the Stronger Pieces setting and the mod on
///     the server.
///     Each piece stores the builder's skill factor (0..1) in its ZDO, never the results, and its maximum health and
///     support loss follow from that factor; with Repair Upgrades on, a repair by a more skilled player raises it.
/// </summary>
internal static class PieceStrength
{
    /// <summary>ZDO key hash, as the string overloads compute it.</summary>
    internal static readonly int SkillKey = "Valchitecture.BuiltAtSkill".GetStableHashCode();

    private const float HealthPerStep = 0.02f;
    private const int HealthLevelsPerStep = 2;

    private const float LossReliefPerStep = 0.016f;
    private const int LossLevelsPerStep = 4;

    /// <summary>2% more health every second level: double at 100.</summary>
    internal static float HealthScale(float factor)
    {
        return Ramp.Stepped(WholeLevel(factor), 0f, HealthLevelsPerStep, 1f, HealthPerStep, 2f);
    }

    /// <summary>1.6% less support loss per joint every fourth level: 0.6x at 100.</summary>
    internal static float LossScale(float factor)
    {
        return Ramp.Stepped(WholeLevel(factor), 0f, LossLevelsPerStep, 1f, -LossReliefPerStep, 0f);
    }

    /// <summary>The factor as a whole level, 0 to 100.</summary>
    private static float WholeLevel(float factor)
    {
        return Mathf.Round(Mathf.Clamp01(factor) * 100f);
    }

    /// <summary>Whether piece strength applies; requires the mod on the server.</summary>
    internal static bool Active => Cfg.Enabled.Value && Cfg.StrongerPieces.Value && ServerMod.Present;

    internal static float StoredFactor(WearNTear piece)
    {
        ZNetView view = View(piece);
        ZDO zdo = view ? view.GetZDO() : null;
        return zdo?.GetFloat(SkillKey, 0f) ?? 0f;
    }

    /// <summary>The piece's view, also before Awake has set <c>m_nview</c>.</summary>
    private static ZNetView View(WearNTear piece)
    {
        return !piece ? null : piece.m_nview ? piece.m_nview : piece.GetComponent<ZNetView>();
    }

    /// <summary>Sets maximum health from the prefab's base and the stored skill; before Awake the game adds its world-level bonus itself.</summary>
    internal static void ApplyStored(WearNTear piece, bool awakePending = false)
    {
        float factor = StoredFactor(piece);

        if (piece && Active && factor > 0f)
        {
            piece.m_health = BaseHealth(piece, awakePending) * HealthScale(factor);
        }
    }

    /// <summary>Whether a repair by the local player would make this piece stronger.</summary>
    internal static bool Outranks(WearNTear piece)
    {
        if (!Active || !Cfg.RepairUpgrades.Value || !piece || !piece.m_nview || !piece.m_nview.IsValid())
        {
            return false;
        }

        float stored = StoredFactor(piece);
        float mine = BuildingSkill.Factor;
        return mine > stored && (HealthScale(mine) > HealthScale(stored) || LossScale(mine) < LossScale(stored));
    }

    /// <summary>Raises the piece to the local player's skill before a repair, claiming ownership.</summary>
    internal static void Upgrade(WearNTear piece)
    {
        // The game's repair cooldown is 1 s.
        if (!Outranks(piece) || Time.time - piece.m_lastRepair < 1f)
        {
            return;
        }

        if (!piece.m_nview.IsOwner())
        {
            piece.m_nview.ClaimOwnership();
        }

        // An undamaged piece stores no health and reads as the maximum; it must be pinned before the maximum rises.
        ZDO zdo = piece.m_nview.GetZDO();
        zdo.Set(ZDOVars.s_health, zdo.GetFloat(ZDOVars.s_health, piece.m_health));

        float factor = BuildingSkill.Factor;
        zdo.Set(SkillKey, factor);
        piece.m_health = BaseHealth(piece, false) * HealthScale(factor);

        if (Log.Shows(Log.InfoLevel.High))
        {
            Log.LogInfo($"Upgraded {piece.name} to skill {factor * 100f:0}: health {piece.m_health:0.#}", Log.InfoLevel.High);
        }
    }

    private static float BaseHealth(WearNTear piece, bool awakePending)
    {
        GameObject prefab = PrefabKey.PrefabOf(piece);
        WearNTear source = prefab ? prefab.GetComponent<WearNTear>() : null;
        float health = source ? source.m_health : piece.m_health;

        // The world-level bonus, as WearNTear.Awake applies it.
        if (!awakePending && Game.m_worldLevel > 0 && Game.instance)
        {
            health += Game.m_worldLevel * Game.instance.m_worldLevelPieceHPMultiplier * health;
        }

        return health;
    }
}
