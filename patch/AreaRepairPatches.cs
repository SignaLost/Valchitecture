using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;
using Valchitecture.Modules;

namespace Valchitecture.Patch;
/// <summary>Replaces <c>Player.Repair</c> once unlocked: one swing repairs the aimed piece, then the nearest pieces around the player, as far as stamina allows.</summary>
[HarmonyPatch(typeof(Player), "Repair")]
internal static class Player_Repair_Patch
{
    private const float RepairCooldown = 1f;

    private static readonly List<WearNTear> _candidates = new();

    /// <summary>The player's position for <see cref="ByDistance" />.</summary>
    private static Vector3 _center;

    private static readonly System.Comparison<WearNTear> ByDistance =
        (a, b) => (a.transform.position - _center).sqrMagnitude.CompareTo((b.transform.position - _center).sqrMagnitude);

    private static bool Prefix(Player __instance, ItemDrop.ItemData toolItem)
    {
        if (__instance != Player.m_localPlayer || !Unlocks.CanRepairArea() || toolItem?.m_shared == null)
        {
            return true;
        }

        if (!__instance.InPlaceMode())
        {
            return false;
        }

        Piece hovering = __instance.GetHoveringPiece();

        if (!hovering || !CanMend(__instance, hovering, announce: true) || !PrivateArea.CheckAccess(hovering.transform.position))
        {
            return false;
        }

        Gather(__instance, hovering, Unlocks.RepairRadius());

        if (_candidates.Count == 0)
        {
            __instance.Message(MessageHud.MessageType.TopLeft, Names.Localize(Tokens.NothingToMend));
            return false;
        }

        BuildCost.AreaBill bill = BuildCost.BillFor(__instance, _candidates.Count);
        bool trimmed = bill.Pieces < _candidates.Count;

        int mended = 0;
        string first = null;

        for (int i = 0; i < bill.Pieces; i++)
        {
            WearNTear piece = _candidates[i];

            if (!piece || !piece.Repair())
            {
                continue;
            }

            Spark(piece, audible: mended == 0, __instance.GetZDOID());

            if (mended == 0)
            {
                first = piece.m_piece ? piece.m_piece.m_name : null;
            }

            mended++;
        }

        _candidates.Clear();

        if (mended <= 0)
        {
            __instance.Message(MessageHud.MessageType.TopLeft, Names.Localize(Tokens.NothingToMend));
            return false;
        }

        BuildCost.AreaBill paid = mended == bill.Pieces ? bill : BuildCost.BillFor(__instance, mended);

        Swing(__instance, toolItem);
        BuildCost.Charge(__instance, paid.Stamina);
        BuildCost.Wear(__instance, paid.Wear);
        __instance.UseEitr(toolItem.m_shared.m_attack.m_attackEitr);

        Announce(__instance, mended, first);

        if (trimmed && Hud.instance)
        {
            Hud.instance.StaminaBarEmptyFlash();
        }

        return false;
    }

    /// <summary>Turns the player and plays the tool's swing animation.</summary>
    private static void Swing(Player player, ItemDrop.ItemData toolItem)
    {
        player.FaceLookDirection();

        ZSyncAnimation animation = player.GetZAnim();
        string trigger = toolItem.m_shared.m_attack.m_attackAnimation;

        if (animation && !string.IsNullOrEmpty(trigger))
        {
            animation.SetTrigger(trigger);
        }
    }

    /// <summary>Spawns the piece's placement effect, muted when not audible; ZSFX starts on its first update.</summary>
    private static void Spark(WearNTear piece, bool audible, ZDOID source)
    {
        EffectList effects = piece.m_piece ? piece.m_piece.m_placeEffect : null;

        if (effects == null || !effects.HasEffects())
        {
            return;
        }

        GameObject[] spawned = effects.Create(piece.transform.position, piece.transform.rotation, null, 1f, -1, source);

        if (audible || spawned == null)
        {
            return;
        }

        foreach (GameObject thing in spawned)
        {
            if (!thing)
            {
                continue;
            }

            foreach (ZSFX sfx in thing.GetComponentsInChildren<ZSFX>(true))
            {
                sfx.m_playOnAwake = false;
            }

            foreach (AudioSource voice in thing.GetComponentsInChildren<AudioSource>(true))
            {
                voice.playOnAwake = false;
                voice.mute = true;
                voice.Stop();
            }
        }
    }

    /// <summary>The game's message for one piece; a count for more.</summary>
    private static void Announce(Player player, int mended, string first)
    {
        string said = mended == 1 && !string.IsNullOrEmpty(first) && Localization.instance != null
            ? Localization.instance.Localize("$msg_repaired", first)
            : Names.Localize(Tokens.Repaired, mended.ToString());

        player.Message(MessageHud.MessageType.TopLeft, said);
    }

    private static void Gather(Player player, Piece aimed, float radius)
    {
        _candidates.Clear();

        Vector3 center = player.transform.position;
        WearNTear chosen = aimed ? aimed.GetComponent<WearNTear>() : null;

        foreach (WearNTear piece in WearNTear.GetAllInstances())
        {
            if (!piece || piece == chosen || !piece.m_piece)
            {
                continue;
            }

            if (Vector3.Distance(center, piece.transform.position) > radius || !WantsRepair(piece))
            {
                continue;
            }

            if (!PrivateArea.CheckAccess(piece.transform.position, 0f, false) || !CanMend(player, piece.m_piece, announce: false))
            {
                continue;
            }

            _candidates.Add(piece);
        }

        _center = center;
        _candidates.Sort(ByDistance);

        if (chosen && WantsRepair(chosen))
        {
            _candidates.Insert(0, chosen);
        }
    }

    /// <summary>The checks of <c>WearNTear.Repair</c>, without repairing; pieces below the player's skill also qualify.</summary>
    private static bool WantsRepair(WearNTear piece)
    {
        if (!piece.m_nview || !piece.m_nview.IsValid())
        {
            return false;
        }

        ZDO zdo = piece.m_nview.GetZDO();
        bool damaged = zdo != null && zdo.GetFloat(ZDOVars.s_health, piece.m_health) < piece.m_health;

        if (!damaged && !PieceStrength.Outranks(piece))
        {
            return false;
        }

        return Time.time - piece.m_lastRepair >= RepairCooldown;
    }

    /// <summary>The game's <c>Player.CheckCanRemovePiece</c>, with or without its messages.</summary>
    private static bool CanMend(Player player, Piece piece, bool announce)
    {
        if (!piece)
        {
            return false;
        }

        if (announce)
        {
            return player.CheckCanRemovePiece(piece);
        }

        if (player.PlacementCostDisabled || !piece.m_craftingStation || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
        {
            return true;
        }

        return CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position)
               && Reach.BenchHolds(player, piece, piece.transform.position);
    }
}
