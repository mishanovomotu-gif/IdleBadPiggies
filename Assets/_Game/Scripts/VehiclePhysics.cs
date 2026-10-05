using System.Collections.Generic;
using UnityEngine;
namespace ContraptionMine
{
    public sealed class VehiclePhysics : MonoBehaviour
    {
        public Rigidbody2D body; public int cargo; public float cargoMass, enginePower, totalMass; public bool broken; public float peakImpact, airSeconds; public int hardLandings; FloorData floor;
        readonly List<(Vector2 position, float force)> balloons = new(), fans = new();
        readonly List<Transform> rotors = new();
        readonly List<(Transform item, Vector3 origin, bool smoke)> feedback = new();
        AudioSource motorAudio; float impactPulse;
        static AudioClip motorClip, impactClip, deliveryClip;
        public static bool SoundEnabled = true;
        public void DeliverySound() { if (SoundEnabled && motorAudio != null) AudioSource.PlayClipAtPoint(deliveryClip, transform.position, .18f); }
        void AddFeedback(Transform item, bool smoke = false) => feedback.Add((item, item.localPosition, smoke));
        void UpdateFeedback()
        {
            if (body == null) return;
            bool active = body.simulated; float speed = body.linearVelocity.magnitude;
            impactPulse = Mathf.MoveTowards(impactPulse, 0, Time.deltaTime * 4);
            foreach (var f in feedback)
            {
                if (f.item == null) continue;
                float phase = Time.time * (f.smoke ? 2 : 12) + f.origin.y * 8;
                f.item.localPosition = f.origin + (active ? new Vector3(Mathf.Sin(phase) * .025f, f.smoke ? Mathf.Repeat(Time.time * .5f + f.origin.y, .5f) : Mathf.Sin(phase) * (.015f + impactPulse * .035f), 0) : Vector3.zero);
                if (!f.smoke) f.item.localRotation = Quaternion.Euler(0, 0, active ? Mathf.Clamp(-body.linearVelocity.y * 2, -12, 12) + Mathf.Sin(phase) * impactPulse * 4 : 0);
            }
            if (motorAudio != null) { motorAudio.volume = active && SoundEnabled ? .055f : 0; motorAudio.pitch = .7f + Mathf.Clamp(speed * .055f, 0, .8f); }
        }
        void InitializeAudio()
        {
            if (motorClip == null)
            {
                const int samples = 22050; var sound = new float[samples];
                for (int i = 0; i < samples; i++) { float t = i / 22050f; sound[i] = Mathf.Sin(t * Mathf.PI * 2 * 90) * .3f + Mathf.Sin(t * Mathf.PI * 2 * 180) * .12f; }
                motorClip = AudioClip.Create("Hauler motor", samples, 1, 22050, false); motorClip.SetData(sound, 0);
            }
            if (impactClip == null) { impactClip = Tone("Landing", 85, .12f); deliveryClip = Tone("Delivery", 660, .35f); }
            motorAudio = gameObject.AddComponent<AudioSource>(); motorAudio.clip = motorClip; motorAudio.loop = true; motorAudio.volume = 0; motorAudio.Play();
        }
        static readonly Dictionary<float, PhysicsMaterial2D> GripMaterials = new();
        readonly List<AxleVisual> axles = new();
        sealed class AxleVisual { public Transform beam, wheel; public Vector2 mount; }
        readonly List<WheelJoint2D> wheels = new(); readonly List<Rigidbody2D> wheelBodies = new(); float lift; bool spring;
        static AudioClip Tone(string name, float frequency, float duration)
        {
            int count = (int)(22050 * duration); var data = new float[count];
            for (int i = 0; i < count; i++) { float t = i / 22050f; data[i] = Mathf.Sin(t * Mathf.PI * 2 * frequency) * (1 - i / (float)count); }
            var clip = AudioClip.Create(name, count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }
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
                if (p.type == PartType.Cargo) { var art = Shape("Cargo visual", o.transform, Vector2.zero, Vector2.one, Color.white, false, 3); art.GetComponent<SpriteRenderer>().sprite = o.GetComponent<SpriteRenderer>().sprite; o.GetComponent<SpriteRenderer>().enabled = false; v.AddFeedback(art.transform); }
                if (p.type == PartType.Propeller) { var rotor = Shape("Propeller hub", o.transform, new Vector2(0, .05f), Vector2.one, Color.clear, true, 5); Shape("Rotor blade", rotor.transform, Vector2.zero, new Vector2(.78f, .09f), new Color(.79f, .88f, .96f), false, 5); Shape("Rotor blade", rotor.transform, Vector2.zero, new Vector2(.09f, .78f), new Color(.79f, .88f, .96f), false, 5); Shape("Rotor bolt", rotor.transform, Vector2.zero, Vector2.one * .14f, new Color(1, .73f, .2f), true, 6); v.rotors.Add(rotor.transform); }
                if (d.IsEngine) { var pig = Shape("Piggy driver", o.transform, new Vector2(-.18f, .77f), new Vector2(.75f, .75f), Color.white, false, 5); pig.GetComponent<SpriteRenderer>().sprite = MineArt.Pig; v.AddFeedback(pig.transform); for (int i = 0; i < 3; i++) v.AddFeedback(Shape("Steam", o.transform, new Vector2(.26f + i * .07f, .85f + i * .2f), Vector2.one * (.16f + i * .035f), new Color(.86f, .92f, 1, .7f - i * .14f), true, 3).transform, true); }
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
            v.InitializeAudio(); return v;
        }
        void LateUpdate() { UpdateAxles(); UpdateFeedback(); foreach (var rotor in rotors) if (rotor != null) rotor.localRotation = Quaternion.Euler(0, 0, Time.time * (body.simulated ? 900 : 0)); }
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
            float impact = hit.contactCount > 0 ? Mathf.Abs(Vector2.Dot(hit.relativeVelocity, hit.GetContact(0).normal)) : hit.relativeVelocity.magnitude; if (impact > 4 && hit.contactCount > 0) for (int i = 0; i < 4; i++) { var dust = Shape("Landing dust", null, hit.GetContact(0).point + Vector2.right * (i - 1.5f) * .15f, Vector2.one * .22f, new Color(.9f, .7f, .4f, .65f), true, 8); dust.AddComponent<LandingDust>().velocity = new Vector2((i - 1.5f) * .65f, .5f + i * .12f); }
            if (impact > 3 && motorAudio != null && SoundEnabled) motorAudio.PlayOneShot(impactClip, Mathf.Min(.3f, impact * .025f)); impactPulse = Mathf.Min(impact, 10); peakImpact = Mathf.Max(peakImpact, impact); if (impact > 7) hardLandings++;
        }
        public void Dispose() { foreach (var w in wheelBodies) if (w != null) Destroy(w.gameObject); Destroy(gameObject); }
    }
    public sealed class LandingDust : MonoBehaviour
    {
        public Vector2 velocity; float age;
        void Update() { age += Time.deltaTime; transform.position += (Vector3)velocity * Time.deltaTime; transform.localScale = Vector3.one * (.22f + age * .6f); var sr = GetComponent<SpriteRenderer>(); sr.color = new Color(.9f, .7f, .4f, Mathf.Max(0, .65f - age)); if (age > .65f) Destroy(gameObject); }
    }
    public sealed class WheelImpactReporter : MonoBehaviour
    {
        public VehiclePhysics owner;
        void OnCollisionEnter2D(Collision2D hit) { if (owner != null) owner.RecordImpact(hit); }
    }
}
