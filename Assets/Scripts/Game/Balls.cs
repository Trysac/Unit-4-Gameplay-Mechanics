using RingOut.Core;
using UnityEngine;

namespace RingOut.Game
{
    /// <summary>What the balls report to the director, which turns it into rules and effects.</summary>
    public interface IArenaEvents
    {
        void PlayerHit(EnemyBall enemy, Collision collision);
        void EnemiesCollided(EnemyBall enemy, Collision collision);
        void SmashLanded(Vector3 position);
        void PickedUp(PowerUpPickup pickup);
        void BossWindUp(EnemyBall boss);
    }

    /// <summary>Shared set-up for the rolling spheres.</summary>
    public static class Ball
    {
        /// <summary>A sphere with a rigidbody, created at <paramref name="position"/> (moving it afterwards can leave the body behind).</summary>
        public static GameObject Create(string name, Vector3 position, float radius, float mass, Material material, PhysicsMaterial physics)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = name;
            ball.transform.position = position;
            ball.transform.localScale = Vector3.one * (radius * 2f);
            ball.GetComponent<MeshRenderer>().sharedMaterial = material;
            ball.GetComponent<SphereCollider>().sharedMaterial = physics;
            var body = ball.AddComponent<Rigidbody>();
            body.mass = mass;
            // Damping caps the rolling speed at force / (mass x damping): about 10 m/s for the player.
            body.linearDamping = 1.2f;
            body.angularDamping = 0.4f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return ball;
        }

        public static Vector3 ToUnity(Vec3 v) => new Vector3(v.X, v.Y, v.Z);

