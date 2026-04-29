using System;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BetterSmithingContinued
{
    /// <summary>
    /// Entry point for the Better Smithing Continued mod.
    /// Loads settings, applies all Harmony patches on load, and reverts them on unload.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        internal const string HarmonyId = "com.bettersmithingcontinued.bannerlord";

        private Harmony _harmony;

        /// <summary>
        /// Called when the module is first loaded by the game. Reads the settings
        /// file and applies all Harmony patches.
        /// </summary>
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                BetterSmithingSettings settings = BetterSmithingSettings.LoadFromDefaultPath();
                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll();

                string staminaState = settings.UnlimitedCraftingStamina ? "Unlimited stamina ON" : "Unlimited stamina OFF";
                float inTown = settings.GetValidatedStaminaRecoveryMultiplierInTowns();
                float outside = settings.GetValidatedStaminaRecoveryMultiplierOutsideTowns();

                InformationManager.DisplayMessage(new InformationMessage(
                    $"Better Smithing Continued: Loaded. {staminaState}. Recovery {inTown:0.##}x in towns, {outside:0.##}x outside.",
                    Colors.Green));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Better Smithing Continued load error: {ex.Message}", Colors.Red));
            }
        }

        /// <summary>
        /// Called when the module is unloaded. Reverts every Harmony patch
        /// applied by this mod.
        /// </summary>
        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            _harmony?.UnpatchAll(HarmonyId);
        }
    }
}
