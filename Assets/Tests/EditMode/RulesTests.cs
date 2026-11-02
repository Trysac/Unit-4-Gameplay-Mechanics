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
    }
}
