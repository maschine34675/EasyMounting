using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace EasyMountingClient.Patches
{
    internal class SkipMountClipCheckPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2667), "method_5");
        }

        [PatchPrefix]
        static bool Prefix(GClass2667 __instance, ref bool __result)
        {
            if (!Plugin.SkipClipCheck.Value)
            {
                return true;
            }
            __result = __instance.MovementContext_0.IsGrounded;
            return false;
        }
    }
}
