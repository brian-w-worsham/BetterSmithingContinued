using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace BetterSmithingContinued
{
    /// <summary>
    /// Loads the editable module settings that tune Better Smithing Continued.
    /// </summary>
    /// <remarks>
    /// Settings are read from <c>BetterSmithingContinued.settings.xml</c>
    /// located next to <c>SubModule.xml</c> in the deployed module folder.
    /// </remarks>
    internal sealed class BetterSmithingSettings
    {
        internal const string SettingsFileName = "BetterSmithingContinued.settings.xml";

        internal const bool DefaultUnlimitedCraftingStamina = true;
        internal const float DefaultStaminaRecoveryMultiplierInTowns = 5.0f;
        internal const float DefaultStaminaRecoveryMultiplierOutsideTowns = 5.0f;
        internal const bool DefaultAddWeaponTierPrefixes = true;
        internal const bool DefaultUseOwnPrefixesOnly = false;

        /// <summary>The active settings instance loaded by <see cref="LoadFromDefaultPath"/>.</summary>
        internal static BetterSmithingSettings Current { get; private set; } = new BetterSmithingSettings();

        /// <summary>When true, refining/smelting/smithing all cost zero stamina.</summary>
        public bool UnlimitedCraftingStamina { get; set; } = DefaultUnlimitedCraftingStamina;

        /// <summary>Multiplier applied to hourly smithing stamina recovery while inside a settlement.</summary>
        public float StaminaRecoveryMultiplierInTowns { get; set; } = DefaultStaminaRecoveryMultiplierInTowns;

        /// <summary>Multiplier applied to hourly smithing stamina recovery while in the wilderness.</summary>
        public float StaminaRecoveryMultiplierOutsideTowns { get; set; } = DefaultStaminaRecoveryMultiplierOutsideTowns;

        /// <summary>
        /// When true, prepends a tier word (Rusty/Dull/Balanced/Masterwork/Legendary)
        /// to the displayed name of player-crafted weapons that have an
        /// <see cref="TaleWorlds.Core.ItemModifier"/> applied.
        /// </summary>
        public bool AddWeaponTierPrefixes { get; set; } = DefaultAddWeaponTierPrefixes;

        /// <summary>
        /// When true, the modifier name supplied by the game is ignored and
        /// this mod's own prefix words are always used. When false, the
        /// game's modifier name is preferred and this mod's prefix is only
        /// used when the modifier name would render identically to the bare
        /// item name.
        /// </summary>
        public bool UseOwnPrefixesOnly { get; set; } = DefaultUseOwnPrefixesOnly;

        /// <summary>
        /// Loads settings from the conventional path next to <c>SubModule.xml</c>.
        /// On any failure, falls back to defaults. Sets <see cref="Current"/>.
        /// </summary>
        internal static BetterSmithingSettings LoadFromDefaultPath()
        {
            string settingsFilePath = GetDefaultSettingsFilePath();
            Current = Load(settingsFilePath);
            return Current;
        }

        /// <summary>
        /// Loads settings from the given XML file path. Returns defaults if the
        /// file is missing, blank, or malformed.
        /// </summary>
        internal static BetterSmithingSettings Load(string settingsFilePath)
        {
            if (string.IsNullOrWhiteSpace(settingsFilePath))
            {
                return new BetterSmithingSettings();
            }

            try
            {
                if (!File.Exists(settingsFilePath))
                {
                    return new BetterSmithingSettings();
                }

                XDocument document = XDocument.Load(settingsFilePath);
                XElement root = document.Root;

                return new BetterSmithingSettings
                {
                    UnlimitedCraftingStamina = ParseBool(
                        root?.Element(nameof(UnlimitedCraftingStamina))?.Value,
                        DefaultUnlimitedCraftingStamina),
                    StaminaRecoveryMultiplierInTowns = ParseMultiplier(
                        root?.Element(nameof(StaminaRecoveryMultiplierInTowns))?.Value,
                        DefaultStaminaRecoveryMultiplierInTowns),
                    StaminaRecoveryMultiplierOutsideTowns = ParseMultiplier(
                        root?.Element(nameof(StaminaRecoveryMultiplierOutsideTowns))?.Value,
                        DefaultStaminaRecoveryMultiplierOutsideTowns),
                    AddWeaponTierPrefixes = ParseBool(
                        root?.Element(nameof(AddWeaponTierPrefixes))?.Value,
                        DefaultAddWeaponTierPrefixes),
                    UseOwnPrefixesOnly = ParseBool(
                        root?.Element(nameof(UseOwnPrefixesOnly))?.Value,
                        DefaultUseOwnPrefixesOnly),
                };
            }
            catch
            {
                return new BetterSmithingSettings();
            }
        }

        /// <summary>
        /// Returns the conventional settings file path, located next to the
        /// deployed <c>SubModule.xml</c>.
        /// </summary>
        internal static string GetDefaultSettingsFilePath()
        {
            string assemblyDirectory = Path.GetDirectoryName(typeof(SubModule).Assembly.Location);

            if (string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                assemblyDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            // bin\Win64_Shipping_Client → Module root is two levels up.
            string moduleDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", ".."));

            return Path.Combine(moduleDirectory, SettingsFileName);
        }

        /// <summary>Returns the validated in-town stamina recovery multiplier.</summary>
        internal float GetValidatedStaminaRecoveryMultiplierInTowns()
        {
            return ValidateMultiplier(StaminaRecoveryMultiplierInTowns, DefaultStaminaRecoveryMultiplierInTowns);
        }

        /// <summary>Returns the validated wilderness stamina recovery multiplier.</summary>
        internal float GetValidatedStaminaRecoveryMultiplierOutsideTowns()
        {
            return ValidateMultiplier(StaminaRecoveryMultiplierOutsideTowns, DefaultStaminaRecoveryMultiplierOutsideTowns);
        }

        /// <summary>
        /// Returns a sanitized multiplier: rejects NaN/infinity/negative values
        /// and substitutes the supplied default.
        /// </summary>
        internal static float ValidateMultiplier(float multiplier, float fallback)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
            {
                return fallback;
            }

            return multiplier;
        }

        private static float ParseMultiplier(string raw, float fallback)
        {
            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return fallback;
            }

            return ValidateMultiplier(parsed, fallback);
        }

        private static bool ParseBool(string raw, bool fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            if (bool.TryParse(raw.Trim(), out bool parsed))
            {
                return parsed;
            }

            return fallback;
        }
    }
}
