using System;

namespace RingOut.Core
{
    /// <summary>A small 3D vector so the rules never touch UnityEngine. Y is up.</summary>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 Zero => new Vec3(0f, 0f, 0f);

        public float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);

        public Vec3 Normalized
        {
            get
            {
                float length = Length;
                return length > 1e-6f ? new Vec3(X / length, Y / length, Z / length) : Zero;
            }
        }

        /// <summary>The same vector on the ground plane (Y dropped).</summary>
        public Vec3 Flat => new Vec3(X, 0f, Z);

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator -(Vec3 a) => new Vec3(-a.X, -a.Y, -a.Z);
        public static Vec3 operator *(Vec3 a, float k) => new Vec3(a.X * k, a.Y * k, a.Z * k);

        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static float Distance(Vec3 a, Vec3 b) => (a - b).Length;

        public bool Equals(Vec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        public override bool Equals(object obj) => obj is Vec3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}
