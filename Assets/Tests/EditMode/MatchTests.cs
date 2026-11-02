using System.Linq;
using NUnit.Framework;

namespace RingOut.Core.Tests
{
    public class MatchTests
    {
        static MatchEventKind[] Kinds(Match match) => match.Events.Select(e => e.Kind).ToArray();

        static void Run(Match match, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += 0.05f)
            {
                match.Step(0.05f);
            }
        }

        [Test]
        public void ARunStartsOnWaveOneWithACountdownThenTheFight()
        {
            var match = new Match(1);

            Assert.That(match.Wave, Is.EqualTo(1));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(match.Alive, Is.EqualTo(1));
            Assert.That(match.Shape.Layout, Is.EqualTo(ArenaLayout.Disc));
            Assert.That(Kinds(match), Is.EqualTo(new[] { MatchEventKind.WaveStarted }));
            match.ClearEvents();

            Run(match, Match.CountdownSeconds + 0.1f);

            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Fighting));
            Assert.That(Kinds(match), Is.EqualTo(new[] { MatchEventKind.Fight }));
        }

        [Test]
        public void PushingTheLastEnemyOffClearsTheWaveAndTheNextOneFollows()
        {
            var match = new Match(1);
            Run(match, Match.CountdownSeconds + 4.9f);
            match.ClearEvents();

            match.EnemyFell(EnemyKind.Grunt);

            Assert.That(Kinds(match), Is.EqualTo(new[] { MatchEventKind.RingOut, MatchEventKind.WaveCleared }));
            Assert.That(match.Events[0].Points, Is.EqualTo(100));
            Assert.That(match.Events[1].Points, Is.EqualTo(50 + 10 * 25), "wave 1 cleared about five seconds into the fight");
            Assert.That(match.Score.Points, Is.EqualTo(400));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Cleared));
            match.ClearEvents();

            Run(match, Match.IntermissionSeconds + 0.1f);

            Assert.That(match.Wave, Is.EqualTo(2));
            Assert.That(match.Alive, Is.EqualTo(2));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(match.Events[0].Kind, Is.EqualTo(MatchEventKind.WaveStarted));
            Assert.That(match.Events[0].Value, Is.EqualTo(2));
        }

        [Test]
        public void ASplitterLeavesOneMoreEnemyToPushOff()
        {
            var match = new Match(1);
            match.EnemySplit();

            Assert.That(match.Alive, Is.EqualTo(2));
            match.EnemyFell(EnemyKind.Mini);
            Assert.That(match.Phase, Is.Not.EqualTo(MatchPhase.Cleared));
            match.EnemyFell(EnemyKind.Mini);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Cleared));
            Assert.That(match.Score.Multiplier, Is.EqualTo(2), "two ring-outs in a row chain");
        }

        [Test]
        public void FallingOffEndsTheRunAndNothingCountsAfterIt()
        {
            var match = new Match(1);
            Run(match, 3f);
            match.ClearEvents();

            match.PlayerFell();
            match.EnemyFell(EnemyKind.Grunt);
            match.PowerUpCollected(PowerUpKind.Rockets);
            Run(match, 5f);

            Assert.That(match.IsOver, Is.True);
            Assert.That(Kinds(match), Is.EqualTo(new[] { MatchEventKind.Over }));
            Assert.That(match.Events[0].Value, Is.EqualTo(1));
            Assert.That(match.Score.Points, Is.Zero);
            Assert.That(match.Act(), Is.EqualTo(ActionResult.None));
            Assert.That(match.Wave, Is.EqualTo(1));
        }

        [Test]
        public void PowerUpsAndTheActionButtonReportWhatTheyDo()
        {
            var match = new Match(1);
            match.PowerUpCollected(PowerUpKind.Smash);
            match.PowerUpCollected(PowerUpKind.Rockets);
            match.ClearEvents();

            Assert.That(match.Act(), Is.EqualTo(ActionResult.Smash));
            match.Step(0.01f);

            Assert.That(Kinds(match), Is.EqualTo(new[] { MatchEventKind.Smash, MatchEventKind.RocketVolley }));
            Assert.That(match.Events[0].Value, Is.EqualTo(2), "two smashes left");
            Assert.That(match.Powers.Smashes, Is.EqualTo(2));
        }

        [Test]
        public void TheShrinkingIslandFollowsTheFightNotTheCountdown()
        {
            var match = new Match(1);
            Run(match, Match.CountdownSeconds + 2f);

            Assert.That(match.WaveTime, Is.EqualTo(2f).Within(0.06f));
            Assert.That(match.Elapsed, Is.EqualTo(Match.CountdownSeconds + 2f).Within(0.06f));
        }

        [Test]
        public void ARunCanOpenOnALaterWaveWithItsPlanAndArena()
        {
            var match = new Match(4, 11);

            Assert.That(match.Wave, Is.EqualTo(11));
            Assert.That(match.Plan.Enemies, Is.EqualTo(WavePlanner.Plan(11, 4).Enemies));
            Assert.That(match.Shape.Layout, Is.EqualTo(ArenaLayout.Islands));
            Assert.That(match.Alive, Is.EqualTo(match.Plan.Enemies.Count));
            Assert.That(new Match(4, 0).Wave, Is.EqualTo(1));
        }

        [Test]
        public void TheSameSeedPlaysTheSameWaves()
        {
            var a = new Match(77);
            var b = new Match(77);
            for (int wave = 1; wave < 8; wave++)
            {
                Assert.That(a.Plan.Enemies, Is.EqualTo(b.Plan.Enemies));
                while (a.Alive > 0)
                {
                    a.EnemyFell(EnemyKind.Grunt);
                    b.EnemyFell(EnemyKind.Grunt);
                }
                Run(a, Match.IntermissionSeconds + 0.1f);
                Run(b, Match.IntermissionSeconds + 0.1f);
            }
            Assert.That(a.Wave, Is.EqualTo(8));
            Assert.That(a.Score.Points, Is.EqualTo(b.Score.Points));
        }
    }
}
