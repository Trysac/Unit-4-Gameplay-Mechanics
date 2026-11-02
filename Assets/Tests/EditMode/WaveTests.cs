using System.Linq;
using NUnit.Framework;

namespace RingOut.Core.Tests
{
    public class WaveTests
    {
        [Test]
        public void TheFirstWaveIsOneGruntWithAKnockbackPowerUpOnTheDisc()
        {
            WavePlan plan = WavePlanner.Plan(1, 42);

            Assert.That(plan.Enemies, Is.EqualTo(new[] { EnemyKind.Grunt }));
            Assert.That(plan.PowerUp, Is.EqualTo(PowerUpKind.Knockback));
            Assert.That(plan.Layout, Is.EqualTo(ArenaLayout.Disc));
            Assert.That(plan.Boss, Is.False);
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 3)]
        [TestCase(6, 4)]
        [TestCase(9, 6)]
        [TestCase(41, 9)]
        public void RegularWavesGrowSlowlyUpToNine(int wave, int count)
        {
            Assert.That(WavePlanner.EnemyCount(wave), Is.EqualTo(count));
            Assert.That(WavePlanner.Plan(wave, 7).Enemies.Count, Is.EqualTo(count));
        }

        [Test]
        public void EachKindIsGuaranteedInTheWaveItFirstAppearsAndAbsentBefore()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                Assert.That(WavePlanner.Plan(3, seed).Count(EnemyKind.Fast), Is.GreaterThanOrEqualTo(1));
                Assert.That(WavePlanner.Plan(4, seed).Count(EnemyKind.Heavy), Is.GreaterThanOrEqualTo(1));
                Assert.That(WavePlanner.Plan(6, seed).Count(EnemyKind.Splitter), Is.GreaterThanOrEqualTo(1));
                Assert.That(WavePlanner.Plan(2, seed).Enemies, Is.All.EqualTo(EnemyKind.Grunt));
                Assert.That(WavePlanner.Plan(3, seed).Count(EnemyKind.Heavy), Is.Zero);
                Assert.That(WavePlanner.Plan(4, seed).Count(EnemyKind.Splitter), Is.Zero);
            }
        }

        [TestCase(5, 1)]
        [TestCase(10, 2)]
        [TestCase(15, 3)]
        [TestCase(30, 4)]
        public void EveryFifthWaveIsABossWithEscorts(int wave, int escorts)
        {
            WavePlan plan = WavePlanner.Plan(wave, 3);

            Assert.That(plan.Boss, Is.True);
            Assert.That(plan.Enemies[0], Is.EqualTo(EnemyKind.Boss));
            Assert.That(plan.Count(EnemyKind.Boss), Is.EqualTo(1));
            Assert.That(plan.Enemies.Count, Is.EqualTo(1 + escorts));
        }

        [Test]
        public void TheSameSeedGivesTheSameRunAndOtherSeedsVaryIt()
        {
            for (int wave = 1; wave <= 20; wave++)
            {
                Assert.That(WavePlanner.Plan(wave, 99).Enemies, Is.EqualTo(WavePlanner.Plan(wave, 99).Enemies));
                Assert.That(WavePlanner.Plan(wave, 99).PowerUp, Is.EqualTo(WavePlanner.Plan(wave, 99).PowerUp));
            }
            int differ = Enumerable.Range(0, 30).Count(seed =>
                !WavePlanner.Plan(9, seed).Enemies.SequenceEqual(WavePlanner.Plan(9, seed + 1000).Enemies));
            Assert.That(differ, Is.GreaterThan(15));
        }

        [Test]
        public void PowerUpsUnlockSmashAtWaveTwoAndRocketsAtWaveFour()
        {
            var early = Enumerable.Range(0, 200).Select(seed => WavePlanner.Plan(3, seed).PowerUp).Distinct().ToArray();
            var late = Enumerable.Range(0, 200).Select(seed => WavePlanner.Plan(4, seed).PowerUp).Distinct().ToArray();

            Assert.That(early, Is.EquivalentTo(new[] { PowerUpKind.Knockback, PowerUpKind.Smash }));
            Assert.That(late, Is.EquivalentTo(new[] { PowerUpKind.Knockback, PowerUpKind.Smash, PowerUpKind.Rockets }));
        }

        [Test]
        public void TheMixFollowsTheWeightsOverManyWaves()
        {
            int grunts = 0, fast = 0, heavy = 0, splitters = 0;
            for (int seed = 0; seed < 2000; seed++)
            {
                WavePlan plan = WavePlanner.Plan(7, seed);
                grunts += plan.Count(EnemyKind.Grunt);
                fast += plan.Count(EnemyKind.Fast);
                heavy += plan.Count(EnemyKind.Heavy);
                splitters += plan.Count(EnemyKind.Splitter);
            }
            float total = grunts + fast + heavy + splitters;

            Assert.That(total, Is.EqualTo(2000 * 5));
            Assert.That(grunts / total, Is.EqualTo(5f / 13f).Within(0.02f));
            Assert.That(fast / total, Is.EqualTo(3f / 13f).Within(0.02f));
            Assert.That(heavy / total, Is.EqualTo(2f / 13f).Within(0.02f));
            Assert.That(splitters / total, Is.EqualTo(3f / 13f).Within(0.02f));
        }

        [Test]
        public void EnemyKindsDifferInSizeWeightAndWorth()
        {
            EnemyStats grunt = EnemyCatalog.Base(EnemyKind.Grunt);
            EnemyStats fast = EnemyCatalog.Base(EnemyKind.Fast);
            EnemyStats heavy = EnemyCatalog.Base(EnemyKind.Heavy);
            EnemyStats boss = EnemyCatalog.Base(EnemyKind.Boss);

            Assert.That(fast.Acceleration, Is.GreaterThan(grunt.Acceleration));
            Assert.That(fast.Mass, Is.LessThan(grunt.Mass));
            Assert.That(heavy.Mass, Is.EqualTo(3f * grunt.Mass));
            Assert.That(heavy.Acceleration, Is.LessThan(grunt.Acceleration));
            Assert.That(boss.Radius, Is.GreaterThan(heavy.Radius));
            Assert.That(boss.Points, Is.GreaterThan(heavy.Points));
            Assert.That(EnemyCatalog.Base(EnemyKind.Mini).Radius, Is.LessThan(EnemyCatalog.Base(EnemyKind.Splitter).Radius));
        }

        [TestCase(1, 1f)]
        [TestCase(2, 1.03f)]
        [TestCase(13, 1.36f)]
        [TestCase(50, 1.36f)]
        public void TheChaseGetsStrongerEachWaveUpToACap(int wave, float scale)
        {
            Assert.That(EnemyCatalog.ForceScale(wave), Is.EqualTo(scale).Within(1e-4f));
            Assert.That(EnemyCatalog.For(EnemyKind.Grunt, wave).Force, Is.EqualTo(9f * scale).Within(1e-3f));
            Assert.That(EnemyCatalog.For(EnemyKind.Grunt, wave).Mass, Is.EqualTo(1f), "only the force grows");
        }
    }
}
