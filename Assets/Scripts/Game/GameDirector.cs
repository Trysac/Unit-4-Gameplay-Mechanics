using System.Collections.Generic;
using RingOut.Core;
using UnityEngine;

namespace RingOut.Game
{
    public enum GameScreen
    {
        Title,
        HowToPlay,
        Records,
        Settings,
        Playing,
        Paused,
        Results,
    }

    /// <summary>
    /// Runs the game: the menus over an island where a few enemies roll around, and the runs
    /// themselves. It spawns the balls the engine-free <see cref="Match"/> asks for, reports what
    /// falls, splits and is picked up, applies the power-ups' impulses, and turns the match's
    /// events into sound, particles, shake, hit-stop and floating points.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour, IArenaEvents
    {
        const float FallHeight = -2.5f;
        const float ResultsDelay = 1.8f;
        const float PromptSeconds = 6f;
        const float HitStopScale = 0.05f;
        const int DemoEnemies = 4;

        [SerializeField] GameArt art;
        [SerializeField] Hud hud;
        [SerializeField] CameraRig cameraRig;

        ArenaView arena;
        AudioDirector sound;
        readonly GameInput input = new GameInput();
        readonly List<EnemyBall> enemies = new List<EnemyBall>();
        readonly List<Rocket> rockets = new List<Rocket>();
        IRecordStore store;
        Records records;
        GameSettings settings;
        Rng rng;
        int seed;
        Transform demoTarget;
        float hitStop;
        float overTime;
        float bumpCooldown;
        RecordUpdate lastRecord;

        public GameScreen Screen { get; private set; }
        public Match Match { get; private set; }
        public PlayerBall Player { get; private set; }
        public PowerUpPickup PowerUp { get; private set; }
        public IReadOnlyList<EnemyBall> Enemies => enemies;
        public Records Records => records;
        public GameSettings Settings => settings;
        public Hud Hud => hud;
        public ArenaView Arena => arena;
        public CameraRig CameraRig => cameraRig;
        public RecordUpdate LastRecord => lastRecord;

        /// <summary>Tests and the screenshot hook drive the game instead of the devices.</summary>
        public bool ScriptedInput { get; set; }

        /// <summary>Where scripted input rolls the player (ground plane, length up to 1).</summary>
        public Vector3 ScriptedMove { get; set; }

        /// <summary>Slows time for hit-stop; tests turn it off to keep timings exact.</summary>
        public bool HitStopEnabled { get; set; } = true;

        void Awake()
        {
            seed = System.Environment.TickCount;
            store = new PlayerPrefsStore();
            sound = gameObject.AddComponent<AudioDirector>();
            LoadOptions();
            sound.Init(settings.Volume / 10f, settings.Muted);
            arena = new GameObject("Arena").AddComponent<ArenaView>();
            arena.Init(art);
            demoTarget = new GameObject("Demo Target").transform;
            hud.ItemClicked += OnItemClicked;
            hud.PauseClicked += () =>
            {
                if (Screen == GameScreen.Playing)
                {
                    TogglePause();
                }
            };
        }

        void Start()
        {
            ToTitle();
            UpdateSoundLabel();
        }

        void LoadOptions()
        {
            records = new Records(store);
            settings = GameSettings.Load(store);
            ApplyOptions();
        }

        void ApplyOptions()
        {
            cameraRig.ShakeScale = settings.ScreenShake ? 1f : 0f;
            cameraRig.TurnSpeed = 50f + 30f * settings.CameraSpeed;
            sound.SetVolume(settings.Volume / 10f);
        }

        /// <summary>Keeps records and settings in memory only (tests and screenshots).</summary>
        public void UseMemoryStore()
        {
            store = new MemoryRecordStore();
            LoadOptions();
        }

        /// <summary>Makes the next runs repeatable.</summary>
        public void Seed(int value) => seed = value;

        // ---- Flow ----

        public void ToTitle(int selected = 0)
        {
            Screen = GameScreen.Title;
            Time.timeScale = 1f;
            Match = null;
            ClearArena();
            arena.Build(ArenaShape.Of(ArenaLayout.Disc));
            StartDemo();
            hud.ShowPlayHud(false);
            hud.HideBanner();
            hud.ClearFloaters();
            sound.PlayMusic(art.TitleMusic);
            sound.SetDucked(false);
            sound.MusicPitch = 1f;
            ShowTitleMenu(selected);
        }

        /// <summary>The title backdrop: a few enemies chasing a point that circles the island.</summary>
        void StartDemo()
        {
            sound.Silent = true;
            rng = new Rng(seed);
            cameraRig.Follow = null;
            cameraRig.ArenaCenter = Vector3.zero;
            cameraRig.Drift = 9f;
            cameraRig.Snap();
            for (int i = 0; i < DemoEnemies; i++)
            {
                SpawnDemoEnemy(i);
            }
        }

        void SpawnDemoEnemy(int index)
        {
            EnemyKind kind = (EnemyKind)(index % 4);
            Vec3 at = arena.Shape.SpawnPoint(rng, Vec3.Zero, 2f, 1.2f);
            EnemyBall enemy = EnemyBall.Create(kind, 1, new Vector3(at.X, 3f + index, at.Z), art, this);
            enemy.Target = demoTarget;
            enemies.Add(enemy);
        }

        /// <summary>Starts a run on wave one (tests and screenshots may open on a later wave).</summary>
        public void StartRun(int firstWave = 1)
        {
            Screen = GameScreen.Playing;
            Time.timeScale = 1f;
            sound.Silent = false;
            ClearArena();
            Match = new Match(seed++, firstWave);
            rng = new Rng(Match.Seed);
            overTime = 0f;
            hitStop = 0f;
            lastRecord = default;
            Player = PlayerBall.Create(art, this);
            arena.Build(Match.Shape);
            Player.Place(Ball.ToUnity(Match.Shape.PlayerStart) + Vector3.up * PlayerBall.Radius);
            cameraRig.Follow = Player.transform;
            cameraRig.ArenaCenter = ArenaCenter(Match.Shape);
            cameraRig.Drift = 0f;
            cameraRig.Yaw = 0f;
            cameraRig.Calm();
            cameraRig.Snap();
            hud.HideMenu();
            hud.HideBanner();
            hud.ShowPlayHud(true);
            hud.ResetRun(records.BestScore);
            sound.PlayMusic(art.BattleMusic);
            sound.SetDucked(false);
            sound.MusicPitch = 1f;
            React();
        }

        public void Restart() => StartRun();

        public void TogglePause()
        {
            if (Screen == GameScreen.Playing)
            {
                Screen = GameScreen.Paused;
                Time.timeScale = 0f;
                sound.SetDucked(true);
                sound.PlayUi(art.Pause);
                ShowPauseMenu(0);
            }
            else if (Screen == GameScreen.Paused)
            {
                Screen = GameScreen.Playing;
                Time.timeScale = 1f;
                sound.SetDucked(false);
                sound.PlayUi(art.Pause);
                hud.HideMenu();
            }
        }

        void ShowResults()
        {
            Screen = GameScreen.Results;
            sound.SetDucked(true);
            ShowResultsMenu(0);
        }

        void ClearArena()
        {
            foreach (EnemyBall enemy in enemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            enemies.Clear();
            foreach (Rocket rocket in rockets)
            {
                if (rocket != null)
                {
                    Destroy(rocket.gameObject);
                }
            }
            rockets.Clear();
            if (PowerUp != null)
            {
                Destroy(PowerUp.gameObject);
                PowerUp = null;
            }
            if (Player != null)
            {
                Destroy(Player.gameObject);
                Player = null;
            }
        }

        static Vector3 ArenaCenter(ArenaShape shape) => Vector3.zero;

        // ---- Frame ----

        void Update()
        {
            if (!ScriptedInput && input.Mute)
            {
                settings.Muted = !settings.Muted;
                sound.ToggleMute();
                settings.Save(store);
                UpdateSoundLabel();
            }
            if (!ScriptedInput)
            {
                input.Read();
            }

            switch (Screen)
            {
                case GameScreen.Playing:
                    if (!ScriptedInput && input.Pause)
                    {
                        TogglePause();
                        break;
                    }
                    PlayFrame();
                    break;
                case GameScreen.Paused:
                    if (!ScriptedInput)
                    {
                        MenuInput();
                    }
                    break;
                case GameScreen.Results:
                    if (!ScriptedInput)
                    {
                        MenuInput();
                    }
                    PlayFrame();
                    break;
                default:
                    if (!ScriptedInput)
                    {
                        MenuInput();
                    }
                    DemoFrame();
                    break;
            }

            bool touch = settings.TouchControls && input.Source == InputSource.Touch && Screen == GameScreen.Playing;
            hud.SetTouch(touch, input.StickOrigin, input.StickKnob);
        }

        void DemoFrame()
        {
            float t = Time.time * 0.6f;
            demoTarget.position = new Vector3(Mathf.Cos(t) * 4.5f, 0.5f, Mathf.Sin(t * 1.3f) * 4.5f);
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyBall enemy = enemies[i];
                if (enemy.transform.position.y < -8f)
                {
                    Destroy(enemy.gameObject);
                    enemies.RemoveAt(i);
                    SpawnDemoEnemy(i);
                }
            }
        }

        void PlayFrame()
        {
            if (Match == null)
            {
                return;
            }
            // Hit-stop runs on real time and slows everything, physics included.
            if (hitStop > 0f)
            {
                hitStop -= Time.unscaledDeltaTime;
            }
            if (Screen == GameScreen.Playing)
            {
                Time.timeScale = hitStop > 0f ? HitStopScale : 1f;
            }
            float dt = Time.deltaTime;
            bumpCooldown -= Time.unscaledDeltaTime;

            if (!Match.IsOver)
            {
                Steer(dt);
                Match.Step(dt);
                arena.Sync(Match.WaveTime, dt);
                CheckFalls();
            }
            else
            {
                if (Player != null)
                {
                    Player.MoveInput = Vector3.zero;
                }
                overTime += Time.unscaledDeltaTime;
                if (Screen == GameScreen.Playing && overTime >= ResultsDelay)
                {
                    ShowResults();
                }
            }
            React();
            CleanUp();
            if (Screen == GameScreen.Playing)
            {
                UpdateHud();
            }
        }

        void Steer(float dt)
        {
            Vector2 move = ScriptedInput ? new Vector2(ScriptedMove.x, ScriptedMove.z) : input.Move;
            if (!ScriptedInput)
            {
                cameraRig.Turn(input.Orbit, Time.unscaledDeltaTime);
            }
            Vector3 world = ScriptedInput ? ScriptedMove : cameraRig.Right * move.x + cameraRig.Forward * move.y;
            Player.MoveInput = Vector3.ClampMagnitude(world, 1f);
            if (!ScriptedInput && input.Action)
            {
                Act();
            }
        }

        /// <summary>The action button: a smash while charges last, otherwise a dash.</summary>
        public ActionResult Act()
        {
            if (Match == null || Match.IsOver || Player == null || Player.Smashing)
            {
                return ActionResult.None;
            }
            ActionResult result = Match.Act();
            if (result == ActionResult.Dash)
            {
                Vector3 direction = Player.MoveInput;
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = Player.Body.linearVelocity;
                    direction.y = 0f;
                }
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = cameraRig.Forward;
                }
                Player.Dash(direction);
            }
            else if (result == ActionResult.Smash)
            {
                Player.StartSmash();
            }
            return result;
        }

        void CheckFalls()
        {
            foreach (EnemyBall enemy in enemies)
            {
                if (enemy == null || enemy.Gone || enemy.transform.position.y > FallHeight)
                {
                    continue;
                }
                enemy.Gone = true;
                enemy.Target = null;
                Match.EnemyFell(enemy.Kind);
                Vector3 at = enemy.transform.position;
                Effects.Burst(at, Vector3.up, art.Glow, Palette.Of(enemy.Kind), Color.white, 26, 9f, 0.5f, 0.8f, 30f, 0.4f);
            }
            if (Player != null && Player.transform.position.y < FallHeight)
            {
                Match.PlayerFell();
            }
        }

        /// <summary>Removes balls that have fallen far out of sight.</summary>
        void CleanUp()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                }
                else if (enemies[i].transform.position.y < -40f)
                {
                    Destroy(enemies[i].gameObject);
                    enemies.RemoveAt(i);
                }
            }
            rockets.RemoveAll(rocket => rocket == null);
        }

        // ---- Spawning ----

        void SpawnWave()
        {
            WavePlan plan = Match.Plan;
            if (arena.Layout != plan.Layout)
            {
                arena.Build(Match.Shape);
                cameraRig.ArenaCenter = ArenaCenter(Match.Shape);
                Player.Place(Ball.ToUnity(Match.Shape.PlayerStart) + Vector3.up * 3f);
                if (PowerUp != null)
                {
                    Destroy(PowerUp.gameObject);
                    PowerUp = null;
                }
            }
            Vec3 player = Ball.ToCore(Player.transform.position);
            for (int i = 0; i < plan.Enemies.Count; i++)
            {
                EnemyKind kind = plan.Enemies[i];
                float radius = EnemyCatalog.Base(kind).Radius;
                Vec3 at = Match.Shape.SpawnPoint(rng, player, 7f, radius + 0.4f);
                SpawnEnemy(kind, new Vector3(at.X, 5f + i * 1.2f, at.Z), Vector3.zero);
            }
            // One power-up waits at a time: unclaimed ones do not pile up.
            if (PowerUp == null)
            {
                Vec3 at = Match.Shape.SpawnPoint(rng, player, 3.5f, 1f);
                PowerUp = PowerUpPickup.Create(plan.PowerUp, new Vector3(at.X, 1f, at.Z), art, this);
            }
        }

        EnemyBall SpawnEnemy(EnemyKind kind, Vector3 position, Vector3 velocity)
        {
            EnemyBall enemy = EnemyBall.Create(kind, Match.Wave, position, art, this);
            enemy.Body.linearVelocity = velocity;
            enemy.Target = Player.transform;
            enemies.Add(enemy);
            return enemy;
        }

        /// <summary>Spawns an enemy at a point (tests and screenshots); it does not count for the wave.</summary>
        public EnemyBall PlaceEnemy(EnemyKind kind, Vector3 position) => SpawnEnemy(kind, position, Vector3.zero);

        /// <summary>Breaks a splitter into two minis that fly apart.</summary>
        void Split(EnemyBall splitter, Vector3 away)
        {
            if (splitter.Kind != EnemyKind.Splitter || splitter.Gone || Match == null || Match.IsOver)
            {
                return;
            }
            splitter.Gone = true;
            Vector3 position = splitter.transform.position;
            Vector3 velocity = splitter.Body.linearVelocity;
            Vector3 side = Vector3.Cross(Vector3.up, away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward);
            for (int s = -1; s <= 1; s += 2)
            {
                SpawnEnemy(EnemyKind.Mini, position + side * (0.55f * s), velocity + side * (4f * s) + away.normalized * 2f);
            }
            Destroy(splitter.gameObject);
            Match.EnemySplit();
        }

        // ---- Arena events ----

        public void PlayerHit(EnemyBall enemy, Collision collision)
        {
            if (Match == null || Match.IsOver || enemy.Gone)
            {
                return;
            }
            Vector3 contact = collision.contactCount > 0 ? collision.GetContact(0).point : enemy.transform.position;
            Vector3 away = enemy.transform.position - Player.transform.position;
            if (Match.Powers.HasKnockback && Time.time - enemy.LastKnocked > 0.4f)
            {
                enemy.LastKnocked = Time.time;
                Vec3 impulse = Push.Knockback(Ball.ToCore(Player.transform.position), Ball.ToCore(enemy.transform.position), Push.KnockbackStrength);
                enemy.Body.AddForce(Ball.ToUnity(impulse), ForceMode.Impulse);
                sound.Play(art.Knock, 0.9f);
                Effects.Burst(contact, away, art.Glow, Palette.Knockback, Color.white, 34, 11f, 0.45f, 0.5f, 80f);
                cameraRig.Shake(0.35f);
                cameraRig.Kick(away, 0.25f);
                Freeze(0.07f);
                Split(enemy, away);
                return;
            }
            float strength = collision.relativeVelocity.magnitude;
            Bump(contact, strength);
            if (collision.impulse.magnitude > EnemyCatalog.SplitImpulse)
            {
                Split(enemy, away);
            }
        }

        public void EnemiesCollided(EnemyBall enemy, Collision collision)
        {
            if (Screen != GameScreen.Playing || collision.relativeVelocity.magnitude < 6f)
            {
                return;
            }
            Vector3 contact = collision.contactCount > 0 ? collision.GetContact(0).point : enemy.transform.position;
            Bump(contact, collision.relativeVelocity.magnitude * 0.6f);
        }

        void Bump(Vector3 contact, float strength)
        {
            if (strength < 1.5f || bumpCooldown > 0f)
            {
                return;
            }
            bumpCooldown = 0.06f;
            float amount = Mathf.Clamp01(strength / 12f);
            bool hard = strength > 7f;
            sound.Play(hard ? art.Hit : art.Bump, 0.35f + 0.6f * amount, 0.9f + 0.3f * amount);
            Effects.Burst(contact, Vector3.zero, art.Glow, Palette.Dust, Color.white, (int)(6 + 18 * amount), 3f + 6f * amount, 0.3f, 0.35f);
            cameraRig.Shake(0.05f + 0.2f * amount);
            if (hard)
            {
                Freeze(0.035f);
            }
        }

        public void SmashLanded(Vector3 position)
        {
            if (Match == null || Match.IsOver)
            {
                return;
            }
            Vec3 center = Ball.ToCore(position);
            foreach (EnemyBall enemy in enemies.ToArray())
            {
                if (enemy == null || enemy.Gone)
                {
                    continue;
                }
                Vec3 impulse = Push.Shockwave(center, Ball.ToCore(enemy.transform.position), Push.SmashRadius, Push.SmashStrength);
                if (impulse.Length > 0f)
                {
                    enemy.Body.AddForce(Ball.ToUnity(impulse), ForceMode.Impulse);
                    Split(enemy, enemy.transform.position - position);
                }
            }
            Vector3 floor = new Vector3(position.x, 0.05f, position.z);
            sound.Play(art.Smash, 1f, 1f, 0f);
            Effects.Shockwave(floor, Push.SmashRadius, art.Ring, Palette.Smash);
            Effects.Burst(floor, Vector3.up, art.Glow, Palette.Smash, Palette.Dust, 60, 10f, 0.55f, 0.8f, 160f, 1f);
            cameraRig.Shake(0.8f);
            Freeze(0.12f);
        }

        public void PickedUp(PowerUpPickup pickup)
        {
            if (Match == null || Match.IsOver || pickup != PowerUp)
            {
                return;
            }
            Match.PowerUpCollected(pickup.Kind);
            Destroy(pickup.gameObject);
            PowerUp = null;
        }

        public void BossWindUp(EnemyBall boss)
        {
            if (Screen == GameScreen.Playing)
            {
                sound.Play(art.BossCharge, 0.8f);
            }
        }

        void RocketHit(Rocket rocket, EnemyBall enemy)
        {
            if (enemy == null || enemy.Gone || Match == null || Match.IsOver)
            {
                return;
            }
            Vec3 impulse = Push.Knockback(Ball.ToCore(rocket.transform.position - rocket.Velocity.normalized), Ball.ToCore(enemy.transform.position), Push.RocketStrength);
            enemy.Body.AddForce(Ball.ToUnity(impulse), ForceMode.Impulse);
            sound.Play(art.RocketHit, 0.7f);
            Effects.Burst(rocket.transform.position, Vector3.zero, art.Glow, Palette.Rockets, Palette.Gold, 24, 7f, 0.4f, 0.45f);
            cameraRig.Shake(0.15f);
            Split(enemy, rocket.Velocity);
        }

        void FireVolley()
        {
            if (Player == null)
            {
                return;
            }
            var targets = new List<EnemyBall>();
            foreach (EnemyBall enemy in enemies)
            {
                if (enemy != null && !enemy.Gone)
                {
                    targets.Add(enemy);
                }
            }
            targets.Sort((a, b) =>
                (a.transform.position - Player.transform.position).sqrMagnitude.CompareTo((b.transform.position - Player.transform.position).sqrMagnitude));
            int count = Mathf.Min(targets.Count, 4);
            for (int i = 0; i < count; i++)
            {
                float angle = (i - (count - 1) * 0.5f) * 35f;
                Vector3 toward = targets[i].transform.position - Player.transform.position;
                toward.y = 0f;
                Vector3 launch = Quaternion.Euler(0f, angle, 0f) * (toward.normalized + Vector3.up * 1.6f);
                Rocket rocket = Rocket.Launch(Player.transform.position + Vector3.up * 0.9f, launch, targets[i], art);
                rocket.Hit = RocketHit;
                rockets.Add(rocket);
            }
            if (count > 0)
            {
                sound.Play(art.RocketLaunch, 0.7f);
            }
        }

        void Freeze(float seconds)
        {
            if (HitStopEnabled)
            {
                hitStop = Mathf.Max(hitStop, seconds);
            }
        }

        // ---- Match events ----

        void React()
        {
            if (Match == null)
            {
                return;
            }
            foreach (MatchEvent e in Match.Events)
            {
                switch (e.Kind)
                {
                    case MatchEventKind.WaveStarted:
                        SpawnWave();
                        if (Match.Plan.Boss)
                        {
                            sound.Play(art.BossWave, 0.9f, 1f, 0f);
                            hud.Banner($"WAVE {e.Value}", "BOSS WAVE", "boss", 1500);
                        }
                        else
                        {
                            sound.Play(art.WaveStart, 0.7f, 1f, 0f);
                            hud.Banner($"WAVE {e.Value}", ArenaName(Match.Plan.Layout), null, 1400);
                        }
                        break;
                    case MatchEventKind.Fight:
                        sound.Play(art.Fight, 0.7f, 1f, 0f);
                        break;
                    case MatchEventKind.RingOut:
                        OnRingOut(e);
                        break;
                    case MatchEventKind.Split:
                        sound.Play(art.Split, 0.8f);
                        break;
                    case MatchEventKind.WaveCleared:
                        sound.Play(art.WaveClear, 0.8f, 1f, 0f);
                        hud.Banner("WAVE CLEAR", $"+{e.Points} BONUS", "good", 1600);
                        foreach (Rocket rocket in rockets)
                        {
                            if (rocket != null)
                            {
                                Destroy(rocket.gameObject);
                            }
                        }
                        break;
                    case MatchEventKind.PowerUp:
                        sound.Play(art.PowerUp, 0.9f, 1f, 0f);
                        hud.Banner(PowerName(e.Power), PowerHint(e.Power), "good", 900);
                        Effects.Burst(Player.transform.position, Vector3.up, art.Glow, Palette.Of(e.Power), Color.white, 40, 7f, 0.4f, 0.7f, 120f);
                        cameraRig.Shake(0.15f);
                        break;
                    case MatchEventKind.PowerEnded:
                        sound.Play(art.PowerEnd, 0.7f);
                        break;
                    case MatchEventKind.RocketVolley:
                        FireVolley();
                        break;
                    case MatchEventKind.Dash:
                        sound.Play(art.Dash, 0.7f);
                        Effects.Burst(Player.transform.position, -Player.Body.linearVelocity, art.Glow, Palette.PlayerStripe, Color.white, 16, 5f, 0.35f, 0.35f, 40f, 0f);
                        break;
                    case MatchEventKind.Smash:
                        sound.Play(art.Dash, 0.6f, 0.7f);
                        break;
                    case MatchEventKind.Over:
                        OnOver();
                        break;
                }
            }
            Match.ClearEvents();
        }

        void OnRingOut(MatchEvent e)
        {
            bool boss = e.Enemy == EnemyKind.Boss;
            sound.Play(art.RingOut, 0.8f, 1f + 0.05f * (e.Value - 1));
            if (e.Value >= 2)
            {
                sound.Play(art.Chain, 0.6f, 1f + 0.06f * e.Value);
            }
            Vector3 at = Player != null ? Player.transform.position : Vector3.zero;
            EnemyBall fallen = null;
            foreach (EnemyBall enemy in enemies)
            {
                if (enemy != null && enemy.Gone && enemy.Kind == e.Enemy && enemy.transform.position.y < FallHeight + 0.5f)
                {
                    fallen = enemy;
                }
            }
            if (fallen != null)
            {
                at = fallen.transform.position;
                at.y = 0.5f;
            }
            string text = e.Value >= 2 ? $"+{e.Points}  x{e.Value}" : $"+{e.Points}";
            hud.Float(at, text, e.Value >= 2 ? Palette.Gold : Color.white, 24f + 3f * Mathf.Min(e.Value, 5) + (boss ? 14f : 0f));
            cameraRig.Shake(boss ? 0.9f : 0.12f + 0.04f * e.Value);
            if (boss)
            {
                Freeze(0.25f);
                hud.Banner("BOSS DOWN", $"+{e.Points}", "boss", 1500);
            }
        }

        void OnOver()
        {
            // The enemies stop chasing and watch the player fall.
            foreach (EnemyBall enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.Target = null;
                }
            }
            sound.MusicPitch = 1f;
            sound.PlayMusic(null);
            sound.Play(art.Fall, 0.9f, 1f, 0f);
            lastRecord = records.Submit(Match.Score.Points, Match.Wave, Match.Score.BestChain, Match.Score.RingOuts);
            hud.Banner("RING OUT!", $"YOU FELL ON WAVE {Match.Wave}", "danger", 1700);
            if (lastRecord.NewBestScore)
            {
                sound.Play(art.NewRecord, 0.9f, 1f, 0f);
                hud.SetBest(Match.Score.Points);
            }
            else
            {
                sound.Play(art.GameOver, 0.7f, 1f, 0f);
            }
            cameraRig.Shake(0.5f);
        }

        void UpdateHud()
        {
            if (Match == null)
            {
                return;
            }
            ScoreBoard score = Match.Score;
            hud.SetScore(score.Points);
            hud.SetChain(score.Chain, score.ChainLeft / ScoreBoard.ChainWindow);
            hud.SetWave(Match.Wave, Match.Alive, ArenaName(Match.Plan.Layout), Match.Plan.Boss);
            PlayerPowers powers = Match.Powers;
            hud.SetPowers(powers.KnockbackLeft, powers.RocketsLeft, powers.Smashes);
            float cooldown = powers.Smashes > 0 ? PlayerPowers.SmashCooldown : PlayerPowers.DashCooldown;
            hud.SetAction(powers.Smashes > 0 ? "SMASH" : "DASH", powers.Cooldown <= 0f ? 1f : 1f - powers.Cooldown / cooldown);
            hud.SetPauseButton(true);
            if (Player != null)
            {
                Player.SetPowers(powers.HasKnockback, powers.HasRockets, powers.Smashes);
            }
            string prompt = null;
            if (Match.Wave == 1 && Match.Elapsed < PromptSeconds)
            {
                prompt = input.Source switch
                {
                    InputSource.Gamepad => "LEFT STICK TO ROLL  -  RIGHT STICK TURNS THE CAMERA  -  A TO DASH",
                    InputSource.Touch => "DRAG ON THE LEFT TO ROLL  -  TAP ON THE RIGHT TO DASH",
                    _ => "WASD OR ARROWS TO ROLL  -  Q / E TURN THE CAMERA  -  SPACE TO DASH",
                };
            }
            hud.SetPrompt(prompt);
        }

        // ---- Menus ----

        void ShowTitleMenu(int selected)
        {
            var menu = new MenuModel
            {
                Logo = true,
                Subtitle = "PUSH EVERY ENEMY OFF THE ISLAND  -  DO NOT FALL",
                Selected = selected,
                LightDim = true,
                Hint = MenuHint(),
            };
            menu.Add("play", "PLAY");
            menu.Add("howto", "HOW TO PLAY");
            menu.Add("records", "RECORDS");
            menu.Add("settings", "SETTINGS");
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                menu.Add("quit", "QUIT");
            }
            hud.ShowMenu(menu);
        }

        void ShowHowToPlay()
        {
            Screen = GameScreen.HowToPlay;
            var menu = new MenuModel { Title = "HOW TO PLAY", Subtitle = "ROLL INTO THEM, KEEP YOUR BALANCE", Hint = MenuHint() };
            menu.Body.Add(("GRUNT / FAST / HEAVY", "EVEN / QUICK / 3x YOUR WEIGHT", false));
            menu.Body.Add(("SPLITTER", "BREAKS INTO TWO ON A HARD HIT", false));
            menu.Body.Add(("BOSS, EVERY 5TH WAVE", "HUGE, AND CHARGES WHEN IT GLOWS", false));
            menu.Body.Add(("KNOCKBACK  (ORANGE)", "ANYTHING YOU TOUCH FLIES, 7 S", false));
            menu.Body.Add(("SMASH  (PINK)", "3 LEAPS THAT SLAM A SHOCKWAVE", false));
            menu.Body.Add(("ROCKETS  (CYAN)", "HOMING VOLLEYS FOR 6 S", false));
            menu.Body.Add(("DASH", "A BURST OF SPEED, 1.2 S TO RECHARGE", false));
            menu.Body.Add(("RING-OUTS WITHIN 3 S", "CHAIN UP TO x5", false));
            menu.Body.Add(("ARENAS CHANGE EVERY 5 WAVES", "DISC, RING, ISLANDS, SHRINKING", false));
            menu.Add("back", "BACK");
            hud.ShowMenu(menu);
        }

        void ShowRecordsMenu()
        {
            Screen = GameScreen.Records;
            var menu = new MenuModel { Title = "RECORDS", Subtitle = "ON THIS DEVICE", Hint = MenuHint() };
            menu.Body.Add(("BEST SCORE", records.BestScore.ToString(), false));
            menu.Body.Add(("BEST WAVE", records.BestWave.ToString(), false));
            menu.Body.Add(("LONGEST CHAIN", records.BestChain.ToString(), false));
            menu.Body.Add(("ENEMIES PUSHED OFF", records.RingOuts.ToString(), false));
            menu.Body.Add(("RUNS PLAYED", records.Runs.ToString(), false));
            menu.Add("back", "BACK");
            hud.ShowMenu(menu);
        }

        void ShowSettingsMenu(int selected)
        {
            Screen = GameScreen.Settings;
            var menu = new MenuModel { Title = "SETTINGS", Selected = selected, Hint = MenuHint(withChange: true) };
            menu.Add("volume", "VOLUME", settings.Volume.ToString());
            menu.Add("mute", "SOUND", settings.Muted ? "OFF" : "ON");
            menu.Add("shake", "SCREEN SHAKE", settings.ScreenShake ? "ON" : "OFF");
            menu.Add("camera", "CAMERA TURN SPEED", settings.CameraSpeed.ToString());
            menu.Add("touch", "TOUCH CONTROLS", settings.TouchControls ? "ON" : "OFF");
            menu.Add("back", "BACK");
            hud.ShowMenu(menu);
        }

        void ShowPauseMenu(int selected)
        {
            var menu = new MenuModel
            {
                Title = "PAUSED",
                Subtitle = $"WAVE {Match.Wave}  -  {Match.Score.Points} POINTS",
                Selected = selected,
                LightDim = true,
                Hint = MenuHint(),
            };
            menu.Add("resume", "RESUME");
            menu.Add("restart", "RESTART RUN");
            menu.Add("menu", "MAIN MENU");
            hud.ShowMenu(menu);
        }

        void ShowResultsMenu(int selected)
        {
            var menu = new MenuModel
            {
                Title = "RING OUT!",
                TitleDanger = true,
                Subtitle = $"YOU FELL ON WAVE {Match.Wave}",
                Selected = selected,
                LightDim = true,
                Hint = MenuHint(),
            };
            menu.Body.Add(("SCORE", Match.Score.Points + (lastRecord.NewBestScore ? "   NEW BEST!" : string.Empty), lastRecord.NewBestScore));
            menu.Body.Add(("BEST", records.BestScore.ToString(), false));
            menu.Body.Add(("WAVE REACHED", Match.Wave + (lastRecord.NewBestWave ? "   NEW RECORD" : string.Empty), lastRecord.NewBestWave));
            menu.Body.Add(("ENEMIES PUSHED OFF", Match.Score.RingOuts.ToString(), false));
            menu.Body.Add(("LONGEST CHAIN", Match.Score.BestChain.ToString(), false));
            menu.Add("again", "PLAY AGAIN");
            menu.Add("menu", "MAIN MENU");
            hud.ShowMenu(menu);
        }

        string MenuHint(bool withChange = false)
        {
            if (input.Source == InputSource.Gamepad)
            {
                return withChange ? "D-PAD TO CHOOSE AND CHANGE  -  A TO SELECT  -  B TO GO BACK" : "D-PAD TO CHOOSE  -  A TO SELECT  -  B TO GO BACK";
            }
            if (input.Source == InputSource.Touch)
            {
                return "TAP A ROW";
            }
            return withChange
                ? "ARROWS TO CHOOSE AND CHANGE  -  ENTER TO SELECT  -  ESC TO GO BACK"
                : "ARROWS TO CHOOSE  -  ENTER TO SELECT  -  ESC TO GO BACK";
        }

        void MenuInput()
        {
            MenuModel menu = hud.Menu;
            if (menu == null)
            {
                return;
            }
            int vertical = input.VerticalStep();
            if (vertical != 0 && menu.Items.Count > 0)
            {
                menu.Selected = (menu.Selected + vertical + menu.Items.Count) % menu.Items.Count;
                sound.PlayUi(art.Move, 0.6f);
                hud.RefreshMenu();
            }
            int horizontal = input.HorizontalStep();
            if (horizontal != 0 && menu.Items.Count > 0 && menu.Items[menu.Selected].Value != null)
            {
                Change(menu.Items[menu.Selected].Id, horizontal);
            }
            if (input.Confirm && menu.Items.Count > 0)
            {
                Activate(menu.Items[menu.Selected].Id);
            }
            else if (input.Back || (Screen == GameScreen.Paused && input.Pause))
            {
                GoBack();
            }
        }

        void OnItemClicked(int index)
        {
            MenuModel menu = hud.Menu;
            if (menu == null || index < 0 || index >= menu.Items.Count)
            {
                return;
            }
            menu.Selected = index;
            Activate(menu.Items[index].Id);
        }

        void GoBack()
        {
            switch (Screen)
            {
                case GameScreen.HowToPlay:
                case GameScreen.Records:
                case GameScreen.Settings:
                    Activate("back");
                    break;
                case GameScreen.Paused:
                    TogglePause();
                    break;
                case GameScreen.Results:
                    sound.PlayUi(art.Move, 0.6f);
                    ToTitle();
                    break;
            }
        }

        /// <summary>Runs a menu row by its id, as Enter, a click or a tap would.</summary>
        public void Activate(string id)
        {
            switch (id)
            {
                case "play":
                case "again":
                case "restart":
                    sound.PlayUi(art.Select);
                    StartRun();
                    return;
                case "howto":
                    sound.PlayUi(art.Select);
                    ShowHowToPlay();
                    return;
                case "records":
                    sound.PlayUi(art.Select);
                    ShowRecordsMenu();
                    return;
                case "settings":
                    sound.PlayUi(art.Select);
                    ShowSettingsMenu(0);
                    return;
                case "quit":
                    Application.Quit();
                    return;
                case "back":
                    sound.PlayUi(art.Move, 0.6f);
                    string from = Screen == GameScreen.HowToPlay ? "howto" : Screen == GameScreen.Records ? "records" : "settings";
                    Screen = GameScreen.Title;
                    ShowTitleMenu(0);
                    hud.Menu.Selected = Mathf.Max(0, hud.Menu.IndexOf(from));
                    hud.RefreshMenu();
                    return;
                case "resume":
                    TogglePause();
                    return;
                case "menu":
                    sound.PlayUi(art.Select);
                    ToTitle();
                    return;
                default:
                    Change(id, 1);
                    return;
            }
        }

        /// <summary>Steps a setting row left or right, saves it and redraws the menu.</summary>
        public void Change(string id, int step)
        {
            switch (id)
            {
                case "volume":
                    settings.StepVolume(step);
                    break;
                case "mute":
                    settings.Muted = !settings.Muted;
                    sound.ToggleMute();
                    UpdateSoundLabel();
                    break;
                case "shake":
                    settings.ScreenShake = !settings.ScreenShake;
                    break;
                case "camera":
                    settings.StepCameraSpeed(step);
                    break;
                case "touch":
                    settings.TouchControls = !settings.TouchControls;
                    break;
                default:
                    return;
            }
            settings.Save(store);
            ApplyOptions();
            if (id == "shake" && settings.ScreenShake)
            {
                cameraRig.Shake(0.4f);
            }
            sound.PlayUi(art.Move, 0.6f);
            ShowSettingsMenu(hud.Menu != null ? hud.Menu.Selected : 0);
        }

        void UpdateSoundLabel() => hud.SetSound(settings.Muted ? "SOUND OFF  (M)" : "SOUND ON  (M)");

        public static string ArenaName(ArenaLayout layout) => layout switch
        {
            ArenaLayout.Disc => "THE DISC",
            ArenaLayout.Ring => "THE RING",
            ArenaLayout.Islands => "TWIN ISLANDS",
            _ => "CRUMBLING ISLAND",
        };

        static string PowerName(PowerUpKind kind) => kind switch
        {
            PowerUpKind.Knockback => "KNOCKBACK",
            PowerUpKind.Smash => "SMASH",
            _ => "ROCKETS",
        };

        static string PowerHint(PowerUpKind kind) => kind switch
        {
            PowerUpKind.Knockback => "EVERYTHING YOU TOUCH FLIES",
            PowerUpKind.Smash => "ACTION BUTTON TO LEAP AND SLAM  x3",
            _ => "HOMING VOLLEYS, EVERY SECOND",
        };
    }
}
