using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ContraptionMine
{
    public sealed partial class MineGame
    {
        static readonly Dictionary<int, Sprite> deliveryBackgrounds = new();
        void DrawMine()
        {
            Box("Mine summary", 28, 190, 1024, 178, Blue);
            Bind(Label("", 52, 201, 940, 62, 37, Gold), () => $"MINE OUTPUT  {Money(state.Income)}/min");
            Bind(Label("", 52, 267, 620, 79, 24, Color.white), () => $"Auto collected: {Money(state.CashIncome)}/min\nReady to collect: {Money(state.Stored)} coins");
            var collect = Button("", 694, 275, 331, 65, CollectProduction, Green, 25);
            Bind(collect.GetComponentInChildren<TMP_Text>(), () => $"COLLECT {Money(state.Stored)}"); Enable(collect, () => state.Stored >= 1);
            int pages = (floors.Length + 2) / 3; floorPage = Mathf.Clamp(floorPage, 0, pages - 1);
            Box("Floor pages", 28, 377, 1024, 62, Panel);
            Button("<", 30, 384, 85, 46, () => { floorPage = (floorPage + pages - 1) % pages; Refresh(); }, Blue);
            var title = Label($"FLOORS {floorPage * 3 + 1}–{Math.Min(floors.Length, floorPage * 3 + 3)} / {floors.Length}", 130, 380, 815, 54, 26, Color.white); title.alignment = TextAlignmentOptions.Center;
            Button(">", 965, 384, 85, 46, () => { floorPage = (floorPage + 1) % pages; Refresh(); }, Blue);
            for (int row = 0; row < 3; row++)
            {
                int index = floorPage * 3 + row; if (index >= floors.Length) break; float y = 450 + row * 416; var f = state.floors[index]; var d = floors[index];
                Box("Floor card", 28, y, 1024, 399, f.unlocked ? Blue : Panel);
                Label($"FLOOR {index + 1} · {d.OreName}", 52, y + 15, 715, 48, 34, Color.white);
                var badge = Label(f.automated != null ? (f.manager ? "MANAGED" : "COLLECT") : f.unlocked ? "BUILD" : "LOCKED", 806, y + 17, 220, 46, 22, f.manager ? new Color(.65f, 1, .3f) : Gold); badge.alignment = TextAlignmentOptions.Center;
                Bind(Label("", 52, y + 68, 940, 46, 30, Gold), () => f.automated != null ? $"{Money(MineEconomy.FloorRate(state, index))}/min" : d.challenge);
                if (f.automated != null)
                {
                    DeliveryStrip(48, y + 119, 984, 173, index);
                    Bind(Label("", 52, y + 297, 945, 41, 22, Color.white), () => $"Crew {f.crewLevel} · Loading {f.loadingLevel}  |  " + (f.manager ? "Manager collects automatically" : $"{Money(f.stored)} coins waiting"));
                    Button("MANAGE FLOOR", 52, y + 340, 972, 44, () => OpenFloor(index, false), Cyan, 24);
                }
                else if (f.unlocked)
                {
                    Label("Build and record a hauler to start production.\nYour run becomes this floor's base output.", 52, y + 138, 945, 141, 29, Color.white);
                    Button(f.lastSuccess != null ? "RESUME SAVED HAULER" : "BUILD HAULER", 52, y + 319, 972, 66, () => OpenFloor(index, f.lastSuccess == null), Green, 28);
                }
                else
                {
                    string requirement = d.outputGate > 0 ? $"Automate Floor {index} · Reach {Money(d.outputGate)}/min mine output" : $"Start production on Floor {index} first";
                    Label($"{d.challenge}\n{requirement}", 52, y + 136, 945, 128, 27, Color.white);
                    var unlock = Button($"UNLOCK · {Money(d.unlockPrice)} COINS", 52, y + 319, 972, 66, () => OpenFloor(index, false), Green, 27);
                    Enable(unlock, () => CanUnlock(index));
                }
            }
            Toast();
        }
        bool CanUnlock(int index) => index == 0 || state.floors[index].unlocked || state.floors[index - 1].automated != null && state.coins >= floors[index].unlockPrice && state.Income >= floors[index].outputGate;
        void OpenFloor(int index, bool workshop)
        {
            SelectFloor(index); if (Active != index || !state.floors[index].unlocked) return;
            screen = workshop ? MineScreen.Workshop : MineScreen.Mine; floorDetails = !workshop; message = ""; Refresh();
        }
        void DrawFloorManagement()
        {
            Box("Floor heading", 28, 185, 1024, 76, Blue);
            Button("< MINE", 30, 192, 230, 60, () => { floorDetails = false; message = ""; Refresh(); }, Blue, 25);
            Label($"FLOOR {Active + 1} · {Floor.OreName}", 282, 189, 762, 65, 37, Color.white);
            Box("Production", 28, 275, 1024, 156, Blue);
            Bind(Label("", 52, 285, 940, 57, 39, Gold), () => $"{Money(MineEconomy.FloorRate(state, Active))}/min OUTPUT");
            double basis = Current.automated?.rate ?? 0;
            Bind(Label("", 52, 347, 940, 67, 24, Color.white), () => basis > 0 ? $"Recorded hauler {Money(basis)}/min × {MineEconomy.FloorRate(state, Active) / basis:0.##} production bonus\nCrew, loading, manager, refinery, and permanent bonuses" : "Test a hauler in the workshop, then START PRODUCTION.");
            if (Current.automated != null) DeliveryStrip(28, 451, 1024, 230, Active);
            else { Box("Waiting floor", 28, 451, 1024, 230, Panel); Label(Current.lastSuccess != null ? "Your successful run is saved. USE SAVED RUN starts production.\nNo need to rebuild or repeat the test." : "Your recorded hauler sets the starting output.\nProduction upgrades work without another run.", 55, 480, 960, 160, 30, Color.white); }
            Box("Purchase amount", 28, 700, 1024, 81, Panel);
            Label("BUY AMOUNT", 35, 713, 315, 56, 27, Gold);
            int[] modes = { 1, 10, 0 }; string[] names = { "1", "10", "MAX" };
            for (int i = 0; i < 3; i++) { int mode = modes[i]; Button(names[i], 365 + i * 230, 713, 210, 59, () => { buyMode = mode; Refresh(); }, buyMode == mode ? Cyan : Blue, 27); }
            ProductionCard(ProductionUpgrade.Crew, 801, "MINING CREW", "+14% ore output per level");
            ProductionCard(ProductionUpgrade.Loading, 1043, "LOADING STATION", "+10% delivery output per level");
            Box("Manager", 28, 1285, 1024, 180, Blue);
            Label("FLOOR MANAGER", 52, 1298, 605, 51, 32, Color.white);
            Label(Current.manager ? "Hired · +20% production\nCollects online and earns offline rewards" : "+20% production + automatic collection\nManual earnings wait here until collected", 52, 1350, 605, 98, 24, Gold);
            var hire = Button(Current.manager ? "HIRED" : $"HIRE\n{Money(MineEconomy.ManagerCost(Active))}", 710, 1310, 315, 124, HireManager, Current.manager ? Panel : Green, 27);
            Enable(hire, () => Current.automated != null && !Current.manager && state.coins >= MineEconomy.ManagerCost(Active));
            var collect = Button("", 30, 1494, 505, 82, () => { state.CollectFloor(Active); Save(); Refresh(); }, Green, 27);
            Bind(collect.GetComponentInChildren<TMP_Text>(), () => $"COLLECT {Money(Current.stored)}"); Enable(collect, () => Current.stored >= 1);
            var resume = Button(Current.automated == null ? "USE SAVED RUN" : "PRODUCTION ACTIVE", 550, 1494, 500, 82, Automate, Blue, 25);
            Enable(resume, () => Current.automated == null && Current.lastSuccess != null && Current.lastSuccess.ore >= Floor.requiredOre);
            Button("OPEN CONTRAPTION WORKSHOP", 30, 1620, 1020, 99, () => SetScreen(MineScreen.Workshop), Cyan, 30);
            Toast();
        }
        void ProductionCard(ProductionUpgrade upgrade, float y, string title, string description)
        {
            Box(title, 28, y, 1024, 222, Blue);
            int level = upgrade == ProductionUpgrade.Crew ? Current.crewLevel : Current.loadingLevel;
            Label($"{title} · LV {level}", 52, y + 13, 635, 50, 31, Color.white);
            Label(description, 52, y + 68, 635, 45, 26, Gold);
            Bind(Label("", 52, y + 118, 635, 78, 24, Color.white), () =>
            {
                var quote = MineEconomy.Quote(state, Active, upgrade, buyMode);
                if (level == MineEconomy.MaxLevel) return "Maximum level reached";
                double gain = MineEconomy.FloorRate(state, Active) * (Math.Pow(upgrade == ProductionUpgrade.Crew ? 1.14 : 1.10, quote.count) - 1);
                return quote.count == 0 ? "Save coins for the next level" : $"Buy {quote.count} {(quote.count == 1 ? "level" : "levels")} · +{Money(gain)}/min\nNo contraption test required";
            });
            var buy = Button("", 710, y + 50, 315, 126, () => BuyProduction(upgrade), Green, 26);
            Bind(buy.GetComponentInChildren<TMP_Text>(), () => { var quote = MineEconomy.Quote(state, Active, upgrade, buyMode); return level == MineEconomy.MaxLevel ? "MAX LEVEL" : $"BUY {quote.count}\n{Money(quote.cost)} COINS"; });
            Enable(buy, () => { var quote = MineEconomy.Quote(state, Active, upgrade, buyMode); return Current.automated != null && quote.count > 0 && state.coins >= quote.cost; });
        }
        void BuyProduction(ProductionUpgrade upgrade)
        {
            if (!MineEconomy.Buy(state, Active, upgrade, buyMode)) return;
            message = $"{upgrade} upgraded. Production is now {Money(MineEconomy.FloorRate(state, Active))}/min."; Save(); Refresh();
        }
        void HireManager() { if (!MineEconomy.Hire(state, Active)) return; message = "Manager hired. Earnings are now collected automatically."; Save(); Refresh(); }
        void CollectProduction() { double amount = state.CollectAll(); message = $"Collected {Money(amount)} coins."; Save(); Refresh(); }
        void Toast() { if (string.IsNullOrEmpty(message)) return; Box("Status", 28, 1738, 1024, 73, Panel); Label(message, 50, 1742, 980, 65, 23, Gold); }
        void DeliveryStrip(float x, float y, float w, float h, int index)
        {
            var f = state.floors[index]; var record = f.automated; if (record?.vehicle == null) return;
            var strip = Box("Animated delivery", x, y, w, h, Panel);
            var texture = Resources.Load<Texture2D>("Art/MineBackdrop");
            if (texture != null)
            {
                int cropHeight = Mathf.Clamp(Mathf.RoundToInt(texture.width * (h - 20) / (w - 20)), 1, texture.height);
                if (!deliveryBackgrounds.TryGetValue(cropHeight, out var backdrop)) deliveryBackgrounds[cropHeight] = backdrop = Sprite.Create(texture, new UnityEngine.Rect(0, (texture.height - cropHeight) * .45f, texture.width, cropHeight), Vector2.one * .5f, 100);
                var art = Flat("Cavern strip", 10, 10, w - 20, h - 20, new Color(.65f, .75f, .85f)); Rect(art, 10, 10, w - 20, h - 20, strip.transform); art.GetComponent<Image>().sprite = backdrop;
            }
            var rail = Flat("Track", 25, h - 35, w - 50, 7, Gold); Rect(rail, 25, h - 35, w - 50, 7, strip.transform);
            var depot = Box("Depot", w - 115, h - 100, 80, 68, Wood); Rect(depot, w - 115, h - 100, 80, 68, strip.transform);
            var cart = new GameObject("Recorded hauler", typeof(RectTransform)); var root = Rect(cart, 200, h - 51, 1, 1, strip.transform); var wheels = new List<Image>();
            foreach (var part in record.vehicle.parts) foreach (var other in record.vehicle.parts)
            {
                if (part.x > other.x || part.x == other.x && part.y >= other.y || Mathf.Abs(part.x - other.x) + Mathf.Abs(part.y - other.y) != 1) continue;
                Vector2 a = new((part.x - 3.5f) * 24 + 13, (3 - part.y) * 24 - 30), b = new((other.x - 3.5f) * 24 + 13, (3 - other.y) * 24 - 30);
                Vector2 mid = (a + b) * .5f; bool wheel = parts[(int)part.type].IsWheel || parts[(int)other.type].IsWheel;
                var beam = Flat("Delivery connection", 0, 0, Vector2.Distance(a, b), wheel ? 3 : 4, wheel ? new Color(.7f, .8f, .9f) : Wood);
                var r = Rect(beam, mid.x, mid.y, Vector2.Distance(a, b), wheel ? 3 : 4, cart.transform); r.pivot = Vector2.one * .5f; r.anchoredPosition = new Vector2(mid.x, -mid.y); r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-(b.y - a.y), b.x - a.x) * Mathf.Rad2Deg);
            }
            foreach (var part in record.vehicle.parts)
            {
                float unit = 24; var icon = Flat("Delivery part", (part.x - 3.5f) * unit, (3 - part.y) * unit - 43, 26, 26, Color.white);
                Rect(icon, (part.x - 3.5f) * unit, (3 - part.y) * unit - 43, 26, 26, cart.transform); icon.GetComponent<Image>().sprite = MineArt.Icon(part.type, floors[index].oreType);
                if (parts[(int)part.type].IsWheel) wheels.Add(icon.GetComponent<Image>());
                if (parts[(int)part.type].IsEngine) { var pig = Flat("Delivery piggy", 0, 0, 24, 24, Color.white); Rect(pig, (part.x - 3.5f) * unit, (3 - part.y) * unit - 66, 24, 24, cart.transform); pig.GetComponent<Image>().sprite = MineArt.Pig; }
            }
            var status = Label("", 17, 9, w - 35, 29, 18, Color.white); Rect(status.gameObject, 17, 9, w - 35, 29, strip.transform);
            double bonus = record.rate > 0 ? MineEconomy.FloorRate(state, index) / record.rate : 1;
            var loop = strip.AddComponent<DeliveryLoop>(); loop.cart = root; loop.wheels = wheels.ToArray(); loop.status = status; loop.floor = f; loop.productionRate = MineEconomy.FloorRate(state, index); loop.duration = Mathf.Clamp(record.seconds * .35f / Mathf.Sqrt((float)Math.Max(1, bonus)), 2.5f, 10); loop.from = 190; loop.to = w - 210; loop.y = -(h - 51); loop.phase = index * .17f;
        }
        void DrawResearch()
        {
            Box("Research heading", 28, 185, 1024, 174, Blue);
            Label("MINE RESEARCH", 35, 198, 990, 80, 46, Color.white);
            Label("Refineries improve an ore type across every recorded floor.", 35, 283, 995, 69, 27, Gold);
            for (int ore = 0; ore < 3; ore++)
            {
                int type = ore; float y = 375 + ore * 244; int level = state.refineries[ore];
                Box("Ore refinery", 28, y, 1024, 222, Blue);
                var gem = Flat("Ore gem", 53, y + 51, 102, 117, Color.white); gem.GetComponent<Image>().sprite = MineArt.GemFor((OreType)ore);
                Label($"{((OreType)ore).ToString().ToUpperInvariant()} REFINERY · LV {level}", 174, y + 17, 510, 55, 29, Color.white);
                Label($"×{MineEconomy.RefineryMultiplier(level):0.##} ore earnings\nNext level adds +0.20 to the multiplier", 174, y + 85, 510, 97, 24, Gold);
                double cost = MineEconomy.RefineryCost(ore, level);
                var buy = Button(level >= 50 ? "MAX LEVEL" : $"UPGRADE\n{Money(cost)} COINS", 710, y + 49, 315, 126, () => { if (MineEconomy.BuyRefinery(state, type)) { message = "Refinery upgraded across the mine."; Save(); Refresh(); } }, Green, 26);
                Enable(buy, () => level < 50 && state.coins >= cost);
            }
            Box("Permanent research", 28, 1140, 1024, 294, Blue);
            Label($"FOREMAN TRAINING · LV {state.permanentResearch}", 52, 1157, 940, 55, 31, Color.white);
            Label($"Permanent ×{1 + .25 * state.permanentResearch:0.##} production\nEach level adds +0.25. Survives prestige.\nCertificates available: {state.certificates}", 52, 1215, 600, 161, 25, Gold);
            int researchCost = MineEconomy.ResearchCost(state);
            var research = Button(state.permanentResearch >= 10 ? "MAX LEVEL" : $"TRAIN\n{researchCost} CERTIFICATES", 710, 1240, 315, 126, () => { if (MineEconomy.BuyResearch(state)) { message = "Permanent research upgraded."; Save(); Refresh(); } }, Cyan, 25);
            Enable(research, () => state.permanentResearch < 10 && state.certificates >= researchCost);
            Box("Research guide", 28, 1460, 1024, 241, Panel);
            Label("Certificates come from opening a deeper mine in PRESTIGE.\nFloor crew and loading upgrades are in MINE → MANAGE FLOOR.", 52, 1490, 940, 150, 28, Color.white); Toast();
        }
        void DrawPrestige()
        {
            Box("Prestige heading", 28, 185, 1024, 106, Blue);
            Label("OPEN A DEEPER MINE", 35, 198, 990, 80, 43, Color.white);
            Box("Prestige reward", 28, 315, 1024, 300, Blue);
            Bind(Label("", 52, 339, 940, 77, 48, Gold), () => $"+{MineEconomy.PrestigeReward(state)} MINE CERTIFICATES");
            Bind(Label("", 52, 427, 940, 149, 27, Color.white), () => $"Earned this mine: {Money(state.cycleEarnings)} coins\nUnlock: Floor 9 producing + at least 1M earned\nReward grows with coins earned, including coins already spent.");
            Box("Permanent progress", 28, 648, 1024, 213, Blue);
            Label($"Permanent production: ×{state.PermanentMultiplier:0.##}\n{state.lifetimeCertificates} certificates earned · {state.prestiges} mines rebuilt\nEach earned certificate adds +5%; spend certificates on training.", 52, 674, 940, 164, 28, Gold);
            Box("What changes", 28, 895, 1024, 530, Panel);
            Label("RESET", 52, 916, 940, 56, 34, Gold);
            Label("Coins, floor unlocks, managers, crew, loading,\nand ore refineries. Unclaimed earnings are cleared.", 52, 978, 940, 113, 28, Color.white);
            Label("KEEP", 52, 1117, 940, 56, 34, new Color(.65f, 1, .3f));
            Label("Contraption designs, parts and their upgrades,\nsuccessful run records, certificates, and permanent research.\nUnlock a floor and reuse its saved run to restart production.", 52, 1179, 940, 195, 28, Color.white);
            var open = Button("REVIEW PRESTIGE", 30, 1480, 1020, 105, () => { prestigeConfirm = true; Refresh(); }, Cyan, 33);
            Enable(open, () => MineEconomy.CanPrestige(state));
            Box("Prestige guide", 28, 1604, 1024, 121, Panel);
            Label("A review screen shows the reward before you commit.\nCollect waiting earnings first to include them in the reward.", 52, 1610, 940, 114, 26, Gold); Toast();
        }
        void DrawPrestigeConfirmation()
        {
            ModalBackdrop(); Box("Prestige confirmation", 75, 470, 930, 905, Navy);
            Label("REBUILD THIS MINE?", 115, 506, 850, 74, 40, Gold);
            Bind(Label("", 115, 601, 850, 167, 30, Color.white), () => $"Gain {MineEconomy.PrestigeReward(state)} certificates.\nCoins reset from {Money(state.coins)} to 100.\nUnclaimed earnings cleared: {Money(state.Stored + pendingOffline)}.");
            Label("Floor unlocks and production upgrades reset.\nDesigns, successful runs, part upgrades, and permanent research stay.\nYou can reuse saved runs after unlocking each floor.", 115, 800, 850, 236, 28, Color.white);
            var confirm = Button("OPEN DEEPER MINE", 115, 1090, 850, 100, DoPrestige, Green, 32); Enable(confirm, () => MineEconomy.CanPrestige(state));
            Button("KEEP THIS MINE", 115, 1220, 850, 88, () => { prestigeConfirm = false; Refresh(); }, Blue, 29);
        }
        void DoPrestige()
        {
            if (!MineEconomy.Prestige(state)) return;
            pendingOffline = 0; prestigeConfirm = false; floorPage = 0; screen = MineScreen.Mine; floorDetails = false; showResults = false;
            message = "Deeper mine opened. Your designs and saved runs are ready to reuse."; Save(); BuildTrack(); Refresh();
        }
    }
}
