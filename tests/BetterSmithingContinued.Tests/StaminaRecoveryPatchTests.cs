using System.Reflection;
using BetterSmithingContinued.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using Xunit;

namespace BetterSmithingContinued.Tests
{
    public class StaminaRecoveryPatchTests
    {
        [Theory]
        [InlineData(10, 1.0f, 10)]   // identity
        [InlineData(10, 5.0f, 50)]   // 5x boost
        [InlineData(7, 2.5f, 18)]    // 17.5 -> 18 (banker's rounding away from zero)
        [InlineData(0, 5.0f, 0)]     // zero stays zero
        [InlineData(10, 0f, 0)]      // zero multiplier zeroes recovery
        [InlineData(10, float.NaN, 10)]              // NaN -> original
        [InlineData(10, float.PositiveInfinity, 10)] // +inf -> original
        [InlineData(10, float.NegativeInfinity, 10)] // -inf -> original
        [InlineData(10, -1f, 10)]    // negative -> original
        public void ApplyMultiplier_Returns_Expected(int original, float multiplier, int expected)
        {
            int actual = StaminaRecoveryPatch.ApplyMultiplier(original, multiplier);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ResolveMultiplier_NullSettings_ReturnsOne()
        {
            float multiplier = StaminaRecoveryPatch.ResolveMultiplier(null, null);
            Assert.Equal(1f, multiplier);
        }

        [Fact]
        public void IsHeroInSettlement_NullHero_ReturnsFalse()
        {
            Assert.False(StaminaRecoveryPatch.IsHeroInSettlement(null));
        }

        [Fact]
        public void Patch_Targets_GetStaminaHourlyRecoveryRate_On_CraftingCampaignBehavior()
        {
            HarmonyPatch attr = typeof(StaminaRecoveryPatch).GetCustomAttribute<HarmonyPatch>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(CraftingCampaignBehavior), attr.info.declaringType);
            Assert.Equal("GetStaminaHourlyRecoveryRate", attr.info.methodName);
        }

        [Fact]
        public void Patch_Target_Method_Exists_In_Game_Assembly()
        {
            // Guards against the original bug where the patch targeted a method
            // that no longer exists in the shipping game DLL.
            MethodInfo method = typeof(CraftingCampaignBehavior).GetMethod(
                "GetStaminaHourlyRecoveryRate",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(TaleWorlds.CampaignSystem.Hero) },
                modifiers: null);

            Assert.NotNull(method);
            Assert.Equal(typeof(int), method.ReturnType);
        }
    }
}
