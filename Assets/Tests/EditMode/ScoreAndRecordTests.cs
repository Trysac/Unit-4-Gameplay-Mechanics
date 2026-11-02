using NUnit.Framework;

namespace RingOut.Core.Tests
{
    public class ScoreAndRecordTests
    {
        [Test]
        public void RingOutsInQuickSuccessionMultiplyUpToFive()
        {
            var score = new ScoreBoard();
            var earned = new int[6];
            for (int i = 0; i < 6; i++)
            {
                earned[i] = score.RingOut(EnemyKind.Grunt);
                score.Step(1f);
            }

            Assert.That(earned, Is.EqualTo(new[] { 100, 200, 300, 400, 500, 500 }));
            Assert.That(score.Points, Is.EqualTo(2000));
            Assert.That(score.Chain, Is.EqualTo(6));
            Assert.That(score.BestChain, Is.EqualTo(6));
            Assert.That(score.Multiplier, Is.EqualTo(5));
            Assert.That(score.RingOuts, Is.EqualTo(6));
        }

        [Test]
        public void AChainLapsesAfterThreeQuietSeconds()
        {
            var score = new ScoreBoard();
            score.RingOut(EnemyKind.Heavy);
            score.Step(1f);
            Assert.That(score.ChainLeft, Is.EqualTo(2f).Within(1e-4f));
            score.Step(2.5f);

            Assert.That(score.Chain, Is.Zero);
            Assert.That(score.Multiplier, Is.EqualTo(1));
            Assert.That(score.ChainLeft, Is.Zero);
            Assert.That(score.RingOut(EnemyKind.Heavy), Is.EqualTo(250));
            Assert.That(score.BestChain, Is.EqualTo(1));
        }

        [Test]
        public void TheBossIsWorthAThousand()
        {
            Assert.That(new ScoreBoard().RingOut(EnemyKind.Boss), Is.EqualTo(1000));
        }

        [TestCase(1, 10f, 50 + 200)]
        [TestCase(4, 29.5f, 200)]
        [TestCase(6, 45f, 300)]
        public void ClearingAWaveEarnsItsNumberAndTheSecondsUnderPar(int wave, float seconds, int bonus)
        {
            var score = new ScoreBoard();

            Assert.That(score.WaveCleared(wave, seconds), Is.EqualTo(bonus));
            Assert.That(score.Points, Is.EqualTo(bonus));
        }

        [Test]
        public void RecordsKeepTheBestScoreAndWaveAndCountRuns()
        {
            var store = new MemoryRecordStore();
            var records = new Records(store);

            RecordUpdate first = records.Submit(1200, 4, 3, 9);
            RecordUpdate second = records.Submit(800, 6, 2, 5);
            RecordUpdate third = records.Submit(1500, 2, 5, 4);

            Assert.That(first.NewBestScore && first.NewBestWave, Is.True);
            Assert.That(second.NewBestScore, Is.False);
            Assert.That(second.NewBestWave, Is.True);
            Assert.That(third.NewBestScore, Is.True);
            Assert.That(third.NewBestWave, Is.False);
            Assert.That(third.PreviousBestScore, Is.EqualTo(1200));
            Assert.That(records.BestScore, Is.EqualTo(1500));
            Assert.That(records.BestWave, Is.EqualTo(6));
            Assert.That(records.BestChain, Is.EqualTo(5));
            Assert.That(records.Runs, Is.EqualTo(3));
            Assert.That(records.RingOuts, Is.EqualTo(18));
            Assert.That(store.SaveCount, Is.EqualTo(3), "saved once per run");
            Assert.That(new Records(store).BestScore, Is.EqualTo(1500), "a new session reads them back");
        }

        [Test]
        public void SettingsSurviveARestartAndBrokenValuesFallBackOrClamp()
        {
            var store = new MemoryRecordStore();
            var settings = new GameSettings { Volume = 4, Muted = true, ScreenShake = false, CameraSpeed = 5, TouchControls = false };
            settings.Save(store);

            GameSettings loaded = GameSettings.Load(store);
            Assert.That(loaded.Volume, Is.EqualTo(4));
            Assert.That(loaded.Muted, Is.True);
            Assert.That(loaded.ScreenShake, Is.False);
            Assert.That(loaded.CameraSpeed, Is.EqualTo(5));
            Assert.That(loaded.TouchControls, Is.False);

            store.SetInt("ringout.settings.volume", 99);
            store.SetInt("ringout.settings.camera", -3);
            loaded = GameSettings.Load(store);
            Assert.That(loaded.Volume, Is.EqualTo(GameSettings.MaxVolume));
            Assert.That(loaded.CameraSpeed, Is.EqualTo(1));

            GameSettings fresh = GameSettings.Load(new MemoryRecordStore());
            Assert.That(fresh.Volume, Is.EqualTo(8));
            Assert.That(fresh.ScreenShake && fresh.TouchControls && !fresh.Muted, Is.True);
        }

        [Test]
        public void SettingSteppersStayInRange()
        {
            var settings = new GameSettings();
            for (int i = 0; i < 20; i++)
            {
                settings.StepVolume(1);
                settings.StepCameraSpeed(-1);
            }

            Assert.That(settings.Volume, Is.EqualTo(10));
            Assert.That(settings.CameraSpeed, Is.EqualTo(1));
        }
    }
}
