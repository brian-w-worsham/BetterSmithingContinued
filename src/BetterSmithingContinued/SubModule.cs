using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BetterSmithingContinued
{
    /// <summary>
    /// Entry point for the Better Smithing Continued mod.
    /// Loads settings, applies all Harmony patches on load (each independently
    /// so a single failure does not abort the rest), and reverts them on unload.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        internal const string HarmonyId = "com.bettersmithingcontinued.bannerlord";

        private Harmony _harmony;

        /// <summary>
        /// Called when the module is first loaded by the game. Reads the settings
        /// file and applies every Harmony patch type in this assembly individually,
        /// so that one bad target method cannot prevent the others from applying.
        /// </summary>
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                BetterSmithingSettings settings = BetterSmithingSettings.LoadFromDefaultPath();
                _harmony = new Harmony(HarmonyId);

                List<string> failures = ApplyAllPatches(_harmony, typeof(SubModule).Assembly);

                string staminaState = settings.UnlimitedCraftingStamina ? "Unlimited stamina ON" : "Unlimited stamina OFF";
                float inTown = settings.GetValidatedStaminaRecoveryMultiplierInTowns();
                float outside = settings.GetValidatedStaminaRecoveryMultiplierOutsideTowns();

                InformationManager.DisplayMessage(new InformationMessage(
                    $"Better Smithing Continued: Loaded. {staminaState}. Recovery {inTown:0.##}x in towns, {outside:0.##}x outside.",
                    Colors.Green));

                foreach (string failure in failures)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Better Smithing Continued patch skipped: {failure}", Colors.Red));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Better Smithing Continued load error: {ex.Message}", Colors.Red));
            }
        }

        /// <summary>
        /// Iterates every type in the given assembly that is decorated with a
        /// <see cref="HarmonyPatch"/> attribute and applies it via
        /// <see cref="Harmony.CreateClassProcessor(Type)"/>. Failures are
        /// collected and returned rather than thrown so that one missing
        /// target method does not prevent the remaining patches from
        /// applying.
        /// </summary>
        /// <returns>One human-readable error string per patch type that failed to apply.</returns>
        internal static List<string> ApplyAllPatches(Harmony harmony, Assembly assembly)
        {
            var failures = new List<string>();
            if (harmony == null || assembly == null)
            {
                return failures;
            }

            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                {
                    continue;
                }

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.Name}: {ex.Message}");
                }
            }

            return failures;
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
