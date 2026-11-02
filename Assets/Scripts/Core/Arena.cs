using System;

namespace RingOut.Core
{
    public enum ArenaLayout
    {
        /// <summary>A round island, ten metres across the radius.</summary>
        Disc,
        /// <summary>A ring with a hole in the middle to push enemies into.</summary>
        Ring,
        /// <summary>Two round islands joined by a narrow bridge.</summary>
        Islands,
        /// <summary>A round island whose edge crumbles inwards while the wave lasts.</summary>
        Shrinking,
    }

    /// <summary>
    /// The shape of the floor, on the ground plane (X, Z) with the origin at the arena's centre.
    /// The view builds its meshes from these numbers and the rules use them to place spawns.
    /// </summary>
    public sealed class ArenaShape
    {
        public const float DiscRadius = 10f;
        public const float RingOuter = 11f;
        public const float RingInner = 3.5f;
        public const float IslandRadius = 5.5f;
        public const float IslandOffset = 7f;
        public const float BridgeHalfWidth = 1.6f;
        public const float ShrinkStart = 11f;
        public const float ShrinkMin = 5.5f;
        /// <summary>Metres the shrinking island loses per second.</summary>
        public const float ShrinkRate = 0.12f;

        public ArenaLayout Layout { get; }

        ArenaShape(ArenaLayout layout) => Layout = layout;

        public static ArenaShape Of(ArenaLayout layout) => new ArenaShape(layout);

        /// <summary>
        /// Every five waves end with a boss and the next five move to the next layout:
        /// waves 1-5 on the disc, 6-10 on the ring, 11-15 on the islands, 16-20 on the shrinking
        /// island, then round again.
        /// </summary>
        public static ArenaLayout ForWave(int wave)
        {
            int block = (Math.Max(wave, 1) - 1) / 5;
            return (ArenaLayout)(block % 4);
        }

        /// <summary>The outer radius of a round layout after <paramref name="seconds"/> of the wave.</summary>
        public float OuterRadius(float seconds = 0f) => Layout switch
        {
            ArenaLayout.Disc => DiscRadius,
            ArenaLayout.Ring => RingOuter,
            ArenaLayout.Islands => IslandRadius,
            ArenaLayout.Shrinking => Math.Max(ShrinkMin, ShrinkStart - ShrinkRate * Math.Max(seconds, 0f)),
            _ => DiscRadius,
        };

        /// <summary>True when the point is on the floor (so a ball there does not fall).</summary>
        public bool IsSolid(float x, float z, float seconds = 0f)
        {
            switch (Layout)
            {
                case ArenaLayout.Ring:
                    float distance = Length(x, z);
                    return distance <= RingOuter && distance >= RingInner;
                case ArenaLayout.Islands:
                    return Length(x - IslandOffset, z) <= IslandRadius ||
                        Length(x + IslandOffset, z) <= IslandRadius ||
                        (Math.Abs(x) <= IslandOffset && Math.Abs(z) <= BridgeHalfWidth);
                default:
                    return Length(x, z) <= OuterRadius(seconds);
            }
        }

        /// <summary>
        /// True when a ball of <paramref name="margin"/> radius centred here stays on the floor
        /// (checks the centre and eight points around it).
        /// </summary>
        public bool IsSafe(float x, float z, float margin, float seconds = 0f)
        {
            if (!IsSolid(x, z, seconds))
            {
                return false;
            }
            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4.0;
                if (!IsSolid(x + margin * (float)Math.Cos(angle), z + margin * (float)Math.Sin(angle), seconds))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Where the player starts (and lands for each wave).</summary>
        public Vec3 PlayerStart => Layout switch
        {
            ArenaLayout.Ring => new Vec3(0f, 0f, -(RingOuter + RingInner) * 0.5f),
            ArenaLayout.Islands => new Vec3(-IslandOffset, 0f, 0f),
            _ => Vec3.Zero,
        };

        /// <summary>
        /// A random point on the floor where a ball of <paramref name="margin"/> radius fits and
        /// that is at least <paramref name="minDistance"/> from <paramref name="avoid"/> (the 2021
        /// spawner could drop an enemy on top of the player). Falls back to the safe point
        /// farthest from <paramref name="avoid"/> if none of the tries is far enough.
        /// </summary>
        public Vec3 SpawnPoint(Rng rng, Vec3 avoid, float minDistance, float margin, float seconds = 0f)
        {
            float reach = Layout == ArenaLayout.Islands ? IslandOffset + IslandRadius : OuterRadius(seconds);
            Vec3 best = PlayerStart;
            float bestDistance = -1f;
            for (int i = 0; i < 64; i++)
            {
                float x = rng.Range(-reach, reach);
                float z = rng.Range(-reach, reach);
                if (!IsSafe(x, z, margin, seconds))
                {
                    continue;
                }
                var point = new Vec3(x, 0f, z);
                float distance = Vec3.Distance(point.Flat, avoid.Flat);
                if (distance >= minDistance)
                {
                    return point;
                }
                if (distance > bestDistance)
                {
                    best = point;
                    bestDistance = distance;
                }
            }
            return best;
        }

        static float Length(float x, float z) => (float)Math.Sqrt(x * x + z * z);
    }
}
