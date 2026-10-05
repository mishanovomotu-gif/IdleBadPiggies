# Validation — idle mine economy and separated UI

Unity 6000.3.6f1 (6.3 LTS), macOS. Validation ran in an isolated project copy using a separate PlayerPrefs key, preserving the existing editor and normal save.

## Starter tracks and actual progression

All twelve level-1 starter vehicles completed their authored tracks in standalone physics simulation. The full play-mode loop also completed all twelve, recorded their output, hired managers, and verified exact coin charges for floor unlocks. Floors 10–12 initially rejected insufficient mine output without charging coins; purchasing crew/loading upgrades through the game actions met their gates and allowed progression.

| Floor | Ore | Final play-mode time | Base output/min |
| --- | --- | ---: | ---: |
| 1 | 5 crystal | 11.62s | 25.8 |
| 2 | 10 copper | 24.98s | 55.2 |
| 3 | 15 crystal | 22.54s | 159.7 |
| 4 | 20 crystal | 17.72s | 406.3 |
| 5 | 25 copper | 18.12s | 856.8 |
| 6 | 30 crystal | 18.78s | 1,341.8 |
| 7 | 20 crystal | 21.12s | 1,022.7 |
| 8 | 25 gold | 21.62s | 2,393.6 |
| 9 | 30 gold | 16.52s | 4,903.1 |
| 10 | 20 crystal | 15.08s | 47,744.9 |
| 11 | 25 copper | 20.48s | 176,877.5 |
| 12 | 30 gold | 18.08s | 1,120,008.0 |

These are base recorded rates before idle multipliers. Completion times vary with physics and scheduling. One earlier repeated integration run timed out on Floor 3; the final run passed after the timer was changed to count fixed physics steps, aligning scored time with simulation rather than UI rendering. This is not a guarantee that every user-built or upgraded vehicle will finish.

The real game loop checked ore scoring, missing-engine rejection, production/manager actions, saved designs and records, and preservation of recorded output after edits. Forced fall, flip, stall, timeout, and wheel-break conditions exercised failure feedback without replacing successful production records.

## Idle economy regression checks

The final standalone economy suite passed:

- Unmanaged output enters storage; collection credits it exactly once.
- Managers flush waiting storage, collect automatically, and grant the 20% bonus.
- Crew/loading upgrades increase effective output without modifying the recorded run.
- Buy 10 costs the same as ten Buy 1 purchases; Max buys only affordable levels and cannot overspend. Level caps are enforced.
- Ore refineries affect their corresponding ore floors only.
- Managed offline rewards cap at four hours; manual storage also caps at four hours of current output. Backward clocks earn nothing.
- Legacy recorded floors retain automatic collection through manager migration.
- Certificates fund permanent research; prestige awards the expected amount and cannot be repeated immediately.
- Prestige clears ordinary progression, preserving designs, successful records, part unlocks/upgrades, certificates, and permanent research.
- Economy JSON save roundtrip preserves these fields.
- Deep floor output gates are 30K, 200K, and 1M per minute.

The play-mode prestige action also passed reset/retention checks and successfully restarted Floor 1 from its saved run without another test. The pre-prestige validation state was restored for screenshots.

## Physics and layout checks

Earlier and final checks verified thrust above/below the chassis produces opposite pitch, low ballast lowers centre of mass, gold increases payload mass relative to crystal, and saved icy track prefabs retain their low-friction material. The shared course factory creates ice-zone boundaries, roofs, and matching authored/fallback colliders.

Portrait fitting stays within 1080×1920, the reported 1179×2556 resolution, landscape 1920×1080, and inset safe-area rectangles. Decorative chassis connections add no mass or colliders. Delivery loops are UI illustrations of recorded vehicles; payouts depend on elapsed time and production state, independently of animation.

## Visual checks and limits

Separate Mine, floor management, Workshop, Research, and Prestige views were rendered from the successfully validated test state. Physics diagnostics are optional; developer tools sit inside Menu. Modal overlays block controls behind them. Snapshots are editor-only captures of the actual runtime UI.

