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
            toolbar.style.alignItems = Align.Center;
            toolbar.style.paddingLeft = 4;
            toolbar.style.paddingRight = 4;

            var analyzeBtn = new Button(() => { RunAnalyze(); }) { text = "Analyze" };
            toolbar.Add(analyzeBtn);

            var liveToggle = new Toggle("Live Mode") { value = LiveMode.Enabled };
            liveToggle.RegisterValueChangedCallback(evt => LiveMode.SetEnabled(evt.newValue));
            toolbar.Add(liveToggle);

            var spacer = new VisualElement(); spacer.style.flexGrow = 1; toolbar.Add(spacer);
            root.Add(toolbar);

            var healthRow = new VisualElement();
            healthRow.style.flexDirection = FlexDirection.Row;
            healthRow.style.alignItems = Align.Center;
            healthRow.style.marginTop = 6;
            healthRow.style.marginBottom = 6;

            healthLabel = new Label("Health: —");
            healthLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            healthRow.Add(healthLabel);

            var healthBar = new VisualElement();
            healthBar.style.width = 200;
            healthBar.style.height = 12;
            healthBar.style.marginLeft = 8;
            healthBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
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
            filterRow.style.marginBottom = 6;
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
            left.style.paddingLeft = 2;
            left.style.paddingRight = 2;
            var right = new VisualElement();
            right.style.paddingLeft = 4;

            listView = new ListView();
            listView.selectionType = SelectionType.Single;
            listView.fixedItemHeight = 20;
            listView.onSelectionChange += objs =>
            {
                foreach (var o in objs)
                {
                    var rep = o as BatchingReport;
                    if (rep != null && rep.renderer != null)
                        Selection.activeGameObject = rep.renderer.gameObject;
                }
            };

            listView.makeItem = () =>
            {
                var lbl = new Label();
                lbl.style.unityTextAlign = TextAnchor.MiddleLeft;
                lbl.style.paddingLeft = 4;
                return lbl;
            };

            listView.bindItem = (element, i) =>
            {
                var lbl = (Label)element;
                if (i >= 0 && i < displayed.Count)
                {
                    var rep = displayed[i];
                    lbl.text = rep.Name + "  [" + rep.status.ToString() + "]";
                }
                else lbl.text = "";
            };

            left.Add(listView);

            var details = new VisualElement();
            details.style.flexDirection = FlexDirection.Column;
            details.style.paddingLeft = 6;
            details.style.paddingTop = 2;

            var detailTitle = new Label("Details");
            detailTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            details.Add(detailTitle);

            var issueLabel = new Label();
            issueLabel.name = "issueLabel";
            issueLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            issueLabel.style.whiteSpace = WhiteSpace.Normal;
            issueLabel.style.flexWrap = Wrap.Wrap;
            details.Add(issueLabel);

            var fixStaticBtn = new Button(() =>
            {
                var sel = listView.selectedIndex;
                if (sel >= 0 && sel < displayed.Count)
                {
                    var rep = displayed[sel];
                    FixApplier.MarkStatic(rep);
                    BatchingAnalyzer.RunAnalysis();
                    RefreshList();
                    SceneView.RepaintAll();
                }
            }) { text = "Mark Static" };
            details.Add(fixStaticBtn);

            var fixInstBtn = new Button(() =>
            {
                var sel = listView.selectedIndex;
                if (sel >= 0 && sel < displayed.Count)
                {
                    var rep = displayed[sel];
                    FixApplier.EnableGPUInstancing(rep);
                    BatchingAnalyzer.RunAnalysis();
                    RefreshList();
                    SceneView.RepaintAll();
                }
            }) { text = "Enable GPU Instancing" };
            details.Add(fixInstBtn);

            split.Add(left);
            split.Add(details);

            root.Add(split);

            RunAnalyze();
        }

        void RunAnalyze()
        {
            BatchingAnalyzer.RunAnalysis();
            RefreshList();
            SceneView.RepaintAll();
        }

        void RefreshList()
        {
            var all = BatchSightState.Reports ?? new List<BatchingReport>();
            displayed.Clear();
            foreach (var r in all)
            {
                if (r == null) continue;
                if (r.status == BatchingStatus.Green && !showGreenToggle.value) continue;
                if (r.status == BatchingStatus.Yellow && !showYellowToggle.value) continue;
                if (r.status == BatchingStatus.Red && !showRedToggle.value) continue;
                displayed.Add(r);
            }

            listView.itemsSource = displayed;
            listView.Rebuild();

            int total = all.Count;
            int green = 0;
            foreach (var r in all) if (r != null && r.status == BatchingStatus.Green) green++;
            float pct = total > 0 ? (float)green / total : 0f;
            healthLabel.text = $"Health: {green}/{total} ({Mathf.RoundToInt(pct*100)}%)";
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
