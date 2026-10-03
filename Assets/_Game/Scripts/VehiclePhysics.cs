using System.Collections.Generic;
using UnityEngine;
namespace ContraptionMine
{
    public sealed class VehiclePhysics : MonoBehaviour
    {
        public Rigidbody2D body; public int cargo; public float cargoMass, enginePower, totalMass; public bool broken;
        static readonly Dictionary<float, PhysicsMaterial2D> GripMaterials = new();
        readonly List<WheelJoint2D> wheels = new(); readonly List<Rigidbody2D> wheelBodies = new(); float lift; bool spring;
        public static Sprite Square, Circle;
        public static void InitSprites()
        {
            if (Square != null) return;
            var t = new Texture2D(32, 32); var c = new Color[1024]; for (int i = 0; i < c.Length; i++) c[i] = Color.white; t.SetPixels(c); t.Apply(); Square = Sprite.Create(t, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
            var ct = new Texture2D(64, 64); var cc = new Color[4096]; for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) cc[y * 64 + x] = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * 32) <= 32 ? Color.white : Color.clear; ct.SetPixels(cc); ct.Apply(); Circle = Sprite.Create(ct, new Rect(0, 0, 64, 64), Vector2.one * .5f, 64);
        }
        public static GameObject Shape(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool circle = false, int order = 2)
        {
            InitSprites(); var o = new GameObject(name); o.transform.SetParent(parent, false); o.transform.localPosition = position; o.transform.localScale = size; var sr = o.AddComponent<SpriteRenderer>(); sr.sprite = circle ? Circle : Square; sr.color = color; sr.sortingOrder = order; return o;
        }
        public static VehiclePhysics Spawn(VehicleDefinition design, PartData[] data, int[] levels, Vector2 spawn)
        {
            InitSprites(); var root = new GameObject("Hauler"); root.transform.position = spawn; var v = root.AddComponent<VehiclePhysics>(); v.body = root.AddComponent<Rigidbody2D>(); v.body.interpolation = RigidbodyInterpolation2D.Interpolate; v.body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            float mass = 0; foreach (var p in design.parts)
            {
                var d = data[(int)p.type]; int lv = levels[(int)p.type]; if (d.IsWheel) continue; mass += d.mass * (1 - .04f * (lv - 1)); if (d.IsEngine) v.enginePower = d.Power(lv); if (p.type == PartType.Cargo) { int cap = d.Capacity(lv); v.cargo += cap; v.cargoMass += cap * .32f; }
                if (p.type == PartType.Balloon) v.lift += d.lift * (1 + .12f * (lv - 1)); if (p.type == PartType.Spring) v.spring = true;
                Vector2 pos = new(p.x - 3.5f, p.y - 1); var o = Shape(d.label, root.transform, pos, Vector2.one * .88f, d.color, p.type == PartType.Balloon); o.transform.localRotation = Quaternion.Euler(0, 0, p.rotation); if (p.type != PartType.Balloon) { var col = o.AddComponent<BoxCollider2D>(); col.size = Vector2.one; }
                if (p.type == PartType.Frame) { Shape("Frame inset", o.transform, Vector2.zero, Vector2.one * .65f, new Color(.12f, .19f, .26f), false, 3); }
                if (p.type == PartType.Cargo) { for (int i = 0; i < 3; i++) Shape("Ore", o.transform, new Vector2((i - 1) * .25f, .22f), new Vector2(.24f, .4f), Color.cyan, false, 3); }
            }
            v.body.mass = mass + v.cargoMass; v.totalMass = v.body.mass;
            int wheelCount = design.parts.FindAll(p => data[(int)p.type].IsWheel).Count;
            foreach (var p in design.parts)
            {
                var d = data[(int)p.type]; if (!d.IsWheel) continue; var w = Shape(d.label, root.transform, new Vector2(p.x - 3.5f, p.y - 1), Vector2.one * d.radius * 2, new Color(.16f, .20f, .27f), true, 4); w.transform.SetParent(null, true); var wb = w.AddComponent<Rigidbody2D>(); wb.mass = d.mass; wb.interpolation = RigidbodyInterpolation2D.Interpolate; wb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; var col = w.AddComponent<CircleCollider2D>(); col.radius = .5f; float grip = d.grip * (1 + .1f * (levels[(int)p.type] - 1)); if (!GripMaterials.TryGetValue(grip, out var mat)) { mat = new PhysicsMaterial2D("Wheel grip") { friction = grip, bounciness = 0 }; GripMaterials.Add(grip, mat); }
                col.sharedMaterial = mat;
                Shape("Hub", w.transform, Vector2.zero, Vector2.one * .42f, new Color(1, .72f, .15f), true, 5); var joint = w.AddComponent<WheelJoint2D>(); joint.connectedBody = v.body; joint.autoConfigureConnectedAnchor = false; joint.anchor = Vector2.zero; joint.connectedAnchor = new Vector2(p.x - 3.5f, p.y - 1); joint.suspension = new JointSuspension2D { angle = 90, frequency = v.spring ? 5 : 9, dampingRatio = .8f }; joint.useMotor = true; joint.motor = new JointMotor2D { motorSpeed = 1100, maxMotorTorque = v.enginePower * 10 / Mathf.Max(1, wheelCount) }; joint.breakForce = 12000; v.wheels.Add(joint); v.wheelBodies.Add(wb); v.totalMass += d.mass;
            }
            return v;
        }
        void FixedUpdate()
        {
            if (body == null) return; body.AddForce(Vector2.up * lift); // Lift scales with placement through the body centre of mass.
            if (lift > 0) body.AddTorque(-body.rotation * .12f);
            foreach (var j in wheels) if (j == null) broken = true;
        }
        public void Dispose() { foreach (var w in wheelBodies) if (w != null) Destroy(w.gameObject); Destroy(gameObject); }
    }
}
