using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ContraptionMine
{
    // One pointer owns a drag. The design changes only after a valid drop.
    public sealed class WorkshopDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<PointerEventData> begin, move, end;
        public void OnBeginDrag(PointerEventData e) => begin?.Invoke(e);
        public void OnDrag(PointerEventData e) => move?.Invoke(e);
        public void OnEndDrag(PointerEventData e) => end?.Invoke(e);
    }
    public sealed partial class MineGame
    {
        readonly Stack<VehicleDefinition> undoBuilds = new(), redoBuilds = new();
        GameObject dragGhost; PartType dragType; int dragX, dragY, dragPointer; bool dragging;
        void RememberBuild() { undoBuilds.Push(Current.design.Copy()); redoBuilds.Clear(); }
        void RestoreBuild(bool redo)
        {
            var source = redo ? redoBuilds : undoBuilds; var destination = redo ? undoBuilds : redoBuilds;
            if (running || source.Count == 0) return;
            destination.Push(Current.design.Copy()); Current.design = source.Pop(); showResults = false; Save(); Refresh(); Preview();
        }
        void WireDrag(GameObject target, PartType type, int x, int y, bool occupied)
        {
            if (!occupied) return;
            var handle = target.AddComponent<WorkshopDrag>();
            handle.begin = e => {
                if (running || dragging || !state.partsUnlocked[(int)type]) return;
                dragging = true; dragPointer = e.pointerId; dragType = type; dragX = x; dragY = y; selected = type;
                dragGhost = Flat("Drag preview", 0, 0, 84, 84, Color.white); dragGhost.GetComponent<Image>().sprite = MineArt.Icon(type, Floor.oreType);
                DragPosition(e);
            };
            handle.move = DragPosition;
            handle.end = e => {
                if (!dragging || e.pointerId != dragPointer) return;
                var point = PointerPosition(e); bool grid = GridPosition(point, out int gx, out int gy);
                string error = grid ? DropError(gx, gy) : null;
                bool remove = !grid && dragX >= 0 && DeletePosition(point);
                if ((grid && error == null && (gx != dragX || gy != dragY)) || remove)
                {
                    RememberBuild(); var list = Current.design.parts;
                    var existing = list.Find(p => p.x == dragX && p.y == dragY);
                    if (existing != null) list.Remove(existing);
                    if (!remove) { var replaced = list.Find(p => p.x == gx && p.y == gy); if (replaced != null) list.Remove(replaced); list.Add(new PlacedPart(dragType, gx, gy)); }
                    showResults = false; message = remove ? "Part removed. Undo restores it." : (Attached(gx, gy) ? "Connected. Ready to test your changes." : "Part placed. Connect it edge to edge before testing."); Save();
                }
                else if (error != null) message = error;
                Destroy(dragGhost); dragGhost = null; dragging = false; Refresh(); Preview();
            };
        }
        Vector2 PointerPosition(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(designRoot, e.position, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
            float y = designRoot.sizeDelta.y * .5f - local.y;
            return new Vector2(local.x + 540, y >= 900 ? y - ExtraHeight : y);
        }
        static bool DeletePosition(Vector2 point) =>
            (point.y >= 1177 && point.y < 1617 && (point.x < 188 || point.x >= 892)) ||
            (point.y >= 1617 && point.y < 1775 && point.x >= 25 && point.x < 1055);
        static bool GridPosition(Vector2 point, out int x, out int y)
        {
            x = Mathf.FloorToInt((point.x - 188) / 88); y = 4 - Mathf.FloorToInt((point.y - 1177) / 88);
            return point.x >= 188 && point.x < 892 && point.y >= 1177 && point.y < 1617 && x >= 0 && x < 8 && y >= 0 && y < 5;
        }
        string DropError(int x, int y)
        {
            var list = Current.design.parts; var d = parts[(int)dragType];
            if (dragX < 0 && list.FindAll(p => p.type == dragType).Count >= d.stock) return "All of these parts are in use. Drag an existing part to move it.";
            if (d.IsEngine && list.Exists(p => parts[(int)p.type].IsEngine && !(p.x == dragX && p.y == dragY) && !(p.x == x && p.y == y))) return "One engine per hauler. Drag your engine to move it.";
            return null;
        }
        bool Attached(int x, int y) => Current.design.parts.Exists(p => !parts[(int)p.type].IsWheel && Math.Abs(p.x - x) + Math.Abs(p.y - y) == 1);
        void DragPosition(PointerEventData e)
        {
            if (!dragging || e.pointerId != dragPointer || dragGhost == null) return;
            var point = PointerPosition(e); bool grid = GridPosition(point, out int x, out int y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(designRoot, e.position, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var cursor);
            var r = dragGhost.GetComponent<RectTransform>(); r.anchoredPosition = grid ? new Vector2(188 + x * 88, -LayoutY(1177 + (4 - y) * 88)) : new Vector2(cursor.x + 540 - 42, cursor.y - designRoot.sizeDelta.y * .5f + 42);
            dragGhost.GetComponent<Image>().color = dragX >= 0 && !grid && DeletePosition(point) ? new Color(1, .35f, .35f, .85f) : !grid ? new Color(1, 1, 1, .8f) : DropError(x, y) != null ? new Color(1, .3f, .3f, .9f) : Attached(x, y) ? new Color(.65f, 1, .65f, .9f) : new Color(1, .8f, .35f, .9f);
        }
        HashSet<PlacedPart> ConnectedParts()
        {
            var list = Current.design.parts; var reached = new HashSet<PlacedPart>();
            var start = list.Find(p => parts[(int)p.type].IsEngine) ?? list.Find(p => !parts[(int)p.type].IsWheel);
            if (start == null) return reached; reached.Add(start);
            bool changed = true; while (changed) { changed = false; foreach (var p in list) if (!parts[(int)p.type].IsWheel && !reached.Contains(p)) foreach (var r in new List<PlacedPart>(reached)) if (Math.Abs(p.x - r.x) + Math.Abs(p.y - r.y) == 1) { reached.Add(p); changed = true; break; } }
            foreach (var p in list) if (parts[(int)p.type].IsWheel && list.Exists(q => reached.Contains(q) && !parts[(int)q.type].IsWheel && Math.Abs(p.x - q.x) + Math.Abs(p.y - q.y) == 1)) reached.Add(p);
            return reached;
        }
        void DrawRunView()
        {
            Flat("Run header backing", 0, 165, 1080, 200, Navy);
            Flat("Run controls backing", 0, 1518, 1080, 310, Navy);
            Box("Run heading", 28, 185, 1024, 95, Blue); hud = Label("", 48, 198, 990, 65, 34, Color.white);
            Box("Run objective", 28, 292, 1024, 67, Navy); Label($"{Floor.challenge} · DELIVER {Floor.requiredOre} {Floor.OreName}", 48, 298, 980, 52, 27, Gold);
            Box("Run dashboard", 28, 1525, 1024, 115, Blue);
            Bind(Label("", 48, 1538, 980, 88, 30, Color.white), () => $"CARGO {vehicle?.cargo ?? 0} / {Floor.requiredOre} · {Floor.OreName}\nSAVED {Money(Current.automated?.rate ?? 0)}/min · {(running ? "TESTING" : "TEST FINISHED")}");
            if (running) Button("STOP & EDIT", 28, 1660, 1024, 110, () => { EndRun(false, "Run stopped."); showResults = false; Refresh(); Preview(); }, Blue, 34);
            else { Button("EDIT BUILD", 28, 1650, 500, 76, () => { showResults = false; Refresh(); Preview(); }, Blue, 34); Button("TEST AGAIN", 548, 1650, 504, 76, StartRun, Green, 34); }
            RunResultPanel();
            if (!running && Current.lastAttempt?.success == true)
                Button(Current.automated == null ? "START PRODUCTION" : "SAVE THIS HAULER", 28, 1742, 1024, 76, () => { Automate(); showResults = false; SetScreen(MineScreen.Mine); }, Cyan, 25).interactable = Current.lastSuccess.ore >= Floor.requiredOre;
        }
    }
}
