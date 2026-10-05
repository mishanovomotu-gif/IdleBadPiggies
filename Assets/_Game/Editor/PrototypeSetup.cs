using System;
using System.IO;
using ContraptionMine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ContraptionMineEditor
{
    [InitializeOnLoad]
    public static class PrototypeSetup
    {
        static PrototypeSetup() { EditorApplication.delayCall += Ensure; }
        static void Ensure() { if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Ensure; return; } ConfigureArt(); if (!File.Exists("Assets/_Game/Resources/Floors/Floor1.asset")) Setup(); else if (!File.Exists("Assets/_Game/Resources/Floors/Floor12.asset")) { if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.delayCall += Ensure; else { MakeContent(); AssetDatabase.SaveAssets(); } } if (SessionState.GetBool("ContraptionMine.SmokePending", false)) EditorApplication.update += WaitForSmoke; }
        [MenuItem("Tools/Contraption Mine/Setup Prototype")]
        public static void Setup()
        {
            MakeContent();
            // Import Unity's bundled TMP font assets; no external package or font dependency.
            FinishSetup();
        }
        public static void MakeContent()
        {
            Directory.CreateDirectory("Assets/_Game/Resources/Parts"); Directory.CreateDirectory("Assets/_Game/Resources/Floors"); AssetDatabase.Refresh();
            Directory.CreateDirectory("Assets/_Game/Resources/Surfaces");
            var ice = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/_Game/Resources/Surfaces/Ice.physicsMaterial2D");
            if (ice == null) { ice = new PhysicsMaterial2D("Ice") { friction = .035f, bounciness = 0 }; AssetDatabase.CreateAsset(ice, "Assets/_Game/Resources/Surfaces/Ice.physicsMaterial2D"); }
            MakePart(PartType.Frame, "Frame", 1, 0, 0, 0, 12, new Color(.79f, .44f, .16f)); MakePart(PartType.Wheel, "Wheel", .9f, 0, 0, 0, 6, new Color(.32f, .42f, .55f)); MakePart(PartType.Engine, "Engine", 2, 24, 0, 0, 1, new Color(.82f, .23f, .21f)); MakePart(PartType.Cargo, "Cargo", .8f, 0, 5, 0, 6, new Color(.05f, .65f, .78f)); MakePart(PartType.Spring, "Spring", .4f, 0, 0, 0, 3, new Color(.81f, .26f, .5f)); MakePart(PartType.Balloon, "Balloon", .3f, 0, 0, 42, 3, new Color(.68f, .35f, .83f)); MakePart(PartType.HeavyWheel, "Heavy wheel", 1.8f, 0, 0, 0, 6, new Color(.38f, .47f, .53f), 200); MakePart(PartType.PowerfulEngine, "Power engine", 3.3f, 38, 0, 0, 1, new Color(.95f, .42f, .1f), 350);
            MakeFloor(1, 80, 5, 0, 1, "BASIC HILLS", new[] { new Vector2(-8, 0), new Vector2(15, 0), new Vector2(25, 1), new Vector2(35, 0), new Vector2(48, .7f), new Vector2(60, 0), new Vector2(100, 0) }, -1);
            MakeFloor(2, 110, 10, 250, 2, "ROUGH TERRAIN", new[] { new Vector2(-8, 0), new Vector2(14, 0), new Vector2(23, 2.2f), new Vector2(32, .3f), new Vector2(39, 1), new Vector2(44, .1f), new Vector2(50, .9f), new Vector2(56, -.5f), new Vector2(61, -.5f), new Vector2(69, 1.3f), new Vector2(80, 0), new Vector2(130, 0) }, -1);
            MakeFloor(3, 150, 15, 900, 4, "GAP & OBSTACLE", new[] { new Vector2(-8, 0), new Vector2(18, 0), new Vector2(29, 2.4f), new Vector2(35, 2.4f), new Vector2(38, 1.6f), new Vector2(48, 1.9f), new Vector2(61, .2f), new Vector2(76, 3), new Vector2(90, 1), new Vector2(104, 1.6f), new Vector2(115, 0), new Vector2(175, 0) }, 3);
            MakeFloor(4, 120, 20, 2200, 6, "CRYSTAL RAVINE", new[] { new Vector2(-8, 0), new Vector2(17, 0), new Vector2(26, 1.5f), new Vector2(31, 1.5f), new Vector2(33, .7f), new Vector2(45, 1.2f), new Vector2(54, 0), new Vector2(67, 2), new Vector2(78, .4f), new Vector2(88, 1.2f), new Vector2(103, 0), new Vector2(145, 0) }, 3);
            MakeFloor(5, 130, 25, 5000, 9, "TIMBER RIDGE", new[] { new Vector2(-8, 0), new Vector2(18, 0), new Vector2(29, 2), new Vector2(39, 4), new Vector2(48, 4), new Vector2(59, 1), new Vector2(69, 1.8f), new Vector2(78, .4f), new Vector2(90, 2.2f), new Vector2(104, 0), new Vector2(150, 0) }, -1);
            MakeFloor(6, 140, 30, 11000, 14, "THE DEEP CORE", new[] { new Vector2(-8, 0), new Vector2(17, 0), new Vector2(27, 1.7f), new Vector2(34, 1.7f), new Vector2(37, 1.1f), new Vector2(50, 1), new Vector2(61, 2.5f), new Vector2(70, 2.5f), new Vector2(73, 1.5f), new Vector2(84, 1.4f), new Vector2(97, 2.2f), new Vector2(111, .3f), new Vector2(165, 0) }, 3, new[] { 7 });
            MakePart(PartType.Propeller, "Propeller", 1.2f, 32, 0, 0, 2, new Color(.8f, .3f, .17f), 1800);
            MakePart(PartType.Ballast, "Ballast", 3.5f, 0, 0, 0, 2, new Color(.5f, .6f, .7f), 600);
            MakeFloor(7, 120, 20, 23000, 18, "FROST TUNNEL", new[] { new Vector2(-8, 0), new Vector2(18, 0), new Vector2(28, .6f), new Vector2(40, .2f), new Vector2(57, .2f), new Vector2(69, .3f), new Vector2(79, 1), new Vector2(88, .2f), new Vector2(98, .8f), new Vector2(108, 0), new Vector2(145, 0) }, -1);
            MakeFloor(8, 125, 25, 45000, 23, "GOLDEN ASCENT", new[] { new Vector2(-8, 0), new Vector2(17, 0), new Vector2(28, 2.1f), new Vector2(39, 4.2f), new Vector2(48, 4.2f), new Vector2(59, 1), new Vector2(69, 2.1f), new Vector2(78, .5f), new Vector2(90, 2.4f), new Vector2(104, 0), new Vector2(150, 0) }, -1);
            MakeFloor(9, 135, 30, 85000, 30, "WIND SHAFT", new[] { new Vector2(-8, 0), new Vector2(17, 0), new Vector2(27, 1.8f), new Vector2(34, 1.8f), new Vector2(37, 1.2f), new Vector2(50, 1), new Vector2(61, 2.5f), new Vector2(70, 2.5f), new Vector2(73, 1.5f), new Vector2(84, 1.4f), new Vector2(97, 2.3f), new Vector2(111, .3f), new Vector2(160, 0) }, 3, new[] { 7 });
            MakeFloor(10, 130, 20, 4000000, 600, "GLACIER WORKS", new[] { new Vector2(-8, 0), new Vector2(18, 0), new Vector2(28, .6f), new Vector2(40, .2f), new Vector2(57, .2f), new Vector2(69, .3f), new Vector2(80, 1.1f), new Vector2(90, .2f), new Vector2(102, .8f), new Vector2(115, 0), new Vector2(155, 0) }, -1);
            MakeFloor(11, 140, 25, 32000000, 2100, "COPPER SUMMIT", new[] { new Vector2(-8, 0), new Vector2(17, 0), new Vector2(28, 2.1f), new Vector2(39, 4.1f), new Vector2(48, 4.1f), new Vector2(59, 1), new Vector2(69, 2.1f), new Vector2(80, .5f), new Vector2(94, 2.4f), new Vector2(110, 0), new Vector2(165, 0) }, -1);
            MakeFloor(12, 145, 30, 200000000, 7500, "ROYAL AIRSHAFT", new[] { new Vector2(-8, 0), new Vector2(17, 0), new Vector2(27, 1.8f), new Vector2(34, 1.8f), new Vector2(37, 1.2f), new Vector2(50, 1), new Vector2(61, 2.5f), new Vector2(70, 2.5f), new Vector2(73, 1.5f), new Vector2(84, 1.4f), new Vector2(97, 2.3f), new Vector2(111, .3f), new Vector2(170, 0) }, 3, new[] { 7 });
            ConfigureArt();
        }
        static void FinishSetup()
        {
            // Import Unity's bundled TMP font assets; no external package or font dependency.
            if (!File.Exists("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"))
            {
                string package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMPro.TMP_Text).Assembly).resolvedPath;
                AssetDatabase.ImportPackage(Path.Combine(package, "Package Resources/TMP Essential Resources.unitypackage"), false);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); new GameObject("Contraption Mine", typeof(MineGame)); EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Mine.unity"); EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/_Game/Scenes/Mine.unity", true) };
            PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960; PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait; PlayerSettings.companyName = "Contraption Mine"; PlayerSettings.productName = "Contraption Mine";
            AssetDatabase.SaveAssets(); Debug.Log("CONTRAPTION MINE SETUP COMPLETE");
        }
        static void ConfigureArt()
        {
            var importer = AssetImporter.GetAtPath("Assets/_Game/Resources/Art/MineBackdrop.png") as TextureImporter;
            if (importer != null && (!importer.isReadable || importer.wrapMode != TextureWrapMode.Clamp))
            {
                importer.isReadable = true; importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 2048; importer.SaveAndReimport();
            }
        }
        static void MakePart(PartType type, string label, float mass, float power, int capacity, float lift, int stock, Color color, int cost = 0) { string path = $"Assets/_Game/Resources/Parts/{type}.asset"; var d = AssetDatabase.LoadAssetAtPath<PartData>(path); if (d == null) { d = ScriptableObject.CreateInstance<PartData>(); AssetDatabase.CreateAsset(d, path); } d.type = type; d.label = label; d.mass = mass; d.power = power; d.capacity = capacity; d.lift = lift; d.stock = stock; d.color = color; d.unlockCost = cost; d.grip = type == PartType.HeavyWheel ? 1.8f : 1.1f; d.radius = type == PartType.HeavyWheel ? .55f : .48f; EditorUtility.SetDirty(d); }
        static void MakeFloor(int number, float distance, int ore, int price, float multiplier, string challenge, Vector2[] terrain, int gap, int[] extraGaps = null)
        {
            string path = $"Assets/_Game/Resources/Floors/Floor{number}.asset";
            var d = AssetDatabase.LoadAssetAtPath<FloorData>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<FloorData>(); AssetDatabase.CreateAsset(d, path); }
            d.number = number; d.distance = distance; d.requiredOre = ore; d.unlockPrice = price; d.multiplier = multiplier; d.challenge = challenge; d.terrain = terrain; d.gapAfter = gap; d.extraGaps = extraGaps ?? Array.Empty<int>();
            d.oreType = number == 2 || number == 5 || number == 11 ? OreType.Copper : number == 8 || number == 9 || number == 12 ? OreType.Gold : OreType.Crystal;
            d.outputGate = number == 10 ? 30000 : number == 11 ? 200000 : number == 12 ? 1000000 : 0;
            d.zones = number switch
            {
                2 => new[] { new ChallengeZone(ChallengeKind.Ice, 50, 61, 0) },
                4 => new[] { new ChallengeZone(ChallengeKind.Ceiling, 38, 51, 6.2f) },
                6 => new[] { new ChallengeZone(ChallengeKind.Wind, 58, 83, -.35f) },
                7 or 10 => new[] { new ChallengeZone(ChallengeKind.Ice, 24, 67, 0), new ChallengeZone(ChallengeKind.Ceiling, 39, 68, 3.65f) },
                9 or 12 => new[] { new ChallengeZone(ChallengeKind.Wind, 54, 85, -.55f) },
                _ => Array.Empty<ChallengeZone>()
            };
            d.rockTint = number < 4 ? new Color(.57f, .31f, .15f) : number == 4 ? new Color(.33f, .29f, .53f) : number == 5 ? new Color(.61f, .34f, .14f) : new Color(.25f, .25f, .48f);
            d.blueprint = new VehicleDefinition(); var ps = d.blueprint.parts;
            if (number <= 3)
            {
                ps.Add(new(PartType.Engine, 1, 2)); for (int x = 2; x <= 4; x++) ps.Add(new(PartType.Frame, x, 2));
                ps.Add(new(PartType.Wheel, number == 3 ? 1 : 2, 1)); ps.Add(new(PartType.Wheel, number == 3 ? 5 : 4, 1)); ps.Add(new(PartType.Cargo, 3, 3));
                if (number >= 2) ps.Add(new(PartType.Cargo, 2, 3)); if (number == 3) { ps.Add(new(PartType.Cargo, 4, 3)); ps.Add(new(PartType.Balloon, 3, 4)); ps.Add(new(PartType.Spring, 5, 2)); }
            }
            else
            {
                int bins = number > 6 ? (number == 7 || number == 10 ? 4 : number == 8 || number == 11 ? 5 : 6) : number; ps.Add(new(PartType.PowerfulEngine, 0, 2)); for (int x = 1; x <= Math.Max(5, bins); x++) ps.Add(new(PartType.Frame, x, 2));
                ps.Add(new(PartType.HeavyWheel, 1, 1)); ps.Add(new(PartType.HeavyWheel, Math.Max(5, bins), 1)); for (int x = 1; x <= bins; x++) ps.Add(new(PartType.Cargo, x, 3));
                ps.Add(new(PartType.Spring, Math.Max(5, bins) + 1, 2));
                if (number != 7 && number != 10) { ps.Add(new(PartType.Balloon, 2, 4)); ps.Add(new(PartType.Balloon, number == 4 ? 4 : 5, 4)); }
                if (number == 7 || number == 10) ps.Add(new(PartType.Ballast, 3, 1));
                if (number == 7 || number == 10) ps.Add(new(PartType.Propeller, Math.Max(5, bins) + 1, 3)); if (number == 9 || number == 12) ps.Add(new(PartType.Propeller, 3, 1));
            }
            Directory.CreateDirectory("Assets/_Game/Prefabs/Tracks"); var route = new GameObject(challenge);
            MineChallenges.BuildColliders(route, d);
            d.trackPrefab = PrefabUtility.SaveAsPrefabAsset(route, $"Assets/_Game/Prefabs/Tracks/Floor{number}.prefab"); UnityEngine.Object.DestroyImmediate(route); EditorUtility.SetDirty(d);
        }
        [MenuItem("Tools/Contraption Mine/Validate Blueprints")]
        public static void ValidateBlueprints()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
            var pd = Resources.LoadAll<PartData>("Parts"); Array.Sort(pd, (a, b) => a.type.CompareTo(b.type)); var fd = Resources.LoadAll<FloorData>("Floors"); Array.Sort(fd, (a, b) => a.number.CompareTo(b.number)); var oldMode = Physics2D.simulationMode; Physics2D.simulationMode = SimulationMode2D.Script;
            try
            {
                foreach (var area in new[] { new Rect(0, 0, 1080, 1920), new Rect(0, 0, 1179, 2556), new Rect(0, 0, 1920, 1080), new Rect(20, 60, 1139, 2400) })
                {
                    var fit = MineViewport.Fit(area);
                    if (fit.xMin < area.xMin - .01f || fit.yMin < area.yMin - .01f || fit.xMax > area.xMax + .01f || fit.yMax > area.yMax + .01f || (area.width / area.height <= 1080f / 1920f && (Mathf.Abs(fit.height - area.height) > .01f || Mathf.Abs(fit.width - area.width) > .01f))) throw new Exception("Portrait viewport overflow");
                }
                Debug.Log("PORTRAIT VIEWPORT FIT PASSED");
                foreach (var f in fd)
                {
                    var terrain = (GameObject)PrefabUtility.InstantiatePrefab(f.trackPrefab);
                    var v = VehiclePhysics.Spawn(f.blueprint, pd, new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, new Vector2(5, 2), f); float t = 0; Physics2D.SyncTransforms(); while (t < 30 && v.body.position.x < 5 + f.distance && v.body.position.y > -6) { v.StepPhysics(.02f); Physics2D.Simulate(.02f); t += .02f; }
                    Debug.Log($"BLUEPRINT FLOOR {f.number}: x={v.body.position.x:0.0}, y={v.body.position.y:0.0}, time={t:0.0}s, cargo={v.cargo}"); if (v.body.position.x < 5 + f.distance) throw new Exception($"Floor {f.number} blueprint failed"); foreach (var rb in UnityEngine.Object.FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None)) if (rb.gameObject.scene == scene) UnityEngine.Object.DestroyImmediate(rb.gameObject); UnityEngine.Object.DestroyImmediate(terrain);
                }
                ContraptionChecks.Tradeoffs(pd, fd); IdleEconomyChecks.Validate(fd);
                var legacy = new MineState { coins = 1234, floors = new[] { new FloorState { unlocked = true, automated = new RunRecord { rate = 42 } }, new FloorState(), new FloorState() } }; legacy.levels = new[] { 1, 1, 1, 4, 1, 1, 2, 3 }; legacy.partsUnlocked = new[] { true, true, true, true, true, true, true, false }; legacy.EnsurePartCount(pd.Length); legacy.EnsureFloorCount(fd.Length);
                if (legacy.levels.Length != pd.Length || legacy.levels[3] != 4 || legacy.levels[8] != 1 || !legacy.partsUnlocked[6] || legacy.partsUnlocked[8]) throw new Exception("Part migration failed"); if (legacy.floors.Length != fd.Length || legacy.coins != 1234 || legacy.Income != 42 || !legacy.floors[0].unlocked) throw new Exception("Legacy save migration failed");
                var s = new MineState(); s.floors[0].automated = new RunRecord { rate = 50 }; s.floors[0].manager = true; s.timestamp = 100; if (Math.Abs(SaveManager.Offline(s, 100 + 40000) - 14400) > .01) throw new Exception("Offline cap incorrect"); var clone = JsonUtility.FromJson<MineState>(JsonUtility.ToJson(s)); if (clone.Income != 60) throw new Exception("Save roundtrip incorrect"); Debug.Log("CONTRAPTION MINE VALIDATION PASSED");
            }
            finally { Physics2D.simulationMode = oldMode; EditorSceneManager.CloseScene(scene, true); }
        }
        static void Snapshot(int screen) { SessionState.SetBool("ContraptionMine.SnapshotPreview", true); SessionState.SetInt("ContraptionMine.SnapshotScreen", screen); PlaySmoke(); }
        public static void CaptureMine() => Snapshot(0);
        public static void CaptureManagement() => Snapshot(4);
        public static void CaptureWorkshop() => Snapshot(1);
        public static void CaptureRun() => Snapshot(6);
        public static void CaptureResult() => Snapshot(7);
        public static void CaptureResearch() => Snapshot(2);
        public static void CapturePrestigeConfirmation() => Snapshot(5);
        public static void CapturePrestige() => Snapshot(3);
        public static void CapturePreview() { SessionState.SetBool("ContraptionMine.PreviewOnly", true); PlaySmoke(); }
        public static void PrepareAndSmoke() { SessionState.SetBool("ContraptionMine.SnapshotPreview", false); SessionState.SetBool("ContraptionMine.PreviewOnly", false); Setup(); PlaySmoke(); }
        public static void PlaySmoke() { EditorSettings.asyncShaderCompilation = false; SessionState.SetBool("ContraptionMine.Smoke", true); SaveManager.Reset(); SessionState.SetBool("ContraptionMine.SmokePending", true); EditorApplication.update += WaitForSmoke; }
        static void WaitForSmoke() { if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < 20) return; EditorApplication.update -= WaitForSmoke; SessionState.SetBool("ContraptionMine.SmokePending", false); EditorApplication.EnterPlaymode(); }
        public static void BatchValidate() { Setup(); ValidateBlueprints(); }
    }
}
