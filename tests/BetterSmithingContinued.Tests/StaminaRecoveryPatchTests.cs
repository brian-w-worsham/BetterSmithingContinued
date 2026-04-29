using System.Reflection;
using BetterSmithingContinued.Patches;
using HarmonyLib;
using Xunit;

namespace BetterSmithingContinued.Tests
{
    /// <summary>
    /// Tests for <see cref="StaminaRecoveryPatch"/> — pure helper logic that
    /// does not require constructing in-game objects.
    /// </summary>
    public class StaminaRecoveryPatchTests
    {
        // ── ApplyMultiplier ───────────────────────────────────────────

        [Theory]
        [InlineData(10f, 1f, 10f)]
        [InlineData(10f, 0f, 0f)]
        [InlineData(10f, 5f, 50f)]
        [InlineData(0f, 5f, 0f)]
        [InlineData(2.5f, 4f, 10f)]
        public void ApplyMultiplier_ScalesAsExpected(float original, float multiplier, float expected)
        {
            Assert.Equal(expected, StaminaRecoveryPatch.ApplyMultiplier(original, multiplier));
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(-1f)]
        public void ApplyMultiplier_PreservesOriginal_OnInvalidMultiplier(float invalidMultiplier)
        {
            Assert.Equal(7f, StaminaRecoveryPatch.ApplyMultiplier(7f, invalidMultiplier));
        }

        [Fact]
        public void ApplyMultiplier_ClampsNegativeResult_ToZero()
        {
            // A negative `original` paired with a positive multiplier would produce
            // a negative scaled value; the patch must clamp to zero.
            Assert.Equal(0f, StaminaRecoveryPatch.ApplyMultiplier(-5f, 2f));
        }

        // ── ResolveMultiplier ─────────────────────────────────────────

        [Fact]
        public void ResolveMultiplier_Returns_One_WhenSettingsNull()
        {
            Assert.Equal(1f, StaminaRecoveryPatch.ResolveMultiplier(null, null));
        }

        [Fact]
        public void ResolveMultiplier_Returns_OutsideMultiplier_WhenHeroNull()
        {
            // A null hero is treated as not-in-settlement, so the wilderness
            // multiplier applies. This guards against NullReferenceExceptions.
            var settings = new BetterSmithingSettings
            {
                StaminaRecoveryMultiplierInTowns = 2f,
                StaminaRecoveryMultiplierOutsideTowns = 9f,
            };

            Assert.Equal(9f, StaminaRecoveryPatch.ResolveMultiplier(null, settings));
        }

        [Fact]
        public void ResolveMultiplier_Uses_ValidatedValues()
        {
            // Negative input → falls back to default (5f).
            var settings = new BetterSmithingSettings
            {
                StaminaRecoveryMultiplierInTowns = -3f,
                StaminaRecoveryMultiplierOutsideTowns = float.NaN,
            };

            Assert.Equal(5f, StaminaRecoveryPatch.ResolveMultiplier(null, settings));
        }

        // ── IsHeroInSettlement ────────────────────────────────────────

        [Fact]
        public void IsHeroInSettlement_ReturnsFalse_WhenHeroNull()
        {
            Assert.False(StaminaRecoveryPatch.IsHeroInSettlement(null));
        }

        // ── Patch metadata ────────────────────────────────────────────

        [Fact]
        public void Patch_TargetsGetSmithingStaminaIncreasePerHour()
        {
            var attr = typeof(StaminaRecoveryPatch).GetCustomAttribute<HarmonyPatch>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultSmithingModel), attr.info.declaringType);
            Assert.Equal("GetSmithingStaminaIncreasePerHour", attr.info.methodName);
        }

        [Fact]
        public void Patch_HasPostfix_With_CorrectSignature()
        {
            var postfix = typeof(StaminaRecoveryPatch).GetMethod("Postfix",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(postfix);
            Assert.Equal(typeof(void), postfix.ReturnType);

            var ps = postfix.GetParameters();
            Assert.Equal(2, ps.Length);

            // First param: hero
            Assert.Equal("hero", ps[0].Name);
            Assert.Equal(typeof(TaleWorlds.CampaignSystem.Hero), ps[0].ParameterType);

            // Second param: ref float __result
            Assert.Equal("__result", ps[1].Name);
            Assert.True(ps[1].ParameterType.IsByRef);
            Assert.Equal(typeof(float), ps[1].ParameterType.GetElementType());
        }

        [Fact]
        public void Patch_IsInternalStatic()
        {
            var t = typeof(StaminaRecoveryPatch);
            Assert.True(t.IsAbstract && t.IsSealed);
            Assert.False(t.IsPublic);
        }
    }
}
