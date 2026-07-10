using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EasyMountingClient.Patches;

namespace EasyMountingClient
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.maschine.EasyMounting";
        public const string PluginName = "maschine-EasyMounting";
        public const string PluginVersion = "1.1.0";

        public static ManualLogSource Log;
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> IncludeLowPolyCollider;
        public static ConfigEntry<bool> IncludeDoorCollider;
        public static ConfigEntry<bool> NormalizeSwappedMountAnchors;
        public static ConfigEntry<bool> SkipClipCheck;
        public static ConfigEntry<float> ReachToleranceMeters;
        public static ConfigEntry<bool> SynthesizeThinRailPoints;
        public static ConfigEntry<bool> SkipRotationOverlapCheck;
        public static ConfigEntry<bool> DebugMountLogging;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Widen the raycast layer mask used to find ledge/railing mounting points (mounting without bipod ground contact).");
            IncludeLowPolyCollider = Config.Bind("General", "IncludeLowPolyCollider", true,
                "Include the LowPolyCollider layer. Many thin railings/fences only have collision on this layer, not " +
                "HighPolyCollider, so the vanilla mount-point search never hits them no matter how permissive the " +
                "server-side EasyMounting settings are.");
            IncludeDoorCollider = Config.Bind("General", "IncludeDoorCollider", false,
                "Also include the door low-poly collider layer, for railings/frames near doorways. Off by default: " +
                "mounts found on a door slab don't reliably register the vanilla dismount-when-door-moves hook, so " +
                "a door opening under you can leave the weapon anchored in mid-air.");
            NormalizeSwappedMountAnchors = Config.Bind("General", "NormalizeSwappedMountAnchors", true,
                "Some weapons ship their mounting anchor with the along-the-barrel offset in the sideways component " +
                "(e.g. TRG M10), which makes the mounted weapon float ~1m beside the body with stretched arms and " +
                "balloons the aiming window. When the sideways component dominates, swap it into place at mount time. " +
                "Affects vanilla mounts of such weapons too.");
            SkipClipCheck = Config.Bind("Cursed", "SkipClipCheck", true,
                "Disable the 'does my body fit here' clearance check for standing/crouched ledge mounts. Lets you " +
                "mount at spots that are geometrically valid but too tight for vanilla to allow (e.g. close to a " +
                "wall behind you) - at the cost of possibly visibly clipping into geometry while mounted.");
            ReachToleranceMeters = Config.Bind("Cursed", "ReachToleranceMeters", 0.30f,
                new ConfigDescription(
                    "How far (meters) the player may end up from the computed stand position before the mount " +
                    "aborts. Vanilla ~0.09. Raise when a mount visibly starts pulling you in and then pops back " +
                    "out (colliders blocking the last few centimeters); the pose may anchor slightly off the " +
                    "surface in exchange. Applies live, no restart needed.",
                    new AcceptableValueRange<float>(0.09f, 1.0f)));
            SynthesizeThinRailPoints = Config.Bind("Cursed", "SynthesizeThinRailPoints", true,
                "When the vanilla scan finds nothing, re-scan and synthesize a mount point for thin-sheet colliders. " +
                "Some railings ('metalthin') have collision as a paper-thin vertical sheet: forward rays hit the " +
                "front face, but the downward probes that normally locate the top surface pass right by the " +
                "zero-width top edge, so vanilla can never mount there. This anchors the weapon on the front-face " +
                "top edge instead. Weapon placement on such rails may look slightly off.");
            SkipRotationOverlapCheck = Config.Bind("Cursed", "SkipRotationOverlapCheck", true,
                "While mounted, horizontal aiming normally checks whether the body shift caused by the rotation " +
                "would overlap geometry, and blocks the rotation if so. At clipped-in spots (see SkipClipCheck) " +
                "that check reports overlap permanently, freezing horizontal aim. Skipping it restores horizontal " +
                "aim there; the body may visibly rotate through the clipped geometry.");
            DebugMountLogging = Config.Bind("Debug", "DebugMountLogging", false,
                "Trace every mount attempt to the BepInEx log: surface scan result, validation result, computed " +
                "player stand position, and - on every mount exit - the remaining distance to that position plus " +
                "which code path triggered the exit. Used to diagnose spots that refuse to mount.");

            try
            {
                new WidenMountLayerMaskPatch().Enable();
                Log.LogDebug("WidenMountLayerMaskPatch registered successfully.");
            }
            catch (System.Exception ex)
            {
                Log.LogError("Failed to register WidenMountLayerMaskPatch - the mount layer mask fix is NOT active: " + ex);
            }

            try
            {
                new NormalizeMountAnchorPatch().Enable();
                Log.LogDebug("NormalizeMountAnchorPatch registered successfully.");
            }
            catch (System.Exception ex)
            {
                Log.LogError("Failed to register NormalizeMountAnchorPatch - swapped weapon mounting anchors stay broken: " + ex);
            }

            try
            {
                new SkipMountClipCheckPatch().Enable();
                Log.LogDebug("SkipMountClipCheckPatch registered successfully.");
            }
            catch (System.Exception ex)
            {
                Log.LogError("Failed to register SkipMountClipCheckPatch - the clip-check bypass is NOT active: " + ex);
            }

            try
            {
                new SkipRotationOverlapPatch().Enable();
                Log.LogDebug("SkipRotationOverlapPatch registered successfully.");
            }
            catch (System.Exception ex)
            {
                Log.LogError("Failed to register SkipRotationOverlapPatch - horizontal aim at clipped spots stays locked: " + ex);
            }

            try
            {
                new ReachToleranceUpdatePatch().Enable();
                new ReachToleranceApproachPatch().Enable();
                Log.LogDebug("ReachTolerance patches registered successfully.");
            }
            catch (System.Exception ex)
            {
                Log.LogError("Failed to register ReachTolerance patches - the reach-tolerance override is NOT active: " + ex);
            }

            try
            {
                new StandingScanMarkerPatch().Enable();
                new ProneScanMarkerPatch().Enable();
                new ThinRailSynthesisPatch().Enable();
                Log.LogDebug("ThinRailSynthesis patches registered successfully.");
            }
            catch (System.Exception ex)
            {
                Log.LogError("Failed to register ThinRailSynthesis patches - thin-rail mount synthesis is NOT active: " + ex);
            }

            try
            {
                new DebugPointFoundPatch().Enable();
                new DebugNoPointFoundPatch().Enable();
                new DebugValidatePointPatch().Enable();
                new DebugEnterMountPatch().Enable();
                new DebugStartExitMountPatch().Enable();
                new DebugExitMountPatch().Enable();
                new DebugMountAnchorPatch().Enable();
                Log.LogDebug("Mount debug logging patches registered successfully.");
            }
            catch (System.Exception ex)
            {
                Log.LogError("Failed to register mount debug logging patches: " + ex);
            }

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }
    }
}
