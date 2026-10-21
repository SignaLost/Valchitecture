using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valchitecture.Common;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>Hand-placed snap points for pieces the calculation cannot handle. Null for other pieces.</summary>
internal static class CuratedSnapPoints
{
    internal static SnapPointSpec[] For(GameObject prefab)
    {
        switch (prefab.name)
        {
            /* Fences */
            case "wood_fence":
                // 2 m long; every 0.2 m from just below the ground to 1 m up.
                return Points(Columns(1f, -0.2f, 1f, 0.2f));

            case "piece_sharpstakes":
                return Points(Columns(1.12f, -0.2f, 0.2f, 0.2f));

            case "piece_dvergr_sharpstakes":
                return Points(WithMiddles(new[]
                {
                    new Vector3(-0.5f, -0.5f, 2f),
                    new Vector3(-0.5f, -0.5f, -2f),
                    new Vector3(-0.5f, 0f, 2f),
                    new Vector3(-0.5f, 0f, -2f),
                    new Vector3(1f, 1f, 2f),
                    new Vector3(1f, 1f, -2f),
                }));

            /* Item Stands */
            case "itemstandh": // itemstandh (horizontal)
                return new[]
                {
                    new SnapPointSpec(0f, -0.0346f, 0f, SnapRole.Center),
                    new SnapPointSpec(0f, -0.1346f, 0f, SnapRole.Center),
                    new SnapPointSpec(0f, -0.2346f, 0f, SnapRole.Center),
                };

            case "itemstand":  // itemstand (vertical)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, -0.06f, SnapRole.Center),
                    new SnapPointSpec(0.0f, 0.0f, -0.16f, SnapRole.Center),
                    new SnapPointSpec(0.0f, 0.0f, -0.26f, SnapRole.Center),
                };

            /* Chests */
            case "piece_chest_wood":
                return Table(prefab, 0.8f, 0.37f);

            case "piece_chest": // (Reinforced Chest)
                return Table(prefab, 0.9f, 0.47f);

            case "piece_chest_private":
                return Table(prefab, 0.45f, 0.25f);

            case "piece_chest_blackmetal":
                return Table(prefab, 1.0f, 0.5f);

            /* Torches and lights */
            case "piece_walltorch":  // (sconce)
                return new[]
                {
                    new SnapPointSpec(-0.2f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(-0.25f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(-0.35f, 0.0f, 0.0f, SnapRole.Center),
                };

            case "piece_dvergr_lantern":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.05f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.1f, 0.0f, 0.0f, SnapRole.Center),
                };

            case "piece_dvergr_lantern_pole":
                return new[]
                {
                    new SnapPointSpec(-0.15f, -3.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.1f, -3.0f, -0.25f),
                    new SnapPointSpec(0.1f, -3.0f, 0.25f),
                    new SnapPointSpec(-0.35f, -3.0f, -0.25f),
                    new SnapPointSpec(-0.35f, -3.0f, 0.25f),
                    new SnapPointSpec(-0.15f, 0.5f, 0.0f),
                };

            /* Furniture */
            case "sign":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.0f, 0.0f, -0.05f, SnapRole.Center),
                    new SnapPointSpec(0.0f, 0.0f, -0.1f, SnapRole.Center),
                    new SnapPointSpec(0.0f, 0.0f, -0.2f, SnapRole.Center),
                    new SnapPointSpec(0.0f, 0.0f, -0.25f, SnapRole.Center),
                };

            case "ArmorStand":
                return Points(new[]
                {
                    new Vector3(0.0f, -0.1f, 0.0f),
                    new Vector3(0.5f, -0.1f, 0.0f),
                    new Vector3(-0.5f, -0.1f, 0.0f),
                    new Vector3(0.0f, -0.1f, 0.5f),
                    new Vector3(0.0f, -0.1f, -0.5f),
                });

            /* Rugs & Carpets */
            case "jute_carpet": // (red jute carpet)
                return Points(new[]
                {
                    new Vector3(0.0f, -0.01f, 0.0f),
                    new Vector3(2.0f, -0.01f, -1.25f),
                    new Vector3(2.0f, -0.01f, 1.25f),
                    new Vector3(-2.0f, -0.01f, -1.25f),
                    new Vector3(-2.0f, -0.01f, 1.25f),

                    new Vector3(0.0f, -0.05f, 0.0f),
                    new Vector3(2.0f, -0.05f, -1.25f),
                    new Vector3(2.0f, -0.05f, 1.25f),
                    new Vector3(-2.0f, -0.05f, -1.25f),
                    new Vector3(-2.0f, -0.05f, 1.25f),
                });

            case "rug_fur": // (lox rug)
                return Points(new[]
                {
                    new Vector3(0.0f, -0.01f, 0.0f),
                    new Vector3(1.25f, -0.01f, -2.0f),
                    new Vector3(1.25f, -0.01f, 2.0f),
                    new Vector3(-1.25f, -0.01f, -2.0f),
                    new Vector3(-1.25f, -0.01f, 2.0f),

                    new Vector3(0.0f, -0.05f, 0.0f),
                    new Vector3(1.25f, -0.05f, -2.0f),
                    new Vector3(1.25f, -0.05f, 2.0f),
                    new Vector3(-1.25f, -0.05f, -2.0f),
                    new Vector3(-1.25f, -0.05f, 2.0f),
                });

            case "rug_deer":
            case "rug_wolf":
            case "rug_hare": // (scale rug)
            case "jute_carpet_blue": // (round blue jute carpet)
                return Points(
                    new[] {
                        new Vector3(0.0f, -0.01f, 0.0f),
                        new Vector3(0.0f, -0.05f, 0.0f),
                        new Vector3(0.0f, -0.1f, 0.0f)
                    }
                );

            /* Thrones, Chairs, Benches */
            case "piece_throne01": // (Raven Throne)
            case "piece_throne02": // (Stone Throne)
            case "piece_blackmarble_throne":
            case "piece_chair": // (stool)
            case "piece_chair02": // (finewood chair)
            case "piece_chair03": // (darkwood chair)
                return OriginUnlessPresent(prefab);

            case "piece_bench01":
            case "piece_blackmarble_bench":
            case "piece_logbench01": // sitting log
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(-1.0f, 0.0f, 0.0f),
                    new SnapPointSpec(1.0f, 0.0f, 0.0f),
                };

            /* Banners */
            case "piece_banner01": // (black)
            case "piece_banner02": // (blue)
            case "piece_banner03": // (white & red)
            case "piece_banner04": // (red)
            case "piece_banner05": // (green)
            case "piece_banner06": // (white, blue, red)
            case "piece_banner07": // (white & blue)
            case "piece_banner08": // (yellow)
            case "piece_banner09": // (purple)
            case "piece_banner10": // (orange)
            case "piece_banner11": // (white)
            case "piece_cloth_hanging_door": // (red jute curtain)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.0f, 0.1f, 0.0f, SnapRole.Center),

                    new SnapPointSpec(0.136f, 0.0f, 0.0f, SnapRole.Center), // stone wall
                    new SnapPointSpec(-0.136f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.136f, 0.1f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(-0.136f, 0.1f, 0.0f, SnapRole.Center),

                    new SnapPointSpec(0.236f, 0.0f, 0.0f, SnapRole.Center), // wood wall
                    new SnapPointSpec(-0.236f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.236f, 0.1f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(-0.236f, 0.1f, 0.0f, SnapRole.Center),
                };

            /* Blue Jute Hangings */
            case "piece_cloth_hanging_door_blue2":  // (Blue Jute Curtain)
                return Points(new[]
                {
                    new Vector3(0.0f, 1.5f, 0.0f),
                    new Vector3(0.0f, 1.5f, 2.0f),
                    new Vector3(0.0f, 1.5f, -2.0f),
                    new Vector3(0.14f * 2, 1.5f, 2.0f), // stone walls
                    new Vector3(0.14f * 2, 1.5f, -2.0f), // stone walls
                    new Vector3(-0.14f * 2, 1.5f, -2.0f), // stone walls
                    new Vector3(-0.14f * 2, 1.5f, 2.0f), // stone walls
                    new Vector3(0.236f * 2, 1.5f, 2.0f), // wood walls
                    new Vector3(0.236f * 2, 1.5f, -2.0f), // wood walls
                    new Vector3(-0.236f * 2, 1.5f, -2.0f), // wood walls
                    new Vector3(-0.236f * 2, 1.5f, 2.0f), // wood walls
                });

            case "piece_cloth_hanging_door_blue": // (Blue Jute Drape)
                return Points(new[]
                {
                    new Vector3(0.0f, 4.0f, 0.0f), // center
                    new Vector3(0.16f, 4.0f, 0.0f),
                    new Vector3(-0.16f, 4.0f, 0.0f),
                    new Vector3(0.36f, 4.0f, 0.0f),
                    new Vector3(-0.36f, 4.0f, 0.0f),
                });

            /* Workbench */
            case "piece_workbench":
                return Table(prefab, 1.25f, 0.5f);

            case "piece_workbench_ext1": // (Chopping block)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.25f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, -0.25f),
                    new SnapPointSpec(-0.25f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, 0.25f),
                };

            case "piece_workbench_ext2": // (Tanning rack)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.0f, 0.0f),
                    new SnapPointSpec(1.0f, 0.0f, -1.0f),
                    new SnapPointSpec(-1.0f, 0.0f, 0.0f),
                    new SnapPointSpec(-1.0f, 0.0f, -1.0f),
                };

            case "piece_workbench_ext3": // (Adze)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.0f, 0.5f),
                    new SnapPointSpec(1.0f, 0.0f, -0.5f),
                    new SnapPointSpec(-1.0f, 0.0f, 0.5f),
                    new SnapPointSpec(-1.0f, 0.0f, -0.5f),
                    new SnapPointSpec(1.0f, 0.0f, 0.0f),
                    new SnapPointSpec(-1.0f, 0.0f, 0.0f),
                };

            case "piece_workbench_ext4":  // (Tool shelf)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.25f, -0.125f, SnapRole.Center),
                    new SnapPointSpec(0.75f, 0.25f, -0.125f),
                    new SnapPointSpec(0.75f, 1.25f, -0.125f),
                    new SnapPointSpec(-0.75f, 0.25f, -0.125f),
                    new SnapPointSpec(-0.75f, 1.25f, -0.125f),
                };

            /* Forge */
            case "forge": // (Forge)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.0f, -0.5f),
                    new SnapPointSpec(1.0f, 0.0f, 0.5f),
                    new SnapPointSpec(-1.0f, 0.0f, -0.5f),
                    new SnapPointSpec(-1.0f, 0.0f, 0.5f),
                };

            case "forge_ext1": // (Forge bellows)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.5f, 0.0f, -0.75f),
                    new SnapPointSpec(0.5f, 0.0f, 0.75f),
                    new SnapPointSpec(-0.5f, 0.0f, -0.75f),
                    new SnapPointSpec(-0.5f, 0.0f, 0.75f),
                };

            case "forge_ext2": // (Anvils)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.25f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, -0.25f),
                    new SnapPointSpec(-0.25f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, 0.25f),
                };

            case "forge_ext3": // (Grinding wheel)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.25f, 0.0f, -0.5f),
                    new SnapPointSpec(0.25f, 0.0f, 0.5f),
                    new SnapPointSpec(-0.25f, 0.0f, -0.5f),
                    new SnapPointSpec(-0.25f, 0.0f, 0.5f),
                };

            case "forge_ext4": // (Smith's anvil)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.5f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, -0.5f),
                    new SnapPointSpec(-0.5f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, 0.5f),
                };

            case "forge_ext5": // (Forge cooler)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.5f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, -0.5f),
                    new SnapPointSpec(-0.5f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, 0.5f),
                };

            case "forge_ext6": // (Forge toolrack)
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, -0.125f, SnapRole.Center),
                    new SnapPointSpec(1.25f, 0.0f, -0.125f),
                    new SnapPointSpec(-1.25f, 0.0f, -0.125f),
                };

            /* Black Forge */
            case "blackforge": // galdr table
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.5f, 0.0f, -0.25f),
                    new SnapPointSpec(1.5f, 0.0f, 0.75f),
                    new SnapPointSpec(-1.5f, 0.0f, -0.25f),
                    new SnapPointSpec(-1.5f, 0.0f, 0.75f),
                    new SnapPointSpec(0.0f, 0.0f, -0.75f),
                };

            case "blackforge_ext1": // cooler
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.75f, 0.0f, -0.25f),
                    new SnapPointSpec(0.75f, 0.0f, 0.25f),
                    new SnapPointSpec(-0.75f, 0.0f, -0.25f),
                    new SnapPointSpec(-0.75f, 0.0f, 0.25f),
                };

            case "blackforge_ext2_vise": // vice
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.25f, 0.0f, -0.25f),
                    new SnapPointSpec(0.25f, 0.0f, 0.25f),
                    new SnapPointSpec(-0.25f, 0.0f, -0.25f),
                    new SnapPointSpec(-0.25f, 0.0f, 0.25f),
                };

            /* Galdr Table */
            case "piece_magetable": // galdr table
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.0f, -1.0f),
                    new SnapPointSpec(1.0f, 0.0f, 1.0f),
                    new SnapPointSpec(-1.0f, 0.0f, -1.0f),
                    new SnapPointSpec(-1.0f, 0.0f, 1.0f),
                    new SnapPointSpec(0.0f, 0.0f, -1.25f),
                };

            case "piece_magetable_ext": // rune table
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.5f, 0.0f, -0.5f),
                    new SnapPointSpec(0.5f, 0.0f, 0.5f),
                    new SnapPointSpec(-0.5f, 0.0f, -0.5f),
                    new SnapPointSpec(-0.5f, 0.0f, 0.5f),
                };

            case "piece_magetable_ext2": // candles
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.25f, 0.0f, -0.25f),
                    new SnapPointSpec(0.25f, 0.0f, 0.25f),
                    new SnapPointSpec(-0.25f, 0.0f, -0.25f),
                    new SnapPointSpec(-0.25f, 0.0f, 0.25f),
                };

            /* Cooking Pieces */
            case "piece_cauldron":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.5f, 0.0f, -0.5f),
                    new SnapPointSpec(0.5f, 0.0f, 0.5f),
                    new SnapPointSpec(-0.5f, 0.0f, -0.5f),
                    new SnapPointSpec(-0.5f, 0.0f, 0.5f),
                };
            case "cauldron_ext1_spice":
                return new[]
                {
                    new SnapPointSpec(0.0f, 1.25f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.0f, 0.75f, -0.05f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.75f, -0.05f),
                    new SnapPointSpec(-1.0f, 0.75f, -0.05f),
                };
