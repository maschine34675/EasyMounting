using System.Collections.Generic;
using System.Reflection;
using EFT.WeaponMounting;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace EasyMountingClient.Patches
{
    internal class WidenMountLayerMaskPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Constructor(typeof(GClass2666), new[]
            {
                typeof(IMountingPointDetectionSettings),
                typeof(WeaponMountingView)
            });
        }

        [PatchPostfix]
        static void Postfix(GClass2666 __instance)
        {
            if (!Plugin.Enabled.Value)
            {
                Plugin.Log.LogInfo("Disabled via config - LayerMask_0 left at vanilla value: " + Describe(__instance.LayerMask_0));
                return;
            }

            LayerMask extra = 0;
            if (Plugin.IncludeLowPolyCollider.Value)
            {
                extra |= LayerMaskClass.LowPolyColliderLayerMask;
            }
            if (Plugin.IncludeDoorCollider.Value)
            {
                extra |= 1 << LayerMaskClass.DoorLayer;
            }

            __instance.LayerMask_0 |= extra;
            Plugin.Log.LogInfo("Ledge/railing mount-point LayerMask_0 is now: " + Describe(__instance.LayerMask_0));
        }

        private static string Describe(LayerMask mask)
        {
            int value = mask.value;
            var names = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                if ((value & (1 << i)) == 0)
                {
                    continue;
                }
                string name = LayerMask.LayerToName(i);
                names.Add(string.IsNullOrEmpty(name) ? $"layer{i}" : name);
            }
            return string.Join(", ", names) + $" (0x{value:X})";
        }
    }
}
