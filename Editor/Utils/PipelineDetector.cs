#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace BatchSight.Utils
{
    public static class PipelineDetector
    {
        public static string GetPipeline()
        {
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (rp == null) return "Built-in";
            var n = rp.GetType().Name;
            if (n.Contains("Universal")) return "URP";
            if (n.Contains("HDRender") || n.Contains("HighDefinition")) return "HDRP";
            return n;
        }
    }
}
#endif
