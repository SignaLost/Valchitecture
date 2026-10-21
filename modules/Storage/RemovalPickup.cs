using System;
using System.Collections.Generic;
using Chestworks;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;


namespace Valchitecture.Modules.Storage;
/// <summary>
///     Gives a removed piece's resources to containers, then the inventory, and drops only what is left; unlocked by
///     the Building skill's salvage ability.
///     Amounts match <c>Piece.DropResources</c>, including feast portions, drop conversion and the reduced return for
///     pieces not placed by a player, and the cheated flag carries over to every item.
/// </summary>
internal static class RemovalPickup
{
    /// <summary>True while the local player's hammer removal runs.</summary>
    internal static bool Removing;

    private static readonly List<(GameObject Prefab, int Count)> _leftover = new();

    internal static bool Active(Player player)
    {
        return player && player == Player.m_localPlayer && Unlocks.CanPickUpRemovals();
    }

    /// <summary>Replaces <c>Piece.DropResources</c> for this removal, with the same amounts; returns false to leave the drop to the game.</summary>
    internal static bool Distribute(Piece piece, HitData hitData)
    {
        Player player = Player.m_localPlayer;

        if (!player || !ObjectDB.instance || ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
        {
            return false;
        }

        Feast feast = piece.GetComponent<Feast>();
        bool cheated = piece.m_nview.GetZDO().IsCheated() && !PlayerProfile.s_bypassCheatChecks;

        _leftover.Clear();

        foreach (Piece.Requirement requirement in piece.m_resources)
        {
            if (!requirement.m_resItem || !requirement.m_recover)
            {
                continue;
            }

            GameObject prefab = ObjectDB.instance.GetItemPrefab(Utils.GetPrefabName(requirement.m_resItem.name));
            int count = requirement.m_amount;

            if (feast)
            {
                count = (int)Math.Floor(count * feast.GetStackPercentige());
            }

            if (!prefab || count <= 0)
            {
                continue;
            }

            prefab = Game.instance.CheckDropConversion(hitData, requirement.m_resItem, prefab, ref count);

            if (!piece.IsPlacedByPlayer())
            {
                count = Mathf.Max(1, count / 3);
            }

            if (!prefab || !prefab.TryGetComponent(out ItemDrop drop))
            {
                continue;
            }

            int stored = ChestworksAPI.Store(Purposes.Building, piece.transform.position, prefab, count, cheated, piece);

            if (stored > 0)
            {
                player.Message(MessageHud.MessageType.TopLeft, Names.Localize(Tokens.Stored, Names.Localize(drop.m_itemData.m_shared.m_name)), stored, drop.m_itemData.GetIcon());
                count -= stored;
            }

            int carried = count > 0 ? Carry(player, prefab, drop.m_itemData, count, cheated) : 0;

            if (carried > 0)
            {
                player.ShowPickupMessage(drop.m_itemData, carried);
                count -= carried;
            }

            if (count > 0)
            {
                _leftover.Add((prefab, count));
            }
        }

        Drop(piece, cheated);
        _leftover.Clear();
        return true;
    }

    /// <summary>Adds what fits the inventory's space and the carry weight left.</summary>
    private static int Carry(Player player, GameObject prefab, ItemDrop.ItemData template, int count, bool cheated)
    {
        Inventory inventory = player.GetInventory();
        float unit = template.GetWeight(1);

        if (unit > 0f)
        {
            float room = player.GetMaxCarryWeight() - inventory.GetTotalWeight();
            count = Mathf.Min(count, Mathf.Max(0, Mathf.FloorToInt(room / unit)));
        }

        return count > 0 ? ChestworksAPI.AddItems(inventory, prefab, count, cheated) : 0;
    }

    /// <summary>Drops what is left as loose items, or in a loot bag where the piece has one.</summary>
    private static void Drop(Piece piece, bool cheated)
    {
        Vector3 position = piece.transform.position + Vector3.up * piece.m_returnResourceHeightOffset;
        Container bag = null;

        foreach ((GameObject prefab, int total) in _leftover)
        {
            int count = total;

            while (count > 0 && !piece.m_destroyedLootPrefab)
            {
                ItemDrop loose = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<ItemDrop>();
                loose.SetStack(Mathf.Min(count, loose.m_itemData.m_shared.m_maxStackSize));
                ItemDrop.OnCreateNew(loose);
                loose.m_itemData.m_cheated = cheated;
                count -= loose.m_itemData.m_stack;
            }

            while (count > 0)
            {
                ItemDrop.ItemData item = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
                item.m_dropPrefab = prefab;
                item.m_stack = Mathf.Min(count, item.m_shared.m_maxStackSize);
                count -= item.m_stack;

                if (!bag || !bag.GetInventory().HaveEmptySlot())
                {
                    bag = UnityEngine.Object.Instantiate(piece.m_destroyedLootPrefab, position, Quaternion.identity).GetComponent<Container>();
                }

                item.m_cheated = cheated;
                bag.GetInventory().AddItem(item);
            }
        }
    }
}
