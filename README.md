# Contraption Mine

Unity 6.3 LTS / Universal 2D prototype with twelve floors, ten contraption parts, three ore types, and an idle mine economy. Open `Assets/_Game/Scenes/Mine.unity` and press Play. The portrait UI fits the screen safe area.

## Four views

- **Mine** is the default home: total output, automatic cash income, a Collect All button, and three floor cards per page. Recorded haulers drive, unload, and return in their floor cards. Open **Manage Floor** for production upgrades and hiring.
- **Workshop** contains the actual physics course, build grid, part inventory, and test/record controls. **Details** reveals mass, power, airtime, and impact data. The build tools and palette appear only here.
- **Research** upgrades ore refineries across the mine and purchases permanent foreman training using prestige certificates.
- **Prestige** explains reset/retention, previews certificates, and opens a separate confirmation before rebuilding.

**Menu** contains a short guide and developer tools. Modal backdrops block underlying controls. Tabs cannot leave an active run until it is stopped.

## Production and automation

1. Build a connected hauler and complete a test delivering the required ore.
2. **Start Production** records that run as the floor's base output. No further physics simulation is needed to earn from it.
3. Until a manager is hired, coins accumulate in that floor's storage. Collect on the floor or use **Collect All** on Mine.
4. Hire a manager to collect automatically while playing, earn claimable offline rewards, and add a 20% production bonus.
5. Upgrade mining crew and loading stations independently of the contraption. Choose **Buy 1**, **Buy 10**, or **Max**. Max buys only affordable levels; all bulk prices sum individual costs. Each track caps at level 100.

Effective output is recorded run rate × crew × loading × manager × ore refinery × permanent bonuses. Crew gives a compounded +14% per level; loading gives +10%. Prices increase with level. Higher floors have larger base outputs and appropriately larger upgrade costs.

A new successful run changes production only when explicitly recorded. Failed tests, edits, and part upgrades preserve the existing record and production upgrades. Base rate is delivered ore / physics simulation seconds × 60 × course multiplier × ore value. Run timing counts physics steps rather than rendering time.

## Contraption building

Pick a part, then tap a grid square. Tapping the same part removes it. **Move** selects a part and an empty destination; **Delete** removes cells. Body parts connect edge to edge; each wheel touches a body part. Use exactly one engine, at least one frame, wheel, and cargo bin. **Blueprint** restores the starter design; **Reset** empties only the workshop.

The chassis has a compound rigid body with jointed wheels, explicit mass distribution, softer spring suspension, placed balloon lift, and placed propeller thrust. Moving lift/thrust changes pitch. Low ballast lowers the centre of mass at a weight cost. Cargo is secured; there is no random ore loss. Decorative beams, ropes, and suspension arms make connections visible.

Cargo starts at 5 ore per bin and gains 2 per part upgrade. Engines gain torque, wheels gain grip, balloons gain lift, and ordinary body parts lose weight with upgrades. Ballast upgrades add weight; propeller upgrades increase thrust. Part upgrades cap at level 5. Heavy wheels/power engines are granted on Floor 4; propellers/ballast on Floor 7. Inventory tiles allow earlier purchases.

Runs fail after three physics seconds stuck or overturned, falling below the course, wheel-joint breakage, or 30 simulation seconds. Results show delivery, distance, time, base output, and a tuning hint. **Retry** repeats the build; **Modify Build** reveals the course. Advanced diagnostics are under **Details**. Runs cost no coins.

| Ore | Mass per unit | Run value | Refinery effect |
| --- | ---: | ---: | --- |
| Crystal | 0.32 | ×1.00 | +0.20 multiplier per refinery level |
| Copper | 0.38 | ×1.15 | Same; affects copper floors only |
| Gold | 0.48 | ×1.50 | Same; affects gold floors only |

Refineries cap at 50 levels. Their coin upgrades reset on prestige. Foreman training costs certificates, adds +0.25 to its permanent multiplier per level, and caps at 10 levels.

## Floors

Each paid floor requires production recorded on the preceding floor. Floors 10–12 also require minimum total mine output, making production investment necessary before unlocking.

