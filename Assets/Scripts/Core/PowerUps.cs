using System.Collections.Generic;

namespace RingOut.Core
{
    public enum PowerUpKind
    {
        /// <summary>Every enemy the player touches flies off (the 2021 power-up).</summary>
        Knockback,
        /// <summary>Three charges: the action button leaps and slams a shockwave into the ground.</summary>
        Smash,
        /// <summary>Homing rockets at every enemy, a volley a second.</summary>
        Rockets,
    }

    public enum ActionResult
    {
        None,
        Dash,
        Smash,
    }

    /// <summary>What the player's power-ups did during a step.</summary>
    public enum PowerSignal
    {
        KnockbackEnded,
        RocketsEnded,
        RocketVolley,
    }

    /// <summary>
    /// The player's power-ups and action button: a timed knockback, smash charges, timed rockets
    /// that fire a volley every second, and the dash, which the button does when no smash is
    /// charged. Different power-ups run together; picking up one that runs restarts it.
    /// </summary>
    public sealed class PlayerPowers
    {
        public const float KnockbackSeconds = 7f;
        public const int SmashCharges = 3;
        public const float RocketSeconds = 6f;
        public const float RocketVolleyEvery = 1f;
        public const float DashCooldown = 1.2f;
        public const float SmashCooldown = 0.9f;

        readonly PowerUpTimer knockback = new PowerUpTimer();
        readonly PowerUpTimer rockets = new PowerUpTimer();
        readonly List<PowerSignal> signals = new List<PowerSignal>();
        float untilVolley;

        public bool HasKnockback => knockback.Active;
        public float KnockbackLeft => knockback.Remaining;
        public bool HasRockets => rockets.Active;
        public float RocketsLeft => rockets.Remaining;
        public int Smashes { get; private set; }
        public float Cooldown { get; private set; }

        /// <summary>True while any power-up runs or has charges left.</summary>
        public bool Any => HasKnockback || HasRockets || Smashes > 0;

        public void Grant(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Knockback:
                    knockback.Grant(KnockbackSeconds);
                    break;
                case PowerUpKind.Smash:
                    Smashes = SmashCharges;
                    break;
                case PowerUpKind.Rockets:
                    bool running = rockets.Active;
                    rockets.Grant(RocketSeconds);
                    if (!running)
                    {
                        // The first volley leaves at once.
                        untilVolley = 0f;
                    }
                    break;
            }
        }

        /// <summary>The action button: a smash while charges last, otherwise a dash, once the last move has recharged.</summary>
        public ActionResult Act()
        {
            if (Cooldown > 0f)
            {
                return ActionResult.None;
            }
            if (Smashes > 0)
            {
                Smashes--;
                Cooldown = SmashCooldown;
                return ActionResult.Smash;
            }
            Cooldown = DashCooldown;
            return ActionResult.Dash;
        }

        /// <summary>Runs the timers; returns what ended or fired during the step.</summary>
        public IReadOnlyList<PowerSignal> Step(float deltaTime)
        {
            signals.Clear();
            Cooldown = Cooldown > deltaTime ? Cooldown - deltaTime : 0f;
            if (knockback.Step(deltaTime))
            {
                signals.Add(PowerSignal.KnockbackEnded);
            }
            if (rockets.Active)
            {
                float before = rockets.Remaining;
                float spent = deltaTime < before ? deltaTime : before;
                untilVolley -= spent;
                while (untilVolley <= 1e-4f)
                {
                    // Only volleys that fall due while the rockets still run are fired.
                    float dueAt = spent + untilVolley;
                    if (before - dueAt <= 1e-4f)
                    {
                        break;
                    }
                    signals.Add(PowerSignal.RocketVolley);
                    untilVolley += RocketVolleyEvery;
                }
            }
            if (rockets.Step(deltaTime))
            {
                signals.Add(PowerSignal.RocketsEnded);
            }
            return signals;
        }

        public void Clear()
        {
            knockback.Clear();
            rockets.Clear();
            Smashes = 0;
            Cooldown = 0f;
            untilVolley = 0f;
        }
    }
}
