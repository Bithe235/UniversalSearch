#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BatchSight.Core;
using System.Collections.Generic;

namespace BatchSight.UI
{
    [InitializeOnLoad]
    public static class BatchSightHierarchyIcon
    {
        private static Dictionary<int, BatchingStatus> statusCache = new Dictionary<int, BatchingStatus>();
        private static List<BatchingReport> lastReportsReference;

        static BatchSightHierarchyIcon()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var reports = BatchSightState.Reports;
            if (reports == null || reports.Count == 0) return;

            if (lastReportsReference != reports)
            {
                lastReportsReference = reports;
                statusCache.Clear();
                foreach (var rep in reports)
                {
                    if (rep != null && rep.renderer != null)
                    {
                        var go = rep.renderer.gameObject;
                        if (go != null)
                        {
                            statusCache[go.GetInstanceID()] = rep.status;
                        }
                    }
                }
            }

            if (statusCache.TryGetValue(instanceID, out BatchingStatus status))
            {
                Rect r = new Rect(selectionRect.xMax - 14, selectionRect.yMin + 2, 12, 12);
                
                Color c = Color.white;
                if (status == BatchingStatus.Green) c = Color.green;
                else if (status == BatchingStatus.Yellow) c = Color.yellow;
                else if (status == BatchingStatus.Red) c = Color.red;

                EditorGUI.DrawRect(r, c);
            }
        }
    }
}
#endif
