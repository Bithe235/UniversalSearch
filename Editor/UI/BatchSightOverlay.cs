#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Overlays;
using BatchSight.Core;

namespace BatchSight.UI
{
    [Overlay(typeof(SceneView), "BatchSight.Overlay")]
    [Icon("Assets/BatchSight/Editor/Resources/BatchSight_Icons/overlay.png")]
    public class BatchSightOverlay : Overlay
    {
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            root.Add(new IMGUIContainer(OnGUI));
            return root;
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("BatchSight", EditorStyles.boldLabel);
            var reports = BatchSightState.Reports ?? new System.Collections.Generic.List<BatchingReport>();
            int total = reports.Count;
            int red = 0, yellow = 0, green = 0;
            foreach (var r in reports)
            {
                if (r == null) continue;
                if (r.status == BatchingStatus.Red) red++;
                else if (r.status == BatchingStatus.Yellow) yellow++;
                else if (r.status == BatchingStatus.Green) green++;
            }
            GUILayout.Label($"Red: {red}   Yellow: {yellow}   Green: {green}   Total: {total}");
            if (GUILayout.Button("Analyze"))
            {
                BatchingAnalyzer.RunAnalysis();
                SceneView.RepaintAll();
            }
            GUILayout.EndVertical();
        }
    }
}
#endif
