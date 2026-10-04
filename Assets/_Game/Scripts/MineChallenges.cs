using System;
using UnityEngine;
namespace ContraptionMine
{
    public enum OreType { Crystal, Copper, Gold }
    public enum ChallengeKind { Ice, Ceiling, Wind }
    [Serializable] public sealed class ChallengeZone
    {
        public ChallengeKind kind; public float from, to, strength;
        public ChallengeZone(ChallengeKind k, float start, float end, float value) { kind = k; from = start; to = end; strength = value; }
        public string Label => kind switch { ChallengeKind.Ice => "ICE: KEEP MOMENTUM", ChallengeKind.Ceiling => "LOW ROOF: BUILD LOW", _ => "WIND: WATCH BALANCE" };
    }
    public static class MineChallenges
    {
        static PhysicsMaterial2D ice;
        // Used by authored prefabs, runtime fallback, and simulation tests.
        public static void BuildColliders(GameObject route, FloorData floor)
        {
            for (int i = 0; i < floor.terrain.Length - 1; i++)
            {
                if (floor.HasGap(i)) continue;
                Vector2 a = floor.terrain[i], b = floor.terrain[i + 1];
                // Split at zone boundaries so ice starts exactly where the sign says it does.
                var cuts = new System.Collections.Generic.List<float> { 0, 1 };
                foreach (var zone in floor.zones) if (zone.kind == ChallengeKind.Ice && b.x > a.x)
                    foreach (float x in new[] { zone.from, zone.to }) if (x > a.x && x < b.x) cuts.Add((x - a.x) / (b.x - a.x));
                cuts.Sort();
                for (int c = 0; c < cuts.Count - 1; c++)
                {
                    var p = Vector2.Lerp(a, b, cuts[c]); var q = Vector2.Lerp(a, b, cuts[c + 1]);
                    var edge = route.AddComponent<EdgeCollider2D>(); edge.points = new[] { p, q }; edge.edgeRadius = .07f;
                    foreach (var zone in floor.zones) if (zone.kind == ChallengeKind.Ice && (p.x + q.x) * .5f >= zone.from && (p.x + q.x) * .5f <= zone.to)
                    { if (ice == null) ice = Resources.Load<PhysicsMaterial2D>("Surfaces/Ice"); ice ??= new PhysicsMaterial2D("Icy ore trail") { friction = .035f }; edge.sharedMaterial = ice; }
                }
            }
            foreach (var zone in floor.zones) if (zone.kind == ChallengeKind.Ceiling)
            {
                var roof = new GameObject("Low ceiling collider"); roof.transform.SetParent(route.transform, false);
                roof.transform.localPosition = new Vector2((zone.from + zone.to) * .5f, zone.strength);
                roof.AddComponent<BoxCollider2D>().size = new Vector2(zone.to - zone.from, .45f);
            }
        }
        public static float GroundHeight(FloorData floor, float x)
        {
            for (int i = 0; i < floor.terrain.Length - 1; i++) if (x >= floor.terrain[i].x && x <= floor.terrain[i + 1].x)
                return Mathf.Lerp(floor.terrain[i].y, floor.terrain[i + 1].y, Mathf.InverseLerp(floor.terrain[i].x, floor.terrain[i + 1].x, x));
            return 0;
        }
        public static string Brief(FloorData floor) => floor.number switch
        {
            1 => "Rolling hills + a downhill landing. Keep the load balanced.",
            2 => "Bumps + a slick valley. Suspension and momentum help.",
            3 => "Ramp gap + landing ridge. Lift helps; top-heavy loads tip.",
            4 => "Ravine + a low roof. Lift must fit under the timber.",
            5 => "Heavy copper + two climbs. Power needs traction.",
            6 => "Two gaps + a crosswind. Spread lift across the chassis.",
            7 => "Ice run + a low tunnel + exit bumps. Keep a low profile.",
            8 => "Dense gold + steep climbs + rough descent. Balance the load.",
            10 => "Long icy valley + low timber + exit bumps. Keep output growing.",
            11 => "Copper payload + two climbs + a rough descent. Keep the wheels planted.",
            _ => "Two jumps + a wind shaft + landing bumps. Try a low propeller."
        };
    }
}
