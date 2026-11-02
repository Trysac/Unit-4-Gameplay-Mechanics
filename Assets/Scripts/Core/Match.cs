using System.Collections.Generic;

namespace RingOut.Core
{
    public enum MatchPhase
    {
        /// <summary>The wave's enemies drop in; a short count before the fight.</summary>
        Countdown,
        Fighting,
        /// <summary>The wave is clear; a breather before the next one.</summary>
        Cleared,
        /// <summary>The player fell off.</summary>
        Over,
    }

    public enum MatchEventKind
    {
        WaveStarted,
        Fight,
        RingOut,
        Split,
        WaveCleared,
        PowerUp,
        PowerEnded,
        RocketVolley,
        Dash,
        Smash,
        Over,
    }

    /// <summary>Something the run did that the view turns into sound, particles or text.</summary>
    public readonly struct MatchEvent
    {
        public readonly MatchEventKind Kind;
        public readonly EnemyKind Enemy;
        public readonly PowerUpKind Power;
        public readonly int Points;
        /// <summary>The wave for wave events, the multiplier for a ring-out.</summary>
        public readonly int Value;

        public MatchEvent(MatchEventKind kind, EnemyKind enemy = EnemyKind.Grunt, PowerUpKind power = PowerUpKind.Knockback,
            int points = 0, int value = 0)
        {
            Kind = kind;
            Enemy = enemy;
            Power = power;
            Points = points;
            Value = value;
        }

        public override string ToString() => $"{Kind} {Enemy} {Power} {Points} {Value}";
    }

    /// <summary>
    /// One run, without the engine: the waves in order, what is left of each, the score, the
    /// player's power-ups and the end when the player falls. The view runs the physics and tells
    /// the match what fell, split or was picked up; the match says what to spawn and what
    /// happened.
    /// </summary>
    public sealed class Match
    {
        public const float CountdownSeconds = 1.6f;
        public const float IntermissionSeconds = 2.2f;

        readonly int seed;
        readonly List<MatchEvent> events = new List<MatchEvent>();

        /// <param name="firstWave">The wave to open on (1 for a normal run; tests and screenshots jump ahead).</param>
        public Match(int seed, int firstWave = 1)
        {
            this.seed = seed;
            BeginWave(firstWave < 1 ? 1 : firstWave);
        }

        public int Seed => seed;
        public int Wave { get; private set; }
        public WavePlan Plan { get; private set; }
        public ArenaShape Shape { get; private set; }
        public MatchPhase Phase { get; private set; }
        public float PhaseTime { get; private set; }
        /// <summary>Seconds of fighting in the current wave (the shrinking island follows it).</summary>
        public float WaveTime { get; private set; }
        public float Elapsed { get; private set; }
        /// <summary>Enemies of the current wave still on the floor.</summary>
        public int Alive { get; private set; }
        public ScoreBoard Score { get; } = new ScoreBoard();
        public PlayerPowers Powers { get; } = new PlayerPowers();
        public bool IsOver => Phase == MatchPhase.Over;
        public IReadOnlyList<MatchEvent> Events => events;

        public void ClearEvents() => events.Clear();

        void BeginWave(int wave)
        {
            Wave = wave;
            Plan = WavePlanner.Plan(wave, seed);
            Shape = ArenaShape.Of(Plan.Layout);
            Alive = Plan.Enemies.Count;
            Phase = MatchPhase.Countdown;
            PhaseTime = 0f;
            WaveTime = 0f;
            events.Add(new MatchEvent(MatchEventKind.WaveStarted, value: wave));
        }

        public void Step(float deltaTime)
        {
            if (IsOver || deltaTime <= 0f)
            {
                return;
            }
            Elapsed += deltaTime;
            PhaseTime += deltaTime;
            Score.Step(deltaTime);
            foreach (PowerSignal signal in Powers.Step(deltaTime))
            {
                switch (signal)
                {
                    case PowerSignal.KnockbackEnded:
                        events.Add(new MatchEvent(MatchEventKind.PowerEnded, power: PowerUpKind.Knockback));
                        break;
                    case PowerSignal.RocketsEnded:
                        events.Add(new MatchEvent(MatchEventKind.PowerEnded, power: PowerUpKind.Rockets));
                        break;
                    case PowerSignal.RocketVolley:
                        events.Add(new MatchEvent(MatchEventKind.RocketVolley, power: PowerUpKind.Rockets));
                        break;
                }
            }
            switch (Phase)
            {
                case MatchPhase.Countdown:
                    if (PhaseTime >= CountdownSeconds)
                    {
                        Phase = MatchPhase.Fighting;
                        PhaseTime = 0f;
                        events.Add(new MatchEvent(MatchEventKind.Fight, value: Wave));
                    }
                    break;
                case MatchPhase.Fighting:
                    WaveTime += deltaTime;
                    break;
                case MatchPhase.Cleared:
                    if (PhaseTime >= IntermissionSeconds)
                    {
                        BeginWave(Wave + 1);
                    }
                    break;
            }
        }

        /// <summary>An enemy of the current wave fell off: score it and clear the wave on the last one.</summary>
        public void EnemyFell(EnemyKind kind)
        {
            if (IsOver || Alive <= 0)
            {
                return;
            }
            Alive--;
            int points = Score.RingOut(kind);
            events.Add(new MatchEvent(MatchEventKind.RingOut, kind, points: points, value: Score.Multiplier));
            if (Alive == 0)
            {
                int bonus = Score.WaveCleared(Wave, WaveTime);
                events.Add(new MatchEvent(MatchEventKind.WaveCleared, points: bonus, value: Wave));
                Phase = MatchPhase.Cleared;
                PhaseTime = 0f;
            }
        }

        /// <summary>A splitter broke into two minis: one more enemy to push off.</summary>
        public void EnemySplit()
        {
            if (IsOver || Alive <= 0)
            {
                return;
            }
            Alive++;
            events.Add(new MatchEvent(MatchEventKind.Split, EnemyKind.Splitter));
        }

        public void PowerUpCollected(PowerUpKind kind)
        {
            if (IsOver)
            {
                return;
            }
            Powers.Grant(kind);
            events.Add(new MatchEvent(MatchEventKind.PowerUp, power: kind));
        }

        /// <summary>The action button: a smash, a dash, or nothing while it recharges.</summary>
        public ActionResult Act()
        {
            if (IsOver)
            {
                return ActionResult.None;
            }
            ActionResult result = Powers.Act();
            if (result == ActionResult.Dash)
            {
                events.Add(new MatchEvent(MatchEventKind.Dash));
            }
            else if (result == ActionResult.Smash)
            {
                events.Add(new MatchEvent(MatchEventKind.Smash, power: PowerUpKind.Smash, value: Powers.Smashes));
            }
            return result;
        }

        public void PlayerFell()
        {
            if (IsOver)
            {
                return;
            }
            Phase = MatchPhase.Over;
            PhaseTime = 0f;
            events.Add(new MatchEvent(MatchEventKind.Over, value: Wave));
        }
    }
}
