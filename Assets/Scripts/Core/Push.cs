namespace RingOut.Core
{
    /// <summary>The impulses of the player's moves, in N s.</summary>
    public static class Push
    {
        /// <summary>The knockback power-up's hit on contact.</summary>
        public const float KnockbackStrength = 16f;
        /// <summary>The dash, along the move direction.</summary>
        public const float DashStrength = 9f;
        /// <summary>The smash shockwave at its centre, and how far it reaches.</summary>
        public const float SmashStrength = 26f;
        public const float SmashRadius = 7f;
        /// <summary>A rocket's hit.</summary>
        public const float RocketStrength = 9f;

        /// <summary>
        /// <paramref name="strength"/> along the ground, straight away from the player. The 2021
        /// rule multiplied the raw offset, so the push grew with the distance between the centres
        /// and drove enemies that sat a little lower into the ground.
        /// </summary>
        public static Vec3 Knockback(Vec3 player, Vec3 enemy, float strength) => (enemy - player).Flat.Normalized * strength;

        /// <summary>
        /// A smash: away from the centre and a little up, full strength at the centre and fading
        /// linearly to nothing at <paramref name="radius"/>.
        /// </summary>
        public static Vec3 Shockwave(Vec3 center, Vec3 target, float radius, float strength)
        {
            Vec3 offset = (target - center).Flat;
            float distance = offset.Length;
            if (distance >= radius)
            {
                return Vec3.Zero;
            }
            float falloff = 1f - distance / radius;
            Vec3 away = distance > 1e-4f ? offset * (1f / distance) : new Vec3(1f, 0f, 0f);
            return (away + new Vec3(0f, 0.3f, 0f)) * (strength * falloff);
        }

        /// <summary>A dash along the ground in the direction of the stick or keys.</summary>
        public static Vec3 Dash(Vec3 direction, float strength) => direction.Flat.Normalized * strength;
    }
}