        public static Vec3 ToCore(Vector3 v) => new Vec3(v.x, v.y, v.z);
    }

    /// <summary>
    /// The player's ball: rolls with a camera-relative force, dashes, leaps and slams for the
    /// smash, and wears a coloured halo for each running power-up.
    /// </summary>
    public sealed class PlayerBall : MonoBehaviour
    {
        public const float Radius = 0.75f;
        public const float Mass = 1f;
        public const float Force = 12f;
        const float SmashLeap = 7.5f;
        const float SmashDive = 22f;

        IArenaEvents events;
        Transform halos;
        Renderer knockHalo;
        Renderer rocketHalo;
        Transform smashPips;
        float smashTime;
        bool diving;

        public Rigidbody Body { get; private set; }

        /// <summary>The direction and strength to roll, on the ground plane, at most length 1.</summary>
        public Vector3 MoveInput { get; set; }

        public bool Smashing { get; private set; }

        public static PlayerBall Create(GameArt art, IArenaEvents events)
        {
            GameObject ball = Ball.Create("Player", new Vector3(0f, 40f, 0f), Radius, Mass, art.Player, art.BallPhysics);
            var player = ball.AddComponent<PlayerBall>();
            player.events = events;
            player.Body = ball.GetComponent<Rigidbody>();
            player.BuildHalos(art);
            return player;
        }

        void BuildHalos(GameArt art)
        {
            halos = new GameObject("Player Halos").transform;
            Mesh ring = Meshes.Torus(1f, 0.06f, 40, 6);
            knockHalo = Halo(ring, art.Ring, Palette.Knockback, 1.25f, -0.55f);
            rocketHalo = Halo(ring, art.Ring, Palette.Rockets, 1.05f, -0.35f);
            smashPips = new GameObject("Smash Charges").transform;
            smashPips.SetParent(halos, false);
            Mesh gem = Meshes.Gem(0.16f, 0.24f);
            for (int i = 0; i < PlayerPowers.SmashCharges; i++)
            {
                var pip = new GameObject("Charge");
                pip.transform.SetParent(smashPips, false);
                float angle = i * Mathf.PI * 2f / PlayerPowers.SmashCharges;
                pip.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.1f, 1.05f, Mathf.Sin(angle) * 1.1f);
                pip.AddComponent<MeshFilter>().sharedMesh = gem;
                pip.AddComponent<MeshRenderer>().sharedMaterial = art.Pickup;
                Tint(pip.GetComponent<Renderer>(), Palette.Smash);
            }
            SetPowers(false, false, 0);
        }

        Renderer Halo(Mesh ring, Material material, Color color, float size, float height)
        {
            var halo = new GameObject("Halo");
            halo.transform.SetParent(halos, false);
            halo.transform.localPosition = new Vector3(0f, height, 0f);
            halo.transform.localScale = new Vector3(size, 1f, size);
            halo.AddComponent<MeshFilter>().sharedMesh = ring;
            var renderer = halo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Tint(renderer, color, "_TintColor");
            return renderer;
        }

        static void Tint(Renderer renderer, Color color, string property = "_Color")
        {
            var block = new MaterialPropertyBlock();
            block.SetColor(property, color);
            if (property == "_Color")
            {
                block.SetColor("_EmissionColor", color * 1.6f);
            }
            renderer.SetPropertyBlock(block);
        }

        /// <summary>Shows a halo for each running power-up and a gem for each smash charge.</summary>
        public void SetPowers(bool knockback, bool rockets, int smashes)
        {
            knockHalo.enabled = knockback;
            rocketHalo.enabled = rockets;
            for (int i = 0; i < smashPips.childCount; i++)
            {
                smashPips.GetChild(i).gameObject.SetActive(i < smashes);
            }
        }

        public void Dash(Vector3 direction)
        {
            Vector3 impulse = Ball.ToUnity(Push.Dash(Ball.ToCore(direction), Push.DashStrength));
            Body.AddForce(impulse, ForceMode.Impulse);
        }

        public void StartSmash()
        {
            Smashing = true;
            diving = false;
            smashTime = 0f;
            Vector3 velocity = Body.linearVelocity;
            Body.linearVelocity = new Vector3(velocity.x * 0.5f, 0f, velocity.z * 0.5f);
            Body.AddForce(Vector3.up * SmashLeap, ForceMode.VelocityChange);
        }

        /// <summary>Puts the ball at rest at a point (a new run, a new arena).</summary>
        public void Place(Vector3 position)
        {
            Smashing = false;
            Body.position = position;
            transform.position = position;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            MoveInput = Vector3.zero;
        }

        void FixedUpdate()
        {
            Body.AddForce(MoveInput * Force);
            if (!Smashing)
            {
                return;
            }
            smashTime += Time.fixedDeltaTime;
            if (!diving && smashTime > 0.32f)
            {
                diving = true;
                Body.AddForce(Vector3.down * SmashDive, ForceMode.VelocityChange);
            }
            if (smashTime > 1.5f)
            {
                // Never landed: it went over the edge.
                Smashing = false;
            }
        }

        void LateUpdate()
        {
            if (halos == null)
            {
                return;
            }
            halos.position = transform.position;
            halos.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
        }

        void OnCollisionEnter(Collision collision)
        {
            var enemy = collision.rigidbody != null ? collision.rigidbody.GetComponent<EnemyBall>() : null;
            if (Smashing && diving)
            {
                // The slam goes off on whatever the dive hits first: the floor or an enemy.
                Smashing = false;
                events?.SmashLanded(transform.position);
            }
            if (enemy != null)
            {
                events?.PlayerHit(enemy, collision);
            }
        }

        void OnDestroy()
        {
            if (halos != null)
            {
                Destroy(halos.gameObject);
            }
        }
    }

    /// <summary>
    /// An enemy ball: chases its target with its kind's force, and the boss winds up and charges
    /// every few seconds.
    /// </summary>
    public sealed class EnemyBall : MonoBehaviour
    {
        IArenaEvents events;
        float chargeClock;
        bool windingUp;
        Renderer view;

        public EnemyKind Kind { get; private set; }
        public EnemyStats Stats { get; private set; }
        public Rigidbody Body { get; private set; }
        public Transform Target { get; set; }

        /// <summary>Set once its fall has been scored (or it was replaced by minis).</summary>
        public bool Gone { get; set; }

        /// <summary>When the knockback power-up last hit it (one hit per contact, not one per bounce).</summary>
        public float LastKnocked { get; set; } = -10f;

        public bool WindingUp => windingUp;

        public static EnemyBall Create(EnemyKind kind, int wave, Vector3 position, GameArt art, IArenaEvents events)
        {
            EnemyStats stats = EnemyCatalog.For(kind, wave);
            GameObject ball = Ball.Create(kind.ToString(), position, stats.Radius, stats.Mass, art.EnemyMaterial(kind), art.BallPhysics);
            var enemy = ball.AddComponent<EnemyBall>();
            enemy.Kind = kind;
            enemy.Stats = stats;
            enemy.events = events;
            enemy.Body = ball.GetComponent<Rigidbody>();
            enemy.view = ball.GetComponent<Renderer>();
            // Stagger the boss's charges so it does not charge the moment it lands.
            enemy.chargeClock = -1.5f;
            return enemy;
        }

        void FixedUpdate()
        {
            if (Target == null || Gone)
            {
                return;
            }
            Vector3 toTarget = Target.position - transform.position;
            toTarget.y = 0f;
            Vector3 direction = toTarget.normalized;
            Body.AddForce(direction * Stats.Force);

            if (Kind != EnemyKind.Boss)
            {
                return;
            }
            chargeClock += Time.fixedDeltaTime;
            if (!windingUp && chargeClock > EnemyCatalog.BossChargeEvery - 0.7f)
            {
                windingUp = true;
                events?.BossWindUp(this);
            }
            if (chargeClock >= EnemyCatalog.BossChargeEvery)
            {
                chargeClock = 0f;
                windingUp = false;
                Body.AddForce(direction * EnemyCatalog.BossChargeStrength, ForceMode.Impulse);
            }
        }

        void Update()
        {
            if (Kind == EnemyKind.Boss && view != null)
            {
                // The boss glows brighter as a charge winds up.
                float glow = windingUp ? 1.5f + Mathf.PingPong(Time.time * 8f, 1.5f) : 0.6f;
                var block = new MaterialPropertyBlock();
                view.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", Palette.Of(EnemyKind.Boss) * glow);
                view.SetPropertyBlock(block);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            var other = collision.rigidbody != null ? collision.rigidbody.GetComponent<EnemyBall>() : null;
            if (other != null)
            {
                events?.EnemiesCollided(this, collision);
            }
        }
    }

    /// <summary>A floating power-up gem; rolling into it picks it up.</summary>
    public sealed class PowerUpPickup : MonoBehaviour
    {
        static Mesh gemMesh;
        static Mesh haloMesh;

        IArenaEvents events;
        Transform gem;
        float phase;

        public PowerUpKind Kind { get; private set; }

        public static PowerUpPickup Create(PowerUpKind kind, Vector3 position, GameArt art, IArenaEvents events)
        {
            var root = new GameObject($"PowerUp {kind}");
            root.transform.position = position;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;
            var pickup = root.AddComponent<PowerUpPickup>();
            pickup.Kind = kind;
            pickup.events = events;

            if (gemMesh == null)
            {
                gemMesh = Meshes.Gem(0.45f, 0.7f);
                haloMesh = Meshes.Torus(0.9f, 0.05f, 32, 5);
            }
            var gem = new GameObject("Gem");
            gem.transform.SetParent(root.transform, false);
            gem.AddComponent<MeshFilter>().sharedMesh = gemMesh;
            var renderer = gem.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = art.Pickup;
            var block = new MaterialPropertyBlock();
            Color color = Palette.Of(kind);
            block.SetColor("_Color", color);
            block.SetColor("_EmissionColor", color * 1.8f);
            renderer.SetPropertyBlock(block);
            pickup.gem = gem.transform;

            var halo = new GameObject("Halo");
            halo.transform.SetParent(root.transform, false);
            halo.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            halo.AddComponent<MeshFilter>().sharedMesh = haloMesh;
            var haloRenderer = halo.AddComponent<MeshRenderer>();
            haloRenderer.sharedMaterial = art.Ring;
            var haloBlock = new MaterialPropertyBlock();
            haloBlock.SetColor("_TintColor", color.WithAlpha(0.6f));
            haloRenderer.SetPropertyBlock(haloBlock);

            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.type = LightType.Point;
            light.color = color;
            light.range = 4f;
            light.intensity = 2f;
            return pickup;
        }

        void Update()
        {
            phase += Time.deltaTime;
            gem.localPosition = new Vector3(0f, Mathf.Sin(phase * 2.5f) * 0.18f, 0f);
            gem.localRotation = Quaternion.Euler(0f, phase * 120f, 0f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerBall>() != null)
            {
                events?.PickedUp(this);
            }
        }
    }

    /// <summary>A homing rocket from the rockets power-up.</summary>
    public sealed class Rocket : MonoBehaviour
    {
        static Mesh bodyMesh;

        const float Speed = 15f;
        const float TurnRate = 7f;
        const float Lifetime = 3f;

        float age;

        public EnemyBall Target { get; private set; }
        public Vector3 Velocity { get; private set; }

        /// <summary>Raised when the rocket reaches its target.</summary>
        public System.Action<Rocket, EnemyBall> Hit;

        public static Rocket Launch(Vector3 from, Vector3 initialDirection, EnemyBall target, GameArt art)
        {
            var body = new GameObject("Rocket");
            body.transform.position = from;
            var rocket = body.AddComponent<Rocket>();
            rocket.Target = target;
            rocket.Velocity = initialDirection.normalized * Speed;

            var visual = new GameObject("Body");
            visual.transform.SetParent(body.transform, false);
            visual.transform.localScale = new Vector3(1f, 1f, 1.8f);
            if (bodyMesh == null)
            {
                bodyMesh = Meshes.Gem(0.14f, 0.14f);
            }
            visual.AddComponent<MeshFilter>().sharedMesh = bodyMesh;
            visual.AddComponent<MeshRenderer>().sharedMaterial = art.Rocket;

            var trail = body.AddComponent<TrailRenderer>();
            trail.sharedMaterial = art.Glow;
            trail.time = 0.35f;
            trail.startWidth = 0.32f;
            trail.endWidth = 0f;
            trail.startColor = Palette.Rockets;
            trail.endColor = Palette.Rockets.WithAlpha(0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return rocket;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (age > Lifetime || Target == null || Target.Gone)
            {
                Destroy(gameObject);
                return;
            }
            Vector3 toTarget = Target.transform.position - transform.position;
            Vector3 wanted = toTarget.normalized * Speed;
            Velocity = Vector3.Lerp(Velocity, wanted, 1f - Mathf.Exp(-TurnRate * dt));
            transform.position += Velocity * dt;
            if (Velocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(Velocity);
            }
            if (toTarget.magnitude < Target.Stats.Radius + 0.25f)
            {
                Hit?.Invoke(this, Target);
                Destroy(gameObject);
            }
        }
    }
}
