using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace BetterSmithingContinued.Patches
{
    /// <summary>
    /// Patches <see cref="DefaultSmithingModel.GetEnergyCostForRefining"/> to
    /// return zero when <see cref="BetterSmithingSettings.UnlimitedCraftingStamina"/>
    /// is enabled.
    /// </summary>
    [HarmonyPatch(typeof(DefaultSmithingModel), "GetEnergyCostForRefining",
        new[] { typeof(Crafting.RefiningFormula), typeof(Hero) },
        new[] { ArgumentType.Ref, ArgumentType.Normal })]
    internal static class RefiningEnergyCostPatch
    {
        /// <param name="__result">Replaced output of the original method when skipped.</param>
        /// <returns><c>false</c> to skip the original method when unlimited stamina is on; otherwise <c>true</c>.</returns>
        internal static bool Prefix(ref int __result)
        {
            if (!BetterSmithingSettings.Current.UnlimitedCraftingStamina)
            {
                return true;
            }

            __result = 0;
            return false;
        }
    }

    /// <summary>
    /// Patches <see cref="DefaultSmithingModel.GetEnergyCostForSmithing"/> to
    /// return zero when <see cref="BetterSmithingSettings.UnlimitedCraftingStamina"/>
    /// is enabled.
    /// </summary>
    [HarmonyPatch(typeof(DefaultSmithingModel), "GetEnergyCostForSmithing",
        new[] { typeof(ItemObject), typeof(Hero) })]
    internal static class SmithingEnergyCostPatch
    {
        /// <param name="__result">Replaced output of the original method when skipped.</param>
        /// <returns><c>false</c> to skip the original method when unlimited stamina is on; otherwise <c>true</c>.</returns>
        internal static bool Prefix(ref int __result)
        {
            if (!BetterSmithingSettings.Current.UnlimitedCraftingStamina)
            {
                return true;
            }

            __result = 0;
            return false;
        }
    }

    /// <summary>
    /// Patches <see cref="DefaultSmithingModel.GetEnergyCostForSmelting"/> to
    /// return zero when <see cref="BetterSmithingSettings.UnlimitedCraftingStamina"/>
    /// is enabled.
    /// </summary>
    [HarmonyPatch(typeof(DefaultSmithingModel), "GetEnergyCostForSmelting",
        new[] { typeof(ItemObject), typeof(Hero) })]
    internal static class SmeltingEnergyCostPatch
    {
        /// <param name="__result">Replaced output of the original method when skipped.</param>
        /// <returns><c>false</c> to skip the original method when unlimited stamina is on; otherwise <c>true</c>.</returns>
        internal static bool Prefix(ref int __result)
        {
            if (!BetterSmithingSettings.Current.UnlimitedCraftingStamina)
            {
                return true;
            }

            __result = 0;
            return false;
        }
    }
}
