#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BatchSight.Core;

namespace BatchSight.UI
{
    [InitializeOnLoad]
    public static class SceneOverlay
    {
        static SceneOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView sv)
        {
            if (BatchSightState.Reports == null) return;

            foreach (var rep in BatchSightState.Reports)
            {
                if (rep == null || rep.renderer == null) continue;
                var b = rep.renderer.bounds;
                Color col = rep.status == BatchingStatus.Green ? Color.green : (rep.status == BatchingStatus.Yellow ? Color.yellow : Color.red);
                var face = new Color(col.r, col.g, col.b, 0.08f);
                var outline = new Color(col.r, col.g, col.b, 0.9f);

                var verts = new Vector3[4]
                {
                    new Vector3(b.min.x, b.min.y, b.min.z),
                    new Vector3(b.max.x, b.min.y, b.min.z),
                    new Vector3(b.max.x, b.min.y, b.max.z),
                    new Vector3(b.min.x, b.min.y, b.max.z)
                };

                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.DrawSolidRectangleWithOutline(verts, face, outline);
                Handles.color = outline;
                Handles.DrawWireCube(b.center, b.size);
                Handles.Label(b.center + Vector3.up * (b.extents.y + 0.2f), rep.Name);
            }
        }
    }
}
#endif
