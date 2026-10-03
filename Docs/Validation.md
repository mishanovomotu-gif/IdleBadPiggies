# Prototype validation

Validated on Unity 6000.3.6f1 (6.3 LTS), macOS, using a temporary project copy so the existing Unity session stayed open.

## Physics and gameplay

Unity compiled the runtime and editor scripts successfully. The final play-mode smoke test ran the actual MineGame interface, track prefabs, vehicle spawner, motors, suspension, lift, cargo weights, success checks, and scoring.

| Starter | Delivered | Time | Production |
| --- | ---: | ---: | ---: |
| Floor 1 | 5 ore | 10.69s | 28.1 coins/min |
| Floor 2 | 10 ore | 17.16s | 69.9 coins/min |
| Floor 3 | 15 ore | 23.60s | 152.5 coins/min |

All starters met their floor requirement before the 30-second limit. Floor 3 currently runs slightly longer than the brief's approximate 10–20-second target. Simulation timing is measured inside Unity and small variations between runs are expected.

The smoke test also verified automation after each run, preservation of automated rates while designs are edited, rejection of a missing-engine build without charging coins, and a PlayerPrefs roundtrip preserving automated records and grid configurations. Standalone checks verified the eight-hour offline cap and JSON serialization. A portrait render was inspected for readable controls and the physics viewport.

No gameplay errors were logged in the final smoke run. Unity's editor SearchDatabase indexer logged startup exceptions in the temporary batch project; these came from UnityEditor.Search, before the gameplay test, rather than the prototype. Desktop click/touch interaction and mobile device deployment have not been manually tested.

## Deliberate prototype constraints

Colored generated shapes replace the reference illustration's art. Frames are a single compound rigid body; wheels are separate rigid bodies attached with WheelJoint2D. Critical wheel detachment fails a run; structural frames do not break. Springs affect suspension on the whole hauler, cargo remains secured, and balloons add lift with light stabilization. Automated floors use recorded results without continuing physics simulation.
