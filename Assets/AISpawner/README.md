# AISpawner

Runtime AI organism browser + AR placement for the Marine Biology AR application.
Everything for this feature lives in `Assets/AISpawner/` — the existing `AR_Spawn`
scene and its scripts are **never modified**; they are reused as-is.

## What it does

1. On entering `AISpawnerScene`, reads Firestore `aiContent/marine`
   (`manifestUrl`, `dataset`, `version`, `count`, `enabled`). Nothing is hardcoded.
2. If `enabled == false` → the AI feature disables gracefully.
3. Downloads `manifest.json` fresh **every scene entry** (metadata only, never cached),
   so organisms added to GitHub appear without an app update.
4. The **AI button** opens a searchable browser (293+ organisms) with loading /
   empty / error+retry states, generated dynamically from the manifest.
5. Selecting an organism downloads its GLB + facts via `UnityWebRequest` into
   `Application.temporaryCachePath/aispawner/` with a progress card, then loads it
   through the project's existing **glTFast** pipeline.
6. Placement / move / rotate / scale / delete reuse the existing XRI pipeline:
   `ObjectSpawner` + `ARInteractorSpawnTrigger` for tap-to-place on detected planes,
   `XRGrabInteractable` + `ARTransformer` (configuration copied from `OctoPrefab`)
   for manipulation. Only one placement operation is active at a time.
7. **LRU cache, max 2 models** — downloading a third deletes the least recently used.
8. On scene exit every cached GLB + facts file is deleted. Next visit re-downloads.
9. Failed downloads (404 / timeout / offline / corrupt asset) show the friendly
   "limited compute capacity" message, clean up partial files, and offer retry.

## Building the scene

**Marine AR → AI Spawner → Build AISpawner Scene**

The builder:
- copies `AR_Spawn.unity` → `Assets/AISpawner/Scenes/AISpawnerScene.unity`
  (source untouched) and strips the template object menu UI,
- keeps the whole AR stack: XR Origin (AR Rig), AR Session, screen-space
  interactor, ObjectSpawner/spawn trigger, EventSystem, BackNavigator,
- generates `OrganismRig.prefab` (manipulation components copied from OctoPrefab),
  `OrganismListItem.prefab`, `FactsRow.prefab` and the UI sprites,
- builds the AISpawner UI (browser panel, search, progress card, prompt banner,
  facts sheet, delete/facts buttons) and wires `AISpawnManager`,
- registers the scene in the **Essential Package** Addressables group as
  `AISpawnerScene` — it ships with the essential download automatically.

Safe to re-run; it rebuilds everything from scratch. Load the scene with
`Addressables.LoadSceneAsync("AISpawnerScene")` (same pattern as `AR_Spawn`).

## Architecture

```
AISpawner/
├── Scripts/
│   ├── Models/        Manifest, MarineOrganism (JSON contract), FactsDocument
│   ├── Services/      FirestoreService, ManifestService, DownloadService, CacheService
│   ├── Repository/    MarineRepository (in-memory, session-scoped)
│   ├── Runtime/       AISpawnManager (composition root), DownloadManager,
│   │                  PlacementManager, OrganismModelBuilder
│   └── UI/            AIButton, AIListController, SearchController,
│                      DownloadProgressUI, PromptBanner, FactsSheetController,
│                      OrganismListItemView, UITween, UISpinner
├── Editor/            AISpawnerSceneBuilder, AISpawnerUiFactory
├── Prefabs/           OrganismRig, OrganismListItem, FactsRow   (generated)
├── Scenes/            AISpawnerScene.unity                      (generated)
└── UI/Sprites/        procedural sprites                        (generated)
```

Services are plain C# classes constructed and injected by `AISpawnManager`
(the only MonoBehaviour entry point — no singletons, no statics). UI views are
passive and event-driven. The manifest schema is treated as a contract; the facts
schema is open and parsed tolerantly so the dataset can evolve.

## Integration points (read-only reuse)

| Existing asset | Used for |
|---|---|
| `AR_Spawn.unity` | copied as the scene base (AR rig, session, interactors) |
| `ObjectSpawner` / `ARInteractorSpawnTrigger` (XRI samples) | tap-to-place |
| `OctoPrefab` | source of XRGrabInteractable/ARTransformer tuning |
| glTFast (`com.unity.cloud.gltfast`) | GLB import (same as `ModelFetcher`) |
| `BackNavigation` + `UICanvasTag` | Android back closes panels, then exits |
| `ARFeatheredPlane` | plane visualization while scanning |
| Firestore (`Firebase.Firestore`) | remote config, same flow as `StartupFlowValidation` |
| "Essential Package" Addressables group | scene delivery |
