using System.Collections.Generic;
using UnityEngine;

namespace RingOut.Game
{
    /// <summary>The arena, ring and pickup meshes, built in code.</summary>
    public static class Meshes
    {
        /// <summary>
        /// The walkable top of a round floor with its outer (and inner) side band: a disc, or a
        /// ring when <paramref name="inner"/> is above zero. The top's UVs are world metres / 2 so
        /// the tile texture lines up across layouts.
        /// </summary>
        public static Mesh Floor(float outer, float inner, float thickness, int segments = 96)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            // Top.
            if (inner <= 0f)
            {
                vertices.Add(Vector3.zero);
                uvs.Add(Vector2.zero);
                for (int i = 0; i <= segments; i++)
                {
                    Vector3 p = Circle(i, segments, outer, 0f);
                    vertices.Add(p);
                    uvs.Add(new Vector2(p.x, p.z) * 0.5f);
                }
                for (int i = 0; i < segments; i++)
                {
                    triangles.Add(0);
                    triangles.Add(i + 2);
                    triangles.Add(i + 1);
                }
            }
            else
            {
                int start = vertices.Count;
                for (int i = 0; i <= segments; i++)
                {
                    Vector3 o = Circle(i, segments, outer, 0f);
                    Vector3 n = Circle(i, segments, inner, 0f);
                    vertices.Add(o);
                    vertices.Add(n);
                    uvs.Add(new Vector2(o.x, o.z) * 0.5f);
                    uvs.Add(new Vector2(n.x, n.z) * 0.5f);
                }
                for (int i = 0; i < segments; i++)
                {
                    int a = start + i * 2;
                    triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }

            Band(vertices, uvs, triangles, outer, 0f, -thickness, segments, outward: true);
            if (inner > 0f)
            {
                Band(vertices, uvs, triangles, inner, 0f, -thickness, segments, outward: false);
            }
            return Build("Floor", vertices, uvs, triangles);
        }

        /// <summary>
        /// The rocky underside of a floating island: a jagged cone (or, for a ring, a funnel)
        /// hanging below the floor.
        /// </summary>
        public static Mesh Underside(float outer, float inner, float top, float depth, int seed, int segments = 48)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var random = new System.Random(seed);
            int rings = 4;
            float[] jitter = new float[segments + 1];
            for (int i = 0; i < segments; i++)
            {
                jitter[i] = (float)random.NextDouble();
            }
            jitter[segments] = jitter[0];

            // Outer cone: from the floor's edge down to the tip (or the funnel's mouth).
            float bottomRadius = inner > 0f ? inner * 0.8f : 0.3f;
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                float radius = Mathf.Lerp(outer, bottomRadius, Mathf.Pow(t, 0.7f));
                float y = top - depth * t;
                for (int i = 0; i <= segments; i++)
                {
                    float wobble = r == 0 ? 0f : (jitter[i] - 0.5f) * 0.18f * outer * (1f - t * 0.5f);
                    Vector3 p = Circle(i, segments, Mathf.Max(radius + wobble, 0.1f), y);
                    vertices.Add(p);
                    uvs.Add(new Vector2(i / (float)segments * 6f, t * 2f));
                }
            }
            for (int r = 0; r < rings; r++)
            {
                for (int i = 0; i < segments; i++)
                {
                    int a = r * (segments + 1) + i;
                    int b = a + segments + 1;
                    triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            }
            if (inner > 0f)
            {
                // The funnel's inside, seen through the hole.
                Band(vertices, uvs, triangles, inner * 0.999f, top, top - depth, segments, outward: false, taper: 0.8f);
            }
            return Build("Underside", vertices, uvs, triangles);
        }

        /// <summary>A flat glowing strip just inside a circle's edge, a hair above the floor.</summary>
        public static Mesh Trim(float radius, float width, int segments = 96)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                vertices.Add(Circle(i, segments, radius, 0.02f));
                vertices.Add(Circle(i, segments, radius - width, 0.02f));
                uvs.Add(new Vector2(i / (float)segments * radius * 2f, 0f));
                uvs.Add(new Vector2(i / (float)segments * radius * 2f, 1f));
            }
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2;
                triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
            }
            return Build("Trim", vertices, uvs, triangles);
        }

        /// <summary>A ring around the Y axis (power-up halos and shockwaves).</summary>
        public static Mesh Torus(float major, float minor, int segments = 48, int sides = 10)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(u) * major, 0f, Mathf.Sin(u) * major);
                for (int j = 0; j <= sides; j++)
                {
                    float v = j / (float)sides * Mathf.PI * 2f;
                    Vector3 normal = new Vector3(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(v), Mathf.Sin(u) * Mathf.Cos(v));
                    vertices.Add(center + normal * minor);
                    uvs.Add(new Vector2(i / (float)segments, j / (float)sides));
                }
            }
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    int a = i * (sides + 1) + j;
                    int b = a + sides + 1;
                    triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            }
            return Build("Torus", vertices, uvs, triangles);
        }

        /// <summary>A gem for the power-up pickups.</summary>
        public static Mesh Gem(float radius, float height)
        {
            var points = new[]
            {
                new Vector3(0f, height, 0f), new Vector3(0f, -height, 0f),
                new Vector3(radius, 0f, 0f), new Vector3(0f, 0f, radius),
                new Vector3(-radius, 0f, 0f), new Vector3(0f, 0f, -radius),
            };
            int[] faces = { 0, 3, 2, 0, 4, 3, 0, 5, 4, 0, 2, 5, 1, 2, 3, 1, 3, 4, 1, 4, 5, 1, 5, 2 };
            // Flat shading: every face gets its own three vertices.
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i < faces.Length; i++)
            {
                vertices.Add(points[faces[i]]);
                uvs.Add(Vector2.zero);
                triangles.Add(i);
            }
            return Build("Gem", vertices, uvs, triangles);
        }

        static void Band(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, float radius, float top, float bottom,
            int segments, bool outward, float taper = 1f)
        {
            int start = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                vertices.Add(Circle(i, segments, radius, top));
                vertices.Add(Circle(i, segments, radius * taper, bottom));
                uvs.Add(new Vector2(i / (float)segments * radius, 0f));
                uvs.Add(new Vector2(i / (float)segments * radius, 0.5f));
            }
            for (int i = 0; i < segments; i++)
            {
                int a = start + i * 2;
                if (outward)
                {
                    triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                }
                else
                {
                    triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
        }

        static Vector3 Circle(int i, int segments, float radius, float y)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }

        static Mesh Build(string name, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