Physical phone deployment and long-term human playtesting were not performed. Late-floor gates, costs, and multiplier curves are a first balance pass, intended for iteration after player feedback. The tests establish transaction correctness, save compatibility, functional progression, and starter completion rather than proving the economy's pacing is final.

Preview artifacts: `Idle-mine-preview.png`, `Floor-management-preview.png`, `Workshop-preview.png`, `Research-preview.png`, `Prestige-preview.png`, and `Prestige-confirmation-preview.png`. Final runtime/editor scripts were compared with the compiled preview copy and matched.

## Workshop interaction and run presentation — 2026-10-05

- Unity integration exercised pointer drag handlers: tray-to-grid placement, grid-to-grid movement, return-to-tray removal, undo, and redo. All passed (`/tmp/contraption-workshop-smoke.log`).
- All twelve courses completed in the same runtime validation, followed by fall/flip/stall/timeout/wheel-break feedback, save/load, idle progression, and prestige retention checks.
- Workshop now uses direct drag gestures instead of Move/Delete modes. Invalid drops leave the design unchanged; disconnected parts are highlighted. Palette clicks still inspect/unlock parts.
- A separate expanded run view hides the build grid, tracks the vehicle, and offers edit/retry and recorded-output comparison. Visual cargo movement is on a child sprite, preserving the cargo collider.
- Final presentation adds exhaust/driver animation, landing dust, generated prototype audio, and a sound toggle. Final source compiled through UI preview capture after the full runtime pass. No physical model constants changed.
- Desktop pointer events and portrait screenshots were checked. Real touch-device feel and the subjective sound mix still need hands-on playtesting.

## Run UI cleanup and side deletion — 2026-10-05

- Replaced cached-object cleanup with direct hierarchy cleanup, preserving only a dedicated persistent header. This prevents stale workshop and result controls from surviving when editor script reload loses the UI cache. Existing header layouts are rebuilt safely.
- Integration tests clear the cached UI list before entering a run and again before refreshing run/results. Both screens contain one dashboard/result panel and no old workshop or stop controls. Legacy-header migration also passed.
- Existing parts can be removed by dropping to either side of the grid, with a red drag preview and labeled right-side delete zone. Removal is undoable. Pointer integration verified side deletion together with placement, movement, undo, and redo.
- Twelve-floor runtime regression, failure feedback, idle progression, and prestige checks passed: `/tmp/contraption-ui-fix-smoke.log`.
- Layout/cache-loss captures passed: `/tmp/contraption-ui-run-fix.log`, `/tmp/contraption-ui-result-fix.log`, `/tmp/contraption-ui-delete-fix.log`. Result capture uses a synthetic successful-result fixture; course completion is checked separately by the runtime regression.

## Full-height portrait layout — 2026-10-05

- Tall portrait safe areas now use the full available height rather than a fixed 9:16 letterbox. UI stays at a uniform width scale; extra height enlarges the course, while workshop tools and run controls remain anchored above bottom navigation. A full-screen background camera covers safe-area margins.
- Running/results have opaque control backings. The header clips to its own area, and script re-enable rebuilds the UI to discard stale layouts. Save Hauler uses a separate full-width row.
- Tall snapshots use 540 × 1171 (matching the 1179 × 2556 reference aspect ratio). Workshop, run, and synthetic successful-result screenshots were inspected: no top/bottom bands, no underlying palette or build buttons, and separated actions.
- Tall-screen pointer placement and side deletion, legacy header migration, and cache-loss UI checks passed: `/tmp/contraption-responsive-workshop.log`, `/tmp/contraption-responsive-run.log`, `/tmp/contraption-responsive-result-final.log`.
- Safe-area bounds, twelve standalone blueprint courses, physics tradeoffs, and economy checks passed: `/tmp/contraption-responsive-validation.log`. Real-device safe-area behavior remains a hands-on check.
