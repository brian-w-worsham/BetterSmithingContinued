using System.Reflection;
using BetterSmithingContinued.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using Xunit;

namespace BetterSmithingContinued.Tests
{
    public class WeaponTierPrefixPatchTests
    {
        [Fact]
        public void Patch_Targets_GetModifiedItemName_On_EquipmentElement()
        {
            HarmonyPatch attr = typeof(WeaponTierPrefixPatch).GetCustomAttribute<HarmonyPatch>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(EquipmentElement), attr.info.declaringType);
            Assert.Equal("GetModifiedItemName", attr.info.methodName);
        }

        [Fact]
        public void Patch_Target_Method_Exists_In_Game_Assembly()
        {
            // Same regression guard as StaminaRecoveryPatchTests.
            MethodInfo method = typeof(EquipmentElement).GetMethod(
                "GetModifiedItemName",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.NotNull(method);
            Assert.Equal(typeof(TextObject), method.ReturnType);
        }

        [Fact]
        public void TryApplyTierPrefix_NullSettings_ReturnsOriginal()
        {
            TextObject original = new TextObject("orig");
            TextObject result = WeaponTierPrefixPatch.TryApplyTierPrefix(default, original, null);
            Assert.Same(original, result);
        }

        [Fact]
        public void TryApplyTierPrefix_FeatureDisabled_ReturnsOriginal()
        {
            TextObject original = new TextObject("orig");
            BetterSmithingSettings settings = new BetterSmithingSettings { AddWeaponTierPrefixes = false };
            TextObject result = WeaponTierPrefixPatch.TryApplyTierPrefix(default, original, settings);
            Assert.Same(original, result);
        }

        [Theory]
        [InlineData(ItemQuality.Poor, "Rusty")]
        [InlineData(ItemQuality.Inferior, "Dull")]
        [InlineData(ItemQuality.Fine, "Balanced")]
        [InlineData(ItemQuality.Masterwork, "Masterwork")]
        [InlineData(ItemQuality.Legendary, "Legendary")]
        public void GetTierTemplate_ReturnsTemplateBeginningWithExpectedWord(ItemQuality quality, string expectedWord)
        {
            TextObject template = WeaponTierPrefixPatch.GetTierTemplate(quality);
            Assert.NotNull(template);

            template.SetTextVariable("ITEMNAME", new TextObject(string.Empty));
            string rendered = template.ToString();

            Assert.StartsWith(expectedWord, rendered);
        }

        [Fact]
        public void GetTierTemplate_Common_ReturnsNull()
        {
            Assert.Null(WeaponTierPrefixPatch.GetTierTemplate(ItemQuality.Common));
        }

        [Fact]
        public void BuildPrefixedName_NullItem_ReturnsNull()
        {
            Assert.Null(WeaponTierPrefixPatch.BuildPrefixedName(null, null, false));
        }
    }
}
