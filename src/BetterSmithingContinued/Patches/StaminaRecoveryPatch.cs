using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace BetterSmithingContinued.Patches
{
    /// <summary>
    /// Patches <see cref="DefaultSmithingModel.GetSmithingStaminaIncreasePerHour"/>
    /// to multiply hourly smithing stamina recovery by the configured multiplier.
    /// Uses the in-town multiplier when the hero's party is inside a settlement,
    /// otherwise the wilderness multiplier.
    /// </summary>
    [HarmonyPatch(typeof(DefaultSmithingModel), "GetSmithingStaminaIncreasePerHour",
        new[] { typeof(Hero) })]
    internal static class StaminaRecoveryPatch
    {
        /// <param name="hero">The hero whose hourly stamina recovery is being computed.</param>
        /// <param name="__result">Hourly stamina recovery returned by the original method; multiplied in place.</param>
        internal static void Postfix(Hero hero, ref float __result)
        {
            float multiplier = ResolveMultiplier(hero, BetterSmithingSettings.Current);
            __result = ApplyMultiplier(__result, multiplier);
        }

        /// <summary>
        /// Returns the recovery multiplier appropriate for the hero's current
        /// party location: in-town when their party is in a settlement, else
        /// wilderness. Defaults to the wilderness multiplier when location
        /// cannot be determined.
        /// </summary>
        internal static float ResolveMultiplier(Hero hero, BetterSmithingSettings settings)
        {
            if (settings == null)
            {
                return 1f;
            }

            float inTowns = settings.GetValidatedStaminaRecoveryMultiplierInTowns();
            float outside = settings.GetValidatedStaminaRecoveryMultiplierOutsideTowns();

            return IsHeroInSettlement(hero) ? inTowns : outside;
        }

        /// <summary>
        /// Applies the multiplier to the original recovery value, guarding
        /// against negative results.
        /// </summary>
        internal static float ApplyMultiplier(float originalRecovery, float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
            {
                return originalRecovery;
            }

            float scaled = originalRecovery * multiplier;
            return scaled < 0f ? 0f : scaled;
        }

        /// <summary>
        /// Returns true when the hero's current party is located in a settlement.
        /// Returns false when the hero is null, has no party, or is in the open map.
        /// </summary>
        internal static bool IsHeroInSettlement(Hero hero)
        {
            if (hero == null)
            {
                return false;
            }

            MobileParty party = hero.PartyBelongedTo;
            if (party == null)
            {
                // Hero has no mobile party (e.g. wanderer in a town); fall back
                // to the hero's own current settlement reference.
                return hero.CurrentSettlement != null;
            }

            return party.CurrentSettlement != null;
        }
    }
}
