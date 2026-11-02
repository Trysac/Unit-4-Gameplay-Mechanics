namespace RingOut.Core
{
    /// <summary>How many enemies each wave sends (the 2021 rule: wave n has n enemies).</summary>
    public static class Waves
    {
        public static int EnemiesIn(int wave) => wave < 1 ? 0 : wave;
    }
}
