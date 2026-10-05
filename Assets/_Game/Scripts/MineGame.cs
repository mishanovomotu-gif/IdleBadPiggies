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
        static Material rockMaterial; static Sprite cavernSprite;
        MineState state; Canvas canvas; RectTransform designRoot; Camera cam; Transform track; VehiclePhysics vehicle; SpriteRenderer cavern;
        TMP_Text wallet, hud, telemetry, notice; readonly List<GameObject> dynamicUI = new();
        PartType selected = PartType.Frame; bool delete, moving, debug, running, suspended, showResults; int moveX = -1, moveY = -1; float elapsed, stalled, flipped, saveClock, progress, nextUIUpdate; double pendingOffline; string message = "Choose a part, then tap a grid cell. Try the starter blueprint."; int Active => state.selectedFloor; FloorState Current => state.floors[Active]; FloorData Floor => floors[Active];
        static readonly Color Navy = new(.025f, .08f, .15f), Panel = new(.04f, .17f, .28f), Blue = new(.02f, .38f, .65f), Gold = new(1, .73f, .2f), Green = new(.1f, .65f, .36f);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Bootstrap() { if (FindFirstObjectByType<MineGame>() != null) return; var o = new GameObject("Contraption Mine"); o.AddComponent<MineGame>(); }
        void OnEnable() { if (state != null && canvas != null && designRoot != null) Refresh(); }
        void Start()
        {
            parts = Resources.LoadAll<PartData>("Parts"); Array.Sort(parts, (a, b) => a.type.CompareTo(b.type)); floors = Resources.LoadAll<FloorData>("Floors"); Array.Sort(floors, (a, b) => a.number.CompareTo(b.number)); if (parts.Length != Enum.GetValues(typeof(PartType)).Length || floors.Length < 1) { Debug.LogError("Missing mine data. Run Tools > Contraption Mine > Setup Prototype."); enabled = false; return; }
            state = SaveManager.Load(); state.EnsurePartCount(parts.Length); state.EnsureFloorCount(floors.Length); state.selectedFloor = Mathf.Clamp(state.selectedFloor, 0, floors.Length - 1); floorPage = state.selectedFloor / 3; state.floors[0].unlocked = true; for (int i = 0; i < floors.Length; i++) state.floors[i].design ??= floors[i].blueprint.Copy(); if (!Current.unlocked) state.selectedFloor = 0; floorPage = state.selectedFloor / 3; GrantDeepMineParts(); state.EnsureEconomy(floors);
            pendingOffline = state.uncollectedOffline + state.ApplyOffline(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); Save();
            Screen.orientation = ScreenOrientation.Portrait; Application.targetFrameRate = 60; Time.fixedDeltaTime = .02f;
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
            cam = new GameObject("Mine camera").AddComponent<Camera>(); cam.gameObject.AddComponent<AudioListener>(); cam.orthographic = true; cam.orthographicSize = 4.8f; cam.rect = new Rect(0, .455f, 1, .25f); cam.backgroundColor = new Color(.10f, .12f, .27f); cam.transform.position = new Vector3(5, 1.6f, -10);
            var co = new GameObject("Portrait UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas = co.GetComponent<Canvas>();
            var backdropCamera = new GameObject("Full screen background").AddComponent<Camera>(); backdropCamera.depth = -100; backdropCamera.cullingMask = 0; backdropCamera.backgroundColor = Navy; backdropCamera.clearFlags = CameraClearFlags.SolidColor;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; var scale = co.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1080, 1920); scale.matchWidthOrHeight = .5f;
            designRoot = new GameObject("1080 × 1920 portrait content", typeof(RectTransform)).GetComponent<RectTransform>(); designRoot.SetParent(canvas.transform, false);
            co.AddComponent<MineViewport>().Initialize(canvas, designRoot, cam);
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            message = ""; BuildInterface();
            BuildTrack(); Refresh();
        }
        void Pick(int i) { if (!state.partsUnlocked[i]) { if (state.coins < parts[i].unlockCost) { message = "Earn more coins to unlock this part."; } else { state.coins -= parts[i].unlockCost; state.partsUnlocked[i] = true; Save(); message = parts[i].label + " unlocked."; } Refresh(); return; } selected = (PartType)i; delete = moving = false; message = $"{parts[i].label}: {Description(parts[i])} Drag from the tray to place."; Refresh(); }
        string Description(PartData p) => p.type switch { PartType.Engine or PartType.PowerfulEngine => $"power {p.Power(state.levels[(int)p.type]):0}; one engine per hauler.", PartType.Wheel or PartType.HeavyWheel => $"grip {p.grip * (1 + .1f * (state.levels[(int)p.type] - 1)):0.0}; spread wheels for balance.", PartType.Cargo => $"{p.Capacity(state.levels[(int)p.type])} {Floor.OreName}, +{Floor.OreMass:0.00} mass each.", PartType.Balloon => "reduces effective weight with lift.", PartType.Spring => "bouncy suspension absorbs rough landings.", PartType.Propeller => "forward thrust; mount low to avoid tipping.", PartType.Ballast => $"{p.mass * (1 + .12f * (state.levels[(int)p.type] - 1)):0.0} mass; upgrades add weight. Mount low for balance.", _ => $"mass {p.mass:0.0}" };
        void Cell(int x, int y)
        {
            if (running) return;
            var part = Current.design.parts.Find(p => p.x == x && p.y == y);
            if (part != null) { selected = part.type; message = parts[(int)selected].label + ": " + Description(parts[(int)selected]); Refresh(); }
        }
        string Validate()
        {
            var ps = Current.design.parts;
            if (ps.Exists(p => p.x < 0 || p.x >= 8 || p.y < 0 || p.y >= 5 || (int)p.type >= parts.Length)) return "Keep all parts inside the build grid.";
            if (ps.Exists(p => !state.partsUnlocked[(int)p.type])) return "Unlock the parts used in this design first.";
            if (!ps.Exists(p => p.type == PartType.Frame)) return "Add at least one frame."; if (!ps.Exists(p => parts[(int)p.type].IsWheel)) return "Add at least one wheel."; if (ps.FindAll(p => parts[(int)p.type].IsEngine).Count != 1) return "Add exactly one engine."; if (!ps.Exists(p => p.type == PartType.Cargo)) return "Add a cargo bin.";
            var structural = ps.FindAll(p => !parts[(int)p.type].IsWheel); var reached = new HashSet<PlacedPart> { structural[0] }; bool more = true; while (more) { more = false; foreach (var p in structural) if (!reached.Contains(p)) foreach (var r in new List<PlacedPart>(reached)) if (Math.Abs(p.x - r.x) + Math.Abs(p.y - r.y) == 1) { reached.Add(p); more = true; break; } }
            if (reached.Count != structural.Count) return "Connect all body parts edge to edge."; foreach (var p in ps) if (parts[(int)p.type].IsWheel && !structural.Exists(q => Math.Abs(q.x - p.x) + Math.Abs(q.y - p.y) == 1)) return "Attach each wheel next to a body part."; return null;
        }
        void Upgrade() { var i = (int)selected; if (!state.partsUnlocked[i]) return; if (state.levels[i] >= 5) { message = "Maximum level reached."; Refresh(); return; } int cost = parts[i].upgradeCost * (1 << (state.levels[i] - 1)); if (state.coins < cost) { message = $"Upgrade {parts[i].label} costs {cost} coins."; } else { state.coins -= cost; state.levels[i]++; message = $"{parts[i].label} upgraded to Lv {state.levels[i]}. Retest to update production."; Save(); } Refresh(); Preview(); }
        void GrantDeepMineParts() { if (state.floors.Length > 3 && state.floors[3].unlocked) { state.partsUnlocked[(int)PartType.PowerfulEngine] = true; state.partsUnlocked[(int)PartType.HeavyWheel] = true; } if (state.floors.Length > 6 && state.floors[6].unlocked) { state.partsUnlocked[(int)PartType.Propeller] = true; state.partsUnlocked[(int)PartType.Ballast] = true; } }
        void SelectFloor(int i)
        {
            if (running) return; if (!state.floors[i].unlocked) { if (i == 0) return; if (state.floors[i - 1].automated == null) { message = $"Start production on Floor {i} first."; Refresh(); return; } if (state.coins < floors[i].unlockPrice) { message = $"Need {floors[i].unlockPrice:N0} coins to unlock Floor {i + 1}."; Refresh(); return; } if (state.Income < floors[i].outputGate) { message = $"Raise mine output to {Money(floors[i].outputGate)}/min first. Upgrade crews and loading."; Refresh(); return; } state.coins -= floors[i].unlockPrice; state.floors[i].unlocked = true; GrantDeepMineParts(); }
            GrantDeepMineParts(); showResults = false; undoBuilds.Clear(); redoBuilds.Clear(); state.selectedFloor = i; floorPage = i / 3; message = $"Floor {i + 1}: {Floor.challenge}. Deliver {Floor.requiredOre} {Floor.OreName} to start production."; Save(); BuildTrack(); Refresh();
        }
        void BuildTrack()
        {
            if (track != null) Destroy(track.gameObject); if (vehicle != null) vehicle.Dispose(); track = Floor.trackPrefab != null ? Instantiate(Floor.trackPrefab).transform : new GameObject("Floor " + Floor.number + " · " + Floor.challenge).transform;
            if (Floor.trackPrefab == null) MineChallenges.BuildColliders(track.gameObject, Floor);
            var points = Floor.terrain; for (int i = 0; i < points.Length - 1; i++)
            {
                if (Floor.HasGap(i)) continue; var a = points[i]; var b = points[i + 1]; var o = new GameObject("Rock"); o.transform.SetParent(track, false); o.transform.position = (a + b) * .5f + Vector2.down * 2; var mesh = new Mesh(); mesh.vertices = new[] { (Vector3)(a - o.transform.position.xy()), (Vector3)(b - o.transform.position.xy()), (Vector3)(b - o.transform.position.xy() + Vector2.down * 8), (Vector3)(a - o.transform.position.xy() + Vector2.down * 8) }; mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.colors = new[] { Floor.rockTint * 1.3f, Floor.rockTint * 1.3f, Floor.rockTint * .65f, Floor.rockTint * .65f }; mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }; mesh.RecalculateNormals(); o.AddComponent<MeshFilter>().mesh = mesh; var renderer = o.AddComponent<MeshRenderer>(); if (rockMaterial == null) { rockMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")); rockMaterial.color = Color.white; }
                renderer.sharedMaterial = rockMaterial; o.AddComponent<GeneratedMesh>().mesh = mesh; renderer.sortingOrder = 0; 
                Vector2 mid = (a + b) * .5f; var lip = VehiclePhysics.Shape("Amber rock edge", track, mid, new Vector2(Vector2.Distance(a, b), .17f), Gold, false, 1); lip.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            }
            VehiclePhysics.Shape("Delivery beacon", track, new Vector2(Floor.distance, 4), new Vector2(.25f, 8), Green, false, 1); VehiclePhysics.Shape("Ore depot", track, new Vector2(Floor.distance + 2, 1.5f), new Vector2(3, 2), Blue, false, 1);
            for (int i = 0; i < Floor.distance / 7; i++) { float x = i * 7; VehiclePhysics.Shape("Mine support", track, new Vector2(x, 3), new Vector2(.16f, 12), new Color(.15f, .21f, .26f), false, -2); VehiclePhysics.Shape("Lantern", track, new Vector2(x + .4f, 6), new Vector2(.25f, .42f), Gold, false, -1); }
            DecorateCourse(); DecorateChallenges(); Preview();
        }
        void DecorateCourse()
        {
            var texture = Resources.Load<Texture2D>("Art/MineBackdrop");
            if (texture != null) { var o = new GameObject("Painted cavern backdrop"); o.transform.SetParent(track, false); cavern = o.AddComponent<SpriteRenderer>(); if (cavernSprite == null) cavernSprite = Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, 100); cavern.sprite = cavernSprite; cavern.sortingOrder = -20; float height = 14; float width = height * cam.aspect; o.transform.localScale = new Vector3(width / cavern.sprite.bounds.size.x, height / cavern.sprite.bounds.size.y, 1); o.transform.position = new Vector3(5, 3, 4); }
            // Faceted ore deposits and timber braces give the physical surface depth.
            for (int i = 0; i < Floor.terrain.Length - 1; i++)
            {
                if (Floor.HasGap(i)) continue; var a = Floor.terrain[i]; var b = Floor.terrain[i + 1]; float length = Vector2.Distance(a, b);
                for (float along = 2; along < length; along += 5) { var p = Vector2.Lerp(a, b, along / length); var rock = VehiclePhysics.Shape("Rock facet", track, p + Vector2.down * .75f, new Vector2(2, 1.3f), Floor.rockTint * 1.5f, true, -1); rock.transform.rotation = Quaternion.Euler(0, 0, (i % 3 - 1) * 18); }
                if (i % 3 == 1) { var p = Vector2.Lerp(a, b, .6f); var ore = VehiclePhysics.Shape("Crystal seam", track, p + Vector2.up * .18f, new Vector2(1.3f, 1.3f), Color.white, false, 1); ore.GetComponent<SpriteRenderer>().sprite = MineArt.GemFor(Floor.oreType); }
            }
            for (int i = 0; i < Floor.distance / 12; i++) { float x = i * 12; VehiclePhysics.Shape("Timber post", track, new Vector2(x, 3), new Vector2(.25f, 9), new Color(.64f, .34f, .14f), false, -3); var brace = VehiclePhysics.Shape("Wood brace", track, new Vector2(x + .55f, 5.4f), new Vector2(1.8f, .22f), Wood, false, -3); brace.transform.rotation = Quaternion.Euler(0, 0, 40); VehiclePhysics.Shape("Lantern glow", track, new Vector2(x + .9f, 5.8f), Vector2.one * .6f, new Color(1, .7f, .05f, .18f), true, -2); VehiclePhysics.Shape("Lantern glass", track, new Vector2(x + .9f, 5.8f), new Vector2(.23f, .39f), new Color(1, .88f, .29f), false, -1); }
        }
        void Preview()
        {
            if (running) return; if (vehicle != null) vehicle.Dispose(); cam.orthographicSize = 4.8f; cam.transform.position = new Vector3(5, 1.6f, -10); if (cavern != null) cavern.transform.position = new Vector3(5, 1.6f, 4); progress = 0; // Preview shares the exact construction used by the test.
            if (Current.design.parts.Count > 0) { vehicle = VehiclePhysics.Spawn(Current.design, parts, state.levels, new Vector2(5, .65f), Floor); vehicle.body.simulated = false; foreach (var rb in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None)) rb.simulated = false; }
        }
        void StartRun() { string invalid = Validate(); if (invalid != null) { message = invalid; Refresh(); return; } if (vehicle != null) vehicle.Dispose(); vehicle = VehiclePhysics.Spawn(Current.design, parts, state.levels, new Vector2(5, 2), Floor); showResults = false; screen = MineScreen.Workshop; running = true; elapsed = stalled = flipped = 0; progress = 0; message = "Hauler test in progress. STOP / MODIFY returns to editing."; Refresh(); }
        void FixedUpdate()
        {
            if (!running || vehicle == null) return;
            elapsed += Time.fixedDeltaTime;
            stalled = elapsed > 2 && vehicle.body.linearVelocity.magnitude < .35f ? stalled + Time.fixedDeltaTime : 0;
            flipped = Mathf.Abs(Mathf.DeltaAngle(vehicle.body.rotation, 0)) > 110 ? flipped + Time.fixedDeltaTime : 0;
        }
        void Update()
        {
            if (state == null || wallet == null) return; if (!suspended) state.Tick(Time.unscaledDeltaTime); wallet.text = $"<size=42><color=#FFF2AA>{Money(state.coins)}</color></size> <size=21>COINS</size>\n<size=30><color=#AAFF71>+{Money(state.CashIncome)}/min AUTO</color></size>";
            if (running && vehicle != null)
            {
                progress = Mathf.Clamp(vehicle.body.position.x - 5, 0, Floor.distance); var target = new Vector3(vehicle.body.position.x + 1.7f, Mathf.Max(1.6f, vehicle.body.position.y + .8f), -10); cam.transform.position = Vector3.Lerp(cam.transform.position, target, Time.deltaTime * 6); cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, Mathf.Max(6.5f, 6.2f / cam.aspect) + Mathf.Clamp(vehicle.body.linearVelocity.magnitude * .06f, 0, 1.5f), Time.deltaTime * 2); if (cavern != null) { cavern.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 4); float height = Mathf.Max(14, cam.orthographicSize * 2.1f); cavern.transform.localScale = new Vector3(height * cam.aspect / cavern.sprite.bounds.size.x, height / cavern.sprite.bounds.size.y, 1); } 
                if (progress >= Floor.distance) EndRun(true, "Delivery complete!"); else if (vehicle.body.position.y < -6) EndRun(false, "Fell into the mine. Try lift or a wider wheelbase."); else if (vehicle.broken) EndRun(false, "A wheel disconnected."); else if (flipped >= 3) EndRun(false, "Flipped and could not recover. Try a lower centre of mass."); else if (stalled >= 3) EndRun(false, "Stalled for 3 seconds. Try less cargo or more power."); else if (elapsed >= 30) EndRun(false, "Time limit reached (30 seconds). Improve your hauler.");
            }
            if (hud != null) hud.text = $"FLOOR {Active + 1}     {progress:0} / {Floor.distance:0} m     {(running ? elapsed.ToString("0.0") + "s" : showResults ? "RESULT" : "WORKSHOP")}";
            if (telemetry != null) telemetry.text = vehicle != null ? $"{Floor.OreName} {vehicle.cargo} / need {Floor.requiredOre}  ·  Mass {vehicle.totalMass:0.0}  ·  Ore mass {vehicle.cargoMass:0.0}\nPower {vehicle.enginePower:0}  ·  Speed {vehicle.body.linearVelocity.magnitude:0.0} m/s  ·  Current {Current.automated?.rate ?? 0:0}/min" : $"Deliver {Floor.requiredOre} {Floor.OreName} to start production. Current {Current.automated?.rate ?? 0:0}/min";
            if (Time.unscaledTime >= nextUIUpdate) { nextUIUpdate = Time.unscaledTime + .15f; foreach (var update in liveUI) update(); }
            if (!suspended) saveClock += Time.unscaledDeltaTime; if (saveClock > 10) { saveClock = 0; Save(); }
        }
        void EndRun(bool success, string reason)
        {
            if (!running) return; running = false; Time.timeScale = 1; if (vehicle != null) { vehicle.body.simulated = false; foreach (var rb in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None)) rb.simulated = false; }
            if (success) { vehicle.DeliverySound(); float rate = vehicle.cargo / Mathf.Max(elapsed, .1f) * 60 * Floor.multiplier * Floor.OreValue; bool best = rate > Current.best; Current.lastSuccess = new RunRecord { vehicle = Current.design.Copy(), ore = vehicle.cargo, seconds = elapsed, rate = rate }; Current.best = Mathf.Max(Current.best, rate); message = $"{(best ? "NEW BEST!" : "SUCCESS!")} {vehicle.cargo} ore · {elapsed:0.0}s · {rate:0}/min. " + (vehicle.cargo >= Floor.requiredOre ? (Current.automated == null ? "START PRODUCTION to use this hauler." : "REPLACE RECORD to use this hauler.") : $"Need {Floor.requiredOre} ore to automate; add cargo."); } else message = "TEST FAILED — " + reason;
            Current.lastAttempt = new RunAttempt { success = success, reason = success ? (vehicle.cargo >= Floor.requiredOre ? "DELIVERY COMPLETE" : "DELIVERED — MORE ORE NEEDED") : reason, hint = RunHint(success, reason), ore = vehicle.cargo, seconds = elapsed, distance = progress, rate = success ? Current.lastSuccess.rate : 0, airSeconds = vehicle.airSeconds, impact = vehicle.peakImpact };
            showResults = true; Save(); Refresh();
        }
        void Automate() { if (!Current.unlocked || running) return; var r = Current.lastSuccess; if (r == null || r.ore < Floor.requiredOre) return; Current.automated = JsonUtility.FromJson<RunRecord>(JsonUtility.ToJson(r)); message = $"Floor {Active + 1} production recorded. Hire a manager to collect automatically."; Save(); Refresh(); }
        void OfflinePanel() { ModalBackdrop(); Box("Offline", 115, 655, 850, 340, Navy, true); Label("YOUR PIGGIES WERE BUSY!", 155, 690, 770, 70, 36, Gold, true); Label($"+{pendingOffline:N0} coins · up to {SaveManager.OfflineHours} hours", 155, 770, 770, 55, 30, Color.white, true); Button("COLLECT", 155, 855, 770, 90, () => { state.Earn(pendingOffline); pendingOffline = 0; Save(); Refresh(); }, Green, 34, true); }
        void DebugPanel()
        {
            ModalBackdrop(); Box("Debug tools", 65, 630, 950, 375, Navy, true); Label("DEVELOPMENT TOOLS", 90, 640, 900, 45, 29, Gold, true);
            Button("+1000 COINS", 90, 700, 430, 65, () => { state.coins += 1000; Save(); }, Blue, 24, true); Button("UNLOCK NEXT FLOOR", 540, 700, 450, 65, () => { for (int i = 1; i < floors.Length; i++) if (!state.floors[i].unlocked) { state.floors[i].unlocked = true; break; } Save(); Refresh(); }, Blue, 24, true);
            Button("ALL PARTS", 90, 780, 275, 65, () => { for (int i = 0; i < parts.Length; i++) state.partsUnlocked[i] = true; Save(); Refresh(); }, Blue, 23, true); Button("COMPLETE TEST", 380, 780, 310, 65, () => { if (running) { elapsed = Mathf.Max(elapsed, 10); EndRun(true, "Debug completion"); } }, Blue, 23, true); Button("RESET SAVE", 705, 780, 285, 65, () => { if (running) EndRun(false, "Save reset"); SaveManager.Reset(); state = new MineState(); state.EnsureFloorCount(floors.Length); floorPage = 0; state.floors[0].unlocked = true; for (int i = 0; i < floors.Length; i++) state.floors[i].design = floors[i].blueprint.Copy(); pendingOffline = 0; state.EnsureEconomy(floors); Save(); BuildTrack(); Refresh(); }, new Color(.65f, .15f, .2f), 23, true);
            for (int i = 0; i < 3; i++) { float speed = i == 0 ? .5f : i == 1 ? 1 : 2; Button($"PHYSICS x{speed}", 90 + i * 305, 865, 290, 65, () => Time.timeScale = speed, Panel, 23, true); }
            Button("CLOSE TOOLS", 90, 943, 890, 45, () => { debug = false; Refresh(); }, Blue, 22, true);
        }
        void Save() { state.uncollectedOffline = pendingOffline; SaveManager.Save(state); }
        void OnApplicationPause(bool pause) { if (state == null) return; if (pause) { Save(); suspended = true; } else if (suspended) { pendingOffline += state.ApplyOffline(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); suspended = false; Save(); Refresh(); } }
        void OnApplicationQuit() { if (state != null) Save(); }
    }
    public sealed class GeneratedMesh : MonoBehaviour { public Mesh mesh; void OnDestroy() { if (mesh != null) Destroy(mesh); } }
    public static class VectorExtensions { public static Vector2 xy(this Vector3 v) => new(v.x, v.y); }
}
