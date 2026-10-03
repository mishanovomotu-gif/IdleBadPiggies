# Validation — nine floors, challenges, contraption tradeoffs

Validated with Unity 6000.3.6f1 (6.3 LTS), macOS, in an isolated temporary project copy. The existing editor session and normal save were preserved; smoke tests use a separate PlayerPrefs key.

## Real game loop

All nine level-1 starter blueprints completed their courses, met the ore requirement, and could be automated. The integration test used the same selection/start/automation methods as the UI, checked preceding-floor automation and paid unlocking, and verified the ore-value scoring formula.

| Floor | Ore delivered | Completion | Coins/min |
| --- | --- | ---: | ---: |
| 1 | 5 crystal | 11.59s | 25.9 |
| 2 | 10 copper | 24.98s | 55.2 |
| 3 | 15 crystal | 23.23s | 155.0 |
| 4 | 20 crystal | 16.83s | 427.9 |
| 5 | 25 copper | 21.43s | 724.5 |
| 6 | 30 crystal | 21.34s | 1,180.6 |
| 7 | 20 crystal | 18.57s | 1,162.9 |
| 8 | 25 gold | 21.73s | 2,382.0 |
| 9 | 30 gold | 17.99s | 4,502.4 |

Completion times vary with scheduling/physics; both full integration runs passed all nine floors. Floor 2 is the slowest starter because of its icy valley; all passed the 30-second limit.

The play-mode test also checked missing-engine rejection, successful-result metrics, saved designs/records/new part arrays/latest attempts, and preservation of automated income after edits. Forced fall, flip, stall, timeout, and wheel-break conditions exercised the actual failure detection and result/hint branches; they did not replace successful records or automated income.

## Physics and authored course checks

Standalone physics simulation passed all nine authored collider prefabs. The course factory is shared by setup and the runtime fallback, including boundaries of ice zones and physical roofs. Regression checks verified:

- Propellers above/below a chassis generate opposite pitch directions and positive forward thrust.
- Ballast below the frame lowers centre of mass compared with the same ballast above it.
- Gold adds more payload/body mass than crystal for the same cargo capacity.
- Authored icy surfaces retain a persistent low-friction material in their saved prefabs.
- Migration retains old eight-part upgrades/unlocks while adding default levels and locked new parts.
- Legacy floor expansion preserves coins, floor state, and recorded automated income.
- Save JSON roundtrip and eight-hour offline-income cap.
- Fixed portrait fitting stays within 1080×1920, 1179×2556, landscape 1920×1080, and inset safe-area rectangles.

The physics uses a compound chassis and wheel joints, with explicit mass distribution, softer spring suspension, placed balloon lift, placed propeller thrust, and local wind forces. Decorative connections add no mass or colliders. Airtime uses wheel contact; impact speed measures the collision velocity normal, including wheel impacts. Cargo is secured and does not spill.

## Visual verification

The expanded workshop and third floor page were rendered and inspected. The final result panel, ore appearances, ten-item palette, briefing, and controls are captured in `Run-feedback-preview.png`. Screenshots use the same runtime UI and an editor-only offscreen rendering helper.

No physical phone deployment was tested. The portrait safe-area fit is covered by geometric regression checks; the rendered preview is 540×960. These checks establish starter completion and functional progression, rather than proving every possible player build is balanced.
