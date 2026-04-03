# UniversalSearch

BatchSight is an editor-only Unity tool to analyze scene renderers for batching compatibility and provide visual overlays and fixes.

Installation: copy the `BatchSight` folder into your project's `Assets/` folder. Open the tool via `Tools → BatchSight`.

Features implemented in this scaffold:
- Core data model (`BatchingReport`, `BatchingIssue`)
- `BatchingAnalyzer` with common RED/YELLOW/GREEN checks (SkinnedMeshRenderer, multi-material, static batching flag, lightmap mismatch, basic SRP shader heuristics)
- `FixApplier` that can mark GameObjects static or enable GPU instancing. Fixes applied to prefab instances will attempt to modify the prefab asset safely using `PrefabUtility.LoadPrefabContents`.
- `LiveMode` (periodic dirty-check reanalysis)
- Scene overlay visualization (`SceneOverlay` drawing colorized bounds and labels)
- Scene View `Overlay` widget (`BatchSightOverlay`) for quick stats and analyze button
- UIElements-based `BatchSightWindow` with `ListView`, filters, health bar, and detail pane

Notes & Limitations:
- SRP Batcher deep shader-inspection is heuristic-based; for full correctness use `ShaderUtil` analysis (planned next).
- `MaterialPropertyBlock` usage is hard to detect in editor without runtime sampling; the tool flags probable causes heuristically.
- Prefab edits are applied to the prefab asset. The implementation uses `LoadPrefabContents`/`SaveAsPrefabAsset` and attempts to find matching children by name hierarchy — in complex cases you should review changes before committing.

Next planned work:
- Deep SRP Batcher shader inspection using `UnityEditor.ShaderUtil`
- Better prefab mapping, change previews, and undo support for asset edits
- Icons, documentation polish, and automated tests
