#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BatchSight.Utils;

namespace BatchSight.Core
{
    public static class BatchingAnalyzer
    {
        public static List<BatchingReport> RunAnalysis()
        {
            var reports = new List<BatchingReport>();
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            foreach (var r in renderers)
            {
                if (r == null) continue;
                var rep = new BatchingReport(r);
                AnalyzeRenderer(r, rep, renderers);
                reports.Add(rep);
            }

            BatchSightState.Reports = reports;
            return reports;
        }

        static void AnalyzeRenderer(Renderer r, BatchingReport rep, Renderer[] allRenderers)
        {
            rep.issues.Clear();
            rep.status = BatchingStatus.Yellow;

            if (r is SkinnedMeshRenderer)
            {
                rep.issues.Add(BatchingIssue.SkinnedMeshRenderer);
                rep.status = BatchingStatus.Red;
                return;
            }

            if (r.sharedMaterials != null && r.sharedMaterials.Length > 1)
            {
                rep.issues.Add(BatchingIssue.MultiMaterial);
                rep.status = BatchingStatus.Red;
            }

            bool isPartOfStatic = false;
            try { isPartOfStatic = r.isPartOfStaticBatch; } catch { }
            if (isPartOfStatic)
            {
                rep.status = BatchingStatus.Green;
                rep.issues.Add(BatchingIssue.None);
                return;
            }

            var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            if ((flags & StaticEditorFlags.BatchingStatic) == 0)
            {
                rep.issues.Add(BatchingIssue.NotStatic);
                if (rep.status != BatchingStatus.Red) rep.status = BatchingStatus.Yellow;
            }

            try
            {
                if (PrefabUtility.IsPartOfPrefabInstance(r.gameObject))
                {
                    if (!rep.issues.Contains(BatchingIssue.PrefabInstance)) rep.issues.Add(BatchingIssue.PrefabInstance);
                    if (rep.status != BatchingStatus.Red) rep.status = BatchingStatus.Yellow;
                }
            }
            catch { }

            var pipeline = PipelineDetector.GetPipeline();
            var mats = r.sharedMaterials;
            if (mats != null && mats.Length > 0)
            {
                foreach (var m in mats)
                {
                    if (m == null) continue;
                    var sh = m.shader;
                    if (sh == null || !sh.isSupported)
                    {
                        if (!rep.issues.Contains(BatchingIssue.SRPIncompatible)) rep.issues.Add(BatchingIssue.SRPIncompatible);
                        rep.status = BatchingStatus.Red;
                    }

                    if (m.name != null && m.name.Contains("(Instance)"))
                    {
                        if (!rep.issues.Contains(BatchingIssue.UniqueMaterialInstance)) rep.issues.Add(BatchingIssue.UniqueMaterialInstance);
                        if (rep.status != BatchingStatus.Red) rep.status = BatchingStatus.Red;
                    }

                    if (m.enableInstancing)
                    {
                        rep.status = BatchingStatus.Green;
                    }
                }
            }

            // PROPERTY BLOCK CHECK
            bool hasBlock = false;
            try { hasBlock = r.HasPropertyBlock(); } catch { }
            if (hasBlock)
            {
                if (!rep.issues.Contains(BatchingIssue.MaterialPropertyBlock)) rep.issues.Add(BatchingIssue.MaterialPropertyBlock);
                rep.status = BatchingStatus.Red;
            }

            if (pipeline != "Built-in")
            {
                try
                {
                    var srpBatch = UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
                    if (!srpBatch)
                    {
                        if (!rep.issues.Contains(BatchingIssue.SRPIncompatible)) rep.issues.Add(BatchingIssue.SRPIncompatible);
                        if (rep.status != BatchingStatus.Red) rep.status = BatchingStatus.Yellow;
                    }
                }
                catch { }
            }

            try
            {
                var myIndex = r.lightmapIndex;
                var myShadowMode = r.shadowCastingMode;
                var inLOD = r.GetComponentInParent<LODGroup>() != null;

                foreach (var other in allRenderers)
                {
                    if (other == null || other == r) continue;
                    if (other.GetType() != r.GetType()) continue;
                    if (other.sharedMaterial == null || r.sharedMaterial == null) continue;

                    if (other.sharedMaterial == r.sharedMaterial)
                    {
                        // LIGHTMAP MISMATCH
                        if (other.lightmapIndex != myIndex)
                        {
                            if (!rep.issues.Contains(BatchingIssue.DifferentLightmap)) rep.issues.Add(BatchingIssue.DifferentLightmap);
                            rep.status = BatchingStatus.Red;
                        }

                        // SHADOW MODE MISMATCH
                        if (other.shadowCastingMode != myShadowMode)
                        {
                            if (!rep.issues.Contains(BatchingIssue.ShadowMismatch)) rep.issues.Add(BatchingIssue.ShadowMismatch);
                            rep.status = BatchingStatus.Red;
                        }
                    }
                }

                if (inLOD && rep.status != BatchingStatus.Red)
                {
                    if (!rep.issues.Contains(BatchingIssue.LODMismatch)) rep.issues.Add(BatchingIssue.LODMismatch);
                    if (rep.status != BatchingStatus.Red) rep.status = BatchingStatus.Yellow;
                }
            }
            catch { }

            if (rep.issues.Count == 0)
            {
                rep.status = BatchingStatus.Green;
                rep.issues.Add(BatchingIssue.None);
            }
        }
    }
}
#endif
