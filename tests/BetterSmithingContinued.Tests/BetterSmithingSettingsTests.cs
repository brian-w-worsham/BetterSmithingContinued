using System.IO;
using Xunit;

namespace BetterSmithingContinued.Tests
{
    /// <summary>
    /// Tests for <see cref="BetterSmithingSettings"/> — defaults, parsing,
    /// validation, and resilient fallback behavior.
    /// </summary>
    public class BetterSmithingSettingsTests
    {
        // ── Defaults ──────────────────────────────────────────────────

        [Fact]
        public void Defaults_Match_DocumentedValues()
        {
            var settings = new BetterSmithingSettings();

            Assert.True(settings.UnlimitedCraftingStamina);
            Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierInTowns);
            Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierOutsideTowns);
            Assert.True(settings.AddWeaponTierPrefixes);
            Assert.False(settings.UseOwnPrefixesOnly);
            Assert.True(settings.BatchOperationsEnabled);
            Assert.Equal(5, settings.ShiftMultiplier);
            Assert.Equal(10, settings.CtrlMultiplier);
            Assert.Equal(0, settings.CtrlShiftMultiplier);
        }

        [Fact]
        public void Defaults_Constants_AreExposed()
        {
            Assert.True(BetterSmithingSettings.DefaultUnlimitedCraftingStamina);
            Assert.Equal(5.0f, BetterSmithingSettings.DefaultStaminaRecoveryMultiplierInTowns);
            Assert.Equal(5.0f, BetterSmithingSettings.DefaultStaminaRecoveryMultiplierOutsideTowns);
            Assert.True(BetterSmithingSettings.DefaultAddWeaponTierPrefixes);
            Assert.False(BetterSmithingSettings.DefaultUseOwnPrefixesOnly);
            Assert.True(BetterSmithingSettings.DefaultBatchOperationsEnabled);
            Assert.Equal(5, BetterSmithingSettings.DefaultShiftMultiplier);
            Assert.Equal(10, BetterSmithingSettings.DefaultCtrlMultiplier);
            Assert.Equal(0, BetterSmithingSettings.DefaultCtrlShiftMultiplier);
        }

        [Fact]
        public void SettingsFileName_IsExpected()
        {
            Assert.Equal("BetterSmithingContinued.settings.xml", BetterSmithingSettings.SettingsFileName);
        }

        // ── Load: missing / invalid input ────────────────────────────

        [Fact]
        public void Load_Returns_Defaults_When_PathIsNull()
        {
            var settings = BetterSmithingSettings.Load(null);

            Assert.True(settings.UnlimitedCraftingStamina);
            Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierInTowns);
            Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierOutsideTowns);
        }

        [Fact]
        public void Load_Returns_Defaults_When_PathIsWhitespace()
        {
            var settings = BetterSmithingSettings.Load("   ");

            Assert.True(settings.UnlimitedCraftingStamina);
        }

        [Fact]
        public void Load_Returns_Defaults_When_FileDoesNotExist()
        {
            string missingPath = Path.Combine(Path.GetTempPath(), "bsc-does-not-exist-" + Path.GetRandomFileName());

            var settings = BetterSmithingSettings.Load(missingPath);

            Assert.True(settings.UnlimitedCraftingStamina);
            Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierInTowns);
        }

        [Fact]
        public void Load_Returns_Defaults_When_FileIsMalformed()
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, "<<not-valid-xml>>");

                var settings = BetterSmithingSettings.Load(path);

                Assert.True(settings.UnlimitedCraftingStamina);
                Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierInTowns);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // ── Load: valid input ────────────────────────────────────────

        [Fact]
        public void Load_Parses_AllValuesFromXml()
        {
            string path = WriteSettings("<BetterSmithingContinuedSettings>" +
                "<UnlimitedCraftingStamina>false</UnlimitedCraftingStamina>" +
                "<StaminaRecoveryMultiplierInTowns>3.5</StaminaRecoveryMultiplierInTowns>" +
                "<StaminaRecoveryMultiplierOutsideTowns>2.25</StaminaRecoveryMultiplierOutsideTowns>" +
                "</BetterSmithingContinuedSettings>");
            try
            {
                var settings = BetterSmithingSettings.Load(path);

                Assert.False(settings.UnlimitedCraftingStamina);
                Assert.Equal(3.5f, settings.StaminaRecoveryMultiplierInTowns);
                Assert.Equal(2.25f, settings.StaminaRecoveryMultiplierOutsideTowns);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Load_Uses_InvariantCulture_ForDecimalPoint()
        {
            // Even on locales using "," as decimal separator, "1.75" must parse.
            string path = WriteSettings("<BetterSmithingContinuedSettings>" +
                "<StaminaRecoveryMultiplierInTowns>1.75</StaminaRecoveryMultiplierInTowns>" +
                "</BetterSmithingContinuedSettings>");
            try
            {
                var settings = BetterSmithingSettings.Load(path);
                Assert.Equal(1.75f, settings.StaminaRecoveryMultiplierInTowns);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Load_FallsBack_When_MultiplierIsUnparseable()
        {
            string path = WriteSettings("<BetterSmithingContinuedSettings>" +
                "<StaminaRecoveryMultiplierInTowns>not-a-number</StaminaRecoveryMultiplierInTowns>" +
                "</BetterSmithingContinuedSettings>");
            try
            {
                var settings = BetterSmithingSettings.Load(path);
                Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierInTowns);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Load_FallsBack_When_BoolIsUnparseable()
        {
            string path = WriteSettings("<BetterSmithingContinuedSettings>" +
                "<UnlimitedCraftingStamina>maybe</UnlimitedCraftingStamina>" +
                "</BetterSmithingContinuedSettings>");
            try
            {
                var settings = BetterSmithingSettings.Load(path);
                Assert.True(settings.UnlimitedCraftingStamina);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Load_NegativeMultiplier_FallsBack_ToDefault()
        {
            string path = WriteSettings("<BetterSmithingContinuedSettings>" +
                "<StaminaRecoveryMultiplierOutsideTowns>-2.0</StaminaRecoveryMultiplierOutsideTowns>" +
                "</BetterSmithingContinuedSettings>");
            try
            {
                var settings = BetterSmithingSettings.Load(path);
                Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierOutsideTowns);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Load_ZeroMultiplier_IsAccepted()
        {
            // 0 disables recovery entirely, which BSC treated as a valid setting.
            string path = WriteSettings("<BetterSmithingContinuedSettings>" +
                "<StaminaRecoveryMultiplierInTowns>0</StaminaRecoveryMultiplierInTowns>" +
                "</BetterSmithingContinuedSettings>");
            try
            {
                var settings = BetterSmithingSettings.Load(path);
                Assert.Equal(0f, settings.StaminaRecoveryMultiplierInTowns);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Load_PartialFile_UsesDefaults_ForMissingFields()
        {
            string path = WriteSettings("<BetterSmithingContinuedSettings>" +
                "<UnlimitedCraftingStamina>false</UnlimitedCraftingStamina>" +
                "</BetterSmithingContinuedSettings>");
            try
            {
                var settings = BetterSmithingSettings.Load(path);
                Assert.False(settings.UnlimitedCraftingStamina);
                Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierInTowns);
                Assert.Equal(5.0f, settings.StaminaRecoveryMultiplierOutsideTowns);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // ── Validation ────────────────────────────────────────────────

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(0.5f, 0.5f)]
        [InlineData(5f, 5f)]
        [InlineData(100f, 100f)]
        public void ValidateMultiplier_Accepts_NonNegativeFinite(float input, float expected)
        {
            Assert.Equal(expected, BetterSmithingSettings.ValidateMultiplier(input, 5f));
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(-0.0001f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void ValidateMultiplier_FallsBack_ForInvalidValues(float input)
        {
            Assert.Equal(5f, BetterSmithingSettings.ValidateMultiplier(input, 5f));
        }

        [Fact]
        public void GetValidated_Methods_ReturnSanitizedValues()
        {
            var settings = new BetterSmithingSettings
            {
                StaminaRecoveryMultiplierInTowns = -7f,
                StaminaRecoveryMultiplierOutsideTowns = float.NaN,
            };

            Assert.Equal(5.0f, settings.GetValidatedStaminaRecoveryMultiplierInTowns());
            Assert.Equal(5.0f, settings.GetValidatedStaminaRecoveryMultiplierOutsideTowns());
        }

        // ── Default settings file path ────────────────────────────────

        [Fact]
        public void GetDefaultSettingsFilePath_EndsWithSettingsFileName()
        {
            string path = BetterSmithingSettings.GetDefaultSettingsFilePath();
            Assert.EndsWith(BetterSmithingSettings.SettingsFileName, path);
        }

        // ── Static Current ────────────────────────────────────────────

        [Fact]
        public void Current_IsAlwaysNonNull()
        {
            Assert.NotNull(BetterSmithingSettings.Current);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static string WriteSettings(string xml)
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, "<?xml version=\"1.0\"?>" + xml);
            return path;
        }
    }
}
