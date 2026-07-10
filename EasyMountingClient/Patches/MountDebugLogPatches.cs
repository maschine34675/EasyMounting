using System;
using System.Diagnostics;
using System.Reflection;
using EFT;
using EFT.WeaponMounting;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace EasyMountingClient.Patches
{
    internal static class MountDebugLog
    {
        public static bool Enabled => Plugin.DebugMountLogging.Value;

        public static void Log(string message)
        {
            if (Enabled)
            {
                Plugin.Log.LogInfo("[MountDebug] " + message);
            }
        }

        public static string Callers()
        {
            var frames = new StackTrace(2, false).GetFrames();
            if (frames == null)
            {
                return "?";
            }
            var names = new System.Collections.Generic.List<string>();
            foreach (var frame in frames)
            {
                if (names.Count >= 4)
                {
                    break;
                }
                var m = frame.GetMethod();
                if (m == null)
                {
                    continue;
                }
                names.Add((m.DeclaringType != null ? m.DeclaringType.Name + "." : "") + m.Name);
            }
            return string.Join(" <- ", names);
        }

        public static string V(Vector3 v)
        {
            return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
        }
    }

    internal class DebugPointFoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2666), "method_0");
        }

        [PatchPostfix]
        static void Postfix(MountPointData pointData)
        {
            if (!MountDebugLog.Enabled)
            {
                return;
            }
            MountDebugLog.Log($"Surface scan FOUND point {MountDebugLog.V(pointData.MountPoint)} side={pointData.MountSideDirection} dir={MountDebugLog.V(pointData.MountDirection)}");
        }
    }

    internal class DebugNoPointFoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2666), "method_2");
        }

        [PatchPostfix]
        static void Postfix()
        {
            MountDebugLog.Log("Surface scan found NO point");
        }
    }

    internal class DebugValidatePointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2667), "method_4");
        }

        [PatchPostfix]
        static void Postfix(GClass2667 __instance, MountPointData mountPointData, bool __result)
        {
            if (!MountDebugLog.Enabled)
            {
                return;
            }
            if (!__result)
            {
                MountDebugLog.Log($"Validation REJECTED point {MountDebugLog.V(mountPointData.MountPoint)} (pitch/yaw window or clearance check)");
                return;
            }

            var data = __instance.MovementContext_0.PlayerMountingPointData;
            var playerPos = __instance.MovementContext_0.TransformPosition;
            float dist = (playerPos - data.PlayerTargetPos).magnitude;
            MountDebugLog.Log(
                $"Validation ACCEPTED point {MountDebugLog.V(mountPointData.MountPoint)}: " +
                $"playerTargetPos={MountDebugLog.V(data.PlayerTargetPos)} playerPos={MountDebugLog.V(playerPos)} " +
                $"walkDistance={dist:F3}m approachTime={data.CurrentApproachTime:F2}s");
        }
    }

    internal class DebugEnterMountPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), nameof(MovementContext.EnterMountedState));
        }

        [PatchPostfix]
        static void Postfix(MovementContext __instance)
        {
            if (!MountDebugLog.Enabled)
            {
                return;
            }
            MountDebugLog.Log(
                $"ENTER mounted state: playerPos={MountDebugLog.V(__instance.TransformPosition)} " +
                $"targetPos={MountDebugLog.V(__instance.PlayerMountingPointData.PlayerTargetPos)}");
        }
    }

    internal class DebugStartExitMountPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), nameof(MovementContext.StartExitingMountedState));
        }

        [PatchPrefix]
        static void Prefix(MovementContext __instance)
        {
            LogExit(__instance, "StartExitingMountedState");
        }
        internal static void LogExit(MovementContext ctx, string what)
        {
            if (!MountDebugLog.Enabled)
            {
                return;
            }
            var data = ctx.PlayerMountingPointData;
            float dist = (ctx.TransformPosition - data.PlayerTargetPos).magnitude;
            MountDebugLog.Log(
                $"{what}: playerPos={MountDebugLog.V(ctx.TransformPosition)} targetPos={MountDebugLog.V(data.PlayerTargetPos)} " +
                $"distanceToTarget={dist:F3}m transitionProgress={data.TransitionProgress:F2} " +
                $"overlapDepth={ctx.OverlapDepth:F3} canWalk={ctx.CanWalk} caller: {MountDebugLog.Callers()}");
        }
    }

    internal class DebugExitMountPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), nameof(MovementContext.ExitMountedState));
        }

        [PatchPrefix]
        static void Prefix(MovementContext __instance)
        {
            DebugStartExitMountPatch.LogExit(__instance, "ExitMountedState");
        }
    }
    internal class DebugMountAnchorPatch : ModulePatch
    {
        private static float _nextLogTime;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(IdleWeaponMountingStateClass), nameof(IdleWeaponMountingStateClass.ManualAnimatorMoveUpdate));
        }

        [PatchPostfix]
        static void Postfix(IdleWeaponMountingStateClass __instance)
        {
            if (!MountDebugLog.Enabled || Time.time < _nextLogTime)
            {
                return;
            }
            _nextLogTime = Time.time + 0.5f;

            try
            {
                var player = __instance.Player_0;
                var data = __instance.PlayerMountingPointData_0;
                var pwa = player.ProceduralWeaponAnimation;
                bool bipodBranch = data.MountPointData.MountSideDirection == EMountSideDirection.Forward && pwa.IsBipodUsed;
                Vector3 anchorLocal = bipodBranch
                    ? pwa.HandsContainer.MountingRotationCenterBipods
                    : pwa.HandsContainer.MountingRotationCenter;
                Transform weaponRoot = player.HandsController.HandsHierarchy.GetTransform(ECharacterWeaponBones.Weapon_root);
                MountDebugLog.Log(
                    "Anchor: mountPoint=" + MountDebugLog.V(data.MountPointData.MountPoint) +
                    " vOff=" + data.CurrentMountingPointVerticalOffset.ToString("F3") +
                    " anchorLocal=" + MountDebugLog.V(anchorLocal) +
                    " bipodBranch=" + bipodBranch +
                    " anchorWorld=" + MountDebugLog.V(weaponRoot.TransformPoint(anchorLocal)) +
                    " weaponRoot=" + MountDebugLog.V(weaponRoot.position) +
                    " handsController=" + MountDebugLog.V(player.HandsController.ControllerGameObject.transform.position) +
                    " weaponMountingPos=" + MountDebugLog.V(data.WeaponMountingPos) +
                    " ribcage=" + MountDebugLog.V(player.PlayerBones.Ribcage.Original.position) +
                    " progress=" + data.TransitionProgress.ToString("F2"));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("[MountDebug] Anchor dump failed: " + ex);
            }
        }
    }
}