;
            case "cauldron_ext3_butchertable":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.5f, 0.0f, -0.5f),
                    new SnapPointSpec(0.5f, 0.0f, 0.5f),
                    new SnapPointSpec(-0.5f, 0.0f, -0.5f),
                    new SnapPointSpec(-0.5f, 0.0f, 0.5f),
                };

            case "cauldron_ext4_pots":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.5f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.0f, 0.0f),
                    new SnapPointSpec(1.0f, 0.75f, 0.0f),
                    new SnapPointSpec(-1.0f, 0.0f, 0.0f),
                    new SnapPointSpec(-1.0f, 0.75f, 0.0f),
                };

            case "cauldron_ext5_mortarandpestle":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.6f, 0.0f, -0.3f),
                    new SnapPointSpec(0.6f, 0.0f, 0.3f),
                    new SnapPointSpec(-0.6f, 0.0f, -0.3f),
                    new SnapPointSpec(-0.6f, 0.0f, 0.3f),
                };
            case "fermenter": // a barrel: its base, and its lid
                return Layered(prefab);

            case "piece_cookingstation":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.0f, -0.25f),
                    new SnapPointSpec(1.0f, 0.0f, 0.25f),
                    new SnapPointSpec(-1.0f, 0.0f, -0.25f),
                    new SnapPointSpec(-1.0f, 0.0f, 0.25f),
                    new SnapPointSpec(1.0f, 0.0f, 0.0f),
                    new SnapPointSpec(-1.0f, 0.0f, 0.0f),
                };

            case "piece_cookingstation_iron":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(2.0f, 0.0f, -0.5f),
                    new SnapPointSpec(2.0f, 0.0f, 0.5f),
                    new SnapPointSpec(-2.0f, 0.0f, -0.5f),
                    new SnapPointSpec(-2.0f, 0.0f, 0.5f),
                    new SnapPointSpec(2.0f, 0.0f, 0.0f),
                    new SnapPointSpec(-2.0f, 0.0f, 0.0f),
                };

            /* Fires */
            case "hearth": // already has snappoints but not a center one
                return OriginUnlessPresent(prefab);

            /* Beds */
            case "bed":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.5f, 0.0f, -1.5f),
                    new SnapPointSpec(0.5f, 0.0f, 1.5f),
                    new SnapPointSpec(-0.5f, 0.0f, -1.5f),
                    new SnapPointSpec(-0.5f, 0.0f, 1.5f),
                };

            case "piece_bed02":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.0f, -1.5f),
                    new SnapPointSpec(1.0f, 0.0f, 1.5f),
                    new SnapPointSpec(-1.0f, 0.0f, -1.5f),
                    new SnapPointSpec(-1.0f, 0.0f, 1.5f),
                };

            /* Tables */
            case "piece_table":
            case "piece_blackmarble_table":
                return Layered(prefab,
                    new Vector3(-1.0f, 0.0f, 0.0f),
                    new Vector3(1.0f, 0.0f, 0.0f));

            case "piece_table_round": // round table
                return Layered(prefab,
                    new Vector3(1.25f, 0.0f, 0.0f),
                    new Vector3(0.0f, 0.0f, -1.25f),
                    new Vector3(-1.25f, 0.0f, 0.0f),
                    new Vector3(0.0f, 0.0f, 1.25f));

            case "piece_table_oak": // long heavy table
                return Table(prefab, 3.0f, 0.5f);

            /* Misc */
            case "piece_bathtub": // has snap points but no center one
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.5f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, -1.5f),
                    new SnapPointSpec(-1.5f, 0.0f, 0.0f),
                    new SnapPointSpec(0.0f, 0.0f, 1.5f),
                };

            case "piece_cartographytable":
                return Table(prefab, 1.5f, 1.0f);
            case "piece_spinningwheel":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.0f, -1.0f),
                    new SnapPointSpec(1.0f, 0.0f, 1.0f),
                    new SnapPointSpec(-1.0f, 0.0f, -1.0f),
                    new SnapPointSpec(-1.0f, 0.0f, 1.0f),
                };

            case "piece_stonecutter":
                return Layered(prefab,
                    new Vector3(0.7f, 0.0f, -0.4f),
                    new Vector3(0.7f, 0.0f, 0.4f),
                    new Vector3(-1.5f, 0.0f, -0.6f),
                    new Vector3(-1.5f, 0.0f, 0.6f));

            case "piece_artisanstation":
                return Layered(prefab,
                    new Vector3(1.25f, 0.0f, -0.75f),
                    new Vector3(1.25f, 0.0f, 0.5f),
                    new Vector3(-1.25f, 0.0f, -0.75f),
                    new Vector3(-1.25f, 0.0f, 0.5f));

            case "piece_barber":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.0f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, 0.0f, -0.75f),
                    new SnapPointSpec(1.0f, 0.0f, 0.75f),
                    new SnapPointSpec(-1.0f, 0.0f, -0.75f),
                    new SnapPointSpec(-1.0f, 0.0f, 0.75f),
                };

            case "piece_wisplure": // wisp fountain
                return new[]
                {
                    new SnapPointSpec(0.0f, -0.05f, 0.0f),
                };

            case "eitrrefinery":
                return new[]
                {
                    new SnapPointSpec(0.0f, -0.005f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(2.75f, -0.005f, -1.0f),
                    new SnapPointSpec(2.75f, -0.005f, 1.0f),
                    new SnapPointSpec(-2.75f, -0.005f, -1.0f),
                    new SnapPointSpec(-2.75f, -0.005f, 1.0f),
                };

            case "windmill":
                return new[]
                {
                    new SnapPointSpec(0.0f, -0.005f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(2.0f, -0.005f, -2.0f),
                    new SnapPointSpec(2.0f, -0.005f, 2.0f),
                    new SnapPointSpec(-2.0f, -0.005f, -2.0f),
                    new SnapPointSpec(-2.0f, -0.005f, 2.0f),
                };

            case "smelter":
                return new[]
                {
                    new SnapPointSpec(0.0f, -0.05f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.0f, -0.05f, -1.0f),
                    new SnapPointSpec(1.0f, -0.05f, 1.0f),
                    new SnapPointSpec(-1.0f, -0.05f, -1.0f),
                    new SnapPointSpec(-1.0f, -0.05f, 1.0f),
                };

            case "blastfurnace":
                return new[]
                {
                    new SnapPointSpec(0.0f, -0.05f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(2.0f, -0.05f, -1.25f),
                    new SnapPointSpec(2.0f, -0.05f, 1.25f),
                    new SnapPointSpec(-1.75f, -0.05f, -1.25f),
                    new SnapPointSpec(-1.75f, -0.05f, 1.25f),
                };

            case "Piece_grausten_pillarbase_tapered":
                return new[]
                {
                    new SnapPointSpec(0.0f, 0.75f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(0.0f, 0.5f, 0.0f, SnapRole.Center)
                };

            /* Portals */
            case "portal_wood":
                return new[]
                {
                    new SnapPointSpec(0.0f, -0.05f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.5f, -0.05f, 0.0f),
                    new SnapPointSpec(-1.5f, -0.05f, 0.0f)
                };

            case "portal_stone":
                return new[]
                {
                    new SnapPointSpec(0.0f, -0.25f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(3.5f, -0.25f, 0.0f),
                    new SnapPointSpec(-3.5f, -0.25f, 0.0f),
                    new SnapPointSpec(1.5f, -0.25f, 1.5f),
                    new SnapPointSpec(-1.5f, -0.25f, 1.5f),
                    new SnapPointSpec(1.5f, -0.25f, -1.5f),
                    new SnapPointSpec(-1.5f, -0.25f, -1.5f),
                };

            case "portal":  // enabled by MVBP
                return new[]
                {
                    new SnapPointSpec(0.0f, -0.25f, 0.0f, SnapRole.Center),
                    new SnapPointSpec(1.5f, -0.25f, 0.0f),
                    new SnapPointSpec(-1.5f, -0.25f, 0.0f),
                    new SnapPointSpec(1.5f, -0.25f, 1.5f),
                    new SnapPointSpec(-1.5f, -0.25f, 1.5f),
                    new SnapPointSpec(1.5f, -0.25f, -1.5f),
                    new SnapPointSpec(-1.5f, -0.25f, -1.5f),
                };

            /* MVBP */
            case "Grausten_Floor_4x4":
                List<Vector3> pts = new();
                for (float y = -0.5f; y <= 0.5f; y += 1)
                {
                    for (int x = -2; x <= 2; x += 1)
                    {
                        for (int z = -2; z <= 2; z += 1)
                        {
                            pts.Add(new Vector3(x, y, z));
                        }
                    }
                }
                return Points(pts);

            default:
                return null;
        }
    }

    /// <summary>An origin point, unless the piece already has one.</summary>
    private static SnapPointSpec[] OriginUnlessPresent(GameObject prefab)
    {
        return prefab.HasOriginSnapPoint() ? SnapPointBuilder.Empty : SnapPointBuilder.OriginOnly;
    }

    /// <summary>A rectangular table: see <see cref="Layered" />.</summary>
    private static SnapPointSpec[] Table(GameObject prefab, float halfLength, float halfWidth)
    {
        return Layered(prefab,
            new Vector3(halfLength, 0f, -halfWidth),
            new Vector3(halfLength, 0f, halfWidth),
            new Vector3(-halfLength, 0f, -halfWidth),
            new Vector3(-halfLength, 0f, halfWidth));
    }

    /// <summary>A flat-topped piece with its origin at its base: the center and footprint at the base and at the measured collider top.</summary>
    private static SnapPointSpec[] Layered(GameObject prefab, params Vector3[] footprint)
    {
        float top = SnapPointGeometry.SolidExtent(prefab, out Bounds solid) ? solid.max.y : 0f;

        if (top <= 0f)
        {
            Log.LogWarning($"{prefab.name} has no collider to take a top height from; only its base gets snap points.");
        }

        float[] levels = top > 0f ? new[] { 0f, top } : new[] { 0f };
        List<SnapPointSpec> points = new(levels.Length * (footprint.Length + 1));

        foreach (float y in levels)
        {
            points.Add(new SnapPointSpec(new Vector3(0f, y, 0f), SnapRole.Center));

            foreach (Vector3 corner in footprint)
            {
                points.Add(new SnapPointSpec(new Vector3(corner.x, y, corner.z)));
            }
        }

        Log.LogInfo($"{prefab.name}: top at {top:0.##} m.", Log.InfoLevel.Medium);
        return points.ToArray();
    }

    /// <summary>Columns of points at both ends and the middle along X, one per height step.</summary>
    private static IEnumerable<Vector3> Columns(float halfLength, float bottom, float top, float step)
    {
        int steps = Mathf.RoundToInt((top - bottom) / step);

        for (int i = 0; i <= steps; i++)
        {
            float y = bottom + i * step;
            yield return new Vector3(halfLength, y, 0f);
            yield return new Vector3(-halfLength, y, 0f);
            yield return new Vector3(0f, y, 0f);
        }
    }

    /// <summary>Point pairs, one per end, with each pair's midpoint added.</summary>
    private static Vector3[] WithMiddles(Vector3[] pairs)
    {
        List<Vector3> result = new(pairs.Length * 3 / 2);

        for (int i = 0; i + 1 < pairs.Length; i += 2)
        {
            result.Add(pairs[i]);
            result.Add(pairs[i + 1]);
            result.Add((pairs[i] + pairs[i + 1]) * 0.5f);
        }

        return result.ToArray();
    }

    private static SnapPointSpec[] Points(IEnumerable<Vector3> positions)
    {
        return positions.Select(position => new SnapPointSpec(position)).ToArray();
    }
}
