using dc.h2d.col;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using ModCore.Hooks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TestRunner
{
    public class HarmonyXTest
    {
        [HarmonyPatch(typeof(Bounds), nameof(Bounds.load))]
        [HarmonyPriority(400)]
        private static class Patch_1
        {
            static bool Prefix(Bounds b)
            {
                b.xMin = 114514;
                b.yMax = 123;
                return true;
            }
            static void Postfix(Bounds __instance)
            {
                __instance.xMax = 20071003;
            }
        }

        [HarmonyPatch(typeof(Bounds), nameof(Bounds.load))]
        [HarmonyPriority(100)]
        private static class Patch_2
        {
            static bool Prefix(Bounds b)
            {
                b.xMin = 54250;
                return true;
            }
        }

        [HarmonyPatch(typeof(Bounds), nameof(Bounds.load))]
        [HarmonyPriority(400)]
        private static class Patch_3
        {
            static bool Prefix(Bounds b)
            {
                b.xMin = 114514;
                b.yMax = 123;
                return true;
            }
            static void Postfix(Bounds __instance)
            {
                __instance.xMax = 20071003;
            }
        }
        [Fact]
        public void Test_1()
        {
            var inst = Harmony.CreateAndPatchAll(typeof(Patch_1));

            var original = typeof(Bounds).GetMethod(nameof(Bounds.load));
            var patcher = original.GetMethodPatcher();

            Assert.True(patcher is HashlinkFunctionPatcher);

            var b1 = new Bounds();

            b1.load(new()
            {
                xMin = 1,
                xMax = 2
            });

            Assert.Equal(114514, b1.xMin);
            Assert.Equal(20071003, b1.xMax);

            inst.UnpatchSelf();

            b1.load(new()
            {
                xMin = 1145
            });

            Assert.Equal(1145, b1.xMin);
        }

        [Fact]
        public void Test_2()
        {
            var inst1 = Harmony.CreateAndPatchAll(typeof(Patch_3));
            var inst2 = Harmony.CreateAndPatchAll(typeof(Patch_2));

            var original = typeof(Bounds).GetMethod(nameof(Bounds.load));
            var patcher = original.GetMethodPatcher();

            Assert.True(patcher is HashlinkFunctionPatcher);

            var b1 = new Bounds();
            b1.load(new());

            Assert.Equal(54250, b1.xMin);

            Hook_Bounds.load += Hook_Bounds_load;

            b1.load(new());

            Assert.Equal(666, b1.xMin);
            Assert.Equal(749, b1.yMin);
            Assert.Equal(123, b1.yMax);

            Hook_Bounds.load -= Hook_Bounds_load;
            //inst1.UnpatchSelf();
            //inst2.UnpatchSelf();
        }

   

        private void Hook_Bounds_load(Hook_Bounds.orig_load orig, Bounds self, Bounds b)
        {
            b.xMin = 666;
            b.yMin = 749;
            orig(self, b);
        }
    }
}
