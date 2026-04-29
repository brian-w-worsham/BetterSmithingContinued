using System.Reflection;
using BetterSmithingContinued.Patches;
using HarmonyLib;
using Xunit;

namespace BetterSmithingContinued.Tests
{
    /// <summary>
    /// Tests for the three "unlimited stamina" Harmony prefix patches:
    /// <see cref="RefiningEnergyCostPatch"/>, <see cref="SmithingEnergyCostPatch"/>,
    /// and <see cref="SmeltingEnergyCostPatch"/>.
    /// </summary>
    public class UnlimitedStaminaPatchTests
    {
        // ── Refining ──────────────────────────────────────────────────

        [Fact]
        public void Refining_Prefix_ZerosResult_AndSkipsOriginal_WhenEnabled()
        {
            using (new SettingsScope(unlimited: true))
            {
                int result = 999;
                bool runOriginal = RefiningEnergyCostPatch.Prefix(ref result);

                Assert.False(runOriginal);
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public void Refining_Prefix_LeavesResult_AndRunsOriginal_WhenDisabled()
        {
            using (new SettingsScope(unlimited: false))
            {
                int result = 999;
                bool runOriginal = RefiningEnergyCostPatch.Prefix(ref result);

                Assert.True(runOriginal);
                Assert.Equal(999, result);
            }
        }

        [Fact]
        public void Refining_Patch_TargetsCorrectMethod()
        {
            var attr = typeof(RefiningEnergyCostPatch).GetCustomAttribute<HarmonyPatch>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultSmithingModel), attr.info.declaringType);
            Assert.Equal("GetEnergyCostForRefining", attr.info.methodName);
        }

        // ── Smithing ──────────────────────────────────────────────────

        [Fact]
        public void Smithing_Prefix_ZerosResult_AndSkipsOriginal_WhenEnabled()
        {
            using (new SettingsScope(unlimited: true))
            {
                int result = 150;
                bool runOriginal = SmithingEnergyCostPatch.Prefix(ref result);

                Assert.False(runOriginal);
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public void Smithing_Prefix_LeavesResult_AndRunsOriginal_WhenDisabled()
        {
            using (new SettingsScope(unlimited: false))
            {
                int result = 150;
                bool runOriginal = SmithingEnergyCostPatch.Prefix(ref result);

                Assert.True(runOriginal);
                Assert.Equal(150, result);
            }
        }

        [Fact]
        public void Smithing_Patch_TargetsCorrectMethod()
        {
            var attr = typeof(SmithingEnergyCostPatch).GetCustomAttribute<HarmonyPatch>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultSmithingModel), attr.info.declaringType);
            Assert.Equal("GetEnergyCostForSmithing", attr.info.methodName);
        }

        // ── Smelting ──────────────────────────────────────────────────

        [Fact]
        public void Smelting_Prefix_ZerosResult_AndSkipsOriginal_WhenEnabled()
        {
            using (new SettingsScope(unlimited: true))
            {
                int result = 75;
                bool runOriginal = SmeltingEnergyCostPatch.Prefix(ref result);

                Assert.False(runOriginal);
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public void Smelting_Prefix_LeavesResult_AndRunsOriginal_WhenDisabled()
        {
            using (new SettingsScope(unlimited: false))
            {
                int result = 75;
                bool runOriginal = SmeltingEnergyCostPatch.Prefix(ref result);

                Assert.True(runOriginal);
                Assert.Equal(75, result);
            }
        }

        [Fact]
        public void Smelting_Patch_TargetsCorrectMethod()
        {
            var attr = typeof(SmeltingEnergyCostPatch).GetCustomAttribute<HarmonyPatch>();
            Assert.NotNull(attr);
            Assert.Equal(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultSmithingModel), attr.info.declaringType);
            Assert.Equal("GetEnergyCostForSmelting", attr.info.methodName);
        }

        // ── Cross-cutting ─────────────────────────────────────────────

        [Fact]
        public void All_Patches_Are_InternalStatic()
        {
            foreach (var t in new[] { typeof(RefiningEnergyCostPatch), typeof(SmithingEnergyCostPatch), typeof(SmeltingEnergyCostPatch) })
            {
                Assert.True(t.IsAbstract && t.IsSealed, $"{t.Name} should be a static class.");
                Assert.False(t.IsPublic, $"{t.Name} should be internal.");
            }
        }

        [Fact]
        public void All_Prefix_Methods_HaveConsistentSignature()
        {
            var prefixes = new[]
            {
                typeof(RefiningEnergyCostPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic),
                typeof(SmithingEnergyCostPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic),
                typeof(SmeltingEnergyCostPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic),
            };

            foreach (var m in prefixes)
            {
                Assert.NotNull(m);
                Assert.Equal(typeof(bool), m.ReturnType);
                var ps = m.GetParameters();
                Assert.Single(ps);
                Assert.Equal("__result", ps[0].Name);
                Assert.True(ps[0].ParameterType.IsByRef);
                Assert.Equal(typeof(int), ps[0].ParameterType.GetElementType());
            }
        }

        /// <summary>
        /// Temporarily replaces <see cref="BetterSmithingSettings.Current"/> for
        /// the duration of a test, and restores the previous value on Dispose.
        /// </summary>
        private sealed class SettingsScope : System.IDisposable
        {
            private readonly BetterSmithingSettings _previous;

            internal SettingsScope(bool unlimited)
            {
                _previous = BetterSmithingSettings.Current;
                SetCurrent(new BetterSmithingSettings { UnlimitedCraftingStamina = unlimited });
            }

            public void Dispose() => SetCurrent(_previous);

            private static void SetCurrent(BetterSmithingSettings value)
            {
                var prop = typeof(BetterSmithingSettings).GetProperty(
                    nameof(BetterSmithingSettings.Current),
                    BindingFlags.Static | BindingFlags.NonPublic);
                prop.SetValue(null, value);
            }
        }
    }
}
