using NUnit.Framework;

namespace RingOut.Core.Tests
{
    /// <summary>Pins the 2021 prototype's rules as they were extracted.</summary>
    public class RulesTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(7, 7)]
        public void WaveNumberNSendsNEnemies(int wave, int enemies)
        {
            Assert.That(Waves.EnemiesIn(wave), Is.EqualTo(enemies));
        }

        [Test]
        public void KnockbackPushesTheEnemyAwayFromThePlayer()
        {
            Vec3 impulse = Push.Knockback(new Vec3(0f, 0f, 0f), new Vec3(1f, 0f, 0f), 10f);

            Assert.That(impulse.X, Is.GreaterThan(0f));
            Assert.That(impulse.Z, Is.EqualTo(0f));
        }

        [Test]
        public void KnockbackFromTheOtherSidePushesTheOtherWay()
        {
            Vec3 impulse = Push.Knockback(new Vec3(2f, 0f, 2f), new Vec3(2f, 0f, 1f), 10f);

            Assert.That(impulse.Z, Is.LessThan(0f));
            Assert.That(impulse.X, Is.EqualTo(0f));
        }

        [Test]
        public void KnockbackHasTheSameStrengthAtAnyDistance()
        {
            Vec3 touching = Push.Knockback(Vec3.Zero, new Vec3(1.5f, 0f, 0f), 10f);
            Vec3 overlapping = Push.Knockback(Vec3.Zero, new Vec3(0.5f, 0f, 0f), 10f);

            Assert.That(touching.Length, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(overlapping.Length, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void KnockbackStaysOnTheGround()
        {
            Vec3 impulse = Push.Knockback(new Vec3(0f, 0.5f, 0f), new Vec3(1f, -0.3f, 1f), 10f);

            Assert.That(impulse.Y, Is.EqualTo(0f), "an enemy a little lower is pushed out, not into the ground");
            Assert.That(impulse.Length, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(impulse.X, Is.EqualTo(impulse.Z).Within(1e-4f));
        }

        [Test]
        public void KnockbackOfTwoBodiesAtTheSameSpotIsZero()
        {
            Assert.That(Push.Knockback(Vec3.Zero, Vec3.Zero, 10f), Is.EqualTo(Vec3.Zero));
        }
    }
}
