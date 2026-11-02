using UnityEngine;

namespace RingOut.Game
{
    /// <summary>
    /// A third-person camera that orbits the arena: it looks at a point between the arena's
    /// centre and the player (so both the player and the edge stay in view), turns with Q/E or
    /// the right stick, drifts on its own behind the menus, and shakes with trauma.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        const float Pitch = 40f;
        const float BaseDistance = 20f;

        Camera view;
        Vector3 focus;
        float trauma;
        Vector3 kick;

        /// <summary>Scales every shake (the "screen shake" setting turns it off with 0).</summary>
        public float ShakeScale { get; set; } = 1f;

        /// <summary>Degrees the camera turns per second at full stick.</summary>
        public float TurnSpeed { get; set; } = 110f;

        /// <summary>Degrees per second it drifts by itself (the title screen).</summary>
        public float Drift { get; set; }

        public Transform Follow { get; set; }

        public Vector3 ArenaCenter { get; set; }

        public float Yaw { get; set; }

        public Camera View => view != null ? view : view = GetComponent<Camera>();

        /// <summary>The camera's forward direction on the ground, for camera-relative controls.</summary>
        public Vector3 Forward => Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;

        public Vector3 Right => Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;

        void Awake() => view = GetComponent<Camera>();

        public void Turn(float amount, float deltaTime) => Yaw += amount * TurnSpeed * deltaTime;

        /// <summary>Adds screen shake; 1 is the strongest.</summary>
        public void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount * ShakeScale);

        /// <summary>Nudges the view a little along <paramref name="direction"/>.</summary>
        public void Kick(Vector3 direction, float amount) => kick += direction.normalized * (amount * ShakeScale);

        public void Calm()
        {
            trauma = 0f;
            kick = Vector3.zero;
        }

        /// <summary>Jumps straight to the current target instead of easing towards it.</summary>
        public void Snap()
        {
            focus = Target();
            Place(Vector3.zero, 0f);
        }

        Vector3 Target()
        {
            if (Follow == null)
            {
                return ArenaCenter;
            }
            Vector3 player = Follow.position;
            // Once the player drops below the floor, keep watching where it went over.
            player.y = Mathf.Max(player.y, -1.5f);
            return Vector3.Lerp(ArenaCenter, player, 0.7f);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            Yaw += Drift * dt;
            focus = Vector3.Lerp(focus, Target(), 1f - Mathf.Exp(-dt * 4f));
            trauma = Mathf.Max(trauma - dt * 1.5f, 0f);
            kick = Vector3.Lerp(kick, Vector3.zero, 1f - Mathf.Exp(-dt * 12f));
            float strength = trauma * trauma;
            float t = Time.unscaledTime * 28f;
            var shake = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, Mathf.PerlinNoise(t, 5.1f) - 0.5f) * (1.2f * strength);
            float roll = (Mathf.PerlinNoise(t * 0.6f, 4.2f) - 0.5f) * 4f * strength;
            Place(shake + kick, roll);
        }

        void Place(Vector3 offset, float roll)
        {
            // Narrow screens pull the camera back so the arena still fits across.
            float aspect = Mathf.Max(View.aspect, 0.4f);
            float distance = BaseDistance * Mathf.Max(1f, 1.5f / aspect);
            Quaternion orbit = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 position = focus + orbit * new Vector3(0f, 0f, -distance);
            transform.SetPositionAndRotation(position + offset, orbit * Quaternion.Euler(0f, 0f, roll));
        }
    }
}
