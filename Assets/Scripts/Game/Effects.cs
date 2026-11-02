using UnityEngine;

namespace RingOut.Game
{
    /// <summary>Particle bursts and the smash shockwave, made in code.</summary>
    public static class Effects
    {
        static Mesh ringMesh;

        /// <summary>A one-shot spray of glowing particles that cleans itself up.</summary>
        /// <param name="direction">Main direction of the spray; zero sprays all around.</param>
        public static void Burst(Vector3 position, Vector3 direction, Material material, Color colorA, Color colorB,
            int count, float speed, float size, float lifetime, float spread = 70f, float gravity = 0.6f)
        {
            if (material == null || count <= 0)
            {
                return;
            }
            var host = new GameObject("Burst");
            host.transform.position = position;
            if (direction.sqrMagnitude > 1e-4f)
            {
                host.transform.rotation = Quaternion.LookRotation(direction);
            }
            var system = host.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            ParticleSystem.ShapeModule shape = system.shape;
            if (direction.sqrMagnitude > 1e-4f)
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = spread * 0.5f;
                shape.radius = 0.1f;
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;
            }

            ParticleSystem.LimitVelocityOverLifetimeModule drag = system.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 2f;

            ParticleSystem.SizeOverLifetimeModule shrink = system.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            system.Play();
        }

        /// <summary>A flat expanding ring on the floor that fades out (the smash).</summary>
        public static void Shockwave(Vector3 position, float radius, Material material, Color color, float seconds = 0.45f)
        {
            if (material == null)
            {
                return;
            }
            var ring = new GameObject("Shockwave");
            ring.transform.position = position + Vector3.up * 0.15f;
            if (ringMesh == null)
            {
                ringMesh = Meshes.Torus(1f, 0.08f, 64, 6);
            }
            ring.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.AddComponent<Expand>().Begin(radius, seconds, color);
        }

        /// <summary>Grows a ring to its radius while it fades, then removes it.</summary>
        sealed class Expand : MonoBehaviour
        {
            float radius;
            float seconds;
            float age;
            Color color;
            MaterialPropertyBlock block;
            Renderer view;

            public void Begin(float radius, float seconds, Color color)
            {
                this.radius = radius;
                this.seconds = seconds;
                this.color = color;
                block = new MaterialPropertyBlock();
                view = GetComponent<Renderer>();
                Apply();
            }

            void Update()
            {
                age += Time.deltaTime;
                if (age >= seconds)
                {
                    Destroy(gameObject);
                    return;
                }
                Apply();
            }

            void Apply()
            {
                float t = age / seconds;
                float size = Mathf.Lerp(0.6f, radius, 1f - (1f - t) * (1f - t));
                transform.localScale = new Vector3(size, 1f + 3f * (1f - t), size);
                block.SetColor("_TintColor", color.WithAlpha(0.5f * (1f - t)));
                view.SetPropertyBlock(block);
            }
        }
    }
}
