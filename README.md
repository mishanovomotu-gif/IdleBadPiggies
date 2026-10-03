# Contraption Mine

Playable Unity 6.3 LTS / Universal 2D prototype. Open `Assets/_Game/Scenes/Mine.unity` and press Play. Nine floors, ten contraption parts, and three ore types. The portrait UI fits the screen safe area, including tall phone and landscape Game views. The runtime creates its uGUI/TMP interface and visible course; balancing assets live in `Assets/_Game/Resources`. Matching collider prefabs live in `Assets/_Game/Prefabs/Tracks`.

## Play

1. Floor 1 starts with a blueprint. Press **TEST RUN** to drive automatically.
2. Pick a part and tap a grid cell. Tapping the same part removes it. **MOVE** selects a part then an empty destination; **DELETE** removes cells.
3. Connect body parts edge to edge. Each wheel must touch a body part. Use exactly one engine, at least one frame, wheel, and cargo bin.
4. Deliver enough ore and press **AUTOMATE**. Recorded income is delivered ore / simulation seconds × 60 × floor multiplier × ore value.
5. Coins unlock subsequent floors after the previous floor is automated. Upgrade, edit, and retest to replace recorded production.

The briefing above the course describes its ore, value, and combined challenges. Signs mark icy sections, roofs, and wind. Results show delivered ore, distance, time, income compared with automation, airtime, impact speed, and a tuning hint. **RETRY** starts the same build; **MODIFY BUILD** reveals the course and leaves the workshop ready to edit.

Existing automated income survives failed tests, workshop edits/resets, and part upgrades. **AUTOMATE** explicitly replaces it with the last successful eligible run, including a slower one. **BLUEPRINT** restores the course's starter design. Runs do not cost coins.

## Contraption physics

The chassis uses a rigid compound body with jointed wheels. Mass and centre of mass reflect each part and its payload. Springs soften suspension and allow more bounce; wheels have grip and motor torque. Cargo stays secured in its bin. Impact and airtime are measured rather than causing arbitrary damage or ore loss.

Balloon lift acts at the balloon's position. Moving lift between the front and rear changes pitch. Propellers apply continuous forward thrust in the chassis direction at their placed position: high thrust can pitch the nose down; low thrust can lift it. Low ballast lowers the centre of mass at the cost of extra weight. Ballast upgrades add weight; propeller upgrades increase thrust. Body connections, balloon ropes, and moving suspension arms make the assembly visible.

Cargo bins start at 5 ore and gain 2 per upgrade. Engines gain torque, wheels gain grip, balloons gain lift, and ordinary body parts lose weight with upgrades. Upgrades cap at level 5. Heavy wheels and power engines are granted on Floor 4; propellers and ballast are granted on Floor 7. Their inventory tiles also allow earlier purchases.

| Ore | Mass per unit | Value multiplier | Tradeoff |
| --- | ---: | ---: | --- |
| Crystal | 0.32 | 1.00 | Light standard payload |
| Copper | 0.38 | 1.15 | More weight, modest extra reward |
| Gold | 0.48 | 1.50 | Heavy payload, greater reward |

Ore is assigned by floor. Bins, deposits, telemetry, and briefings use the corresponding ore appearance/name. Existing automated scores remain unchanged until explicitly replaced by a new test.

## Floors and progression

Use the arrows above the floor cards to browse three pages.

| Floor | Course | Distance | Ore needed | Unlock coins | Combined challenges |
| --- | --- | ---: | --- | ---: | --- |
| 1 | Basic Hills | 80m | 5 crystal | Free | Hills and landing |
| 2 | Rough Terrain | 110m | 10 copper | 250 | Bumps and icy valley |
| 3 | Gap & Obstacle | 150m | 15 crystal | 900 | Ramp gap and landing ridge |
| 4 | Crystal Ravine | 120m | 20 crystal | 2,200 | Ravine and timber roof |
| 5 | Timber Ridge | 130m | 25 copper | 5,000 | Heavy load and two climbs |
| 6 | The Deep Core | 140m | 30 crystal | 11,000 | Two gaps and crosswind |
| 7 | Frost Tunnel | 120m | 20 crystal | 23,000 | Ice, low tunnel, exit bumps |
| 8 | Golden Ascent | 125m | 25 gold | 45,000 | Dense ore, steep climbs, rough descent |
| 9 | Wind Shaft | 135m | 30 gold | 85,000 | Two jumps, wind shaft, landing bumps |

Each paid floor requires the preceding floor to be automated. All starter builds finish at level 1, within the 30-second run limit. Floor 2 is deliberately slower because of its slick valley. Wind acts only inside marked sections; ice changes contact friction, and low roofs use physical colliders.

Existing three/six-floor saves and eight-part arrays migrate automatically, preserving currency, designs, upgrades, records, income, and offline rewards. The PlayerPrefs save key remains v1. Current designs, successful/automated records, latest attempts, levels, unlocks, and currency are saved every ten seconds, on pause, and on exit. Offline income caps at eight hours and requires **COLLECT**; uncollected rewards persist.

Runs fail after three seconds stuck or overturned, falling below the course, a broken wheel joint, or 30 seconds elapsed. Failure feedback identifies the reason and suggests an adjustment. Failed attempts preserve the last successful record and automated income.

## Try these comparisons

- Floor 7: compare the blueprint against the same build without ballast, then move ballast above the cargo. Watch balance and completion time on ice.
- Floor 8: add cargo or remove a balloon. Compare the heavier gold payload's climb speed and landing impact.
- Floor 9: move the low propeller above the chassis. Compare pitch on the first ramp, then redistribute balloons to recover balance.
- Retry a failed run, automate an eligible result, restart Play mode, and check that income and the edited design persist.

## Development

**DEV** opens coin, unlock, reset-save, force-completion, and simulation-speed controls. Timing uses simulation seconds. Force-completion is only a development shortcut.

**Tools → Contraption Mine → Setup Prototype** regenerates balancing assets and the Mine scene. It imports Unity's bundled TMP essentials if missing. `MakeContent` updates content without replacing the scene. **Validate Blueprints** simulates all nine authored tracks, checks meaningful contraption tradeoffs, safe-area fitting, migration, serialization, and the offline cap.

Batch entry points: `ContraptionMineEditor.PrototypeSetup.BatchValidate`, `PrepareAndSmoke` (actual nine-floor progression, scoring, saves, failure feedback, screenshot). Smoke runs use a separate PlayerPrefs key and preserve the normal save.

`MineGame` orchestrates building, runs, and economy; `MineInterface` draws the UI; `MineRunFeedback` draws results and challenge signs; `VehiclePhysics` models forces and joints; `MineChallenges` creates shared course colliders; `MineState` handles saves. `PrototypeSmoke` and `ContraptionChecks` are editor validation helpers.

Preview images and test results are in `Docs`. Art provenance is in `Docs/ArtDirection.md`; the new icons and ore variants use the existing procedural art system.
