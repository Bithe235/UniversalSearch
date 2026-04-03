# BatchSight Implementation Plan

## Issue 1: "Mark Static" Button Not Working
**Cause:** 
1. In `BatchSightWindow.cs`, the `Mark Static` button is wrapped in `if (!PrefabUtility.IsPartOfPrefabInstance(rep.renderer.gameObject))`. Since most GameObjects are prefabs, the button is effectively dead for them.
2. Inside `FixApplier.cs`, the prefab logic `ApplyMarkStaticToPrefab` has bugs. Specifically, `GetRelativePathToRoot` calculates paths incorrectly which causes `FindByPath` to fail silently. Even if it worked, modifying the base prefab instead of the instance overrides might not be the user's intention.

**Solution:**
- Update `BatchSightWindow.cs` to remove the restriction and allow the button to execute.
- In `FixApplier.cs`, bypass the custom and buggy `ApplyMarkStaticToPrefab` and `ApplyEnableInstancingToPrefab` functions. Instead, apply the `MarkStatic` and `EnableInstancing` operations directly to the instance, and use `PrefabUtility.RecordPrefabInstancePropertyModifications(go)` to record them as valid prefab overrides. This is standard Unity behavior and works correctly without needing to unpack and overwrite the base prefab on disk.

## Issue 2: "Enable GPU Instancing" Not Working on Private Materials
**Cause:**
1. Built-in materials (like `Default-Material`) or internal/read-only materials cannot be modified on disk. Setting `enableInstancing = true` on them may superficially change the property in memory, but it doesn't actually affect the real asset or build. 
2. The user noted that "matrial is private matrial not chaning system have but still showing its enabled gpu isntacing". This fits the description of an immutable built-in material.

**Solution:**
- In `FixApplier.cs`, check if the material is an immutable built-in material by checking if `AssetDatabase.GetAssetPath(m)` is null or doesn't end in `.mat` (specifically, check if it contains `unity_builtin_extra`). 
- If the material cannot be modified, alert the user using `EditorUtility.DisplayDialog`.
- Provide a robust way to enable instancing on valid materials and skip invalid ones.

## Issue 3: "Live Mode" Toggle On but Not Working
**Cause:**
1. `LiveMode.cs` does continuous checking using a `ComputeHash` function to see if renderers changed. When a difference is found, it calls `BatchingAnalyzer.RunAnalysis()`.
2. However, `BatchingAnalyzer.RunAnalysis()` clears and recreates the `BatchSightState.Reports` list from scratch. When it recreates the list, the `lastHash` field of all reports defaults to `0`. 
3. On the next tick, `LiveMode` compares the actual hash against `lastHash` (`0`), detects a "change", and reruns analysis in an infinite loop. 
4. Crucially, `BatchSightWindow` doesn't automatically detect these updates because `RefreshList()` is never called when LiveMode reruns the analysis, so the UI list never updates!

**Solution:**
- Provide an `OnAnalysisUpdated` event in `BatchSightState` or explicitly notify the `BatchSightWindow` when LiveMode updates the analysis.
- Update `BatchingAnalyzer.RunAnalysis()` so that instead of throwing away existing reports, it preserves the `lastHash` by carrying it over from the old reports to the new ones, preventing the infinite loop.
- Alternatively, move the hash update to be done *after* `RunAnalysis()` by re-fetching the updated reports.

## Execution Steps
1. Edit `BatchingAnalyzer.cs` to assign `lastHash = ComputeHash(r)` when a `BatchingReport` is created or analyzed, so it's accurate immediately upon analysis. Wait, we can implement `LiveMode.ComputeHash(r)` independently or inside `BatchingAnalyzer.cs`. A better way is to move `ComputeHash` to `BatchingReport`.
2. Fix `LiveMode.cs` to trigger a window refresh and only re-analyze when actual changes occur.
3. Update `FixApplier.cs` to rely on `PrefabUtility.RecordPrefabInstancePropertyModifications` instead of custom prefab writing, and add checks/dialogs for immutable materials.
4. Update `BatchSightWindow.cs` to fix the button conditions and subscribe to an event when LiveMode updates data.
