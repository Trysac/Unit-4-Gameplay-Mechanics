using System;

namespace RingOut.Core
{
    /// <summary>
    /// Points for ring-outs and cleared waves. Ring-outs within <see cref="ChainWindow"/> seconds
    /// of each other build a chain whose multiplier is its length, up to x5.
    /// </summary>
    public sealed class ScoreBoard
    {
        public const float ChainWindow = 3f;
        public const int MaxMultiplier = 5;
        public const int WaveBonusPerWave = 50;
        /// <summary>A wave cleared faster than this earns 10 points per second to spare.</summary>
        public const float ParSeconds = 30f;

        float sinceLast = float.MaxValue;

        public int Points { get; private set; }

        /// <summary>Ring-outs in the running chain (0 when it has lapsed).</summary>
        public int Chain { get; private set; }

        public int BestChain { get; private set; }

        public int RingOuts { get; private set; }

        public int Multiplier => Math.Clamp(Chain, 1, MaxMultiplier);

        /// <summary>Seconds left to extend the chain, 0 when there is none.</summary>
        public float ChainLeft => Chain > 0 ? Math.Max(0f, ChainWindow - sinceLast) : 0f;

        /// <summary>Scores an enemy pushed off; returns the points it earned.</summary>
        public int RingOut(EnemyKind kind)
        {
            Chain = sinceLast <= ChainWindow ? Chain + 1 : 1;
            BestChain = Math.Max(BestChain, Chain);
            sinceLast = 0f;
            RingOuts++;
            int points = EnemyCatalog.Base(kind).Points * Multiplier;
            Points += points;
            return points;
        }

        /// <summary>Scores a cleared wave; returns the bonus.</summary>
        public int WaveCleared(int wave, float seconds)
        {
            int bonus = WaveBonusPerWave * wave + 10 * (int)Math.Max(0f, ParSeconds - seconds);
            Points += bonus;
            return bonus;
        }

        public void Step(float deltaTime)
        {
            if (sinceLast < float.MaxValue)
            {
                sinceLast += deltaTime;
            }
            if (sinceLast > ChainWindow)
            {
                Chain = 0;
            }
        }
    }
}
