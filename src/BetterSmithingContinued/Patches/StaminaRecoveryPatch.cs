using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace BetterSmithingContinued.Patches
{
    /// <summary>
    /// Patches <see cref="CraftingCampaignBehavior"/>.<c>GetStaminaHourlyRecoveryRate</c>
    /// (a private static method) to multiply hourly smithing stamina recovery
    /// by the configured multiplier. Uses the in-town multiplier when the
    /// hero's party is inside a settlement, otherwise the wilderness multiplier.
    /// </summary>
    /// <remarks>
    /// The current version of Bannerlord computes hourly recovery in
    /// <c>CraftingCampaignBehavior.GetStaminaHourlyRecoveryRate(Hero)</c>, which
    /// is invoked from <c>HourlyTick</c> for every hero with a crafting record.
    /// Earlier Bannerlord versions exposed this on
    /// <c>DefaultSmithingModel.GetSmithingStaminaIncreasePerHour</c>; that
    /// member no longer exists and patching it now throws at load time.
    /// </remarks>
    [HarmonyPatch(typeof(CraftingCampaignBehavior), "GetStaminaHourlyRecoveryRate",
        new[] { typeof(Hero) })]
    internal static class StaminaRecoveryPatch
    {
        /// <param name="hero">The hero whose hourly stamina recovery is being computed.</param>
        /// <param name="__result">Hourly stamina recovery (int) returned by the original method; multiplied in place.</param>
        internal static void Postfix(Hero hero, ref int __result)
        {
            float multiplier = ResolveMultiplier(hero, BetterSmithingSettings.Current);
            __result = ApplyMultiplier(__result, multiplier);
        }

        /// <summary>
        /// Returns the recovery multiplier appropriate for the hero's current
        /// party location: in-town when their party is in a settlement, else
        /// wilderness. Defaults to <c>1.0</c> when settings are unavailable.
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
        /// against negative results and invalid multipliers. Rounds to nearest int.
        /// </summary>
        internal static int ApplyMultiplier(int originalRecovery, float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
            {
                return originalRecovery;
            }

            float scaled = originalRecovery * multiplier;
            if (scaled < 0f)
            {
                return 0;
            }

            return (int)System.Math.Round(scaled, System.MidpointRounding.AwayFromZero);
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
