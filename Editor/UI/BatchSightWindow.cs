#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BatchSight.Core;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace BatchSight.UI
{
    public class BatchSightWindow : EditorWindow
    {
        [MenuItem("Tools/BatchSight")]
        public static void Open()
        {
            var w = GetWindow<BatchSightWindow>("BatchSight");
            w.minSize = new Vector2(380, 320);
        }

        ListView listView;
        List<BatchingReport> displayed = new List<BatchingReport>();
        Toggle showGreenToggle, showYellowToggle, showRedToggle;
        Label healthLabel;
        VisualElement healthBarFill;

        void OnEnable()
        {
            BatchSightState.OnReportsUpdated += OnLiveModeUpdated;

            var root = rootVisualElement;
            root.Clear();

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 4;
            toolbar.style.paddingRight = 4;
            toolbar.style.paddingTop = 2;
            toolbar.style.paddingBottom = 2;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f, 1f);

            var analyzeBtn = new Button(() => { RunAnalyze(); }) { text = "Analyze" };
            toolbar.Add(analyzeBtn);

            var exportBtn = new Button(() => { ExportReport(); }) { text = "Export" };
            toolbar.Add(exportBtn);

            searchField = new TextField();
            searchField.placeholderText = "Search renderers...";
            searchField.style.flexGrow = 1;
            searchField.style.marginLeft = 8;
            searchField.RegisterValueChangedCallback(evt => RefreshList());
            toolbar.Add(searchField);

            var liveToggle = new Toggle("Live") { value = LiveMode.Enabled };
            liveToggle.style.marginLeft = 8;
            liveToggle.RegisterValueChangedCallback(evt => LiveMode.SetEnabled(evt.newValue));
            toolbar.Add(liveToggle);

            root.Add(toolbar);

            var healthRow = new VisualElement();
            healthRow.style.flexDirection = FlexDirection.Row;
            healthRow.style.alignItems = Align.Center;
            healthRow.style.paddingLeft = 6;
            healthRow.style.marginTop = 4;
            healthRow.style.marginBottom = 4;

            healthLabel = new Label("Health: —");
            healthLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            healthRow.Add(healthLabel);

            var healthBar = new VisualElement();
            healthBar.style.width = 150;
            healthBar.style.height = 10;
            healthBar.style.marginLeft = 8;
            healthBar.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            healthBar.style.borderTopWidth = 1;
            healthBar.style.borderBottomWidth = 1;
            healthBar.style.borderLeftWidth = 1;
            healthBar.style.borderRightWidth = 1;
            healthBarFill = new VisualElement();
            healthBarFill.style.height = Length.Percent(100);
            healthBarFill.style.width = 0;
            healthBarFill.style.backgroundColor = Color.green;
            healthBar.Add(healthBarFill);
            healthRow.Add(healthBar);

            root.Add(healthRow);

            var filterRow = new VisualElement();
            filterRow.style.flexDirection = FlexDirection.Row;
            filterRow.style.paddingLeft = 6;
            filterRow.style.marginBottom = 4;
            showGreenToggle = new Toggle("Green") { value = true };
            showYellowToggle = new Toggle("Yellow") { value = true };
            showRedToggle = new Toggle("Red") { value = true };
            showGreenToggle.RegisterValueChangedCallback(evt => RefreshList());
            showYellowToggle.RegisterValueChangedCallback(evt => RefreshList());
            showRedToggle.RegisterValueChangedCallback(evt => RefreshList());
            filterRow.Add(showRedToggle);
            filterRow.Add(showYellowToggle);
            filterRow.Add(showGreenToggle);
            root.Add(filterRow);

            var split = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            var left = new VisualElement();
            var right = new VisualElement();
            right.style.paddingLeft = 8;
            right.style.paddingTop = 6;

            listView = new ListView();
            listView.selectionType = SelectionType.Single;
            listView.fixedItemHeight = 22;
            listView.itemsSource = displayed;

            var details = new VisualElement();
            var detailTitle = new Label("Renderer Details");
            detailTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            detailTitle.style.fontSize = 13;
            detailTitle.style.marginBottom = 8;
            details.Add(detailTitle);

            var issueLabel = new Label("Select an item to see details.");
            issueLabel.style.whiteSpace = WhiteSpace.Normal;
            issueLabel.style.marginBottom = 12;
            issueLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            details.Add(issueLabel);

            var fixBtnRow = new VisualElement();
            fixBtnRow.style.flexDirection = FlexDirection.Row;
            fixBtnRow.style.flexWrap = Wrap.Wrap;

            var fixStaticBtn = new Button(() =>
            {
                var sel = listView.selectedIndex;
                if (sel >= 0 && sel < displayed.Count)
                {
                    FixApplier.MarkStatic(displayed[sel]);
                    RunAnalyze();
                }
            }) { text = "Mark Static" };
            fixBtnRow.Add(fixStaticBtn);

            var fixInstBtn = new Button(() =>
            {
                var sel = listView.selectedIndex;
                if (sel >= 0 && sel < displayed.Count)
                {
                    FixApplier.EnableGPUInstancing(displayed[sel]);
                    RunAnalyze();
                }
            }) { text = "Instancing" };
            fixBtnRow.Add(fixInstBtn);

            details.Add(fixBtnRow);

            var bulkRow = new VisualElement();
            bulkRow.style.marginTop = 16;
            bulkRow.style.paddingTop = 8;
            bulkRow.style.borderTopWidth = 1;
            bulkRow.style.borderTopColor = new Color(0.2f, 0.2f, 0.2f, 1f);

            var bulkTitle = new Label("Bulk Actions (Filtered)");
            bulkTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            bulkTitle.style.marginBottom = 4;
            bulkRow.Add(bulkTitle);

            var bulkBtns = new VisualElement();
            bulkBtns.style.flexDirection = FlexDirection.Row;

            bulkBtns.Add(new Button(() =>
            {
                if (EditorUtility.DisplayDialog("UniversalSearch", $"Apply 'Batching Static' to {displayed.Count} filtered renderers?", "Yes", "Cancel"))
                {
                    foreach (var r in displayed) FixApplier.MarkStatic(r);
                    RunAnalyze();
                }
            }) { text = "Fix Static" });

            bulkBtns.Add(new Button(() =>
            {
                if (EditorUtility.DisplayDialog("UniversalSearch", $"Enable GPU Instancing for {displayed.Count} filtered renderers?", "Yes", "Cancel"))
                {
                    foreach (var r in displayed) FixApplier.EnableGPUInstancing(r, true);
                    RunAnalyze();
                }
            }) { text = "Fix Instancing" });

            bulkBtns.Add(new Button(() =>
            {
                var gos = new List<GameObject>();
                foreach (var r in displayed) if (r.renderer != null) gos.Add(r.renderer.gameObject);
                Selection.objects = gos.ToArray();
            }) { text = "Select All" });

            bulkRow.Add(bulkBtns);
            details.Add(bulkRow);

            right.Add(details);

            listView.onSelectionChange += objs =>
            {
                foreach (var o in objs)
                {
                    var rep = o as BatchingReport;
                    if (rep != null)
                    {
                        if (rep.renderer != null) Selection.activeGameObject = rep.renderer.gameObject;
                        
                        string text = "Issues:\n";
                        foreach (var issue in rep.issues)
                        {
                            text += " • " + GetIssueDescription(issue) + "\n";
                        }
                        issueLabel.text = text;
                    }
                }
            };

            listView.makeItem = () => new Label() { style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4 } };
            listView.bindItem = (element, i) =>
            {
                var lbl = (Label)element;
                var rep = displayed[i];
                lbl.text = (rep.status == BatchingStatus.Green ? "🟢 " : rep.status == BatchingStatus.Yellow ? "🟡 " : "🔴 ") + rep.Name;
            };

            left.Add(listView);
            split.Add(left);
            split.Add(right);
            root.Add(split);

            RunAnalyze();
        }

        string GetIssueDescription(BatchingIssue issue)
        {
            switch (issue)
            {
                case BatchingIssue.None: return "No major batching issues detected.";
                case BatchingIssue.SkinnedMeshRenderer: return "SkinnedMeshRenderers cannot be static batched.";
                case BatchingIssue.MultiMaterial: return "Multiple materials cause multiple draw calls.";
                case BatchingIssue.NotStatic: return "GameObject is not marked 'Batching Static'.";
                case BatchingIssue.DifferentLightmap: return "Shares material but uses a different lightmap index.";
                case BatchingIssue.SRPIncompatible: return "Shader or settings are incompatible with SRP Batcher.";
                case BatchingIssue.MaterialPropertyBlock: return "Renderer has a MaterialPropertyBlock (per-instance properties break batching).";
                case BatchingIssue.UniqueMaterialInstance: return "Material is a unique instance (e.g. accessed via .material instead of .sharedMaterial).";
                case BatchingIssue.PrefabInstance: return "Part of a prefab instance; changes should be applied to the prefab asset.";
                case BatchingIssue.LODMismatch: return "Part of an LODGroup; requires consistent LOD settings to batch.";
                case BatchingIssue.ShadowMismatch: return "Shadow casting/receiving settings differ from other objects sharing this material.";
                default: return issue.ToString();
            }
        }

        void RunAnalyze()
        {
            BatchingAnalyzer.RunAnalysis();
            RefreshList();
        }

        void RefreshList()
        {
            var all = BatchSightState.Reports ?? new List<BatchingReport>();
            displayed.Clear();
            string filter = searchField?.value?.ToLower() ?? "";

            foreach (var r in all)
            {
                if (r == null) continue;
                if (!string.IsNullOrEmpty(filter) && !r.Name.ToLower().Contains(filter)) continue;
                if (r.status == BatchingStatus.Green && !showGreenToggle.value) continue;
                if (r.status == BatchingStatus.Yellow && !showYellowToggle.value) continue;
                if (r.status == BatchingStatus.Red && !showRedToggle.value) continue;
                displayed.Add(r);
            }

            listView.Rebuild();

            int total = all.Count;
            int green = 0;
            foreach (var r in all) if (r != null && r.status == BatchingStatus.Green) green++;
            float pct = total > 0 ? (float)green / total : 0f;
            healthLabel.text = $"Scene Health: {Mathf.RoundToInt(pct * 100)}%";
            healthBarFill.style.width = Length.Percent(pct * 100f);
            healthBarFill.style.backgroundColor = Color.Lerp(Color.red, Color.green, pct);
        }

        void OnDisable()
        {
            BatchSightState.OnReportsUpdated -= OnLiveModeUpdated;
        }

        void OnLiveModeUpdated()
        {
            RefreshList();
        }
    }
}
#endif
