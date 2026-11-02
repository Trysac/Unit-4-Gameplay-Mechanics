using NUnit.Framework;

namespace RingOut.Core.Tests
{
    public class PowerUpTimerTests
    {
        [Test]
        public void AGrantedPowerUpRunsForItsDurationAndThenEnds()
        {
            var timer = new PowerUpTimer();
            timer.Grant(5f);

            Assert.That(timer.Step(4.9f), Is.False);
            Assert.That(timer.Active, Is.True);
            Assert.That(timer.Remaining, Is.EqualTo(0.1f).Within(1e-4f));
            Assert.That(timer.Step(0.2f), Is.True, "the step that runs it out says so");
            Assert.That(timer.Active, Is.False);
            Assert.That(timer.Remaining, Is.EqualTo(0f));
            Assert.That(timer.Step(1f), Is.False, "an idle timer never ends again");
        }

        [Test]
        public void ASecondPickUpRestartsTheFullDuration()
        {
            var timer = new PowerUpTimer();
            timer.Grant(5f);
            timer.Step(3f);
            timer.Grant(5f);

            Assert.That(timer.Remaining, Is.EqualTo(5f));
            Assert.That(timer.Step(2.5f), Is.False);
            Assert.That(timer.Active, Is.True, "5.5 s after the first pick-up");
            Assert.That(timer.Step(2.5f), Is.True);
        }

        [Test]
        public void AShorterGrantNeverCutsARunningPowerUpShort()
        {
            var timer = new PowerUpTimer();
            timer.Grant(5f);
            timer.Grant(2f);

            Assert.That(timer.Remaining, Is.EqualTo(5f));
        }

        [Test]
        public void ClearEndsItAtOnce()
        {
            var timer = new PowerUpTimer();
            timer.Grant(5f);
            timer.Clear();

            Assert.That(timer.Active, Is.False);
            Assert.That(timer.Step(1f), Is.False);
        }
    }
}
