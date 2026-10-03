using System.Collections.Generic;
using UnityEngine;
namespace ContraptionMine
{
    public sealed class VehiclePhysics : MonoBehaviour
    {
        public Rigidbody2D body; public int cargo; public float cargoMass, enginePower, totalMass; public bool broken; public float peakImpact, airSeconds; public int hardLandings; FloorData floor;
        readonly List<(Vector2 position, float force)> balloons = new(), fans = new();
        readonly List<Transform> rotors = new();
        static readonly Dictionary<float, PhysicsMaterial2D> GripMaterials = new();
        readonly List<AxleVisual> axles = new();
        sealed class AxleVisual { public Transform beam, wheel; public Vector2 mount; }
        readonly List<WheelJoint2D> wheels = new(); readonly List<Rigidbody2D> wheelBodies = new(); float lift; bool spring;
        public static Sprite Square, Circle; static Material spriteMaterial;
        public static void InitSprites()
        {
            if (Square != null) return;
            var t = new Texture2D(32, 32); var c = new Color[1024]; for (int i = 0; i < c.Length; i++) c[i] = Color.white; t.SetPixels(c); t.Apply(); Square = Sprite.Create(t, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
            var ct = new Texture2D(64, 64); var cc = new Color[4096]; for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) cc[y * 64 + x] = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * 32) <= 32 ? Color.white : Color.clear; ct.SetPixels(cc); ct.Apply(); Circle = Sprite.Create(ct, new Rect(0, 0, 64, 64), Vector2.one * .5f, 64);
        }
        public static GameObject Shape(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool circle = false, int order = 2)
        {
            InitSprites(); var o = new GameObject(name); o.transform.SetParent(parent, false); o.transform.localPosition = position; o.transform.localScale = size; var sr = o.AddComponent<SpriteRenderer>(); if (spriteMaterial == null) spriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")); sr.sharedMaterial = spriteMaterial; sr.sprite = circle ? Circle : Square; sr.color = color; sr.sortingOrder = order; return o;
        }
        public static VehiclePhysics Spawn(VehicleDefinition design, PartData[] data, int[] levels, Vector2 spawn, FloorData course = null)
        {
            InitSprites(); var root = new GameObject("Hauler"); root.transform.position = spawn; var v = root.AddComponent<VehiclePhysics>(); v.body = root.AddComponent<Rigidbody2D>(); v.body.interpolation = RigidbodyInterpolation2D.Interpolate; v.body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; v.body.angularDamping = .18f; v.floor = course;
            float mass = 0, inertia = 0; Vector2 weighted = Vector2.zero; foreach (var p in design.parts)
            {
                var d = data[(int)p.type]; int lv = levels[(int)p.type]; if (d.IsWheel) continue; float partMass = d.mass * (p.type == PartType.Ballast ? 1 + .12f * (lv - 1) : 1 - .04f * (lv - 1)); if (d.IsEngine) v.enginePower = d.Power(lv); if (p.type == PartType.Cargo) { int cap = d.Capacity(lv); v.cargo += cap; v.cargoMass += cap * (course != null ? course.OreMass : .32f); partMass += cap * (course != null ? course.OreMass : .32f); }
                if (p.type == PartType.Balloon) v.lift += d.lift * (1 + .12f * (lv - 1)); if (p.type == PartType.Spring) v.spring = true;
                Vector2 pos = new(p.x - 3.5f, p.y - 1); mass += partMass; weighted += pos * partMass; inertia += partMass * (pos.sqrMagnitude + .16f);
                if (p.type == PartType.Balloon) v.balloons.Add((pos, d.lift * (1 + .12f * (lv - 1))));
                if (p.type == PartType.Propeller) v.fans.Add((pos, d.Power(lv)));
                var o = Shape(d.label, root.transform, pos, Vector2.one * .88f, d.color, p.type == PartType.Balloon); o.GetComponent<SpriteRenderer>().sprite = MineArt.Icon(p.type, course != null ? course.oreType : OreType.Crystal); o.GetComponent<SpriteRenderer>().color = Color.white; o.transform.localRotation = Quaternion.Euler(0, 0, p.rotation); if (p.type != PartType.Balloon) { var col = o.AddComponent<BoxCollider2D>(); col.size = Vector2.one; }
                if (p.type == PartType.Propeller) { var rotor = Shape("Propeller hub", o.transform, new Vector2(0, .05f), Vector2.one, Color.clear, true, 5); Shape("Rotor blade", rotor.transform, Vector2.zero, new Vector2(.78f, .09f), new Color(.79f, .88f, .96f), false, 5); Shape("Rotor blade", rotor.transform, Vector2.zero, new Vector2(.09f, .78f), new Color(.79f, .88f, .96f), false, 5); Shape("Rotor bolt", rotor.transform, Vector2.zero, Vector2.one * .14f, new Color(1, .73f, .2f), true, 6); v.rotors.Add(rotor.transform); }
                if (d.IsEngine) { var pig = Shape("Piggy driver", o.transform, new Vector2(-.18f, .77f), new Vector2(.75f, .75f), Color.white, false, 5); pig.GetComponent<SpriteRenderer>().sprite = MineArt.Pig; for (int i = 0; i < 3; i++) Shape("Steam", o.transform, new Vector2(.26f + i * .07f, .85f + i * .2f), Vector2.one * (.16f + i * .035f), new Color(.86f, .92f, 1, .7f - i * .14f), true, 3); }
            }
            // The rigid body already joins all structural parts; show those connections too.
            foreach (var a in design.parts) foreach (var b in design.parts)
                {
                    if (data[(int)a.type].IsWheel || data[(int)b.type].IsWheel) continue;
                    if (a.x > b.x || (a.x == b.x && a.y >= b.y)) continue;
                    if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) continue;
                    var start = new Vector2(a.x - 3.5f, a.y - 1); var end = new Vector2(b.x - 3.5f, b.y - 1);
                    bool rope = a.type == PartType.Balloon || b.type == PartType.Balloon;
                    var beam = Shape(rope ? "Balloon rope" : "Chassis connector", root.transform, (start + end) * .5f, new Vector2(1, rope ? .045f : .18f), rope ? new Color(.8f, .59f, .3f) : new Color(.69f, .37f, .11f), false, 1);
                    beam.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
                    if (!rope) Shape("Connector bolt", root.transform, (start + end) * .5f, Vector2.one * .12f, new Color(.72f, .8f, .89f), true, 3);
                }
            v.body.mass = mass; v.body.centerOfMass = weighted / Mathf.Max(.1f, mass); v.body.inertia = Mathf.Max(.5f, inertia - mass * v.body.centerOfMass.sqrMagnitude); v.totalMass = v.body.mass;
            int wheelCount = design.parts.FindAll(p => data[(int)p.type].IsWheel).Count;
            foreach (var p in design.parts)
            {
                var d = data[(int)p.type]; if (!d.IsWheel) continue; var w = Shape(d.label, root.transform, new Vector2(p.x - 3.5f, p.y - 1), Vector2.one * d.radius * 2, new Color(.16f, .20f, .27f), true, 4); w.GetComponent<SpriteRenderer>().sprite = MineArt.Icon(p.type); w.GetComponent<SpriteRenderer>().color = Color.white; w.transform.SetParent(null, true); w.AddComponent<WheelImpactReporter>().owner = v; var wb = w.AddComponent<Rigidbody2D>(); wb.mass = d.mass; wb.interpolation = RigidbodyInterpolation2D.Interpolate; wb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; var col = w.AddComponent<CircleCollider2D>(); col.radius = .5f; float grip = d.grip * (1 + .1f * (levels[(int)p.type] - 1)); if (!GripMaterials.TryGetValue(grip, out var mat)) { mat = new PhysicsMaterial2D("Wheel grip") { friction = grip, bounciness = 0 }; GripMaterials.Add(grip, mat); }
                col.sharedMaterial = mat;
                var joint = w.AddComponent<WheelJoint2D>(); joint.connectedBody = v.body; joint.autoConfigureConnectedAnchor = false; joint.anchor = Vector2.zero; joint.connectedAnchor = new Vector2(p.x - 3.5f, p.y - 1); joint.suspension = new JointSuspension2D { angle = 90, frequency = v.spring ? 4.5f : 8, dampingRatio = v.spring ? .58f : .72f }; joint.useMotor = true; joint.motor = new JointMotor2D { motorSpeed = 1100, maxMotorTorque = v.enginePower * 10 / Mathf.Max(1, wheelCount) }; joint.breakForce = 12000; v.wheels.Add(joint); v.wheelBodies.Add(wb);
                var mountPart = design.parts.Find(q => !data[(int)q.type].IsWheel && Mathf.Abs(q.x - p.x) + Mathf.Abs(q.y - p.y) == 1);
                if (mountPart != null) { var mount = new Vector2(mountPart.x - 3.5f, mountPart.y - 1); var beam = Shape("Suspension arm", root.transform, Vector2.zero, Vector2.one, new Color(.47f, .57f, .69f), false, 3); v.axles.Add(new AxleVisual { beam = beam.transform, wheel = w.transform, mount = mount }); }
                v.UpdateAxles(); v.totalMass += d.mass;
            }
            return v;
        }
        void LateUpdate() { UpdateAxles(); foreach (var rotor in rotors) if (rotor != null) rotor.localRotation = Quaternion.Euler(0, 0, Time.time * (body.simulated ? 900 : 0)); }
        void UpdateAxles() { foreach (var axle in axles) { if (axle.wheel == null) continue; Vector2 end = transform.InverseTransformPoint(axle.wheel.position); Vector2 delta = end - axle.mount; axle.beam.localPosition = (axle.mount + end) * .5f; axle.beam.localScale = new Vector3(delta.magnitude, .12f, 1); axle.beam.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg); } }
        void FixedUpdate() => StepPhysics(Time.fixedDeltaTime);
        public void StepPhysics(float dt)
        {
            if (body == null || !body.simulated) return;
            // Lift and thrust act at the placed part, so placement changes pitch and balance.
            foreach (var balloon in balloons) body.AddForceAtPosition(Vector2.up * balloon.force, transform.TransformPoint(balloon.position));
            foreach (var fan in fans) body.AddForceAtPosition(transform.right * fan.force, transform.TransformPoint(fan.position));
            if (floor != null) foreach (var zone in floor.zones) if (zone.kind == ChallengeKind.Wind && body.position.x >= zone.from && body.position.x <= zone.to)
                body.AddForce(new Vector2(zone.strength, Mathf.Sin(body.position.x * .6f) * Mathf.Abs(zone.strength) * .25f) * body.mass);
            bool grounded = false; foreach (var wheel in wheelBodies) if (wheel != null && wheel.IsTouchingLayers()) grounded = true;
            if (!grounded) airSeconds += dt;
            foreach (var joint in wheels) if (joint == null) broken = true;
        }
        void OnCollisionEnter2D(Collision2D hit) => RecordImpact(hit);
        public void RecordImpact(Collision2D hit)
        {
            float impact = hit.contactCount > 0 ? Mathf.Abs(Vector2.Dot(hit.relativeVelocity, hit.GetContact(0).normal)) : hit.relativeVelocity.magnitude; peakImpact = Mathf.Max(peakImpact, impact); if (impact > 7) hardLandings++;
        }
        public void Dispose() { foreach (var w in wheelBodies) if (w != null) Destroy(w.gameObject); Destroy(gameObject); }
    }
    public sealed class WheelImpactReporter : MonoBehaviour
    {
        public VehiclePhysics owner;
        void OnCollisionEnter2D(Collision2D hit) { if (owner != null) owner.RecordImpact(hit); }
    }
}
