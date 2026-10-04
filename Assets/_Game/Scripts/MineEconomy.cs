using System;
namespace ContraptionMine
{
    public enum ProductionUpgrade { Crew, Loading }
    public readonly struct PurchaseQuote
    {
        public readonly int count; public readonly double cost;
        public PurchaseQuote(int quantity, double price) { count = quantity; cost = price; }
    }
    public static class MineEconomy
    {
        public const int MaxLevel = 100;
        public static double CrewMultiplier(int level) => Math.Pow(1.14, level);
        public static double LoadingMultiplier(int level) => Math.Pow(1.10, level);
        public static double RefineryMultiplier(int level) => 1 + .2 * level;
        public static double FloorRate(MineState state, int index)
        {
            var floor = state.floors[index]; if (floor.automated == null) return 0;
            int ore = Math.Clamp(floor.oreIndex, 0, 2);
            return floor.automated.rate * CrewMultiplier(floor.crewLevel) * LoadingMultiplier(floor.loadingLevel) * RefineryMultiplier(state.refineries[ore]) * (floor.manager ? 1.2 : 1) * state.PermanentMultiplier;
        }
        static readonly double[] CrewCosts = { 35, 80, 200, 550, 1100, 1750, 1900, 3500, 6300, 50000, 200000, 1000000 };
        public static double Cost(int floor, ProductionUpgrade upgrade, int level)
            => CrewCosts[Math.Min(floor, CrewCosts.Length - 1)] * (upgrade == ProductionUpgrade.Crew ? 1 : 1.4) * Math.Pow(upgrade == ProductionUpgrade.Crew ? 1.20 : 1.18, level);
        public static PurchaseQuote Quote(MineState state, int floor, ProductionUpgrade upgrade, int mode)
        {
            int level = upgrade == ProductionUpgrade.Crew ? state.floors[floor].crewLevel : state.floors[floor].loadingLevel;
            int wanted = mode == 0 ? MaxLevel - level : Math.Min(mode, MaxLevel - level);
            double total = 0; int count = 0;
            for (int i = 0; i < wanted; i++) { double next = Cost(floor, upgrade, level + i); if (mode == 0 && total + next > state.coins) break; total += next; count++; }
            return new PurchaseQuote(count, total);
        }
        public static bool Buy(MineState state, int floor, ProductionUpgrade upgrade, int mode)
        {
            if (!state.floors[floor].unlocked || state.floors[floor].automated == null) return false;
            var quote = Quote(state, floor, upgrade, mode); if (quote.count == 0 || quote.cost > state.coins) return false;
            state.coins -= quote.cost;
            if (upgrade == ProductionUpgrade.Crew) state.floors[floor].crewLevel += quote.count; else state.floors[floor].loadingLevel += quote.count;
            return true;
        }
        public static double ManagerCost(int floor) => 150 + 75 * (floor + 1) * Math.Pow(1.85, floor);
        public static bool Hire(MineState state, int floor)
        {
            var f = state.floors[floor]; double price = ManagerCost(floor);
            if (!f.unlocked || f.automated == null || f.manager || state.coins < price) return false;
            state.coins -= price; state.CollectFloor(floor); f.manager = true; return true;
        }
        public static double RefineryCost(int ore, int level) => 600 * (ore + 1) * Math.Pow(1.32, level);
        public static bool BuyRefinery(MineState state, int ore)
        {
            int level = state.refineries[ore]; double cost = RefineryCost(ore, level);
            if (level >= 50 || state.coins < cost) return false;
            state.coins -= cost; state.refineries[ore]++; return true;
        }
        public static int ResearchCost(MineState state) => 3 * (state.permanentResearch + 1);
        public static bool BuyResearch(MineState state)
        {
            int cost = ResearchCost(state); if (state.permanentResearch >= 10 || state.certificates < cost) return false;
            state.certificates -= cost; state.permanentResearch++; return true;
        }
        public static int PrestigeReward(MineState state) => (int)Math.Min(1000000, Math.Floor(Math.Sqrt(Math.Max(0, state.cycleEarnings) / 1000000)));
        public static bool CanPrestige(MineState state) => state.floors.Length >= 9 && state.floors[8].automated != null && PrestigeReward(state) > 0;
        public static bool Prestige(MineState state)
        {
            if (!CanPrestige(state)) return false;
            int reward = PrestigeReward(state); state.certificates += reward; state.lifetimeCertificates += reward; state.prestiges++;
            state.coins = 100; state.cycleEarnings = 0; state.uncollectedOffline = 0; state.selectedFloor = 0; state.refineries = new int[3];
            for (int i = 0; i < state.floors.Length; i++)
            {
                var old = state.floors[i];
                state.floors[i] = new FloorState { unlocked = i == 0, design = old.design, lastSuccess = old.lastSuccess ?? old.automated, best = old.best, oreIndex = old.oreIndex };
            }
            return true;
        }
    }
}
