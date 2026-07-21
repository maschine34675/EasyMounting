using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EasyMountingClient.Patches;
using SPT.Reflection.Patching;
using UnityEngine;

namespace EasyMountingClient
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.maschine.EasyMounting";
        public const string PluginName = "maschine-EasyMounting";
        public const string PluginVersion = "1.2.0";

        public static ManualLogSource Log;
        internal static ConfigFile BoundConfig;
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> IncludeLowPolyCollider;
        public static ConfigEntry<bool> IncludeDoorCollider;
        public static ConfigEntry<bool> NormalizeSwappedMountAnchors;
        public static ConfigEntry<bool> SkipClipCheck;
        public static ConfigEntry<float> ReachToleranceMeters;
        public static ConfigEntry<bool> SynthesizeThinRailPoints;
        public static ConfigEntry<bool> SkipRotationOverlapCheck;
        public static ConfigEntry<KeyboardShortcut> GenerateSupportPackage;
        public static ConfigEntry<float> CaptureWindowSeconds;
        public static ConfigEntry<bool> CleanupOldPackages;
        public static ConfigEntry<bool> DebugMountLogging;

        private int _patchesApplied;
        private int _patchesTotal;
        private Coroutine _captureRoutine;

        private void Awake()
        {
            Log = Logger;
            BoundConfig = Config;

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
            GenerateSupportPackage = Config.Bind("Support", "GenerateSupportPackage", new KeyboardShortcut(KeyCode.None),
                "Unbound by default - most players never need this, and a default key is likely to already be " +
                "taken by another mod or the game itself. Set a key here (F12 config manager) if you want to " +
                "generate a support package: it collects LogOutput.log, both EasyMounting configs, the newest " +
                "SPT server log and a summary of loaded plugins and active mounting settings into a zip under " +
                "<GameRoot>/EasyMounting-Support/, then opens Explorer with the zip selected - attach that file " +
                "when reporting issues. If DebugMountLogging is currently off, the first press arms it and waits " +
                "CaptureWindowSeconds so you can reproduce the issue with mount traces included; press the " +
                "hotkey again during that window to capture immediately instead of waiting it out.");
            CaptureWindowSeconds = Config.Bind("Support", "CaptureWindowSeconds", 15f,
                new ConfigDescription(
                    "How long (seconds) to wait after arming debug logging before packaging, giving you time to " +
                    "reproduce the mount issue. Only applies when DebugMountLogging was off at the time you " +
                    "pressed the hotkey - if it's already on, the package is generated immediately.",
                    new AcceptableValueRange<float>(3f, 120f)));
            CleanupOldPackages = Config.Bind("Support", "CleanupOldPackages", true,
                "Delete previous EasyMounting-Support-*.zip files after creating a new one, so the folder never " +
                "ends up with several packages that are unclear which one to actually attach to a report.");
            DebugMountLogging = Config.Bind("Debug", "DebugMountLogging", false,
                "Trace every mount attempt to the BepInEx log: surface scan result, validation result, computed " +
                "player stand position, and - on every mount exit - the remaining distance to that position plus " +
                "which code path triggered the exit. Used to diagnose spots that refuse to mount.");

            EnablePatch(new WidenMountLayerMaskPatch(), "the mount layer mask fix is NOT active");
            EnablePatch(new NormalizeMountAnchorPatch(), "swapped weapon mounting anchors stay broken");
            EnablePatch(new SkipMountClipCheckPatch(), "the clip-check bypass is NOT active");
            EnablePatch(new SkipRotationOverlapPatch(), "horizontal aim at clipped spots stays locked");
            EnablePatch(new ReachToleranceUpdatePatch(), "the reach-tolerance override is NOT active");
            EnablePatch(new ReachToleranceApproachPatch(), "the reach-tolerance override and safe snap are NOT active");
            EnablePatch(new StandingScanMarkerPatch(), "thin-rail synthesis may misfire after prone scans");
            EnablePatch(new ProneScanMarkerPatch(), "thin-rail synthesis may misfire after prone scans");
            EnablePatch(new ThinRailSynthesisPatch(), "thin-rail mount synthesis is NOT active");
            EnablePatch(new DebugPointFoundPatch(), "mount debug logging is incomplete");
            EnablePatch(new DebugNoPointFoundPatch(), "mount debug logging is incomplete");
            EnablePatch(new DebugValidatePointPatch(), "mount debug logging is incomplete");
            EnablePatch(new DebugEnterMountPatch(), "mount debug logging is incomplete");
            EnablePatch(new DebugStartExitMountPatch(), "mount debug logging is incomplete");
            EnablePatch(new DebugExitMountPatch(), "mount debug logging is incomplete");
            EnablePatch(new DebugMountAnchorPatch(), "mount debug logging is incomplete");

            if (_patchesApplied == _patchesTotal)
            {
                Log.LogInfo($"{PluginName} v{PluginVersion} active, {_patchesApplied}/{_patchesTotal} patches applied.");
            }
            else
            {
                Log.LogWarning($"{PluginName} v{PluginVersion} active with FAILURES: only {_patchesApplied}/{_patchesTotal} patches applied - see errors above.");
            }
        }

        private void Update()
        {
            if (GenerateSupportPackage == null)
            {
                return;
            }
            var shortcut = GenerateSupportPackage.Value;
            if (shortcut.MainKey == KeyCode.None || !Input.GetKeyDown(shortcut.MainKey))
            {
                return;
            }
            foreach (var modifier in shortcut.Modifiers)
            {
                if (!Input.GetKey(modifier))
                {
                    return;
                }
            }

            OnSupportHotkeyPressed();
        }

        private void OnSupportHotkeyPressed()
        {
            if (_captureRoutine != null)
            {
                StopCoroutine(_captureRoutine);
                _captureRoutine = null;
                FinishArmedCapture();
                return;
            }

            if (DebugMountLogging.Value)
            {
                SupportPackage.Generate();
                return;
            }

            DebugMountLogging.Value = true;
            Log.LogInfo(
                $"[EasyMounting] Debug logging armed for the support package - try to reproduce the mount issue " +
                $"now. Capturing automatically in {CaptureWindowSeconds.Value:F0}s, or press " +
                $"{GenerateSupportPackage.Value} again to capture immediately.");
            _captureRoutine = StartCoroutine(CaptureAfterDelay());
        }

        private IEnumerator CaptureAfterDelay()
        {
            yield return new WaitForSeconds(CaptureWindowSeconds.Value);
            _captureRoutine = null;
            FinishArmedCapture();
        }
        private void FinishArmedCapture()
        {
            SupportPackage.Generate();
            DebugMountLogging.Value = false;
        }
        private void EnablePatch(ModulePatch patch, string onFailure)
        {
            _patchesTotal++;
            try
            {
                patch.Enable();
                _patchesApplied++;
                Log.LogDebug(patch.GetType().Name + " registered successfully.");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to register {patch.GetType().Name} - {onFailure}: {ex}");
            }
        }
    }
}
