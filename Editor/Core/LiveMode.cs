#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BatchSight.Core
{
    [InitializeOnLoad]
    public static class LiveMode
    {
        static double lastTick = 0f;
        public static bool Enabled { get; private set; } = false;
        public static double IntervalSeconds = 2.0;

        // Cache of hashes keyed by renderer instance ID
        private static Dictionary<int, int> hashCache = new Dictionary<int, int>();

        static LiveMode()
        {
            EditorApplication.update += Update;
        }

        public static void SetEnabled(bool on)
        {
            Enabled = on;
            if (on)
            {
                // Run initial analysis and build hash cache
                BatchingAnalyzer.RunAnalysis();
                RebuildHashCache();
                BatchSightState.NotifyUpdated();
                lastTick = EditorApplication.timeSinceStartup;
            }
            else
            {
                hashCache.Clear();
            }
        }

        static void Update()
        {
            if (!Enabled) return;
            if (EditorApplication.timeSinceStartup - lastTick < IntervalSeconds) return;
            lastTick = EditorApplication.timeSinceStartup;

            // Check if any renderer's hash changed
            bool dirty = false;
            var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                int id = r.GetInstanceID();
                int newHash = ComputeHash(r);

                if (!hashCache.TryGetValue(id, out int oldHash) || oldHash != newHash)
                {
                    dirty = true;
                    break;
                }
            }

            // Also dirty if renderer count changed (object added/removed)
            if (!dirty && allRenderers.Length != hashCache.Count)
                dirty = true;

            if (dirty)
            {
                BatchingAnalyzer.RunAnalysis();
                RebuildHashCache();
                BatchSightState.NotifyUpdated();
                SceneView.RepaintAll();
            }
        }

        static void RebuildHashCache()
        {
            hashCache.Clear();
            var reports = BatchSightState.Reports;
            if (reports == null) return;
            foreach (var rep in reports)
            {
                if (rep == null || rep.renderer == null) continue;
                hashCache[rep.renderer.GetInstanceID()] = ComputeHash(rep.renderer);
            }
        }

        static int ComputeHash(Renderer r)
        {
            unchecked
            {
                int hash = 17;
                if (r.sharedMaterials != null)
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        hash = hash * 23 + (m != null ? m.GetInstanceID() : 0);
                        if (m != null)
                            hash = hash * 23 + (m.enableInstancing ? 1 : 0);
                    }
                }
                var t = r.transform;
                hash = hash * 23 + t.localPosition.GetHashCode();
                hash = hash * 23 + t.localRotation.GetHashCode();
                hash = hash * 23 + t.localScale.GetHashCode();
                try { hash = hash * 23 + (int)GameObjectUtility.GetStaticEditorFlags(r.gameObject); } catch { }
                try { hash = hash * 23 + r.lightmapIndex; } catch { }
                return hash;
            }
        }
    }
}
#endif
