namespace RingOut.Core
{
    /// <summary>The impulse a powered-up player gives an enemy it bumps into.</summary>
    public static class Push
    {
        /// <summary>
        /// <paramref name="strength"/> along the ground, straight away from the player. The 2021
        /// rule multiplied the raw offset, so the push grew with the distance between the centres
        /// and drove enemies that sat a little lower into the ground.
        /// </summary>
        public static Vec3 Knockback(Vec3 player, Vec3 enemy, float strength) => (enemy - player).Flat.Normalized * strength;
    }
}
