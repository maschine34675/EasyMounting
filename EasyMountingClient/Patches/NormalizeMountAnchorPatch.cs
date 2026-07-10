using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace EasyMountingClient.Patches
{
    internal class NormalizeMountAnchorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2667), nameof(GClass2667.TryMountWeapon));
        }

        [PatchPrefix]
        static void Prefix(GClass2667 __instance)
        {
            if (!Plugin.NormalizeSwappedMountAnchors.Value)
            {
                return;
            }

            var spring = __instance.PlayerSpring_0;
            Vector3 anchor = spring.MountingRotationCenter;
            if (Mathf.Abs(anchor.x) <= Mathf.Abs(anchor.y))
            {
                return;
            }

            var fixedAnchor = new Vector3(anchor.y, anchor.x, Mathf.Clamp(anchor.z, -0.05f, 0.05f));
            spring.MountingRotationCenter = fixedAnchor;
            Plugin.Log.LogInfo($"Normalized sideways mounting anchor for current weapon: ({anchor.x:F2}, {anchor.y:F2}, {anchor.z:F2}) -> ({fixedAnchor.x:F2}, {fixedAnchor.y:F2}, {fixedAnchor.z:F2})");
        }
    }
}
