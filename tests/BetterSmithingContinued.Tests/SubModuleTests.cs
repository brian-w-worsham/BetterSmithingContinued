using System.Reflection;
using HarmonyLib;
using Xunit;

namespace BetterSmithingContinued.Tests
{
    /// <summary>
    /// Tests for <see cref="SubModule"/> — entry point structure and assembly
    /// invariants required for the Bannerlord runtime to load the mod.
    /// </summary>
    public class SubModuleTests
    {
        [Fact]
        public void SubModule_Inherits_MBSubModuleBase()
        {
            Assert.True(typeof(TaleWorlds.MountAndBlade.MBSubModuleBase)
                .IsAssignableFrom(typeof(SubModule)));
        }

        [Fact]
        public void SubModule_IsPublic()
        {
            Assert.True(typeof(SubModule).IsPublic);
        }

        [Fact]
        public void SubModule_RootNamespace_IsCorrect()
        {
            Assert.Equal("BetterSmithingContinued", typeof(SubModule).Namespace);
        }

        [Fact]
        public void SubModule_HarmonyId_IsExpectedConstant()
        {
            var field = typeof(SubModule).GetField("HarmonyId",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(field);
            Assert.Equal("com.bettersmithingcontinued.bannerlord", field.GetValue(null));
        }

        [Fact]
        public void SubModule_OverridesOnSubModuleLoad()
        {
            var m = typeof(SubModule).GetMethod("OnSubModuleLoad",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(m);
            Assert.True(m.IsFamily || m.IsFamilyOrAssembly);
        }

        [Fact]
        public void SubModule_OverridesOnSubModuleUnloaded()
        {
            var m = typeof(SubModule).GetMethod("OnSubModuleUnloaded",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(m);
            Assert.True(m.IsFamily || m.IsFamilyOrAssembly);
        }

        [Fact]
        public void SubModule_HarmonyField_IsPresent()
        {
            var f = typeof(SubModule).GetField("_harmony",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(f);
            Assert.Equal(typeof(Harmony), f.FieldType);
        }

        [Fact]
        public void SubModule_CanBeInstantiated()
        {
            Assert.NotNull(new SubModule());
        }

        [Fact]
        public void Assembly_Exposes_InternalsTo_Tests()
        {
            var attrs = typeof(SubModule).Assembly
                .GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>();

            bool found = false;
            foreach (var a in attrs)
            {
                if (a.AssemblyName == "BetterSmithingContinued.Tests")
                {
                    found = true;
                    break;
                }
            }

            Assert.True(found, "BetterSmithingContinued must expose internals to BetterSmithingContinued.Tests.");
        }

        [Fact]
        public void Assembly_Contains_AllExpectedPatchTypes()
        {
            var asm = typeof(SubModule).Assembly;
            Assert.NotNull(asm.GetType("BetterSmithingContinued.Patches.RefiningEnergyCostPatch"));
            Assert.NotNull(asm.GetType("BetterSmithingContinued.Patches.SmithingEnergyCostPatch"));
            Assert.NotNull(asm.GetType("BetterSmithingContinued.Patches.SmeltingEnergyCostPatch"));
            Assert.NotNull(asm.GetType("BetterSmithingContinued.Patches.StaminaRecoveryPatch"));
        }

        [Fact]
        public void Assembly_Has_Exactly_FourHarmonyPatchClasses()
        {
            int count = 0;
            foreach (var t in typeof(SubModule).Assembly.GetTypes())
            {
                if (t.GetCustomAttribute<HarmonyPatch>() != null)
                {
                    count++;
                }
            }

            Assert.Equal(4, count);
        }

        [Fact]
        public void All_PatchClasses_Target_DefaultSmithingModel()
        {
            var expected = typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultSmithingModel);
            foreach (var t in typeof(SubModule).Assembly.GetTypes())
            {
                var attr = t.GetCustomAttribute<HarmonyPatch>();
                if (attr != null)
                {
                    Assert.Equal(expected, attr.info.declaringType);
                }
            }
        }
    }
}
