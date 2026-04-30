using System.Reflection;
using BetterSmithingContinued.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using Xunit;

namespace BetterSmithingContinued.Tests
{
    public class BatchSmithingPatchTests
    {
        [Fact]
        public void BatchRefinePatch_Targets_DoRefinement_On_CraftingCampaignBehavior()
        {
            HarmonyPatch attr = typeof(BatchRefinePatch).GetCustomAttribute<HarmonyPatch>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(CraftingCampaignBehavior), attr.info.declaringType);
            Assert.Equal("DoRefinement", attr.info.methodName);
        }

        [Fact]
        public void BatchSmeltPatch_Targets_DoSmelting_On_CraftingCampaignBehavior()
        {
            HarmonyPatch attr = typeof(BatchSmeltPatch).GetCustomAttribute<HarmonyPatch>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(CraftingCampaignBehavior), attr.info.declaringType);
            Assert.Equal("DoSmelting", attr.info.methodName);
        }

        [Fact]
        public void BatchCraftPatch_Targets_CreateCraftedWeaponInFreeBuildMode_On_CraftingCampaignBehavior()
        {
            HarmonyPatch attr = typeof(BatchCraftPatch).GetCustomAttribute<HarmonyPatch>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(CraftingCampaignBehavior), attr.info.declaringType);
            Assert.Equal("CreateCraftedWeaponInFreeBuildMode", attr.info.methodName);
        }

        [Fact]
        public void DoRefinement_Method_Exists_In_Game_Assembly()
        {
            MethodInfo m = typeof(CraftingCampaignBehavior).GetMethod(
                "DoRefinement",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(m);
        }

        [Fact]
        public void DoSmelting_Method_Exists_In_Game_Assembly()
        {
            MethodInfo m = typeof(CraftingCampaignBehavior).GetMethod(
                "DoSmelting",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(m);
        }

        [Fact]
        public void CreateCraftedWeaponInFreeBuildMode_Method_Exists_In_Game_Assembly()
        {
            MethodInfo m = typeof(CraftingCampaignBehavior).GetMethod(
                "CreateCraftedWeaponInFreeBuildMode",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(m);
        }

        // ── ResolveDesiredCount: pure logic, no game state ────────────

        [Fact]
        public void ResolveDesiredCount_NullSettings_ReturnsOne()
        {
            Assert.Equal(1, BatchSmithing.ResolveDesiredCount(null));
        }

        [Fact]
        public void ResolveDesiredCount_FeatureDisabled_ReturnsOne()
        {
            var settings = new BetterSmithingSettings { BatchOperationsEnabled = false };
            Assert.Equal(1, BatchSmithing.ResolveDesiredCount(settings));
        }

        [Fact]
        public void ResolveDesiredCount_NoModifierKey_ReturnsOne()
        {
            // In a headless test process no key is held — should fall through to vanilla.
            var settings = new BetterSmithingSettings
            {
                BatchOperationsEnabled = true,
                ShiftMultiplier = 5,
                CtrlMultiplier = 10,
                CtrlShiftMultiplier = 0,
            };
            Assert.Equal(1, BatchSmithing.ResolveDesiredCount(settings));
        }

        [Fact]
        public void SafeIsKeyDown_NeverThrows_InHeadlessProcess()
        {
            // Should silently return false rather than blowing up the test host.
            bool result = BatchSmithing.SafeIsKeyDown(InputKey.LeftShift);
            Assert.False(result);
        }

        [Fact]
        public void HasStaminaFor_UnlimitedStamina_AlwaysTrue()
        {
            var settings = new BetterSmithingSettings { UnlimitedCraftingStamina = true };
            Assert.True(BatchSmithing.HasStaminaFor(null, null, 999999, settings));
        }

        [Fact]
        public void HasStaminaFor_NullBehavior_AndStaminaRequired_ReturnsFalse()
        {
            var settings = new BetterSmithingSettings { UnlimitedCraftingStamina = false };
            Assert.False(BatchSmithing.HasStaminaFor(null, null, 1, settings));
        }
    }
}
