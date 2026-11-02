using System;

namespace RingOut.Core
{
    public enum EnemyKind
    {
        /// <summary>The 2021 enemy: an even match for the player.</summary>
        Grunt,
        /// <summary>Small, light and quick.</summary>
        Fast,
        /// <summary>Big, slow and three times the player's mass.</summary>
        Heavy,
        /// <summary>Breaks into two minis the first time it is hit hard.</summary>
        Splitter,
        /// <summary>Half of a splitter.</summary>
        Mini,
        /// <summary>Every fifth wave: huge, and charges at the player.</summary>
        Boss,
    }

    /// <summary>How an enemy rolls and how much it is worth.</summary>
    public readonly struct EnemyStats
    {
        /// <summary>Sphere radius in metres (the player's is 0.75).</summary>
        public readonly float Radius;
        public readonly float Mass;
        /// <summary>The force it chases with, in newtons.</summary>
        public readonly float Force;
        public readonly int Points;

        public EnemyStats(float radius, float mass, float force, int points)
        {
            Radius = radius;
            Mass = mass;
            Force = force;
            Points = points;
        }

        /// <summary>The acceleration it chases with (force over mass).</summary>
        public float Acceleration => Force / Mass;
    }

    public static class EnemyCatalog
    {
        /// <summary>Every wave after the first makes the chase 3% stronger, up to +36% at wave 13.</summary>
        public const float ForceGrowthPerWave = 0.03f;
        public const int GrowthStopsAtWave = 13;

        /// <summary>An impact above this impulse (N s) splits a splitter.</summary>
        public const float SplitImpulse = 4f;

        /// <summary>The boss charges every this many seconds, with this impulse.</summary>
        public const float BossChargeEvery = 4f;
        public const float BossChargeStrength = 60f;

        public static EnemyStats Base(EnemyKind kind) => kind switch
        {
            EnemyKind.Grunt => new EnemyStats(0.75f, 1f, 9f, 100),
            EnemyKind.Fast => new EnemyStats(0.55f, 0.6f, 6.6f, 150),
            EnemyKind.Heavy => new EnemyStats(1.05f, 3f, 21f, 250),
            EnemyKind.Splitter => new EnemyStats(0.9f, 1.4f, 12f, 150),
            EnemyKind.Mini => new EnemyStats(0.5f, 0.5f, 5.5f, 50),
            EnemyKind.Boss => new EnemyStats(1.7f, 8f, 52f, 1000),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        /// <summary>The stats in a given wave: the chase force grows with the wave number.</summary>
        public static EnemyStats For(EnemyKind kind, int wave)
        {
            EnemyStats stats = Base(kind);
            return new EnemyStats(stats.Radius, stats.Mass, stats.Force * ForceScale(wave), stats.Points);
        }

        public static float ForceScale(int wave)
        {
            int steps = Math.Clamp(wave, 1, GrowthStopsAtWave) - 1;
            return 1f + ForceGrowthPerWave * steps;
        }
    }
}
