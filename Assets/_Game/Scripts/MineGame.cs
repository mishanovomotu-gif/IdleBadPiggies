using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace ContraptionMine
{
    public sealed partial class MineGame : MonoBehaviour
    {
        public PartData[] parts; public FloorData[] floors;
        static Material rockMaterial;
        MineState state; Canvas canvas; Camera cam; Transform track; VehiclePhysics vehicle;
        TMP_Text wallet, hud, telemetry, notice; readonly List<GameObject> dynamicUI = new();
        PartType selected = PartType.Frame; bool delete, moving, debug, running, suspended; int moveX = -1, moveY = -1; float elapsed, stalled, flipped, saveClock, progress; double pendingOffline; string message = "Choose a part, then tap a grid cell. Try the starter blueprint."; int Active => state.selectedFloor; FloorState Current => state.floors[Active]; FloorData Floor => floors[Active];
        static readonly Color Navy = new(.025f, .08f, .15f), Panel = new(.04f, .17f, .28f), Blue = new(.02f, .38f, .65f), Gold = new(1, .73f, .2f), Green = new(.1f, .65f, .36f);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Bootstrap() { if (FindFirstObjectByType<MineGame>() != null) return; var o = new GameObject("Contraption Mine"); o.AddComponent<MineGame>(); }
        void Start()
        {
            parts = Resources.LoadAll<PartData>("Parts"); Array.Sort(parts, (a, b) => a.type.CompareTo(b.type)); floors = Resources.LoadAll<FloorData>("Floors"); Array.Sort(floors, (a, b) => a.number.CompareTo(b.number)); if (parts.Length != 8 || floors.Length < 1) { Debug.LogError("Missing mine data. Run Tools > Contraption Mine > Setup Prototype."); enabled = false; return; }
            state = SaveManager.Load(); state.EnsureFloorCount(floors.Length); state.selectedFloor = Mathf.Clamp(state.selectedFloor, 0, floors.Length - 1);floorPage=state.selectedFloor/3; state.floors[0].unlocked = true; for (int i = 0; i < floors.Length; i++) state.floors[i].design ??= floors[i].blueprint.Copy(); if (!Current.unlocked) state.selectedFloor = 0;
            pendingOffline = state.uncollectedOffline + SaveManager.Offline(state, DateTimeOffset.UtcNow.ToUnixTimeSeconds()); Save();
            Screen.orientation = ScreenOrientation.Portrait; Application.targetFrameRate = 60; Time.fixedDeltaTime = .02f;
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
            cam = new GameObject("Mine camera").AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 6; cam.rect = new Rect(0, .405f, 1, .30f); cam.backgroundColor = new Color(.04f, .10f, .18f); cam.transform.position = new Vector3(5, 3, -10);
            var co = new GameObject("Portrait UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas = co.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; var scale = co.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1080, 1920); scale.matchWidthOrHeight = .5f;
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            BuildInterface();
            BuildTrack(); Refresh();
        }
        string PartLabel(PartType t) => t switch { PartType.Frame => "FRAME", PartType.Wheel => "WHEEL", PartType.Engine => "ENGINE", PartType.Cargo => "ORE", PartType.Spring => "SPRING", PartType.Balloon => "LIFT", PartType.HeavyWheel => "HEAVY", _ => "POWER" };
        void Pick(int i) { if (!state.partsUnlocked[i]) { if (state.coins < parts[i].unlockCost) { message = "Earn more coins to unlock this part."; } else { state.coins -= parts[i].unlockCost; state.partsUnlocked[i] = true; Save(); message = parts[i].label + " unlocked."; } Refresh(); return; } selected = (PartType)i; delete = moving = false; message = $"{parts[i].label}: {Description(parts[i])} Tap a cell to place."; Refresh(); }
        string Description(PartData p) => p.type switch { PartType.Cargo => $"{p.Capacity(state.levels[(int)p.type])} ore, +0.32 mass per ore.", PartType.Balloon => "reduces effective weight with lift.", PartType.Spring => "softens all wheel suspension.", _ => $"mass {p.mass:0.0}" };
        void Cell(int x, int y)
        {
            if (running) return; var list = Current.design.parts; var old = list.Find(p => p.x == x && p.y == y);
            if (delete) { if (old != null) list.Remove(old); } else if (moving) { if (moveX < 0) { if (old == null) return; moveX = x; moveY = y; message = "Tap an empty destination cell."; Refresh(); return; } if (old != null) return; var p = list.Find(q => q.x == moveX && q.y == moveY); if (p != null) { p.x = x; p.y = y; } moveX = -1; message = "Part moved. Select another part to move."; } else { var d = parts[(int)selected]; if (old?.type == selected) { list.Remove(old); } else { if (list.FindAll(p => p.type == selected).Count >= d.stock) { message = "No more of this part available."; Refresh(); return; } if (d.IsEngine && list.Exists(p => p != old && parts[(int)p.type].IsEngine)) { message = "Use exactly one engine. Delete or move the existing engine."; Refresh(); return; } if (old != null) list.Remove(old); list.Add(new PlacedPart(selected, x, y)); } }
            Save(); Refresh(); Preview();
        }
        string Validate()
        {
            var ps = Current.design.parts; if (!ps.Exists(p => p.type == PartType.Frame)) return "Add at least one frame."; if (!ps.Exists(p => parts[(int)p.type].IsWheel)) return "Add at least one wheel."; if (ps.FindAll(p => parts[(int)p.type].IsEngine).Count != 1) return "Add exactly one engine."; if (!ps.Exists(p => p.type == PartType.Cargo)) return "Add a cargo bin.";
            var structural = ps.FindAll(p => !parts[(int)p.type].IsWheel); var reached = new HashSet<PlacedPart> { structural[0] }; bool more = true; while (more) { more = false; foreach (var p in structural) if (!reached.Contains(p)) foreach (var r in new List<PlacedPart>(reached)) if (Math.Abs(p.x - r.x) + Math.Abs(p.y - r.y) == 1) { reached.Add(p); more = true; break; } }
            if (reached.Count != structural.Count) return "Connect all body parts edge to edge."; foreach (var p in ps) if (parts[(int)p.type].IsWheel && !structural.Exists(q => Math.Abs(q.x - p.x) + Math.Abs(q.y - p.y) == 1)) return "Attach each wheel next to a body part."; return null;
        }
        void Upgrade() { var i = (int)selected; if (!state.partsUnlocked[i]) return; if (state.levels[i] >= 5) { message = "Maximum level reached."; Refresh(); return; } int cost = parts[i].upgradeCost * (1 << (state.levels[i] - 1)); if (state.coins < cost) { message = $"Upgrade {parts[i].label} costs {cost} coins."; } else { state.coins -= cost; state.levels[i]++; message = $"{parts[i].label} upgraded to Lv {state.levels[i]}. Retest to update production."; Save(); } Refresh(); Preview(); }
        void SelectFloor(int i)
        {
            if (running) return; if (!state.floors[i].unlocked) { if (i == 0) return; if (state.floors[i - 1].automated == null) { message = $"Automate Floor {i} first."; Refresh(); return; } if (state.coins < floors[i].unlockPrice) { message = $"Need {floors[i].unlockPrice:N0} coins to unlock Floor {i + 1}."; Refresh(); return; } state.coins -= floors[i].unlockPrice; state.floors[i].unlocked = true; }
            state.selectedFloor = i;floorPage=i/3; message = $"Floor {i + 1}: {Floor.challenge}. Deliver {Floor.requiredOre} ore to automate."; Save(); BuildTrack(); Refresh();
        }
        void BuildTrack()
        {
            if (track != null) Destroy(track.gameObject); if (vehicle != null) vehicle.Dispose(); track = Floor.trackPrefab != null ? Instantiate(Floor.trackPrefab).transform : new GameObject("Floor " + Floor.number + " · " + Floor.challenge).transform;
            var points = Floor.terrain; for (int i = 0; i < points.Length - 1; i++)
            {
                if (Floor.HasGap(i)) continue; var a = points[i]; var b = points[i + 1]; var o = new GameObject("Rock"); o.transform.SetParent(track, false); o.transform.position = (a + b) * .5f + Vector2.down * 2; var mesh = new Mesh(); mesh.vertices = new[] { (Vector3)(a - o.transform.position.xy()), (Vector3)(b - o.transform.position.xy()), (Vector3)(b - o.transform.position.xy() + Vector2.down * 8), (Vector3)(a - o.transform.position.xy() + Vector2.down * 8) }; mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.colors = new[] { Floor.rockTint*1.3f, Floor.rockTint*1.3f, Floor.rockTint*.65f, Floor.rockTint*.65f }; mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }; mesh.RecalculateNormals(); o.AddComponent<MeshFilter>().mesh = mesh; var renderer = o.AddComponent<MeshRenderer>(); if (rockMaterial == null) { rockMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")); rockMaterial.color = Color.white; }
                renderer.sharedMaterial = rockMaterial; o.AddComponent<GeneratedMesh>().mesh = mesh; renderer.sortingOrder = 0; if (Floor.trackPrefab == null) { var edge = o.AddComponent<EdgeCollider2D>(); edge.points = new[] { a - o.transform.position.xy(), b - o.transform.position.xy() }; edge.edgeRadius = .07f; }
                Vector2 mid = (a + b) * .5f; var lip = VehiclePhysics.Shape("Amber rock edge", track, mid, new Vector2(Vector2.Distance(a, b), .17f), Gold, false, 1); lip.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            }
            VehiclePhysics.Shape("Delivery beacon", track, new Vector2(Floor.distance, 4), new Vector2(.25f, 8), Green, false, 1); VehiclePhysics.Shape("Ore depot", track, new Vector2(Floor.distance + 2, 1.5f), new Vector2(3, 2), Blue, false, 1);
            for (int i = 0; i < Floor.distance / 7; i++) { float x = i * 7; VehiclePhysics.Shape("Mine support", track, new Vector2(x, 3), new Vector2(.16f, 12), new Color(.15f, .21f, .26f), false, -2); VehiclePhysics.Shape("Lantern", track, new Vector2(x + .4f, 6), new Vector2(.25f, .42f), Gold, false, -1); }
            Preview();
        }
        void Preview()
        {
            if (running) return; if (vehicle != null) vehicle.Dispose(); cam.transform.position = new Vector3(5, 3, -10); progress = 0; // Preview shares the exact construction used by the test.
            if (Current.design.parts.Count > 0) { vehicle = VehiclePhysics.Spawn(Current.design, parts, state.levels, new Vector2(5, 2)); vehicle.body.simulated = false; foreach (var rb in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None)) rb.simulated = false; }
        }
        void StartRun() { string invalid = Validate(); if (invalid != null) { message = invalid; Refresh(); return; } if (vehicle != null) vehicle.Dispose(); vehicle = VehiclePhysics.Spawn(Current.design, parts, state.levels, new Vector2(5, 2)); running = true; elapsed = stalled = flipped = 0; progress = 0; message = "Testing: cargo weight, wheel placement and power affect the run."; Refresh(); }
        void Update()
        {
            if (state == null || wallet == null) return; if (!suspended) state.coins += state.Income / 60 * Time.unscaledDeltaTime; wallet.text = $"<size=42><color=#FFF2AA>{state.coins:N0}</color></size> <size=21>COINS</size>\n<size=30><color=#AAFF71>+{state.Income:0}/min</color></size>";
            if (running && vehicle != null)
            {
                elapsed += Time.deltaTime; progress = Mathf.Clamp(vehicle.body.position.x - 5, 0, Floor.distance); var target = new Vector3(vehicle.body.position.x + 3, Mathf.Max(3, vehicle.body.position.y + 1), -10); cam.transform.position = Vector3.Lerp(cam.transform.position, target, Time.deltaTime * 6); float speed = vehicle.body.linearVelocity.magnitude; stalled = elapsed > 2 && speed < .35f ? stalled + Time.deltaTime : 0;
                flipped = Mathf.Abs(Mathf.DeltaAngle(vehicle.body.rotation, 0)) > 110 ? flipped + Time.deltaTime : 0;
                if (progress >= Floor.distance) EndRun(true, "Delivery complete!"); else if (vehicle.body.position.y < -6) EndRun(false, "Fell into the mine. Try lift or a wider wheelbase."); else if (vehicle.broken) EndRun(false, "A wheel disconnected."); else if (flipped >= 3) EndRun(false, "Flipped and could not recover. Try a lower centre of mass."); else if (stalled >= 3) EndRun(false, "Stalled for 3 seconds. Try less cargo or more power."); else if (elapsed >= 30) EndRun(false, "Time limit reached (30 seconds). Improve your hauler.");
            }
            hud.text = $"FLOOR {Active + 1}     {progress:0} / {Floor.distance:0} m     {(running ? elapsed.ToString("0.0") + "s" : "WORKSHOP")}";
            telemetry.text = vehicle != null ? $"Cargo {vehicle.cargo} / need {Floor.requiredOre}  ·  Mass {vehicle.totalMass:0.0}  ·  Ore mass {vehicle.cargoMass:0.0}\nPower {vehicle.enginePower:0}  ·  Speed {vehicle.body.linearVelocity.magnitude:0.0} m/s  ·  Current {Current.automated?.rate ?? 0:0}/min" : $"Deliver {Floor.requiredOre} ore to automate. Current {Current.automated?.rate ?? 0:0}/min";
            saveClock += Time.unscaledDeltaTime; if (saveClock > 10) { saveClock = 0; Save(); }
        }
        void EndRun(bool success, string reason)
        {
            if (!running) return; running = false; Time.timeScale = 1; if (vehicle != null) { vehicle.body.simulated = false; foreach (var rb in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None)) rb.simulated = false; }
            if (success) { float rate = vehicle.cargo / Mathf.Max(elapsed, .1f) * 60 * Floor.multiplier; bool best = rate > Current.best; Current.lastSuccess = new RunRecord { vehicle = Current.design.Copy(), ore = vehicle.cargo, seconds = elapsed, rate = rate }; Current.best = Mathf.Max(Current.best, rate); message = $"{(best ? "NEW BEST!" : "SUCCESS!")} {vehicle.cargo} ore · {elapsed:0.0}s · {rate:0}/min. " + (vehicle.cargo >= Floor.requiredOre ? "AUTOMATE to record this vehicle." : $"Need {Floor.requiredOre} ore to automate; add cargo."); } else message = "TEST FAILED — " + reason + " TEST RUN retries; edit cells to modify."; Save(); Refresh();
        }
        void Automate() { var r = Current.lastSuccess; if (r == null || r.ore < Floor.requiredOre) return; Current.automated = JsonUtility.FromJson<RunRecord>(JsonUtility.ToJson(r)); message = $"Floor {Active + 1} automated at {r.rate:0} coins/min. Editing or upgrading leaves this recorded income intact."; Save(); Refresh(); }
        void OfflinePanel() { Box("Offline", 115, 655, 850, 340, Navy, true); Label("YOUR PIGGIES WERE BUSY!", 155, 690, 770, 70, 36, Gold, true); Label($"+{pendingOffline:N0} coins · up to 8 hours", 155, 770, 770, 55, 30, Color.white, true); Button("COLLECT", 155, 855, 770, 90, () => { state.coins += pendingOffline; pendingOffline = 0; Save(); Refresh(); }, Green, 34, true); }
        void DebugPanel()
        {
            Box("Debug tools", 65, 630, 950, 375, Navy, true); Label("DEVELOPMENT TOOLS", 90, 640, 900, 45, 29, Gold, true);
            Button("+1000 COINS", 90, 700, 430, 65, () => { state.coins += 1000; Save(); }, Blue, 24, true); Button("UNLOCK NEXT FLOOR", 540, 700, 450, 65, () => { for (int i = 1; i < floors.Length; i++) if (!state.floors[i].unlocked) { state.floors[i].unlocked = true; break; } Save(); Refresh(); }, Blue, 24, true);
            Button("ALL PARTS", 90, 780, 275, 65, () => { for (int i = 0; i < 8; i++) state.partsUnlocked[i] = true; Save(); Refresh(); }, Blue, 23, true); Button("COMPLETE TEST", 380, 780, 310, 65, () => { if (running) { elapsed = Mathf.Max(elapsed, 10); EndRun(true, "Debug completion"); } }, Blue, 23, true); Button("RESET SAVE", 705, 780, 285, 65, () => { if (running) EndRun(false, "Save reset"); SaveManager.Reset(); state = new MineState();state.EnsureFloorCount(floors.Length);floorPage=0; state.floors[0].unlocked = true; for (int i = 0; i < floors.Length; i++) state.floors[i].design = floors[i].blueprint.Copy(); pendingOffline = 0; Save(); BuildTrack(); Refresh(); }, new Color(.65f, .15f, .2f), 23, true);
            for (int i = 0; i < 3; i++) { float speed = i == 0 ? .5f : i == 1 ? 1 : 2; Button($"PHYSICS x{speed}", 90 + i * 305, 865, 290, 65, () => Time.timeScale = speed, Panel, 23, true); }
            Label("Close with DEV. Debug completion bypasses route validation.", 90, 943, 890, 45, 22, Color.white, true);
        }
        void Save() { state.uncollectedOffline = pendingOffline; SaveManager.Save(state); }
        void OnApplicationPause(bool pause) { if (state == null) return; if (pause) { Save(); suspended = true; } else if (suspended) { pendingOffline += SaveManager.Offline(state, DateTimeOffset.UtcNow.ToUnixTimeSeconds()); suspended = false; Save(); Refresh(); } }
        void OnApplicationQuit() { if (state != null) Save(); }
    }
    public sealed class GeneratedMesh : MonoBehaviour { public Mesh mesh; void OnDestroy() { if (mesh != null) Destroy(mesh); } }
    public static class VectorExtensions { public static Vector2 xy(this Vector3 v) => new(v.x, v.y); }
}
