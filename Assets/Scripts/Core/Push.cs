namespace RingOut.Core
{
    /// <summary>The impulse a powered-up player gives an enemy it bumps into.</summary>
    public static class Push
    {
        /// <summary>The 2021 rule: the offset from the player to the enemy, times the strength.</summary>
        public static Vec3 Knockback(Vec3 player, Vec3 enemy, float strength) => (enemy - player) * strength;
    }
}
