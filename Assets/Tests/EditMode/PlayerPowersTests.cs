using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RingOut.Core.Tests
{
    public class PlayerPowersTests
    {
        static List<PowerSignal> Run(PlayerPowers powers, float seconds, float step)
        {
            var signals = new List<PowerSignal>();
            for (float t = 0f; t < seconds - 1e-4f; t += step)
            {
                signals.AddRange(powers.Step(step));
            }
            return signals;
        }

        [Test]
        public void WithoutChargesTheButtonDashesThenWaitsForTheCooldown()
        {
            var powers = new PlayerPowers();

            Assert.That(powers.Act(), Is.EqualTo(ActionResult.Dash));
            Assert.That(powers.Cooldown, Is.EqualTo(PlayerPowers.DashCooldown));
            Assert.That(powers.Act(), Is.EqualTo(ActionResult.None));
            powers.Step(1f);
            Assert.That(powers.Act(), Is.EqualTo(ActionResult.None));
            powers.Step(0.25f);
            Assert.That(powers.Act(), Is.EqualTo(ActionResult.Dash));
        }

        [Test]
        public void SmashChargesAreSpentBeforeTheDashReturns()
        {
            var powers = new PlayerPowers();
            powers.Grant(PowerUpKind.Smash);
            var results = new List<ActionResult>();
            for (int i = 0; i < 4; i++)
            {
                results.Add(powers.Act());
                powers.Step(PlayerPowers.DashCooldown);
            }

            Assert.That(results, Is.EqualTo(new[] { ActionResult.Smash, ActionResult.Smash, ActionResult.Smash, ActionResult.Dash }));
            Assert.That(powers.Smashes, Is.Zero);
        }

        [Test]
        public void KnockbackRunsSevenSecondsAndSaysWhenItEnds()
        {
            var powers = new PlayerPowers();
            powers.Grant(PowerUpKind.Knockback);

            Assert.That(powers.HasKnockback, Is.True);
            Assert.That(Run(powers, 6.9f, 0.1f), Is.Empty);
            Assert.That(powers.HasKnockback, Is.True);
            Assert.That(Run(powers, 0.2f, 0.1f), Is.EqualTo(new[] { PowerSignal.KnockbackEnded }));
            Assert.That(powers.HasKnockback, Is.False);
            Assert.That(powers.Any, Is.False);
        }

        [TestCase(1f / 60f)]
        [TestCase(0.1f)]
        [TestCase(1f)]
        public void RocketsFireAVolleyAtOnceAndEverySecondForSixVolleys(float step)
        {
            var powers = new PlayerPowers();
            powers.Grant(PowerUpKind.Rockets);

            List<PowerSignal> first = powers.Step(step).ToList();
            List<PowerSignal> rest = Run(powers, 7f, step);

            Assert.That(first, Has.Member(PowerSignal.RocketVolley), "the first volley leaves at once");
            Assert.That(first.Concat(rest).Count(s => s == PowerSignal.RocketVolley), Is.EqualTo(6));
            Assert.That(rest.Count(s => s == PowerSignal.RocketsEnded), Is.EqualTo(1));
            Assert.That(powers.HasRockets, Is.False);
        }

        [Test]
        public void PickingUpRocketsAgainExtendsThemWithoutAnExtraVolley()
        {
            var powers = new PlayerPowers();
            powers.Grant(PowerUpKind.Rockets);
            List<PowerSignal> signals = Run(powers, 3.5f, 0.05f);
            powers.Grant(PowerUpKind.Rockets);
            signals.AddRange(Run(powers, 10f, 0.05f));

            // Volleys at 0, 1, 2, 3, then the timer restarts at 3.5 s and runs to 9.5 s: 4..9.
            Assert.That(signals.Count(s => s == PowerSignal.RocketVolley), Is.EqualTo(10));
            Assert.That(signals.Count(s => s == PowerSignal.RocketsEnded), Is.EqualTo(1));
        }

        [Test]
        public void DifferentPowerUpsRunTogetherAndClearEndsThemAll()
        {
            var powers = new PlayerPowers();
            powers.Grant(PowerUpKind.Knockback);
            powers.Grant(PowerUpKind.Smash);
            powers.Grant(PowerUpKind.Rockets);

            Assert.That(powers.HasKnockback && powers.HasRockets && powers.Smashes == 3, Is.True);
            powers.Clear();
            Assert.That(powers.Any, Is.False);
            Assert.That(powers.Act(), Is.EqualTo(ActionResult.Dash));
        }
    }
}
