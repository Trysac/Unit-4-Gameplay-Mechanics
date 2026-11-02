using NUnit.Framework;

namespace RingOut.Core.Tests
{
    public class PushTests
    {
        [Test]
        public void TheSmashIsFullStrengthAtTheCentreAndFadesToNothingAtItsRadius()
        {
            Vec3 near = Push.Shockwave(Vec3.Zero, new Vec3(1f, 0f, 0f), 7f, 28f);
            Vec3 half = Push.Shockwave(Vec3.Zero, new Vec3(0f, 0f, -3.5f), 7f, 28f);
            Vec3 edge = Push.Shockwave(Vec3.Zero, new Vec3(7f, 0f, 0f), 7f, 28f);

            Assert.That(near.X, Is.EqualTo(24f).Within(1e-3f));
            Assert.That(near.Y, Is.EqualTo(24f * 0.3f).Within(1e-3f), "a little lift");
            Assert.That(half.Z, Is.EqualTo(-14f).Within(1e-3f));
            Assert.That(half.X, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(edge, Is.EqualTo(Vec3.Zero));
        }

        [Test]
        public void TheSmashIgnoresHeightAndPushesABallRightUnderneathSideways()
        {
            Vec3 above = Push.Shockwave(new Vec3(0f, 3f, 0f), new Vec3(2f, 0f, 0f), 7f, 7f);
            Vec3 under = Push.Shockwave(Vec3.Zero, new Vec3(0f, -1f, 0f), 7f, 7f);

            Assert.That(above.X, Is.EqualTo(5f).Within(1e-3f));
            Assert.That(under.X, Is.EqualTo(7f).Within(1e-3f));
        }

        [Test]
        public void TheDashGoesAlongTheGroundAtFullStrength()
        {
            Vec3 dash = Push.Dash(new Vec3(3f, 2f, 4f), 9f);

            Assert.That(dash.Y, Is.EqualTo(0f));
            Assert.That(dash.Length, Is.EqualTo(9f).Within(1e-4f));
            Assert.That(dash.X / dash.Z, Is.EqualTo(0.75f).Within(1e-4f));
            Assert.That(Push.Dash(Vec3.Zero, 9f), Is.EqualTo(Vec3.Zero));
        }
    }
}
