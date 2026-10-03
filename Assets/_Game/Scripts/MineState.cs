using System;
using System.Collections.Generic;
using UnityEngine;
namespace ContraptionMine
{
    [Serializable] public sealed class PlacedPart { public PartType type; public int x, y; public int rotation; public PlacedPart(PartType t, int px, int py) { type = t; x = px; y = py; } }
    [Serializable] public sealed class VehicleDefinition { public List<PlacedPart> parts = new(); public VehicleDefinition Copy() => JsonUtility.FromJson<VehicleDefinition>(JsonUtility.ToJson(this)); }
    [Serializable] public sealed class RunRecord { public VehicleDefinition vehicle; public int ore; public float seconds, rate; }
    [Serializable] public sealed class RunAttempt { public bool success; public string reason, hint; public int ore; public float seconds, distance, rate, airSeconds, impact; }
    [Serializable] public sealed class FloorState { public bool unlocked; public VehicleDefinition design; public RunRecord lastSuccess, automated; public float best; public RunAttempt lastAttempt; }
    [Serializable]
    public sealed class MineState
    {
        public double coins = 100; public double uncollectedOffline; public long timestamp; public int selectedFloor; public int[] levels = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }; public bool[] partsUnlocked = { true, true, true, true, true, true, false, false, false, false }; public FloorState[] floors = { new(), new(), new(), new(), new(), new(), new(), new(), new() };
        public void EnsureFloorCount(int count)
        {
            var expanded = new FloorState[Math.Max(count, floors?.Length ?? 0)];
            for (int i = 0; i < expanded.Length; i++) expanded[i] = floors != null && i < floors.Length && floors[i] != null ? floors[i] : new FloorState();
            floors = expanded;
        }
        public void EnsurePartCount(int count)
        {
            int oldLevels = levels?.Length ?? 0, oldUnlocks = partsUnlocked?.Length ?? 0;
            Array.Resize(ref levels, count); Array.Resize(ref partsUnlocked, count);
            for (int i = 0; i < count; i++) { if (i >= oldLevels || levels[i] < 1) levels[i] = 1; if (i >= oldUnlocks) partsUnlocked[i] = i < 6; }
        }
        public double Income { get { double n = 0; foreach (var f in floors) if (f.automated != null) n += f.automated.rate; return n; } }
    }
    public static class SaveManager
    {
        static string Key
        {
            get
            {
#if UNITY_EDITOR
 if(UnityEditor.SessionState.GetBool("ContraptionMine.Smoke",false))return "ContraptionMine.Validation.v1";
#endif
                return "ContraptionMine.Save.v1";
            }
        }
        public static MineState Load() { try { if (PlayerPrefs.HasKey(Key)) { var s = JsonUtility.FromJson<MineState>(PlayerPrefs.GetString(Key)); if (s != null && s.floors != null && s.floors.Length > 0 && s.levels != null && s.partsUnlocked != null) { s.EnsurePartCount(Enum.GetValues(typeof(PartType)).Length); return s; } } } catch (Exception e) { Debug.LogWarning("Save could not be loaded: " + e.Message); } return new MineState(); }
        public static void Save(MineState state) { state.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); PlayerPrefs.SetString(Key, JsonUtility.ToJson(state)); PlayerPrefs.Save(); }
        public static void Reset() => PlayerPrefs.DeleteKey(Key);
        public static double Offline(MineState s, long now) => s.timestamp <= 0 ? 0 : s.Income / 60 * Math.Clamp(now - s.timestamp, 0, 8 * 3600);
    }
}
