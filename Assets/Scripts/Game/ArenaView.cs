using System.Collections.Generic;
using RingOut.Core;
using UnityEngine;

namespace RingOut.Game
{
    /// <summary>
    /// Builds the floating island for each <see cref="ArenaLayout"/> from the core's numbers:
    /// a walkable floor with a mesh collider, a glowing trim along every edge and a rocky
    /// underside, plus distant islands for scenery. The shrinking island is rebuilt as its edge
    /// crumbles.
    /// </summary>
    public sealed class ArenaView : MonoBehaviour
    {
        const float Thickness = 0.6f;
        const float TrimWidth = 0.28f;

        GameArt art;
        Transform layoutRoot;
        readonly List<Mesh> owned = new List<Mesh>();
        readonly List<Mesh> sceneryMeshes = new List<Mesh>();
        MeshFilter shrinkFloor;
        MeshCollider shrinkCollider;
        MeshFilter shrinkTrim;
        Transform shrinkRock;
        float shownRadius;
        float crumbleClock;
        Material bridgeMaterial;

        public ArenaShape Shape { get; private set; }

        public ArenaLayout? Layout => Shape?.Layout;

        public void Init(GameArt art)
        {
            this.art = art;
            BuildScenery();
        }

        public void Build(ArenaShape shape)
        {
            Shape = shape;
            if (layoutRoot != null)
            {
                Destroy(layoutRoot.gameObject);
            }
            foreach (Mesh mesh in owned)
            {
                Destroy(mesh);
            }
            owned.Clear();
            shrinkFloor = null;
            layoutRoot = new GameObject(shape.Layout.ToString()).transform;
            layoutRoot.SetParent(transform, false);

            switch (shape.Layout)
            {
                case ArenaLayout.Ring:
                    Island(layoutRoot, Vector3.zero, ArenaShape.RingOuter, ArenaShape.RingInner, 9f, 3);
                    break;
                case ArenaLayout.Islands:
                    Island(layoutRoot, new Vector3(-ArenaShape.IslandOffset, 0f, 0f), ArenaShape.IslandRadius, 0f, 6f, 4);
                    Island(layoutRoot, new Vector3(ArenaShape.IslandOffset, 0f, 0f), ArenaShape.IslandRadius, 0f, 6f, 5);
                    Bridge(layoutRoot);
                    break;
                case ArenaLayout.Shrinking:
                    Island(layoutRoot, Vector3.zero, ArenaShape.ShrinkStart, 0f, 8f, 6, shrinking: true);
                    shownRadius = ArenaShape.ShrinkStart;
                    break;
                default:
                    Island(layoutRoot, Vector3.zero, ArenaShape.DiscRadius, 0f, 8f, 2);
                    break;
            }
        }

        /// <summary>Follows the shrinking island's edge for the seconds fought in this wave.</summary>
        public void Sync(float waveSeconds, float deltaTime)
        {
            if (shrinkFloor == null)
            {
                return;
            }
            float radius = Shape.OuterRadius(waveSeconds);
            if (Mathf.Abs(radius - shownRadius) < 0.04f)
            {
                return;
            }
            {
                shownRadius = radius;
                Replace(shrinkFloor, Meshes.Floor(radius, 0f, Thickness));
                shrinkCollider.sharedMesh = shrinkFloor.sharedMesh;
                Replace(shrinkTrim, Meshes.Trim(radius, TrimWidth));
                float scale = radius / ArenaShape.ShrinkStart;
                shrinkRock.localScale = new Vector3(scale, 1f, scale);
                // Crumbs fall off the edge as it recedes.
                crumbleClock += deltaTime;
                if (crumbleClock > 0.12f)
                {
                    crumbleClock = 0f;
                    float angle = Random.value * Mathf.PI * 2f;
                    var at = new Vector3(Mathf.Cos(angle) * radius, -0.2f, Mathf.Sin(angle) * radius);
                    Effects.Burst(at, Vector3.down, art.Glow, Palette.Trim, Palette.Rock, 6, 2f, 0.3f, 0.9f, 60f, 1.5f);
                }
            }
        }

        void Replace(MeshFilter filter, Mesh mesh)
        {
            owned.Remove(filter.sharedMesh);
            Destroy(filter.sharedMesh);
            filter.sharedMesh = mesh;
            owned.Add(mesh);
        }

