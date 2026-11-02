using System.Collections;
using System.Linq;
using NUnit.Framework;
using RingOut.Core;
using RingOut.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RingOut.Tests
{
    /// <summary>
    /// The game's flow in the real scene: title, starting a run, falling off, clearing a wave,
    /// pause, restart, the knockback power-up, splitters and settings, with what the HUD shows at
    /// each step.
    /// </summary>
    public class RunFlowTests
    {
        GameDirector director;
        VisualElement ui;

        IEnumerator Load()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            director = Object.FindAnyObjectByType<GameDirector>();
            Assert.That(director, Is.Not.Null);
            director.UseMemoryStore();
            director.Seed(1);
            director.HitStopEnabled = false;
            director.ScriptedInput = true;
            ui = director.Hud.GetComponent<UIDocument>().rootVisualElement;
        }

        [TearDown]
        public void RestoreTime() => Time.timeScale = 1f;

        /// <summary>Starts a run and waits until its enemies have landed.</summary>
        IEnumerator StartRun()
        {
            director.Activate("play");
            yield return new WaitForSeconds(Match.CountdownSeconds + 0.4f);
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
        }

        EnemyBall[] Standing => director.Enemies.Where(enemy => enemy != null && !enemy.Gone).ToArray();

        /// <summary>Drops an enemy below the floor, as if it had been pushed over the edge.</summary>
        static void PushOff(EnemyBall enemy)
        {
            Vector3 below = enemy.transform.position + new Vector3(0f, -4f, 0f);
            enemy.Body.position = below;
            enemy.transform.position = below;
        }

        string Text(string name) => ui.Q<Label>(name).text;

        static bool Visible(VisualElement element) => !element.ClassListContains("hidden");

        [UnityTest]
        public IEnumerator TheGameOpensOnTheTitleWithEnemiesRollingBehindIt()
        {
            yield return Load();

            Assert.That(director.Screen, Is.EqualTo(GameScreen.Title));
            Assert.That(director.Match, Is.Null);
            Assert.That(Visible(ui.Q("menu")), Is.True);
            Assert.That(Visible(ui.Q("logo")), Is.True);
            Assert.That(Visible(ui.Q("play-hud")), Is.False);
            Assert.That(director.Hud.Menu.Items.Select(item => item.Id).Take(4), Is.EqualTo(new[] { "play", "howto", "records", "settings" }));
            Assert.That(director.Enemies.Count, Is.EqualTo(4));
            Assert.That(director.Player, Is.Null);
            Assert.That(director.Arena.Layout, Is.EqualTo(ArenaLayout.Disc));
        }

        [UnityTest]
        public IEnumerator PlayStartsWaveOneWithOneEnemyAPowerUpAndTheHud()
        {
            yield return Load();
            yield return StartRun();

            Assert.That(director.Match.Wave, Is.EqualTo(1));
            Assert.That(director.Match.Phase, Is.EqualTo(MatchPhase.Fighting));
            Assert.That(Standing.Length, Is.EqualTo(1));
            Assert.That(Standing[0].Kind, Is.EqualTo(EnemyKind.Grunt));
            Assert.That(Standing[0].Target, Is.EqualTo(director.Player.transform), "it chases the player");
            Assert.That(director.PowerUp, Is.Not.Null);
            Assert.That(director.PowerUp.Kind, Is.EqualTo(PowerUpKind.Knockback));
            Assert.That(director.Player.transform.position.y, Is.GreaterThan(0f), "the player stands on the island");
            Assert.That(Visible(ui.Q("menu")), Is.False);
            Assert.That(Visible(ui.Q("play-hud")), Is.True);
            Assert.That(Text("wave"), Is.EqualTo("WAVE 1"));
            Assert.That(Text("enemies"), Is.EqualTo("1 ENEMY LEFT"));
            Assert.That(Text("arena"), Is.EqualTo("THE DISC"));
            Assert.That(Text("score"), Is.EqualTo("0"));
            Assert.That(Text("action"), Is.EqualTo("DASH"));
            Assert.That(Text("prompt"), Does.Contain("ROLL"));
        }

        [UnityTest]
        public IEnumerator FallingOffEndsTheRunShowsTheResultsAndSavesTheRecords()
        {
            yield return Load();
            yield return StartRun();
            director.Player.Place(new Vector3(0f, -5f, 0f));
            yield return null;
            yield return null;

            Assert.That(director.Match.IsOver, Is.True);
            Assert.That(director.Hud.BannerText, Is.EqualTo("RING OUT!"));
            Assert.That(director.Records.Runs, Is.EqualTo(1));
            Assert.That(director.Records.BestWave, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(2.2f);
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Results));
            Assert.That(Visible(ui.Q("menu")), Is.True);
            Assert.That(ui.Q<Label>("menu-title").text, Is.EqualTo("RING OUT!"));
            Assert.That(ui.Q<Label>("menu-subtitle").text, Is.EqualTo("YOU FELL ON WAVE 1"));
            Assert.That(director.Hud.Menu.Items.Select(item => item.Id), Is.EqualTo(new[] { "again", "menu" }));
            Assert.That(Standing.All(enemy => enemy.Target == null), Is.True, "the enemies stop chasing");
        }

        [UnityTest]
        public IEnumerator PushingTheLastEnemyOffClearsTheWaveAndSendsTheNext()
        {
            yield return Load();
            yield return StartRun();
            PushOff(Standing[0]);
            yield return null;
            yield return null;

            Assert.That(director.Match.Phase, Is.EqualTo(MatchPhase.Cleared));
            Assert.That(director.Match.Score.Points, Is.GreaterThanOrEqualTo(100 + 50));
            Assert.That(Text("score"), Is.EqualTo(director.Match.Score.Points.ToString()));
            Assert.That(Text("enemies"), Is.EqualTo("0 ENEMIES LEFT"));
            Assert.That(director.Hud.BannerText, Is.EqualTo("WAVE CLEAR"));
            Assert.That(director.Hud.FloaterTexts, Has.Member("+100"));

            yield return new WaitForSeconds(Match.IntermissionSeconds + 0.2f);
            Assert.That(director.Match.Wave, Is.EqualTo(2));
            Assert.That(Standing.Length, Is.EqualTo(2));
            Assert.That(Text("wave"), Is.EqualTo("WAVE 2"));
            Assert.That(Text("enemies"), Is.EqualTo("2 ENEMIES LEFT"));
            Assert.That(director.PowerUp, Is.Not.Null, "the unclaimed power-up from wave 1 is still the only one");
            Assert.That(Object.FindObjectsByType<PowerUpPickup>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PauseFreezesTheArenaAndResumeCarriesOn()
        {
            yield return Load();
            yield return StartRun();
            director.TogglePause();
            float elapsed = director.Match.Elapsed;
            Vector3 enemy = Standing[0].transform.position;
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(director.Screen, Is.EqualTo(GameScreen.Paused));
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(director.Match.Elapsed, Is.EqualTo(elapsed));
            Assert.That(Standing[0].transform.position, Is.EqualTo(enemy));
            Assert.That(ui.Q<Label>("menu-title").text, Is.EqualTo("PAUSED"));
            Assert.That(director.Hud.Menu.Items.Select(item => item.Id), Is.EqualTo(new[] { "resume", "restart", "menu" }));

            director.Activate("resume");
            yield return new WaitForSeconds(0.3f);
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Visible(ui.Q("menu")), Is.False);
            Assert.That(director.Match.Elapsed, Is.GreaterThan(elapsed + 0.2f));
        }

        [UnityTest]
        public IEnumerator RestartBeginsAFreshRunOnWaveOne()
        {
            yield return Load();
            yield return StartRun();
            PushOff(Standing[0]);
            yield return new WaitForSeconds(Match.IntermissionSeconds + 0.5f);
            Assert.That(director.Match.Wave, Is.EqualTo(2));
            int score = director.Match.Score.Points;
            Assert.That(score, Is.GreaterThan(0));

            director.TogglePause();
            director.Activate("restart");
            yield return null;
            yield return null;

            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(director.Match.Wave, Is.EqualTo(1));
            Assert.That(director.Match.Score.Points, Is.Zero);
            Assert.That(Text("score"), Is.EqualTo("0"));
            Assert.That(Standing.Length, Is.EqualTo(1), "the old wave's enemies are gone");
            Assert.That(Text("best"), Is.EqualTo("0"), "a run that was restarted is not a record");
        }

        [UnityTest]
        public IEnumerator KnockbackSendsATouchedEnemyFlyingAndShowsItsChip()
        {
            yield return Load();
            yield return StartRun();
            director.Match.PowerUpCollected(PowerUpKind.Knockback);
            Vector3 player = director.Player.transform.position;
            EnemyBall enemy = Standing[0];
            Vector3 start = player + new Vector3(2.5f, 0.1f, 0f);
            enemy.Body.position = start;
            enemy.transform.position = start;
            enemy.Body.linearVelocity = new Vector3(-6f, 0f, 0f);
            yield return new WaitForSeconds(0.5f);

            Assert.That(director.Hud.PowerTexts.First(), Does.StartWith("KNOCKBACK"));
            Assert.That(enemy.transform.position.x - director.Player.transform.position.x, Is.GreaterThan(3f), "it bounced far away");
            Assert.That(enemy.Body.linearVelocity.x, Is.GreaterThan(3f));
        }

        [UnityTest]
        public IEnumerator ASmashSplitsASplitterIntoTwoMinisThatBothCount()
        {
            yield return Load();
            yield return StartRun();
            Vector3 player = director.Player.transform.position;
            EnemyBall splitter = director.PlaceEnemy(EnemyKind.Splitter, player + new Vector3(0f, 0.2f, 3f));
            int alive = director.Match.Alive;
            director.Match.PowerUpCollected(PowerUpKind.Smash);

            Assert.That(director.Act(), Is.EqualTo(ActionResult.Smash));
            float waited = 0f;
            while (director.Player.Smashing && waited < 2f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            yield return null;
            yield return null;

            Assert.That(splitter == null || splitter.Gone, Is.True);
            Assert.That(director.Enemies.Count(enemy => enemy != null && enemy.Kind == EnemyKind.Mini), Is.EqualTo(2));
            Assert.That(director.Match.Alive, Is.EqualTo(alive + 1));
            Assert.That(director.Match.Powers.Smashes, Is.EqualTo(PlayerPowers.SmashCharges - 1));
            Assert.That(Text("action"), Does.StartWith("SMASH"));
        }

        [UnityTest]
        public IEnumerator SettingsChangeTheirRowAndAreSaved()
        {
            yield return Load();
            director.Activate("settings");
            yield return null;
            director.Change("volume", -1);
            director.Change("shake", 1);
            director.Change("camera", 1);

            Assert.That(director.Screen, Is.EqualTo(GameScreen.Settings));
            Assert.That(director.Settings.Volume, Is.EqualTo(7));
            Assert.That(director.Settings.ScreenShake, Is.False);
            Assert.That(director.Settings.CameraSpeed, Is.EqualTo(4));
            Assert.That(director.CameraRig.ShakeScale, Is.EqualTo(0f));
            MenuItem volume = director.Hud.Menu.Items.First(item => item.Id == "volume");
            Assert.That(volume.Value, Is.EqualTo("7"));

            director.Activate("back");
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Title));
            Assert.That(director.Hud.Menu.Items[director.Hud.Menu.Selected].Id, Is.EqualTo("settings"));
        }
    }
}