| Floor | Course | Distance | Ore | Unlock coins | Required mine output/min |
| --- | --- | ---: | --- | ---: | ---: |
| 1 | Basic Hills | 80m | 5 crystal | Free | — |
| 2 | Rough Terrain | 110m | 10 copper | 250 | — |
| 3 | Gap & Obstacle | 150m | 15 crystal | 900 | — |
| 4 | Crystal Ravine | 120m | 20 crystal | 2,200 | — |
| 5 | Timber Ridge | 130m | 25 copper | 5,000 | — |
| 6 | The Deep Core | 140m | 30 crystal | 11,000 | — |
| 7 | Frost Tunnel | 120m | 20 crystal | 23,000 | — |
| 8 | Golden Ascent | 125m | 25 gold | 45,000 | — |
| 9 | Wind Shaft | 135m | 30 gold | 85,000 | — |
| 10 | Glacier Works | 130m | 20 crystal | 4,000,000 | 30,000 |
| 11 | Copper Summit | 140m | 25 copper | 32,000,000 | 200,000 |
| 12 | Royal Airshaft | 145m | 30 gold | 200,000,000 | 1,000,000 |

Combined challenges include hills, icy valleys, rough descents, low timber ceilings, ramp gaps, and wind shafts. Ice uses a saved low-friction material; roofs use colliders; wind applies force only inside marked zones. All level-1 starters are validated within the 30-second limit. Existing upgraded cargo can make a starter heavier; reduce bins or add power if needed.

## Prestige

After Floor 9 is producing and at least 1M coins have been earned this mine, rebuilding grants `floor(sqrt(cycle earnings / 1M))` certificates. Earnings include collected coins that were subsequently spent. Uncollected storage/offline rewards count only after collection.

Each lifetime certificate adds +5% to the permanent production multiplier. Spend available certificates on foreman training for an additional permanent multiplier. Spending certificates does not remove their lifetime bonus.

Prestige resets coins to 100, floor unlocks, managers, crew/loading levels, ore refineries, and unclaimed earnings. It preserves contraption designs, unlocked parts and part upgrades, successful run records, best run scores, certificates, and permanent research. Unlock a floor and **Use Saved Run** to resume its recorded base output without repeating the physics run. The preview and confirmation explain exactly what changes.

## Offline and saves

Managed floors accrue claimable offline rewards for **at most four hours per absence**. Unmanaged floors fill their own storage, capped at four hours of their current production. A full manual store pauses that floor's delivery animation until collected. Online manager cash continues automatically. Already earned/uncollected rewards are preserved across restarts.

The existing v1 PlayerPrefs key is retained. Older floor/part arrays expand; existing recorded floors receive managers so automatic collection is preserved (including the new manager bonus). Current designs, recorded runs, production upgrades, managers, banks, refineries, certificates, and permanent research persist. Saves occur every ten seconds, on pause, and on exit. Clock rollback produces no offline reward.

## Development and validation

**Menu → Developer Tools** provides coin grants, floor unlocks, part unlocks, force-completion, save reset, and simulation speed. Force-completion bypasses route validation. Idle earnings use real elapsed time; changing physics speed does not speed up cash production.

**Tools → Contraption Mine → Setup Prototype** regenerates balancing assets, authored track prefabs, and the Mine scene. `MakeContent` updates content without replacing the scene. **Validate Blueprints** checks all twelve tracks, physical tradeoffs, viewport fitting, economy transactions, migration, offline limits, and prestige retention.

Batch entry points are `ContraptionMineEditor.PrototypeSetup.BatchValidate` and `PrepareAndSmoke`. Smoke uses a separate PlayerPrefs key, exercises actual game actions, and produces `/tmp/contraption-preview.png`. Editor-only snapshot preview entry points inspect the validated test state. No normal user save is reset by validation.

Main modules: `MineGame` runs building/tests; `MineInterface` owns navigation/workshop; `MineIdleInterface` owns economy menus; `MineEconomy` handles purchases and prestige; `MineState` handles accrual/saves; `DeliveryLoop` animates recorded haulers independently of payouts; `MineChallenges` shares course collider generation.

Preview images and verification results are in `Docs`. Bitmap art provenance remains in `Docs/ArtDirection.md`; UI and new part/ore icons use the existing procedural art system.
