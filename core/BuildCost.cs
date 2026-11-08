using UnityEngine;

namespace Valchitecture.Core;

/// <summary>
///     Stamina and tool wear for the mod's own actions: a move costs a removal plus a placement, and an area repair
///     costs half a swing per piece, never less than one swing.
///     Swing stamina is the game's own value, including the skill's stamina bonus and other mods' modifiers.
/// </summary>
internal static class BuildCost
{
    private const float MoveMultiplier = 2f;
    private const float AreaShare = 0.5f;

    internal readonly struct AreaBill
    {
        internal AreaBill(int pieces, float stamina, float wear, bool drained)
        {
            Pieces = pieces;
            Stamina = stamina;
            Wear = wear;
            Drained = drained;
        }

        internal readonly int Pieces;
        internal readonly float Stamina;
        internal readonly float Wear;

        /// <summary>Whether the bill takes every bit of stamina the player has.</summary>
        internal readonly bool Drained;
    }

    /// <summary>One swing's stamina with all modifiers, this mod's included.</summary>
    internal static float PerSwing(Player player)
    {
        if (!player || player.GetRightItem() == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, player.GetBuildStamina());
    }

    /// <summary>One repair swing's wear, including the world's durability rate.</summary>
    internal static float WearPerSwing(Player player)
    {
        ItemDrop.ItemData tool = player ? player.GetRightItem() : null;

        if (tool?.m_shared == null || !tool.m_shared.m_useDurability)
        {
            return 0f;
        }

        return Mathf.Max(0f, tool.m_shared.m_useDurabilityDrain * Game.m_durabilityRate);
    }

    internal static float OfMove(Player player) => PerSwing(player) * MoveMultiplier;

    /// <summary>The game's wear for a removal and a placement.</summary>
    internal static float WearOfMove(Player player)
    {
        ItemDrop.ItemData tool = player ? player.GetRightItem() : null;

        if (tool?.m_shared == null || !tool.m_shared.m_useDurability)
        {
            return 0f;
        }

        return Mathf.Max(0f, player.GetPlaceDurability(tool) * Game.m_durabilityRate * MoveMultiplier);
    }

    /// <summary>
    ///     How many of <paramref name="wanted" /> pieces the player repairs, and their cost. The first is always paid for.
    ///     When the pieces outrun the stamina, the last one is paid with whatever is left, so the bar ends empty.
    /// </summary>
    internal static AreaBill BillFor(Player player, int wanted)
    {
        if (!player || wanted <= 0)
        {
            return new AreaBill(0, 0f, 0f, false);
        }

        float swing = PerSwing(player);
        float wearPerSwing = WearPerSwing(player);

        // UseStamina scales by the world's stamina rate, so the bill is in the same units as the bar.
        float rate = Game.m_staminaRate > 0f ? Game.m_staminaRate : 1f;
        float stamina = player.GetStamina() / rate;

        int affordable = 1;
        float owedStamina = Share(swing, 1);

        for (int pieces = 2; pieces <= wanted; pieces++)
        {
            float nextStamina = Share(swing, pieces);

            if (nextStamina > stamina)
            {
                break;
            }

            affordable = pieces;
            owedStamina = nextStamina;
        }

        bool drained = affordable < wanted && swing > 0f;

        if (drained)
        {
            affordable++;
            owedStamina = Mathf.Max(owedStamina, stamina);
        }

        return new AreaBill(affordable, owedStamina, Share(wearPerSwing, affordable), drained);
    }

    internal static void Charge(Player player, float stamina)
    {
        if (player && stamina > 0f)
        {
            player.UseStamina(stamina);
        }
    }

    /// <summary>Wears the tool, down to zero.</summary>
    internal static void Wear(Player player, float amount)
    {
        ItemDrop.ItemData tool = player ? player.GetRightItem() : null;

        if (tool != null && amount > 0f)
        {
            tool.m_durability = Mathf.Max(0f, tool.m_durability - amount);
        }
    }

    /// <summary>Whether the player can pay, flashing the stamina bar when not.</summary>
    internal static bool Afford(Player player, float cost)
    {
        if (cost <= 0f || (player && player.HaveStamina(cost)))
        {
            return true;
        }

        if (Hud.instance)
        {
            Hud.instance.StaminaBarEmptyFlash();
        }

        return false;
    }

    /// <summary>Half of one per piece, never less than one.</summary>
    private static float Share(float one, int pieces)
    {
        return pieces <= 0 || one <= 0f ? 0f : Mathf.Max(one, one * pieces * AreaShare);
    }
}
