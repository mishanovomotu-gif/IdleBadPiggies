using System;
using System.Linq;
using ContraptionMine;
using UnityEngine;
namespace ContraptionMineEditor
{
    public static class ContraptionChecks
    {
        public static void Tradeoffs(PartData[] parts, FloorData[] floors)
        {
            int[] levels = Enumerable.Repeat(1, parts.Length).ToArray();
            // An off-centre thrust source must pitch the body in opposite directions above/below it.
            var high = new VehicleDefinition(); high.parts.Add(new(PartType.Frame, 3, 2)); high.parts.Add(new(PartType.Propeller, 3, 3));
            var low = new VehicleDefinition(); low.parts.Add(new(PartType.Frame, 3, 2)); low.parts.Add(new(PartType.Propeller, 3, 1));
            var a = VehiclePhysics.Spawn(high, parts, levels, new Vector2(-30, 20)); var b = VehiclePhysics.Spawn(low, parts, levels, new Vector2(-20, 20));
            a.StepPhysics(.02f); b.StepPhysics(.02f); Physics2D.SyncTransforms(); Physics2D.Simulate(.02f);
            if (a.body.angularVelocity >= 0 || b.body.angularVelocity <= 0 || a.body.linearVelocity.x <= 0 || b.body.linearVelocity.x <= 0) throw new Exception("Propeller placement did not change pitch/thrust");
            UnityEngine.Object.DestroyImmediate(a.gameObject); UnityEngine.Object.DestroyImmediate(b.gameObject);
            high.parts[1] = new(PartType.Ballast, 3, 3); low.parts[1] = new(PartType.Ballast, 3, 1);
            a = VehiclePhysics.Spawn(high, parts, levels, new Vector2(-30, 20)); b = VehiclePhysics.Spawn(low, parts, levels, new Vector2(-20, 20));
            if (b.body.centerOfMass.y >= a.body.centerOfMass.y) throw new Exception("Low ballast did not lower centre of mass");
            UnityEngine.Object.DestroyImmediate(a.gameObject); UnityEngine.Object.DestroyImmediate(b.gameObject);
            var bin = new VehicleDefinition(); bin.parts.Add(new(PartType.Cargo, 3, 2));
            a = VehiclePhysics.Spawn(bin, parts, levels, new Vector2(-30, 20), floors[0]); b = VehiclePhysics.Spawn(bin, parts, levels, new Vector2(-20, 20), floors[7]);
            if (b.cargo != a.cargo || b.cargoMass <= a.cargoMass || b.body.mass <= a.body.mass) throw new Exception("Dense ore did not increase payload mass");
            UnityEngine.Object.DestroyImmediate(a.gameObject); UnityEngine.Object.DestroyImmediate(b.gameObject);
            foreach (var floor in floors) if (floor.zones.Any(z => z.kind == ChallengeKind.Ice) && !floor.trackPrefab.GetComponents<EdgeCollider2D>().Any(e => e.sharedMaterial != null && e.sharedMaterial.friction < .05f)) throw new Exception("Authored ice surface lost its material");
            Debug.Log("CONTRAPTION TRADEOFFS PASSED: thrust placement, ballast balance, dense ore, persisted ice");
        }
    }
}
