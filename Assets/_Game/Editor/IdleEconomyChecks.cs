using System;
using ContraptionMine;
using UnityEngine;
namespace ContraptionMineEditor
{
    public static class IdleEconomyChecks
    {
        static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
        static MineState Fresh(FloorData[] data)
        {
            var s = new MineState(); s.EnsureFloorCount(data.Length); s.EnsureEconomy(data); s.coins = 10000000;
            for (int i = 0; i < data.Length; i++) s.floors[i].design = data[i].blueprint.Copy();
            s.floors[0].unlocked = true; return s;
        }
        public static void Validate(FloorData[] data)
        {
            var s = Fresh(data); var baseRun = new RunRecord { rate = 50, vehicle = data[0].blueprint.Copy(), ore = 5, seconds = 6 };
            s.floors[0].automated = s.floors[0].lastSuccess = baseRun;
            double coins = s.coins, earned = s.cycleEarnings; s.Tick(60);
            Check(s.coins == coins && s.floors[0].stored == 50, "Unmanaged production was not stored");
            Check(s.CollectAll() == 50 && s.coins == coins + 50 && s.cycleEarnings == earned + 50, "Manual collection counted incorrectly");
            Check(MineEconomy.Hire(s, 0), "Manager hiring failed"); coins = s.coins; s.Tick(60);
            Check(Math.Abs(s.coins - coins - 60) < .001 && s.floors[0].stored == 0, "Manager collection or 20% output bonus failed");
            Check(MineEconomy.Buy(s, 0, ProductionUpgrade.Crew, 10) && s.floors[0].crewLevel == 10 && ReferenceEquals(baseRun, s.floors[0].automated) && baseRun.rate == 50, "Idle upgrades altered a recorded run");
            var bulk = Fresh(data); var singles = Fresh(data); bulk.floors[0].automated = singles.floors[0].automated = baseRun;
            Check(MineEconomy.Buy(bulk, 0, ProductionUpgrade.Loading, 10), "Buy 10 failed");
            for (int i = 0; i < 10; i++) Check(MineEconomy.Buy(singles, 0, ProductionUpgrade.Loading, 1), "Buy 1 failed");
            Check(Math.Abs(bulk.coins - singles.coins) < .001, "Bulk pricing differs from individual purchases");
            bulk.coins = MineEconomy.Cost(0, ProductionUpgrade.Crew, 0) + MineEconomy.Cost(0, ProductionUpgrade.Crew, 1);
            Check(MineEconomy.Buy(bulk, 0, ProductionUpgrade.Crew, 0) && bulk.floors[0].crewLevel == 2 && bulk.coins >= 0, "Buy Max overspent or chose the wrong quantity");
            bulk.floors[0].crewLevel = MineEconomy.MaxLevel; Check(!MineEconomy.Buy(bulk, 0, ProductionUpgrade.Crew, 10), "Upgrade cap was exceeded");
            s = Fresh(data); s.floors[0].automated = baseRun; s.floors[1].automated = new RunRecord { rate = 70 }; s.floors[1].oreIndex = (int)OreType.Copper;
            Check(MineEconomy.BuyRefinery(s, (int)OreType.Copper) && Math.Abs(MineEconomy.FloorRate(s, 1) - 84) < .001 && MineEconomy.FloorRate(s, 0) == 50, "Refinery affected the wrong ore type");
            s = Fresh(data); s.floors[0].automated = baseRun; s.floors[0].manager = true; s.floors[1].automated = new RunRecord { rate = 70 }; s.timestamp = 100;
            double offline = s.ApplyOffline(40100);
            Check(Math.Abs(offline - 14400) < .001 && Math.Abs(s.floors[1].stored - 16800) < .001, "Four-hour offline cap or manual storage failed");
            s.Tick(40000); Check(Math.Abs(s.floors[1].stored - 16800) < .001, "Manual storage exceeded four hours");
            Check(SaveManager.AbsenceSeconds(s, 0) == 0, "Clock rollback generated offline earnings");
            var legacy = new MineState { economyVersion = 0 }; legacy.EnsureFloorCount(data.Length); legacy.floors[0].automated = baseRun; legacy.EnsureEconomy(data);
            Check(legacy.floors[0].manager && legacy.floors[0].automated.rate == 50, "Legacy automated floor lost passive collection");
            s = Fresh(data); s.floors[0].automated = s.floors[0].lastSuccess = baseRun; s.floors[8].automated = new RunRecord { rate = 4000, vehicle = data[8].blueprint.Copy() }; s.floors[0].crewLevel = 20; s.floors[0].loadingLevel = 15; s.floors[0].manager = true; s.refineries[0] = 2; s.cycleEarnings = 4000000; s.levels[3] = 4; s.partsUnlocked[8] = true; s.certificates = 3;
            Check(MineEconomy.BuyResearch(s) && s.permanentResearch == 1 && s.certificates == 0, "Certificate research purchase failed");
            string design = JsonUtility.ToJson(s.floors[0].design); Check(MineEconomy.PrestigeReward(s) == 2 && MineEconomy.Prestige(s), "Prestige eligibility/reward failed");
            Check(s.coins == 100 && s.floors[0].unlocked && !s.floors[1].unlocked && s.floors[0].automated == null && s.floors[0].crewLevel == 0 && s.floors[0].loadingLevel == 0 && !s.floors[0].manager && s.refineries[0] == 0, "Prestige failed to reset ordinary progression");
            Check(s.levels[3] == 4 && s.partsUnlocked[8] && s.permanentResearch == 1 && s.certificates == 2 && s.lifetimeCertificates == 2 && JsonUtility.ToJson(s.floors[0].design) == design && ReferenceEquals(s.floors[0].lastSuccess, baseRun), "Prestige lost permanent/creative progress");
            Check(!MineEconomy.Prestige(s), "Prestige could be repeated without rebuilding");
            var copy = JsonUtility.FromJson<MineState>(JsonUtility.ToJson(s)); Check(copy.permanentResearch == 1 && copy.certificates == 2 && copy.floors[0].lastSuccess.rate == 50, "Economy save roundtrip failed");
            Check(data.Length == 12 && data[9].outputGate == 30000 && data[10].outputGate == 200000 && data[11].outputGate == 1000000, "Deep floor output gates missing");
            Debug.Log("IDLE ECONOMY PASSED: collection, managers, bulk/max upgrades, refineries, offline cap, migration, prestige, and saves");
        }
    }
}
