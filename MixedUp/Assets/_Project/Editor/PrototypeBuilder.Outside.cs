using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Everything beyond the invisible walls: forest, boulders, bushes and grass on the foothills, and a dirt road winding
    /// in from the far south right up to the delivery truck. None of it can be reached, so none of it has colliders.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        /// <summary>The road from the horizon to the truck (x, z). It enters the map through the gap in the south fence.</summary>
        static readonly Vector2[] RoadLine =
        {
            V(0f, -31f), V(0f, -46f), V(3f, -60f), V(11f, -74f), V(14f, -90f), V(8f, -106f), V(-4f, -122f), V(-6f, -140f), V(2f, -158f), V(10f, -178f)
        };

        const float RoadWidth = 5.4f;

        /// <summary>Smooth points along the road, a metre or two apart.</summary>
        static List<Vector2> SmoothRoad(float step)
        {
            var points = new List<Vector2>();
            for (int i = 0; i + 1 < RoadLine.Length; i++)
            {
                Vector2 p0 = RoadLine[Mathf.Max(0, i - 1)], p1 = RoadLine[i], p2 = RoadLine[i + 1], p3 = RoadLine[Mathf.Min(RoadLine.Length - 1, i + 2)];
                float length = Vector2.Distance(p1, p2);
                int n = Mathf.Max(2, Mathf.CeilToInt(length / step));
                for (int k = 0; k < n; k++)
                {
                    float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                    points.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            points.Add(RoadLine[RoadLine.Length - 1]);
            return points;
        }

        static List<Vector2> roadPoints;
        static List<Vector2> RoadPoints => roadPoints ?? (roadPoints = SmoothRoad(2f));

        static float DistanceToRoad(float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            var points = RoadPoints;
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector2 a = points[i], b = points[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        /// <summary>0 on the road, 1 away from it: the foothills are calmed along the road so it lies flat.</summary>
        static float RoadCalm(float x, float z) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 18f, DistanceToRoad(x, z)));

        static bool OutsidePlayArea(float x, float z) => x < -46.5f || x > 46.5f || z < -36.5f || z > 56.5f;

        /// <summary>A copy of a prop prefab without colliders, standing on the visible hill surface.</summary>
        static GameObject PlaceDecor(Transform parent, GameObject prefab, Vector3 position, float yaw, float scale, Material tint, float sink = 0.12f)
        {
            var instance = Object.Instantiate(prefab, parent);
            instance.name = prefab.name;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            instance.transform.position = position + Vector3.down * sink * scale;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * scale;
            if (tint != null)
            {
                foreach (var r in instance.GetComponentsInChildren<Renderer>())
                {
                    var current = r.sharedMaterials;
                    current[0] = tint;
                    r.sharedMaterials = current;
                }
            }
            SetStaticRecursively(instance);
            return instance;
        }

        static float SlopeAt(LowPoly.HillField field, float x, float z)
        {
            float dx = field.Surface(x + 1f, z) - field.Surface(x - 1f, z);
            float dz = field.Surface(x, z + 1f) - field.Surface(x, z - 1f);
            return Mathf.Sqrt(dx * dx + dz * dz) * 0.5f;
        }

        static void BuildOutside(Transform env, Mats m, ArtAssets art)
        {
            var outside = new GameObject("Outside").transform;
            outside.SetParent(env, false);
            var field = art.hillField;
            var rng = new System.Random(2024);
            var tints = new[] { art.palette, art.palette, art.tintAutumn, art.tintGold, art.tintTeal };

            float OutsideDensity(float x, float z)
            {
                float dx = Mathf.Max(-45f - x, 0f, x - 45f), dz = Mathf.Max(-35f - z, 0f, z - 55f);
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                return Mathf.Clamp01(1.15f - d / 125f);
            }

            bool Spot(out float x, out float z, float minRoad, float maxSlope)
            {
                for (int tries = 0; tries < 12; tries++)
                {
                    x = Mathf.Lerp(-165f, 165f, (float)rng.NextDouble());
                    z = Mathf.Lerp(-185f, 205f, (float)rng.NextDouble());
                    if (!OutsidePlayArea(x, z)) continue;
                    if (rng.NextDouble() > OutsideDensity(x, z)) continue;
                    if (DistanceToRoad(x, z) < minRoad) continue;
                    if (SlopeAt(field, x, z) > maxSlope) continue;
                    return true;
                }
                x = z = 0f;
                return false;
            }

            // the forest: tall trees, thick close to the walls and thinning out into the hills
            int trees = 0;
            for (int i = 0; i < 4000 && trees < 560; i++)
            {
                if (!Spot(out float x, out float z, 6.5f, 0.75f)) continue;
                PlaceDecor(outside, art.trees[rng.Next(art.trees.Length)], new Vector3(x, field.Surface(x, z), z), (float)rng.NextDouble() * 360f,
                    Mathf.Lerp(0.85f, 1.5f, (float)rng.NextDouble()), tints[rng.Next(tints.Length)]);
                trees++;
            }

            // boulders, from small stones to big outcrops
            int rocks = 0;
            for (int i = 0; i < 2000 && rocks < 150; i++)
            {
                if (!Spot(out float x, out float z, 5f, 1.1f)) continue;
                float scale = rng.NextDouble() < 0.12 ? Mathf.Lerp(1.8f, 3.2f, (float)rng.NextDouble()) : Mathf.Lerp(0.7f, 1.7f, (float)rng.NextDouble());
                PlaceDecor(outside, art.rocks[rng.Next(art.rocks.Length)], new Vector3(x, field.Surface(x, z), z), (float)rng.NextDouble() * 360f, scale, null, 0.2f);
                rocks++;
            }

            // bushes, grass and wild flowers close to the map, so the border looks lived in
            for (int i = 0; i < 1500; i++)
            {
                float x = Mathf.Lerp(-95f, 95f, (float)rng.NextDouble()), z = Mathf.Lerp(-85f, 125f, (float)rng.NextDouble());
                if (!OutsidePlayArea(x, z) || DistanceToRoad(x, z) < 3.4f) continue;
                if (rng.NextDouble() > OutsideDensity(x, z)) continue;

                int kind = rng.Next(10);
                Mesh mesh = kind < 3 ? art.bushes[rng.Next(art.bushes.Length)] : kind < 8 ? art.tufts[rng.Next(art.tufts.Length)] : art.flowers[rng.Next(art.flowers.Length)];
                var go = MeshObject(kind < 3 ? "Bush" : kind < 8 ? "Tuft" : "Flower", outside, mesh, art.palette, kind < 3);
                go.transform.position = new Vector3(x, field.Surface(x, z), z);
                go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                go.transform.localScale = Vector3.one * (kind < 3 ? Mathf.Lerp(0.9f, 1.8f, (float)rng.NextDouble()) : Mathf.Lerp(1f, 2f, (float)rng.NextDouble()));
            }

            BuildRoad(outside, m, art, field);
        }

        // -------------------------------------------------------------- road

        static void BuildRoad(Transform outside, Mats m, ArtAssets art, LowPoly.HillField field)
        {
            var road = new GameObject("Road").transform;
            road.SetParent(outside, false);

            var points = SmoothRoad(1.5f);
            var dirt = new LowPoly.MeshBuilder();
            float Height(float x, float z) => (z > -35f ? 0f : field.Surface(x, z)) + 0.06f;

            Vector3 Edge(int i, float offset)
            {
                Vector2 a = points[Mathf.Max(0, i - 1)], b = points[Mathf.Min(points.Count - 1, i + 1)];
                Vector2 dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                Vector2 p = points[i] + side * offset;
                return new Vector3(p.x, Height(p.x, p.y), p.y);
            }

            void Strip(int i, float from, float to, int colour, float lift)
            {
                Vector3 a = Edge(i, from), b = Edge(i, to), c = Edge(i + 1, to), d = Edge(i + 1, from);
                a.y += lift; b.y += lift; c.y += lift; d.y += lift;
                if (Vector3.Cross(b - a, d - a).y > 0f) dirt.Quad(a, d, c, b, colour);
                else dirt.Quad(a, b, c, d, colour);
            }

            float half = RoadWidth * 0.5f;
            for (int i = 0; i + 1 < points.Count; i++)
            {
                int tone = ((i / 3) % 3 == 0) ? 9 : 20;
                Strip(i, -half - 0.55f, -half, 21, 0f);
                Strip(i, half, half + 0.55f, 21, 0f);
                Strip(i, -half, half, tone, 0.012f);
                Strip(i, -1.45f, -0.85f, 8, 0.024f);       // the two wheel ruts
                Strip(i, 0.85f, 1.45f, 8, 0.024f);
                if (i % 4 < 2) Strip(i, -0.07f, 0.07f, 2, 0.03f);     // a worn centre line
            }

            var roadObject = MeshObject("RoadSurface", road, SaveMesh(dirt.ToMesh("Road_Dirt")), art.palette, false);
            roadObject.transform.position = Vector3.zero;

            // lamps along the road, traffic cones and signs by the gate
            var cone = SaveInkedMesh(LowPolyProps.Cone());
            for (int i = 6; i < points.Count - 4; i += 12)
            {
                Vector2 a = points[i - 1], b = points[i + 1], dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                float lampSide = (i / 12) % 2 == 0 ? 1f : -1f;
                Vector2 p = points[i] + side * (half + 1.4f) * lampSide;
                var lamp = MeshObject("RoadLamp", road, lampMesh, art.palette);
                lamp.transform.position = new Vector3(p.x, field.Surface(p.x, p.y), p.y);
                lamp.isStatic = true;
                if (i < 70) AddLampLight(lamp.transform, new Vector3(0f, 2.78f, 0f), 12f, 2.6f);
            }

            foreach (var (x, z) in new[] { (-3.6f, -37.5f), (3.6f, -37.5f), (-3.2f, -41.5f), (3.2f, -41.5f) })
            {
                var go = MeshObject("Cone", road, cone, art.palette);
                go.transform.position = new Vector3(x, field.Surface(x, z) + 0.02f, z);
            }
            PlaceDecor(road, art.cartel, new Vector3(-4.6f, field.Surface(-4.6f, -39f), -39f), 20f, 1.1f, null, 0f);
            PlaceDecor(road, art.cartel, new Vector3(4.6f, field.Surface(4.6f, -48f), -48f), 200f, 1.1f, null, 0f);
        }
    }
}
