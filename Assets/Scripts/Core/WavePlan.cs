using System;
using System.Collections.Generic;

namespace RingOut.Core
{
    /// <summary>What one wave sends: its enemies, the power-up it offers and the arena it is fought on.</summary>
    public sealed class WavePlan
    {
        public int Wave { get; }
        public IReadOnlyList<EnemyKind> Enemies { get; }
        public PowerUpKind PowerUp { get; }
        public ArenaLayout Layout { get; }
        public bool Boss => Wave % WavePlanner.BossEvery == 0;

        public WavePlan(int wave, IReadOnlyList<EnemyKind> enemies, PowerUpKind powerUp, ArenaLayout layout)
        {
            Wave = wave;
            Enemies = enemies;
            PowerUp = powerUp;
            Layout = layout;
        }

        public int Count(EnemyKind kind)
        {
            int count = 0;
            foreach (EnemyKind enemy in Enemies)
            {
                count += enemy == kind ? 1 : 0;
            }
            return count;
        }
    }

    /// <summary>
    /// Composes waves. Wave n of the 2021 prototype sent n identical enemies; here the count grows
    /// more slowly and new kinds join: fast enemies from wave 3, heavies from wave 4, splitters
    /// from wave 6, each guaranteed in the wave it first appears. Every fifth wave is a boss with
    /// a few escorts. The rest of the mix is drawn from a seeded generator, so a seed always gives
    /// the same run.
    /// </summary>
    public static class WavePlanner
    {
        public const int BossEvery = 5;
        public const int MaxEnemies = 9;
        public const int FastFrom = 3;
        public const int HeavyFrom = 4;
        public const int SplitterFrom = 6;
        public const int SmashFrom = 2;
        public const int RocketsFrom = 4;

        /// <summary>Enemies in a regular wave: 1, 2, 3, 3, 4, 4, 5, 5 ... up to nine.</summary>
        public static int EnemyCount(int wave)
        {
            if (wave < 1)
            {
                return 0;
            }
            int count = wave <= 3 ? wave : 3 + (wave - 3) / 2;
            return Math.Min(count, MaxEnemies);
        }

        /// <summary>Escorts that come with a boss: one at wave 5, two at wave 10, up to four.</summary>
        public static int Escorts(int wave) => Math.Min(wave / BossEvery, 4);

        public static WavePlan Plan(int wave, int seed)
        {
            var rng = new Rng(seed * 7919 + wave * 104729);
            var enemies = new List<EnemyKind>();
            if (wave % BossEvery == 0)
            {
                enemies.Add(EnemyKind.Boss);
                for (int i = 0; i < Escorts(wave); i++)
                {
                    enemies.Add(Pick(rng, wave));
                }
            }
            else
            {
                if (wave == FastFrom)
                {
                    enemies.Add(EnemyKind.Fast);
                }
                if (wave == HeavyFrom)
                {
                    enemies.Add(EnemyKind.Heavy);
                }
                if (wave == SplitterFrom)
                {
                    enemies.Add(EnemyKind.Splitter);
                }
                while (enemies.Count < EnemyCount(wave))
                {
                    enemies.Add(Pick(rng, wave));
                }
            }
            return new WavePlan(wave, enemies, PickPowerUp(rng, wave), ArenaShape.ForWave(wave));
        }

        /// <summary>A weighted pick among the kinds unlocked by <paramref name="wave"/>.</summary>
        static EnemyKind Pick(Rng rng, int wave)
        {
            int grunt = 5;
            int fast = wave >= FastFrom ? 3 : 0;
            int heavy = wave >= HeavyFrom ? 2 : 0;
            int splitter = wave >= SplitterFrom ? 3 : 0;
            int roll = rng.Range(0, grunt + fast + heavy + splitter);
            if ((roll -= grunt) < 0) return EnemyKind.Grunt;
            if ((roll -= fast) < 0) return EnemyKind.Fast;
            if ((roll -= heavy) < 0) return EnemyKind.Heavy;
            return EnemyKind.Splitter;
        }

        static PowerUpKind PickPowerUp(Rng rng, int wave)
        {
            if (wave < SmashFrom)
            {
                return PowerUpKind.Knockback;
            }
            int kinds = wave >= RocketsFrom ? 3 : 2;
            return (PowerUpKind)rng.Range(0, kinds);
        }
    }
}
