#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BatchSight.Core
{
    public static class FixApplier
    {
        public static void MarkStatic(BatchingReport report)
        {
            if (report == null || report.renderer == null) return;
            var go = report.renderer.gameObject;

            Undo.RecordObject(go, "BatchSight: Mark Static");
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            flags |= StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            EditorUtility.SetDirty(go);

            // If it's a prefab instance, record the override so it persists
            try
            {
                if (PrefabUtility.IsPartOfPrefabInstance(go))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            }
            catch { }
        }

        public static void EnableGPUInstancing(BatchingReport report, bool silent = false)
        {
            if (report == null || report.renderer == null) return;
            var mats = report.renderer.sharedMaterials;
            if (mats == null) return;

            bool anySkipped = false;

            foreach (var m in mats)
            {
                if (m == null) continue;

                // Check if this is an immutable built-in material
                string assetPath = AssetDatabase.GetAssetPath(m);
                if (string.IsNullOrEmpty(assetPath)
                    || assetPath.Contains("unity_builtin_extra")
                    || assetPath.Contains("unity default resources")
                    || !assetPath.EndsWith(".mat"))
                {
                    anySkipped = true;
                    continue;
                }

                if (m.enableInstancing) continue;

                Undo.RecordObject(m, "BatchSight: Enable GPU Instancing");
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
            }

            if (anySkipped && !silent)
            {
                EditorUtility.DisplayDialog(
                    "BatchSight",
                    "One or more materials are built-in or read-only and cannot be modified.\n\n" +
                    "To fix this, assign a custom material to the renderer first.",
                    "OK");
            }
        }

        public static void TryEnableSRPBatcher(bool enable)
        {
            try
            {
                UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching = enable;
            }
            catch { }
        }
    }
}
#endif
