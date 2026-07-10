using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace EasyMountingClient.Patches
{
    internal static class ReachTolerance
    {
        public static float GetReachSqr()
        {
            float meters = Plugin.ReachToleranceMeters.Value;
            return meters * meters;
        }
        public static void SafeSnap(MovementContext ctx, Vector3 target)
        {
            Vector3 current = ctx.TransformPosition;
            Vector3 delta = target - current;
            if (delta.sqrMagnitude > 0.0001f)
            {
                Vector3 p1 = current + Vector3.up * 0.5f;
                Vector3 p2 = current + Vector3.up * 1.4f;
                if (Physics.CapsuleCast(p1, p2, 0.3f, delta.normalized, delta.magnitude, LayerMaskClass.PlayerStaticCollisionsMask))
                {
                    return;
                }
            }

            ctx.TransformPosition = target;
        }

        public static IEnumerable<CodeInstruction> ReplaceTolerance(IEnumerable<CodeInstruction> instructions, string label)
        {
            var getter = AccessTools.Method(typeof(ReachTolerance), nameof(GetReachSqr));
            var list = instructions.ToList();
            int replaced = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Ldc_R4 && list[i].operand is float f && f == 0.008f)
                {
                    list[i] = new CodeInstruction(list[i]) { opcode = OpCodes.Call, operand = getter };
                    replaced++;
                }
            }
            if (replaced != 1)
            {
                Plugin.Log.LogError($"ReachTolerance transpiler ({label}): expected exactly 1 occurrence of 0.008f, found {replaced} - the reach-tolerance override may be inactive or overreaching.");
            }
            return list;
        }

        public static IEnumerable<CodeInstruction> ReplaceSnap(IEnumerable<CodeInstruction> instructions)
        {
            var safeSnap = AccessTools.Method(typeof(ReachTolerance), nameof(SafeSnap));
            var list = instructions.ToList();
            int replaced = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].operand is MethodInfo mi && mi.Name == "set_TransformPosition" && mi.DeclaringType == typeof(MovementContext))
                {
                    list[i] = new CodeInstruction(list[i]) { opcode = OpCodes.Call, operand = safeSnap };
                    replaced++;
                }
            }

            if (replaced != 1)
            {
                Plugin.Log.LogError($"ReachTolerance transpiler (method_2 snap): expected exactly 1 TransformPosition assignment, found {replaced} - the collision-checked snap may be inactive.");
            }
            return list;
        }
    }

    internal class ReachToleranceUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(IdleWeaponMountingStateClass), nameof(IdleWeaponMountingStateClass.ManualAnimatorMoveUpdate));
        }

        [PatchTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReachTolerance.ReplaceTolerance(instructions, "ManualAnimatorMoveUpdate");
        }
    }

    internal class ReachToleranceApproachPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(IdleWeaponMountingStateClass), "method_2");
        }

        [PatchTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReachTolerance.ReplaceSnap(ReachTolerance.ReplaceTolerance(instructions, "method_2"));
        }
    }
}
