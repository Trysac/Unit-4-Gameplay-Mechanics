namespace RingOut.Core
{
    /// <summary>
    /// Counts a power-up down. Picking one up while it runs restarts the full duration instead
    /// of starting a second countdown whose end would cut the new one short.
    /// </summary>
    public sealed class PowerUpTimer
    {
        public float Remaining { get; private set; }

        public bool Active => Remaining > 0f;

        public void Grant(float seconds)
        {
            if (seconds > Remaining)
            {
                Remaining = seconds;
            }
        }

        /// <summary>Spends time; true on the step that runs it out.</summary>
        public bool Step(float deltaTime)
        {
            if (!Active)
            {
                return false;
            }
            Remaining -= deltaTime;
            if (Remaining > 0f)
            {
                return false;
            }
            Remaining = 0f;
            return true;
        }

        public void Clear() => Remaining = 0f;
    }
}
