using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace EasyMountingClient.Patches
{
    internal class SkipRotationOverlapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), nameof(MovementContext.RotationOverlapPrediction));
        }

        [PatchPrefix]
        static bool Prefix(MovementContext __instance, ref Vector3 __result)
        {
            if (!Plugin.SkipRotationOverlapCheck.Value || !__instance.IsInMountedState)
            {
                return true;
            }

            __result = Vector3.zero;
            return false;
        }
    }
}