        void Island(Transform parent, Vector3 center, float outer, float inner, float depth, int seed, bool shrinking = false)
        {
            var island = new GameObject("Island").transform;
            island.SetParent(parent, false);
            island.localPosition = center;

            GameObject floor = Part(island, "Floor", Meshes.Floor(outer, inner, Thickness), art.Floor);
            var collider = floor.AddComponent<MeshCollider>();
            collider.sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh;
            collider.sharedMaterial = art.FloorPhysics;
            GameObject trim = Part(island, "Trim", Meshes.Trim(outer, TrimWidth), art.Trim);
            trim.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (inner > 0f)
            {
                // The hole's rim glows too, from the inside out.
                GameObject innerTrim = Part(island, "Inner Trim", Meshes.Trim(inner + TrimWidth, TrimWidth), art.Trim);
                innerTrim.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            GameObject rock = Part(island, "Rock", Meshes.Underside(outer, inner, -Thickness, depth, seed), art.Rock);

            if (shrinking)
            {
                shrinkFloor = floor.GetComponent<MeshFilter>();
                shrinkCollider = collider;
                shrinkTrim = trim.GetComponent<MeshFilter>();
                shrinkRock = rock.transform;
            }
        }

        void Bridge(Transform parent)
        {
            float length = ArenaShape.IslandOffset * 2f;
            // The trim only runs over the gap, from one island's edge to the other's.
            float edge = Mathf.Sqrt(ArenaShape.IslandRadius * ArenaShape.IslandRadius - ArenaShape.BridgeHalfWidth * ArenaShape.BridgeHalfWidth);
            float gap = 2f * (ArenaShape.IslandOffset - edge);
            var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = "Bridge";
            bridge.transform.SetParent(parent, false);
            bridge.transform.localPosition = new Vector3(0f, -Thickness * 0.5f, 0f);
            bridge.transform.localScale = new Vector3(length, Thickness, ArenaShape.BridgeHalfWidth * 2f);
            // The cube's top face has 0..1 UVs; tile the texture like the islands' (one tile per 2 m).
            if (bridgeMaterial == null)
            {
                bridgeMaterial = new Material(art.Floor) { name = "Bridge Floor" };
                bridgeMaterial.mainTextureScale = new Vector2(length * 0.5f, ArenaShape.BridgeHalfWidth);
            }
            bridge.GetComponent<MeshRenderer>().sharedMaterial = bridgeMaterial;
            bridge.GetComponent<BoxCollider>().sharedMaterial = art.FloorPhysics;
            for (int side = -1; side <= 1; side += 2)
            {
                var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = "Bridge Trim";
                Destroy(trim.GetComponent<BoxCollider>());
                trim.transform.SetParent(parent, false);
                trim.transform.localPosition = new Vector3(0f, 0.01f, side * (ArenaShape.BridgeHalfWidth - TrimWidth * 0.5f));
                trim.transform.localScale = new Vector3(gap, 0.03f, TrimWidth);
                var renderer = trim.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = art.Trim;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        GameObject Part(Transform parent, string name, Mesh mesh, Material material, List<Mesh> keep = null)
        {
            (keep ?? owned).Add(mesh);
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        /// <summary>Small islands drifting far around and below the arena.</summary>
        void BuildScenery()
        {
            var scenery = new GameObject("Scenery").transform;
            scenery.SetParent(transform, false);
            var random = new System.Random(11);
            for (int i = 0; i < 14; i++)
            {
                float angle = (i / 14f + (float)random.NextDouble() * 0.04f) * Mathf.PI * 2f;
                float distance = 34f + (float)random.NextDouble() * 30f;
                float radius = 1.5f + (float)random.NextDouble() * 3.5f;
                var island = new GameObject("Far Island").transform;
                island.SetParent(scenery, false);
                island.localPosition = new Vector3(Mathf.Cos(angle) * distance, -8f + (float)random.NextDouble() * 14f, Mathf.Sin(angle) * distance);
                island.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                Part(island, "Top", Meshes.Floor(radius, 0f, 0.3f, 24), art.Floor, sceneryMeshes);
                Part(island, "Rock", Meshes.Underside(radius, 0f, -0.3f, radius * 1.6f, 20 + i, 16), art.Rock, sceneryMeshes);
            }
        }

        void OnDestroy()
        {
            foreach (Mesh mesh in owned)
            {
                Destroy(mesh);
            }
            foreach (Mesh mesh in sceneryMeshes)
            {
                Destroy(mesh);
            }
            if (bridgeMaterial != null)
            {
                Destroy(bridgeMaterial);
            }
        }
    }
}
