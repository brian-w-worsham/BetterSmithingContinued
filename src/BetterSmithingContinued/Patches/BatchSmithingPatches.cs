using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace BetterSmithingContinued.Patches
{
    /// <summary>
    /// Shared helpers for the batch refine / smelt / craft postfix patches.
    /// Mirrors the original BSC <c>RefiningRepeater</c>/<c>SmeltingRepeater</c>/<c>CraftingRepeater</c>
    /// modifier-key model: holding Shift/Ctrl/Ctrl+Shift while clicking the
    /// vanilla button repeats the action up to the configured count, stopping
    /// when materials or stamina run out.
    /// </summary>
    internal static class BatchSmithing
    {
        /// <summary>
        /// Returns the total number of operations the player asked for based on the held modifier keys
        /// (Shift / Ctrl / Ctrl+Shift). Returns <c>1</c> when no modifier is held or the feature is off,
        /// meaning vanilla single-shot behaviour. A configured count of <c>0</c> is interpreted as
        /// "unlimited" and translated to <see cref="BetterSmithingSettings.MaxBatchIterations"/>.
        /// </summary>
        internal static int ResolveDesiredCount(BetterSmithingSettings settings)
        {
            if (settings == null || !settings.BatchOperationsEnabled)
            {
                return 1;
            }

            bool ctrl = SafeIsKeyDown(InputKey.LeftControl) || SafeIsKeyDown(InputKey.RightControl);
            bool shift = SafeIsKeyDown(InputKey.LeftShift) || SafeIsKeyDown(InputKey.RightShift);

            if (!ctrl && !shift)
            {
                return 1;
            }

            int configured;
            if (ctrl && shift)
            {
                configured = settings.CtrlShiftMultiplier;
            }
            else if (ctrl)
            {
                configured = settings.CtrlMultiplier;
            }
            else
            {
                configured = settings.ShiftMultiplier;
            }

            if (configured <= 0)
            {
                return BetterSmithingSettings.MaxBatchIterations;
            }

            // Cap to the safety ceiling regardless of configured value.
            return Math.Min(configured, BetterSmithingSettings.MaxBatchIterations);
        }

        /// <summary>
        /// Wraps <see cref="Input.IsKeyDown"/> in a try/catch so headless test environments
        /// (where <c>InputManager</c> is not initialized) do not throw.
        /// </summary>
        internal static bool SafeIsKeyDown(InputKey key)
        {
            try
            {
                return Input.IsKeyDown(key);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true when the hero has enough crafting stamina to perform one more
        /// operation costing <paramref name="cost"/>. When unlimited stamina is on,
        /// always returns true.
        /// </summary>
        internal static bool HasStaminaFor(CraftingCampaignBehavior behavior, Hero hero, int cost, BetterSmithingSettings settings)
        {
            if (settings != null && settings.UnlimitedCraftingStamina)
            {
                return true;
            }

            if (behavior == null || hero == null)
            {
                return false;
            }

            return behavior.GetHeroCraftingStamina(hero) >= cost;
        }

        /// <summary>
        /// Reports a "performed N operations" message (info green) or an error
        /// (red) when batch processing aborts. Safe to call when the campaign
        /// message system is not available — falls back to silent no-op.
        /// </summary>
        internal static void DisplayBatchResult(string verb, int count)
        {
            if (count <= 1)
            {
                // Vanilla one-shot — nothing to report.
                return;
            }

            try
            {
                var msg = new TaleWorlds.Library.InformationMessage(
                    "Better Smithing Continued: " + verb + " " + count + " items.",
                    TaleWorlds.Library.Colors.Green);
                TaleWorlds.Library.InformationManager.DisplayMessage(msg);
            }
            catch
            {
                // No-op — UI may not be ready.
            }
        }
    }

    /// <summary>
    /// Postfix on <see cref="CraftingCampaignBehavior.DoRefinement"/>. After
    /// the vanilla single refine completes, repeats the same refine up to the
    /// configured count when Shift/Ctrl is held, stopping when input materials
    /// or hero stamina are exhausted.
    /// </summary>
    [HarmonyPatch(typeof(CraftingCampaignBehavior), "DoRefinement",
        new[] { typeof(Hero), typeof(Crafting.RefiningFormula) })]
    internal static class BatchRefinePatch
    {
        // Re-entrancy guard: the postfix calls DoRefinement again, which would
        // re-trigger this same postfix without this flag.
        [ThreadStatic]
        private static bool _inProgress;

        internal static void Postfix(CraftingCampaignBehavior __instance, Hero hero, Crafting.RefiningFormula refineFormula)
        {
            if (_inProgress || __instance == null || hero == null)
            {
                return;
            }

            BetterSmithingSettings settings = BetterSmithingSettings.Current;
            int desired = BatchSmithing.ResolveDesiredCount(settings);
            if (desired <= 1)
            {
                return;
            }

            _inProgress = true;
            int performed = 1; // vanilla call already did one
            try
            {
                for (int i = 1; i < desired; i++)
                {
                    if (!HasInputs(refineFormula))
                    {
                        break;
                    }

                    int cost = Campaign.Current.Models.SmithingModel.GetEnergyCostForRefining(ref refineFormula, hero);
                    if (!BatchSmithing.HasStaminaFor(__instance, hero, cost, settings))
                    {
                        break;
                    }

                    __instance.DoRefinement(hero, refineFormula);
                    performed++;
                }
            }
            catch
            {
                // Swallow: never break the player's smithing screen on a batch error.
            }
            finally
            {
                _inProgress = false;
                BatchSmithing.DisplayBatchResult("Refined", performed);
            }
        }

        private static bool HasInputs(Crafting.RefiningFormula refineFormula)
        {
            try
            {
                var roster = MobileParty.MainParty?.ItemRoster;
                if (roster == null)
                {
                    return false;
                }

                var smithingModel = Campaign.Current.Models.SmithingModel;
                if (refineFormula.Input1Count > 0)
                {
                    var item = smithingModel.GetCraftingMaterialItem(refineFormula.Input1);
                    if (item == null || roster.GetItemNumber(item) < refineFormula.Input1Count)
                    {
                        return false;
                    }
                }

                if (refineFormula.Input2Count > 0)
                {
                    var item = smithingModel.GetCraftingMaterialItem(refineFormula.Input2);
                    if (item == null || roster.GetItemNumber(item) < refineFormula.Input2Count)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Postfix on <see cref="CraftingCampaignBehavior.DoSmelting"/>. After the
    /// vanilla single smelt completes, repeats the same smelt up to the
    /// configured count when Shift/Ctrl is held, stopping when no more
    /// matching items are in the inventory or hero stamina is exhausted.
    /// </summary>
    [HarmonyPatch(typeof(CraftingCampaignBehavior), "DoSmelting",
        new[] { typeof(Hero), typeof(EquipmentElement) })]
    internal static class BatchSmeltPatch
    {
        [ThreadStatic]
        private static bool _inProgress;

        internal static void Postfix(CraftingCampaignBehavior __instance, Hero currentCraftingHero, EquipmentElement equipmentElement)
        {
            if (_inProgress || __instance == null || currentCraftingHero == null || equipmentElement.Item == null)
            {
                return;
            }

            BetterSmithingSettings settings = BetterSmithingSettings.Current;
            int desired = BatchSmithing.ResolveDesiredCount(settings);
            if (desired <= 1)
            {
                return;
            }

            _inProgress = true;
            int performed = 1;
            try
            {
                for (int i = 1; i < desired; i++)
                {
                    var roster = MobileParty.MainParty?.ItemRoster;
                    if (roster == null || roster.GetItemNumber(equipmentElement.Item) <= 0)
                    {
                        break;
                    }

                    int cost = Campaign.Current.Models.SmithingModel.GetEnergyCostForSmelting(equipmentElement.Item, currentCraftingHero);
                    if (!BatchSmithing.HasStaminaFor(__instance, currentCraftingHero, cost, settings))
                    {
                        break;
                    }

                    __instance.DoSmelting(currentCraftingHero, equipmentElement);
                    performed++;
                }
            }
            catch
            {
                // Silent.
            }
            finally
            {
                _inProgress = false;
                BatchSmithing.DisplayBatchResult("Smelted", performed);
            }
        }
    }

    /// <summary>
    /// Postfix on <see cref="CraftingCampaignBehavior.CreateCraftedWeaponInFreeBuildMode"/>.
    /// After the vanilla single craft completes, repeats the same craft up to
    /// the configured count when Shift/Ctrl is held, stopping when hero stamina
    /// is exhausted.
    /// </summary>
    [HarmonyPatch(typeof(CraftingCampaignBehavior), "CreateCraftedWeaponInFreeBuildMode",
        new[] { typeof(Hero), typeof(WeaponDesign), typeof(ItemModifier) })]
    internal static class BatchCraftPatch
    {
        [ThreadStatic]
        private static bool _inProgress;

        internal static void Postfix(CraftingCampaignBehavior __instance, ItemObject __result, Hero hero, WeaponDesign weaponDesign, ItemModifier weaponModifier)
        {
            if (_inProgress || __instance == null || hero == null || weaponDesign == null)
            {
                return;
            }

            BetterSmithingSettings settings = BetterSmithingSettings.Current;
            int desired = BatchSmithing.ResolveDesiredCount(settings);
            if (desired <= 1)
            {
                return;
            }

            _inProgress = true;
            int performed = 1;
            try
            {
                // Vanilla just produced one. The crafted weapon's energy cost is
                // computed from the resulting item; reuse __result if present,
                // otherwise skip the stamina check (still capped by desired).
                ItemObject costProbe = __result;

                for (int i = 1; i < desired; i++)
                {
                    if (costProbe != null)
                    {
                        int cost = Campaign.Current.Models.SmithingModel.GetEnergyCostForSmithing(costProbe, hero);
                        if (!BatchSmithing.HasStaminaFor(__instance, hero, cost, settings))
                        {
                            break;
                        }
                    }

                    __instance.CreateCraftedWeaponInFreeBuildMode(hero, weaponDesign, weaponModifier);
                    performed++;
                }
            }
            catch
            {
                // Silent.
            }
            finally
            {
                _inProgress = false;
                BatchSmithing.DisplayBatchResult("Forged", performed);
            }
        }
    }
}
