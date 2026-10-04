using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ContraptionMine
{
    public enum MineScreen { Mine, Workshop, Research, Prestige }
    public sealed partial class MineGame
    {
        int floorPage, buyMode = 1; bool rebuildingUI, floorDetails, menuOpen, showTelemetry, prestigeConfirm;
        MineScreen screen = MineScreen.Mine;
        readonly List<Action> liveUI = new();
        readonly List<RectTransform> uiPanels = new();
        static readonly Color Cyan = new(.04f, .76f, 1), Wood = new(.9f, .49f, .13f);
        public static string Money(double value) => value >= 1e12 ? $"{value / 1e12:0.##}T" : value >= 1e9 ? $"{value / 1e9:0.##}B" : value >= 1e6 ? $"{value / 1e6:0.##}M" : value >= 10000 ? $"{value / 1000:0.##}K" : $"{value:0}";
        void BuildInterface()
        {
            Flat("Sky blue", 0, 0, 1080, 567, new Color(.055f, .51f, .75f));
            Box("Timber logo", 24, 22, 405, 140, Wood);
            Flat("Wood grain", 51, 85, 350, 5, new Color(.64f, .31f, .1f));
            var logo = Label("CONTRAPTION", 40, 26, 372, 57, 41, new Color(1, .9f, .32f)); logo.alignment = TextAlignmentOptions.Center;
            var name = Label("MINE", 43, 76, 365, 74, 63, Color.white); name.alignment = TextAlignmentOptions.Center;
            foreach (float x in new[] { 45f, 400f }) foreach (float y in new[] { 40f, 139f }) RoundDot("Bolt", x, y, 14, new Color(.75f, .88f, .95f));
            Box("Wallet", 449, 28, 470, 127, Blue); RoundDot("Coin", 470, 67, 48, Gold); wallet = Label("", 532, 39, 365, 101, 29, Color.white);
            Button("MENU", 941, 39, 110, 97, () => { menuOpen = !menuOpen; Refresh(); }, Blue, 22);
        }
        RectTransform Rect(GameObject o, float x, float y, float w, float h, Transform parent = null)
        {
            var r = o.GetComponent<RectTransform>(); r.SetParent(parent ? parent : designRoot, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        GameObject Flat(string n, float x, float y, float w, float h, Color c, bool dyn = false)
        {
            var o = new GameObject(n, typeof(RectTransform), typeof(Image)); Rect(o, x, y, w, h); o.GetComponent<Image>().color = c; o.GetComponent<Image>().raycastTarget = false; if (dyn || rebuildingUI) dynamicUI.Add(o); return o;
        }
        GameObject Box(string n, float x, float y, float w, float h, Color c, bool dyn = false)
        {
            var o = Flat(n, x, y, w, h, c, dyn); var im = o.GetComponent<Image>(); im.sprite = MineArt.Panel; im.type = Image.Type.Sliced; uiPanels.Add(o.GetComponent<RectTransform>()); return o;
        }
        void RoundDot(string n, float x, float y, float diameter, Color color, bool dyn = false)
        {
            VehiclePhysics.InitSprites(); var o = Flat(n, x, y, diameter, diameter, color, dyn); o.GetComponent<Image>().sprite = VehiclePhysics.Circle;
        }
        TMP_Text Label(string s, float x, float y, float w, float h, int size, Color c, bool dyn = false)
        {
            var o = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); Rect(o, x, y, w, h); var t = o.GetComponent<TextMeshProUGUI>(); t.text = s; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = c; t.raycastTarget = false; t.verticalAlignment = VerticalAlignmentOptions.Middle; t.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"); t.textWrappingMode = TextWrappingModes.Normal;
            // Keep card copy inside its card hierarchy, with local coordinates.
            for (int i = uiPanels.Count - 1; i >= 0; i--)
            {
                var panel = uiPanels[i]; if (panel == null || panel.parent != designRoot) continue;
                float px = panel.anchoredPosition.x, py = -panel.anchoredPosition.y;
                if (x >= px && y >= py && x + w <= px + panel.sizeDelta.x + .1f && y + h <= py + panel.sizeDelta.y + .1f)
                { Rect(o, x - px, y - py, w, h, panel); break; }
            }
            var shadow = o.AddComponent<Shadow>(); shadow.effectColor = new Color(.015f, .06f, .12f, .75f); shadow.effectDistance = new Vector2(1.5f, -2.5f); if (dyn || rebuildingUI) dynamicUI.Add(o); return t;
        }
        Button Button(string s, float x, float y, float w, float h, Action action, Color color, int size = 26, bool dyn = false)
        {
            var o = Box(s, x, y, w, h, color, dyn); o.GetComponent<Image>().raycastTarget = true; var b = o.AddComponent<Button>(); b.onClick.AddListener(() => action()); var colors = b.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.pressedColor = new Color(.7f, .8f, .9f); colors.disabledColor = new Color(.42f, .5f, .59f, .85f); b.colors = colors;
            var text = Label(s, 0, 0, w, h, size, Color.white); text.rectTransform.SetParent(o.transform, false); text.rectTransform.anchoredPosition = Vector2.zero; text.alignment = TextAlignmentOptions.Center; return b;
        }
        void Icon(PartType type, float x, float y, float w, float h, bool dyn = false, Transform parent = null)
        {
            var o = Flat(type + " icon", x, y, w, h, Color.white, dyn); var im = o.GetComponent<Image>(); im.sprite = MineArt.Icon(type, Floor.oreType); im.preserveAspect = true; if (parent != null) { o.transform.SetParent(parent, false); o.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, -y); }
        }
        void SetScreen(MineScreen next)
        {
            if (running && next != MineScreen.Workshop) return;
            screen = next; floorDetails = false; menuOpen = false; prestigeConfirm = false; message = ""; Refresh();
        }
        void Refresh()
        {
            foreach (var o in dynamicUI) if (o != null) { o.SetActive(false); Destroy(o); }
            dynamicUI.Clear(); liveUI.Clear(); uiPanels.Clear(); hud = telemetry = notice = null; rebuildingUI = true;
            cam.enabled = true; cam.cullingMask = screen == MineScreen.Workshop ? ~0 : 0;
            if (screen != MineScreen.Workshop) Flat("Menu backdrop", 0, 165, 1080, 1675, Navy);
            if (screen == MineScreen.Mine) { if (floorDetails) DrawFloorManagement(); else DrawMine(); }
            else if (screen == MineScreen.Workshop) DrawWorkshop();
            else if (screen == MineScreen.Research) DrawResearch(); else DrawPrestige();
            Box("Navigation", 12, 1828, 1056, 87, Panel);
            string[] tabs = { "MINE", "WORKSHOP", "RESEARCH", "PRESTIGE" };
            for (int i = 0; i < 4; i++) { int tab = i; var b = Button(tabs[i], 25 + i * 260, 1840, 250, 63, () => SetScreen((MineScreen)tab), (int)screen == i ? Cyan : Blue, 25); b.interactable = !running || i == 1; }
            if (menuOpen) DrawMenu(); if (prestigeConfirm) DrawPrestigeConfirmation(); if (pendingOffline > 0) OfflinePanel(); if (debug) DebugPanel();
            rebuildingUI = false;
        }
        void DrawWorkshop()
        {
            Box("Workshop heading", 28, 185, 1024, 83, Blue);
            Label($"FLOOR {Active + 1} · {Floor.OreName}", 35, 192, 665, 70, 40, Color.white);
            Button("MANAGE FLOOR", 715, 198, 335, 58, () => { if (!running) { screen = MineScreen.Mine; floorDetails = true; Refresh(); } }, Blue, 23).interactable = !running;
            Button(running ? "STOP / MODIFY" : "TEST RUN", 30, 288, 505, 75, () => { if (running) EndRun(false, "Run stopped. Modify your hauler and try again."); else StartRun(); }, Green, 32);
            Button(Current.automated != null ? "REPLACE RECORD" : "START PRODUCTION", 550, 288, 500, 75, Automate, Cyan, 28).interactable = !running && Current.lastSuccess != null && Current.lastSuccess.ore >= Floor.requiredOre;
            Box("Workshop notice", 28, 372, 1024, 66, Navy);
            notice = Label(string.IsNullOrEmpty(message) ? "Better deliveries raise base output. Idle upgrades are in Manage Floor." : message, 42, 373, 996, 63, 23, Gold);
            Box("Floor briefing", 28, 443, 1024, 62, Navy);
            Label($"{Floor.challenge} · DELIVER {Floor.requiredOre} {Floor.OreName}\n{MineChallenges.Brief(Floor)}", 44, 445, 990, 59, 21, Gold);
            Box("Course heading", 18, 508, 1044, 62, Blue); hud = Label("", 44, 511, 998, 50, 29, Color.white);
            Box("Cargo dashboard", 18, 955, 1044, 88, Blue);
            if (showTelemetry) telemetry = Label("", 42, 960, 996, 76, 24, Color.white);
            else Bind(Label("", 42, 960, 996, 76, 27, Color.white), () => vehicle == null ? $"Deliver {Floor.requiredOre} {Floor.OreName} to start production." : $"{Floor.OreName} {vehicle.cargo} / {Floor.requiredOre} needed · Speed {vehicle.body.linearVelocity.magnitude:0.0} m/s\nRecorded hauler: {Money(Current.automated?.rate ?? 0)}/min base output");
            Box("Workshop", 8, 1047, 1064, 778, Blue); Box("Workshop inset", 23, 1124, 1034, 650, Panel);
            Label("BUILD YOUR HAULER", 113, 1058, 770, 60, 44, Color.white); Icon(PartType.Frame, 38, 1064, 59, 55);
            Button(showTelemetry ? "LESS INFO" : "DETAILS", 881, 1069, 161, 43, () => { showTelemetry = !showTelemetry; Refresh(); }, Blue, 21);
            Label("Pick a part → tap a square. MOVE relocates parts.", 43, 1128, 994, 43, 22, new Color(.5f, .85f, 1));
            Icon(selected, 43, 1302, 123, 123, true); var hint = Label("SELECT\nTHEN PLACE", 39, 1435, 139, 83, 20, new Color(.45f, .83f, 1), true); hint.alignment = TextAlignmentOptions.Center;
            var gridHint = Label("8 × 5\nBUILD GRID", 902, 1350, 142, 108, 23, new Color(.45f, .83f, 1), true); gridHint.alignment = TextAlignmentOptions.Center;
            for (int y = 4; y >= 0; y--) for (int x = 0; x < 8; x++)
                {
                    int gx = x, gy = y; var p = Current.design.parts.Find(q => q.x == gx && q.y == gy); bool marked = moving && gx == moveX && gy == moveY;
                    var cell = Button("", 188 + x * 88, 1177 + (4 - y) * 88, 84, 84, () => Cell(gx, gy), marked ? Gold : new Color(.05f, .42f, .6f), 21, true); cell.GetComponent<Image>().sprite = MineArt.Slot; cell.interactable = !running;
                    if (p != null) Icon(p.type, 3, 3, 78, 78, false, cell.transform); else { var dot = Label("·", 0, 0, 84, 84, 26, new Color(.21f, .65f, .78f)); dot.rectTransform.SetParent(cell.transform, false); dot.rectTransform.anchoredPosition = Vector2.zero; dot.alignment = TextAlignmentOptions.Center; }
                }
            for (int i = 0; i < parts.Length; i++)
            {
                int pi = i; var d = parts[i]; int count = Current.design.parts.FindAll(p => p.type == d.type).Count; bool unlocked = state.partsUnlocked[i]; float x = 30 + (i % 5) * 204, y = 1622 + (i / 5) * 76;
                var b = Button("", x, y, 200, 70, () => Pick(pi), selected == d.type && !delete && !moving ? Cyan : Blue, 22, true); b.interactable = !running;
                Icon(d.type, 5, 8, 48, 49, false, b.transform); string tileName = d.type == PartType.PowerfulEngine ? "Power eng." : d.type == PartType.HeavyWheel ? "Big wheel" : d.label; var label = Label(unlocked ? $"{tileName}\n<size=17>Lv {state.levels[i]} · {d.stock - count} left</size>" : $"{tileName}\n<size=17>{d.unlockCost} coins</size>", 58, 4, 135, 62, 20, Color.white); label.rectTransform.SetParent(b.transform, false); label.rectTransform.anchoredPosition = new Vector2(58, -4);
            }
            Button(delete ? "DELETE ON" : "DELETE", 30, 1780, 160, 43, () => { delete = !delete; moving = false; Refresh(); }, delete ? Cyan : Blue, 20, true).interactable = !running;
            Button(moving ? "MOVE ON" : "MOVE", 200, 1780, 150, 43, () => { moving = !moving; delete = false; moveX = -1; Refresh(); }, moving ? Cyan : Blue, 20, true).interactable = !running;
            int partCost = parts[(int)selected].upgradeCost * (1 << (state.levels[(int)selected] - 1));
            var partUpgrade = Button(state.levels[(int)selected] >= 5 ? "MAX LV" : $"LV + · {Money(partCost)}", 360, 1780, 180, 43, Upgrade, Blue, 20, true);
            Enable(partUpgrade, () => !running && state.partsUnlocked[(int)selected] && state.levels[(int)selected] < 5 && state.coins >= partCost);
            Button("BLUEPRINT", 550, 1780, 230, 43, () => { showResults = false; Current.design = Floor.blueprint.Copy(); message = "Starter blueprint loaded. Test it, then improve it."; Save(); Refresh(); Preview(); }, Blue, 20, true).interactable = !running;
            Button("RESET", 790, 1780, 260, 43, () => { showResults = false; Current.design = new VehicleDefinition(); message = "Empty workshop. Place connected parts or use BLUEPRINT."; Save(); Refresh(); Preview(); }, Blue, 20, true).interactable = !running;
            if (!showResults && !running && Current.lastSuccess != null) { var r = Current.lastSuccess; Box("Last run", 28, 900, 1024, 45, Blue); Label($"LAST DELIVERY · {r.ore} ore · {r.seconds:0.0}s · {Money(r.rate)}/min base", 46, 902, 986, 39, 25, Gold); }
            RunResultPanel();
        }
        void Bind(TMP_Text label, Func<string> value) { label.text = value(); liveUI.Add(() => { if (label != null) label.text = value(); }); }
        void Enable(Button button, Func<bool> condition) { button.interactable = condition(); liveUI.Add(() => { if (button != null) button.interactable = condition(); }); }
        void ModalBackdrop()
        {
            var shade = Flat("Modal input blocker", 0, 0, 1080, 1920, new Color(0, 0, 0, .70f), true); shade.GetComponent<Image>().raycastTarget = true;
        }
        void DrawMenu()
        {
            ModalBackdrop(); Box("Menu", 100, 490, 880, 820, Navy);
            Label("YOUR MINE", 140, 525, 800, 80, 43, Gold);
            Label("MINE: collect earnings and upgrade production.\nWORKSHOP: test and record a hauler.\nRESEARCH: improve ore values across the mine.\nPRESTIGE: rebuild with permanent bonuses.\n\nOffline earnings and manual storage cover up to 4 hours. Managers collect while you play.", 140, 620, 800, 330, 28, Color.white);
            Button("DEVELOPER TOOLS", 140, 990, 800, 88, () => { menuOpen = false; debug = true; Refresh(); }, Blue, 27);
            Button("BACK TO GAME", 140, 1120, 800, 100, () => { menuOpen = false; Refresh(); }, Green, 30);
        }
    }
}
