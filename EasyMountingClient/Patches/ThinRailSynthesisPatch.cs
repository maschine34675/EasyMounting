using System;
using System.Reflection;
using System.Text;
using EFT.WeaponMounting;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace EasyMountingClient.Patches
{
    internal static class ScanEmulation
    {
        internal sealed class Result
        {
            public Vector3 ColumnTop;
            public float GridSize;
            public float StepSize;
            public int Steps;
            public RaycastHit[] Hits;
            public bool[] HasHit;
            public int EdgeIdx = -1;
            public RaycastHit EdgeHit;
            public bool EdgeTopExposed;
            public bool ClearanceBlocked;
            public RaycastHit ClearanceHit;
            public bool ProbeAborted;
            public bool[] ProbeHasHit;
            public RaycastHit[] ProbeHits;
            public Vector3 BestTop = Vector3.negativeInfinity;
        }

        public static Result Run(GClass2666 inst)
        {
            var s = inst.ImountingPointDetectionSettings_0;
            Vector3 fwd = inst.Vector3_1;
            if (fwd == Vector3.zero)
            {
                return null;
            }

            var r = new Result();
            Vector3 p = inst.Vector3_0;
            p.y -= inst.Float_0;
            float top = Mathf.Clamp(p.y + s.VerticalGridSize / 2f, s.GridMinHeight + inst.Float_1, s.GridMaxHeight + inst.Float_1);
            float bottom = Mathf.Clamp(p.y - s.VerticalGridSize / 2f, s.GridMinHeight + inst.Float_1, s.GridMaxHeight + inst.Float_1);
            r.GridSize = Mathf.Abs(top - bottom) + Mathf.Abs(inst.Float_1 / 2f);
            p.y = inst.Float_0 + top;
            r.ColumnTop = p;
            r.Steps = s.VerticalGridStepsAmount;
            r.StepSize = r.GridSize / r.Steps;

            r.Hits = new RaycastHit[r.Steps];
            r.HasHit = new bool[r.Steps];
            for (int i = 0; i < r.Steps; i++)
            {
                Vector3 origin = p + Vector3.down * (i * r.StepSize);
                r.HasHit[i] = Physics.Raycast(origin, fwd, out r.Hits[i], s.RaycastDistance, inst.LayerMask_0.value);
            }
            float bestHeightDelta = float.MaxValue;
            int lastSelected = 0;
            for (int i = 0; i < r.Steps; i++)
            {
                bool isNewEdge = i == 0 || !r.HasHit[i - 1] || r.Hits[i - 1].distance > s.EdgeDetectionDistance;
                float heightDelta = r.HasHit[i] ? Mathf.Abs(r.Hits[i].point.y - inst.Vector3_0.y) : float.MaxValue;
                if (r.HasHit[i] && r.Hits[i].distance <= s.EdgeDetectionDistance &&
                    ((isNewEdge && bestHeightDelta > heightDelta) ||
                     (r.EdgeHit.distance - r.Hits[i].distance > s.EdgeDetectionDistance * 0.2f && lastSelected == i - 1)))
                {
                    r.EdgeHit = r.Hits[i];
                    r.EdgeIdx = i;
                    r.EdgeTopExposed = isNewEdge;
                    bestHeightDelta = heightDelta;
                    lastSelected = i;
                }
            }

            if (r.EdgeIdx < 0)
            {
                return r;
            }
            Vector3 clearOrigin = r.EdgeHit.point + Vector3.up * s.SecondCheckVerticalGridOffset;
            r.ClearanceBlocked = Physics.Raycast(clearOrigin, fwd, out r.ClearanceHit, s.SecondCheckVerticalGridSize, inst.LayerMask_0.value);
            if (r.ClearanceBlocked)
            {
                return r;
            }

            int steps2 = s.SecondCheckVerticalGridSizeStepsAmount;
            float step2 = s.SecondCheckVerticalGridSize / steps2;
            r.ProbeHasHit = new bool[steps2];
            r.ProbeHits = new RaycastHit[steps2];
            for (int j = 0; j < steps2; j++)
            {
                Vector3 origin = clearOrigin + fwd * (j * step2);
                r.ProbeHasHit[j] = Physics.Raycast(origin, Vector3.down, out r.ProbeHits[j], s.SecondCheckVerticalDistance, inst.LayerMask_0.value);
                if (!r.ProbeHasHit[j])
                {
                    continue;
                }
                var hit = r.ProbeHits[j];
                if (hit.distance == 0f)
                {
                    r.ProbeAborted = true;
                    r.BestTop = Vector3.negativeInfinity;
                    return r;
                }
                if (hit.point.y > r.EdgeHit.point.y && hit.point.y > r.BestTop.y &&
                    Vector3.Dot(hit.normal, Vector3.up) >= s.MaxHorizontalMountAngleDotDelta)
                {
                    r.BestTop = hit.point;
                }
            }

            return r;
        }
    }
    internal class StandingScanMarkerPatch : ModulePatch
    {
        internal static bool LastScanWasProne;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2666), nameof(GClass2666.FindMountingPoint));
        }

        [PatchPrefix]
        static void Prefix()
        {
            LastScanWasProne = false;
        }
    }

    internal class ProneScanMarkerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2666), nameof(GClass2666.FindMountingPointProne));
        }

        [PatchPrefix]
        static void Prefix()
        {
            StandingScanMarkerPatch.LastScanWasProne = true;
        }
    }
    internal class ThinRailSynthesisPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2666), "method_2");
        }

        [PatchPostfix]
        static void Postfix(GClass2666 __instance)
        {
            bool debug = MountDebugLog.Enabled;
            bool synth = Plugin.SynthesizeThinRailPoints.Value;
            if (!debug && !synth)
            {
                return;
            }

            try
            {
                if (StandingScanMarkerPatch.LastScanWasProne)
                {
                    if (debug)
                    {
                        MountDebugLog.Log("DeepScan: prone scan failed - synthesis not applicable, skipping");
                    }
                    return;
                }
                if (!debug && !StateGuardsPass(__instance))
                {
                    return;
                }

                var r = ScanEmulation.Run(__instance);
                if (r == null)
                {
                    if (debug)
                    {
                        MountDebugLog.Log("DeepScan: no scan state captured yet, skipping");
                    }
                    return;
                }
                if (debug)
                {
                    LogDump(__instance, r);
                }
                if (synth)
                {
                    TrySynthesize(__instance, r);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("[MountDebug] Deep scan/synthesis failed: " + ex);
            }
        }
        static bool StateGuardsPass(GClass2666 inst)
        {
            if (Mathf.Abs(inst.Vector3_1.y) > 0.01f)
            {
                return false;
            }
            return (inst.Transform_0.position - inst.Vector3_0).sqrMagnitude <= 0.25f;
        }

        static void TrySynthesize(GClass2666 inst, ScanEmulation.Result r)
        {
            if (r.EdgeIdx < 0 || !StateGuardsPass(inst))
            {
                return;
            }
            if (r.ClearanceBlocked || r.ProbeAborted)
            {
                return;
            }

            var s = inst.ImountingPointDetectionSettings_0;
            Vector3 point;
            string source;
            if (!r.BestTop.Equals(Vector3.negativeInfinity))
            {
                point = r.BestTop + Vector3.up * s.PointHorizontalMountOffset;
                source = "re-scanned top surface";
            }
            else if (r.EdgeTopExposed)
            {
                point = r.EdgeHit.point + Vector3.up * (r.StepSize + s.PointHorizontalMountOffset);
                source = "thin-rail front edge";
            }
            else
            {
                return;
            }

            Vector3 dir = -r.EdgeHit.normal;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }
            dir.Normalize();

            Plugin.Log.LogInfo($"Synthesizing mount point from {source} at {MountDebugLog.V(point)} on {r.EdgeHit.collider.name}");
            inst.method_0(new MountPointData(point, dir, EMountSideDirection.Forward));
        }

        static void LogDump(GClass2666 inst, ScanEmulation.Result r)
        {
            var s = inst.ImountingPointDetectionSettings_0;
            var sb = new StringBuilder();
            sb.AppendLine("DeepScan after NO-POINT result:");
            sb.AppendLine($"  basePos={MountDebugLog.V(inst.Vector3_0)} fwd={MountDebugLog.V(inst.Vector3_1)} baseY={inst.Float_0:F2} hOff={inst.Float_1:F2} mask=0x{inst.LayerMask_0.value:X}");
            sb.AppendLine($"  stage1: column top y={r.ColumnTop.y:F2} span={r.GridSize:F2}m steps={r.Steps} rayLen={s.RaycastDistance:F2} edgeDist={s.EdgeDetectionDistance:F2}");

            AppendGroupedRows(sb, r.Steps,
                i => r.HasHit[i] ? r.Hits[i].collider.name : null,
                (start, end, key) =>
                {
                    if (key == null)
                    {
                        return $"    rows {start}-{end}: MISS";
                    }
                    var first = r.Hits[start];
                    var last = r.Hits[end];
                    string layer = LayerMask.LayerToName(first.collider.gameObject.layer);
                    return $"    rows {start}-{end}: {key} [{layer}] d={first.distance:F2}..{last.distance:F2} y={first.point.y:F2}..{last.point.y:F2}";
                });

            if (r.EdgeIdx < 0)
            {
                sb.AppendLine("  stage1 result: NO edge selected -> only the wall-mount fallback ran");
                MountDebugLog.Log(sb.ToString());
                return;
            }

            sb.AppendLine($"  stage1 result: edge row {r.EdgeIdx} on {r.EdgeHit.collider.name} at {MountDebugLog.V(r.EdgeHit.point)} d={r.EdgeHit.distance:F2} topExposed={r.EdgeTopExposed}");

            if (r.ClearanceBlocked)
            {
                string layer = LayerMask.LayerToName(r.ClearanceHit.collider.gameObject.layer);
                sb.AppendLine($"  stage2 result: clearance ray BLOCKED by {r.ClearanceHit.collider.name} [{layer}] at {r.ClearanceHit.distance:F3}m -> point discarded");
                MountDebugLog.Log(sb.ToString());
                return;
            }

            AppendGroupedRows(sb, r.ProbeHasHit.Length,
                j => r.ProbeHasHit[j] ? r.ProbeHits[j].collider.name : null,
                (start, end, key) =>
                {
                    if (key == null)
                    {
                        return $"    probes {start}-{end}: MISS (nothing within {s.SecondCheckVerticalDistance:F2}m below)";
                    }
                    var first = r.ProbeHits[start];
                    float dot = Vector3.Dot(first.normal, Vector3.up);
                    return $"    probes {start}-{end}: {key} y={first.point.y:F3} dot={dot:F2} aboveEdge={first.point.y > r.EdgeHit.point.y}";
                });

            if (r.ProbeAborted)
            {
                sb.AppendLine("  stage2 result: probe hit at distance 0 -> vanilla hard-discards this candidate");
            }
            else
            {
                sb.AppendLine(r.BestTop.Equals(Vector3.negativeInfinity)
                    ? "  stage2 result: no valid top surface -> point discarded"
                    : $"  stage2 result: top surface at {MountDebugLog.V(r.BestTop)}");
            }

            MountDebugLog.Log(sb.ToString());
        }

        static void AppendGroupedRows(StringBuilder sb, int count, Func<int, string> keyOf, Func<int, int, string, string> format)
        {
            int i = 0;
            while (i < count)
            {
                int start = i;
                string key = keyOf(i);
                while (i < count && keyOf(i) == key)
                {
                    i++;
                }
                sb.AppendLine(format(start, i - 1, key));
            }
        }
    }
}
