using System;
using System.Collections.Generic;
using UnityEngine;
namespace ContraptionMine
{
    [Serializable] public sealed class PlacedPart { public PartType type; public int x, y; public int rotation; public PlacedPart(PartType t, int px, int py) { type = t; x = px; y = py; } }
    [Serializable] public sealed class VehicleDefinition { public List<PlacedPart> parts = new(); public VehicleDefinition Copy() => JsonUtility.FromJson<VehicleDefinition>(JsonUtility.ToJson(this)); }
    [Serializable] public sealed class RunRecord { public VehicleDefinition vehicle; public int ore; public float seconds, rate; }
    [Serializable] public sealed class RunAttempt { public bool success; public string reason, hint; public int ore; public float seconds, distance, rate, airSeconds, impact; }
    [Serializable] public sealed class FloorState { public bool unlocked; public VehicleDefinition design; public RunRecord lastSuccess, automated; public float best; public RunAttempt lastAttempt; public int crewLevel, loadingLevel, oreIndex; public bool manager; public double stored; }
    [Serializable]
    public sealed class MineState
    {
        public int economyVersion, certificates, lifetimeCertificates, permanentResearch, prestiges; public int[] refineries = new int[3]; public double cycleEarnings;
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
        public void EnsureEconomy(FloorData[] data)
        {
            if (refineries == null) refineries = new int[3]; else Array.Resize(ref refineries, 3);
            for (int i = 0; i < data.Length; i++) floors[i].oreIndex = (int)data[i].oreType;
            if (economyVersion == 0)
            {
                // Preserve the passive payouts of every existing recorded floor.
                foreach (var floor in floors) if (floor.automated != null) floor.manager = true;
                cycleEarnings = Math.Max(cycleEarnings, coins); economyVersion = 1;
            }
        }
        public double PermanentMultiplier => (1 + .05 * lifetimeCertificates) * (1 + .25 * permanentResearch);
        public double Income { get { double sum = 0; for (int i = 0; i < floors.Length; i++) sum += MineEconomy.FloorRate(this, i); return sum; } }
        public double CashIncome { get { double sum = 0; for (int i = 0; i < floors.Length; i++) if (floors[i].manager) sum += MineEconomy.FloorRate(this, i); return sum; } }
        public double Stored { get { double sum = 0; foreach (var floor in floors) sum += floor.stored; return sum; } }
        public void Earn(double amount) { coins += amount; cycleEarnings += amount; }
        public void Tick(double seconds)
        {
            Earn(CashIncome * seconds / 60); AccumulateManual(seconds);
        }
        public void AccumulateManual(double seconds)
        {
            for (int i = 0; i < floors.Length; i++)
            {
                var floor = floors[i]; if (floor.manager || floor.automated == null) continue;
                double rate = MineEconomy.FloorRate(this, i), limit = rate * SaveManager.OfflineHours * 60;
                floor.stored = Math.Max(floor.stored, Math.Min(limit, floor.stored + rate * seconds / 60));
            }
        }
        public double CollectFloor(int index) { double amount = floors[index].stored; floors[index].stored = 0; Earn(amount); return amount; }
        public double CollectAll() { double amount = 0; for (int i = 0; i < floors.Length; i++) amount += CollectFloor(i); return amount; }
        public double ApplyOffline(long now)
        {
            double seconds = SaveManager.AbsenceSeconds(this, now); AccumulateManual(seconds); return CashIncome * seconds / 60;
        }

    }
    public static class SaveManager
    {
        public const int OfflineHours = 4;
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
        public static long AbsenceSeconds(MineState s, long now) => s.timestamp <= 0 ? 0 : Math.Clamp(now - s.timestamp, 0, OfflineHours * 3600);
        public static double Offline(MineState s, long now) => s.CashIncome / 60 * AbsenceSeconds(s, now);
    }
}
