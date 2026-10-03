# Contraption Mine

Playable Unity 6.3 LTS / Universal 2D prototype. Open `Assets/_Game/Scenes/Mine.unity` and press Play. Use a portrait 9:16 Game view (540×960 or 1080×1920). The runtime creates its uGUI/TMP interface and physics course; balancing assets live in `Assets/_Game/Resources`. Authored collider prefabs live in `Assets/_Game/Prefabs/Tracks`; their matching terrain coordinates in each FloorData control the placeholder surface visuals.

## Play

1. Floor 1 starts with a blueprint. Press **TEST RUN** to drive automatically.
2. Select a part, then click an empty grid cell. Clicking the same part again removes it. **MOVE** selects a part then an empty destination; **DELETE** removes cells.
3. Connect body parts edge to edge. Each wheel must touch a body part. Use exactly one engine, at least one frame, wheel, and cargo bin.
4. Deliver enough ore and press **AUTOMATE**. Income is the delivered ore divided by elapsed simulation seconds × 60 × floor multiplier.
5. Coins unlock subsequent floors after the previous floor is automated. Improve designs, upgrade the selected part, and retest to replace recorded production.

Existing automated income survives failed tests, edits, resets of the workshop, and part upgrades. **AUTOMATE** explicitly replaces it with the last successful eligible run, even if the rate is lower. **BLUEPRINT** restores a starter design for the selected course.

Cargo bins start at 5 ore and gain 2 per upgrade. Cargo adds mass. Engines increase motor torque, wheels gain grip, balloons gain lift, and body parts lose weight with upgrades. Springs soften wheel suspension. Upgrades cap at level 5. Heavy wheels and powerful engines can be purchased from their inventory tiles.

## Progression and saves

Three floors: basic hills (80m / 5 ore), rough terrain (110m / 10 ore), gap and obstacle (150m / 15 ore). Floor unlocks cost 250 and 900 coins. Last successful and automated vehicle configurations, current designs, best rates, levels, unlocks, and coins use PlayerPrefs. Offline income caps at eight hours and requires **COLLECT**; uncollected rewards persist. The app saves every ten seconds, on pause, and on exit.

Tests fail after three seconds stuck or overturned, falling below the course, a broken wheel joint, or 30 seconds elapsed. No test charges coins. Frame/body connections are deliberately rigid; cargo stays in its bin. Terrain and parts use generated colored shapes rather than the reference illustration's finished art.

## Development

**DEV** opens coin, unlock, reset-save, force-completion, and physics-speed controls. Test timing follows simulation time, so speed controls do not inflate scores. Force-completion bypasses course validation and is only for development.

**Tools → Contraption Mine → Setup Prototype** regenerates balancing assets and the Mine scene. It imports Unity's bundled TMP essentials if missing; no third-party packages are required. **Validate Blueprints** simulates all three starter vehicles and checks serialization and the offline cap. Batch entry points are `ContraptionMineEditor.PrototypeSetup.BatchValidate` and `ContraptionMineEditor.PrototypeSetup.PlaySmoke` (the latter enters play mode, validates real runs/UI/progression invariants, captures `/tmp/contraption-preview.png`, and exits). Smoke runs use a separate PlayerPrefs key and do not overwrite the normal save.

Scripts: `MineGame` orchestrates UI, building, tests, and economy; `VehiclePhysics` builds a compound rigid body and jointed wheels; `MineState` handles persisted records and offline calculation; `PartData` and `FloorData` are editable ScriptableObjects. `PrototypeSmoke` is editor-only.

A verified portrait render is in `Docs/Prototype-preview.png`. Validation details are in `Docs/Validation.md`.
