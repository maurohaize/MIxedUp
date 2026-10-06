using System.Collections.Generic;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// The river does not end at the edge of the map. To the west it comes out of a cave in a cliff and winds through
    /// the foothills; to the east it opens into a big lake with lily pads, reeds and a little pier. Two finer terrain
    /// meshes (valleys) replace the coarse hills there so the channel and the lake bed can be carved.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        // Grid-aligned rectangles (multiples of the 5 m hill grid) where the valley meshes replace the hills.
        static readonly Rect RiverWestRect = new Rect(-125f, -10f, 80f, 40f);
        static readonly Rect RiverEastRect = new Rect(45f, -35f, 90f, 85f);

        const float ChannelHalf = 4f;
        const float RiverWaterY = -0.12f;
        static readonly Vector2 LakeCenter = new Vector2(94f, 8f);
        const float LakeRadius = 27f;
        const float CaveX = -114f;

        static readonly Vector2[] RiverWest =
        {
            new Vector2(-45f, 9f), new Vector2(-58f, 12f), new Vector2(-72f, 7f), new Vector2(-86f, 4.5f), new Vector2(-100f, 9.5f), new Vector2(CaveX + 0.5f, 8f)
        };

        static readonly Vector2[] RiverEast =
        {
            new Vector2(45f, 9f), new Vector2(58f, 6.5f), new Vector2(70f, 9.5f), new Vector2(LakeCenter.x - LakeRadius + 4f, LakeCenter.y)
        };

        static List<Vector2> smoothWest, smoothEast;
        static List<Vector2> SmoothWest => smoothWest ?? (smoothWest = SmoothLine(RiverWest, 2f));
        static List<Vector2> SmoothEast => smoothEast ?? (smoothEast = SmoothLine(RiverEast, 2f));

        static List<Vector2> SmoothLine(Vector2[] line, float step)
        {
            var points = new List<Vector2>();
            for (int i = 0; i + 1 < line.Length; i++)
            {
                Vector2 p0 = line[Mathf.Max(0, i - 1)], p1 = line[i], p2 = line[i + 1], p3 = line[Mathf.Min(line.Length - 1, i + 2)];
                int n = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / step));
                for (int k = 0; k < n; k++)
                {
                    float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                    points.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            points.Add(line[line.Length - 1]);
            return points;
        }

        static float DistanceToLine(List<Vector2> points, float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector2 a = points[i], b = points[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        /// <summary>Distance to the centre of the nearest river (west or east of the map), ignoring the lake.</summary>
        static float RiverDistance(float x, float z) => Mathf.Min(DistanceToLine(SmoothWest, x, z), DistanceToLine(SmoothEast, x, z));

        /// <summary>The lake's outline is not a circle: its radius wobbles with the angle.</summary>
        static float LakeRadiusAt(float x, float z)
        {
            float angle = Mathf.Atan2(z - LakeCenter.y, x - LakeCenter.x);
            return LakeRadius + 3.2f * Mathf.Sin(angle * 3f + 0.7f) + 2f * Mathf.Sin(angle * 5f + 2f);
        }

        static float LakeDistance(float x, float z) => Vector2.Distance(new Vector2(x, z), LakeCenter) - LakeRadiusAt(x, z);

        /// <summary>Is this point in water, or on the bank of the valleys? Used to keep trees and rocks out of them.</summary>
        static bool NearValleyWater(float x, float z, float margin)
        {
            if (x < -45f && x > RiverWestRect.xMin - 5f && DistanceToLine(SmoothWest, x, z) < ChannelHalf + margin) return true;
            if (x > 45f && DistanceToLine(SmoothEast, x, z) < ChannelHalf + margin && x < LakeCenter.x) return true;
            if (x > 45f && LakeDistance(x, z) < margin) return true;
            // the cave and the cliff above it
            return x < CaveX + 12f && x > CaveX - 20f && Mathf.Abs(z - 8f) < 16f;
        }

        static float ValleyHeight(LowPoly.HillField hills, float x, float z)
        {
            float s = hills.Surface(x, z);
            bool west = x < 0f;
            float river = west ? DistanceToLine(SmoothWest, x, z) : DistanceToLine(SmoothEast, x, z);

            // Meadow: low near the water, rising towards the hills the further away it is.
            float meadow = Mathf.Lerp(0.05f, s, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7f, 28f, river)));
            float h = Mathf.Lerp(-0.4f, meadow, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ChannelHalf, ChannelHalf + 0.8f, river)));

            if (!west)
            {
                // The lake: a shallow shelf at the shore dropping towards the middle, with a sandy beach round it.
                float distance = LakeDistance(x, z);
                float shelf = -0.35f - 0.75f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, -9f, distance));
                float shore = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, 0f, distance));
                float beach = Mathf.Lerp(meadow, 0.04f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9f, 2.5f, distance)));
                float lakeHeight = distance <= 0f ? shelf : Mathf.Lerp(beach, shelf, shore);
                float inLake = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14f, 0f, distance));
                h = Mathf.Lerp(h, lakeHeight, inLake);
            }
            else
            {
                // The cliff the cave is cut into, behind the end of the river.
                float cliff = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CaveX + 4f, CaveX - 9f, x));
                h = Mathf.Lerp(h, 3f + 16f * Mathf.PerlinNoise(x * 0.12f, z * 0.12f) + 9f, cliff);
                h = Mathf.Lerp(h, Mathf.Min(h, -0.4f), Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CaveX + 3.5f, CaveX + 1f, x)) * (river < ChannelHalf ? 1f : 0f));
            }

            // Blend into the hills at the three outer sides of the valley (the side facing the map matches the map's river).
            Rect r = west ? RiverWestRect : RiverEastRect;
            float edge = Mathf.Min(west ? x - r.xMin : r.xMax - x, z - r.yMin, r.yMax - z);
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 10f));
            return Mathf.Lerp(s, h, weight);
        }

        /// <summary>The valley mesh: 1 m facets, earthy bed, a bank strip, a sandy beach and grass.</summary>
        static Mesh BuildValley(string name, Rect rect, LowPoly.HillField hills, int seed)
        {
            int ColourAt(float x, float z)
            {
                bool west = x < 0f;
                float river = west ? DistanceToLine(SmoothWest, x, z) : DistanceToLine(SmoothEast, x, z);
                if (west && x < CaveX + 8f && river < ChannelHalf + 6f) return 19;
                if (river < ChannelHalf + 0.25f) return 13;                                       // the bed
                if (river < ChannelHalf + 1.1f) return LowPoly.Bank;                              // the bank
                if (!west)
                {
                    float lake = LakeDistance(x, z);
                    if (lake < 0.5f) return 13;
                    if (lake < 3.2f) return LowPoly.Sand;                                         // beach
                }
                return -1;
            }

            return LowPoly.Terrain(name, rect.xMin, rect.yMin, rect.xMax, rect.yMax, 1f, LowPoly.Grass, seed,
                (x, z) => ValleyHeight(hills, x, z), ColourAt);
        }

        /// <summary>A flat ribbon of water following a river line.</summary>
        static Mesh BuildRiverWater(string name, List<Vector2> line, float halfWidth)
        {
            var b = new LowPoly.MeshBuilder();
            for (int i = 0; i + 1 < line.Count; i++)
            {
                Vector2 a = line[i], c = line[i + 1], dir = (c - a).normalized, side = new Vector2(-dir.y, dir.x);
                Vector2 prevDir = i > 0 ? (a - line[i - 1]).normalized : dir, nextDir = i + 2 < line.Count ? (line[i + 2] - c).normalized : dir;
                Vector2 sideA = new Vector2(-(dir + prevDir).normalized.y, (dir + prevDir).normalized.x), sideC = new Vector2(-(dir + nextDir).normalized.y, (dir + nextDir).normalized.x);
                Vector3 a0 = new Vector3(a.x + sideA.x * halfWidth, RiverWaterY, a.y + sideA.y * halfWidth);
                Vector3 a1 = new Vector3(a.x - sideA.x * halfWidth, RiverWaterY, a.y - sideA.y * halfWidth);
                Vector3 c0 = new Vector3(c.x + sideC.x * halfWidth, RiverWaterY, c.y + sideC.y * halfWidth);
                Vector3 c1 = new Vector3(c.x - sideC.x * halfWidth, RiverWaterY, c.y - sideC.y * halfWidth);
                int colour = (i % 5 == 0) ? 29 : 16;
                if (Vector3.Cross(c0 - a0, a1 - a0).y > 0f) { b.Triangle(a0, c0, a1, colour); b.Triangle(a1, c0, c1, colour); }
                else { b.Triangle(a0, a1, c0, colour); b.Triangle(a1, c1, c0, colour); }
            }
            return b.ToMesh(name);
        }

        static Mesh BuildLakeWater(string name)
        {
            var b = new LowPoly.MeshBuilder();
            const int sectors = 48, rings = 7;
            Vector3 Point(int ring, int sector)
            {
                float angle = sector / (float)sectors * Mathf.PI * 2f;
                float radius = ring == 0 ? 0f : Mathf.Max(0.1f, (LakeRadiusAt(LakeCenter.x + Mathf.Cos(angle) * 40f, LakeCenter.y + Mathf.Sin(angle) * 40f) + 1.1f) * ring / rings);
                return new Vector3(LakeCenter.x + Mathf.Cos(angle) * radius, RiverWaterY, LakeCenter.y + Mathf.Sin(angle) * radius);
            }
            for (int ring = 0; ring < rings; ring++)
            {
                for (int sector = 0; sector < sectors; sector++)
                {
                    int next = (sector + 1) % sectors;
                    Vector3 a = Point(ring, sector), c = Point(ring + 1, sector), d = Point(ring + 1, next), e = Point(ring, next);
                    int colour = ((ring * 7 + sector * 3) % 11 == 0) ? 29 : 16;
                    if (ring == 0) b.Triangle(a, d, c, colour);
                    else { b.Triangle(a, d, c, colour); b.Triangle(a, e, d, colour); }
                }
            }
            return b.ToMesh(name);
        }

        static void CreateRiverMeshes(ArtAssets art)
        {
            art.valleyWest = SaveMesh(BuildValley("Valley_West", RiverWestRect, art.hillField, 3));
            art.valleyEast = SaveMesh(BuildValley("Valley_East", RiverEastRect, art.hillField, 4));
            art.riverWest = SaveMesh(BuildRiverWater("River_West_Water", SmoothWest, ChannelHalf + 0.45f));
            art.riverEast = SaveMesh(BuildRiverWater("River_East_Water", SmoothEast, ChannelHalf + 0.45f));
            art.lake = SaveMesh(BuildLakeWater("Lake_Water"));
            art.cave = SaveInkedMesh(LowPolyProps.CaveArch());
        }

        /// <summary>Height of the visible ground outside the map, valleys included.</summary>
        static float OutsideHeight(ArtAssets art, float x, float z)
        {
            if (RiverWestRect.Contains(new Vector2(x, z)) || RiverEastRect.Contains(new Vector2(x, z))) return ValleyHeight(art.hillField, x, z);
            return art.hillField.Surface(x, z);
        }

        static void BuildRiverExtension(Transform env, Mats m, ArtAssets art)
        {
            var group = new GameObject("RiverBeyond").transform;
            group.SetParent(env, false);

            MeshObject("ValleyWest", group, art.valleyWest, art.palette);
            MeshObject("ValleyEast", group, art.valleyEast, art.palette);
            // The water is not static, so its meshes stay readable (tests check what stands in it).
            MeshObject("WaterWest", group, art.riverWest, fxWaterFar != null ? fxWaterFar : art.paletteWater, false).isStatic = false;
            MeshObject("WaterEast", group, art.riverEast, fxWaterFar != null ? fxWaterFar : art.paletteWater, false).isStatic = false;
            MeshObject("Lake", group, art.lake, fxLake != null ? fxLake : art.paletteWater, false).isStatic = false;

            // The cave the river springs from, in the cliff at the west end.
            float caveGround = ValleyHeight(art.hillField, CaveX, 8f);
            var cave = MeshObject("Cave", group, art.cave, art.palette);
            cave.transform.position = new Vector3(CaveX - 0.4f, Mathf.Min(caveGround, -0.4f), 8f);
            AddLampLight(cave.transform, new Vector3(2.6f, 3.4f, -5.2f), 9f, 2.2f);
            AddLampLight(cave.transform, new Vector3(2.6f, 3.4f, 5.2f), 9f, 2.2f);

            var rng = new System.Random(606);

            // Reeds along the banks of both rivers and the lake.
            var reeds = new[] { SaveInkedMesh(LowPolyProps.Reeds(1)), SaveInkedMesh(LowPolyProps.Reeds(2)) };
            int placed = 0;
            for (int attempt = 0; attempt < 1200 && placed < 150; attempt++)
            {
                float x = Mathf.Lerp(-122f, 130f, (float)rng.NextDouble()), z = Mathf.Lerp(-8f, 28f, (float)rng.NextDouble());
                if (x > -45f && x < 45f) continue;
                bool onEast = x > 0f;
                float edge = onEast ? Mathf.Min(DistanceToLine(SmoothEast, x, z) - ChannelHalf, Mathf.Abs(LakeDistance(x, z)) < 4f ? Mathf.Abs(LakeDistance(x, z)) : 99f)
                                    : DistanceToLine(SmoothWest, x, z) - ChannelHalf;
                if (edge < 0.2f || edge > 1.6f) continue;
                if (!onEast && x < CaveX + 6f) continue;
                var reed = MeshObject("Reeds", group, reeds[rng.Next(2)], art.palette);
                reed.transform.position = new Vector3(x, OutsideHeight(art, x, z) - 0.05f, z);
                reed.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                reed.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.6f, (float)rng.NextDouble());
                placed++;
            }

            // Lily pads on the lake.
            var lilies = new[] { SaveMesh(LowPolyProps.Lily(1)), SaveMesh(LowPolyProps.Lily(2)) };
            for (int i = 0; i < 40; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f, r = (0.3f + (float)rng.NextDouble() * 0.6f) * LakeRadius;
                float x = LakeCenter.x + Mathf.Cos(angle) * r, z = LakeCenter.y + Mathf.Sin(angle) * r;
                if (LakeDistance(x, z) > -3f) continue;
                var lily = MeshObject("Lily", group, lilies[i % 2], art.palette);
                lily.isStatic = false;
                lily.transform.position = new Vector3(x, RiverWaterY + 0.02f, z);
                lily.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
                lily.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.8f, (float)rng.NextDouble());
                var bob = lily.AddComponent<Bob>();
                bob.height = 0.02f;
            }

            // A small wooden pier reaching into the lake, with a lantern at its end.
            Vector3 shore = new Vector3(LakeCenter.x - LakeRadiusAt(LakeCenter.x - 40f, LakeCenter.y) + 1.0f, 0f, LakeCenter.y - 8f);
            var pier = new GameObject("Pier").transform;
            pier.SetParent(group, false);
            pier.position = new Vector3(shore.x, 0f, shore.z);
            for (int i = 0; i < 9; i++)
            {
                Prim(PrimitiveType.Cube, "Plank", pier, new Vector3(i * 0.62f, 0.05f, 0f), new Vector3(0.58f, 0.1f, 1.8f), i % 2 == 0 ? m.wood : m.woodDark, false, null, true);
                if (i % 3 == 0)
                {
                    foreach (var z in new[] { -0.85f, 0.85f })
                        Prim(PrimitiveType.Cube, "PierPost", pier, new Vector3(i * 0.62f, -0.3f, z), new Vector3(0.14f, 1.0f, 0.14f), m.woodDark, false, null, true);
                }
            }
            var lantern = MeshObject("PierLamp", pier, lampMesh, art.palette);
            lantern.transform.localPosition = new Vector3(5.4f, 0.1f, 0.8f);
            AddLampLight(lantern.transform, new Vector3(0f, 2.78f, 0f), 11f, 2.4f);
        }
    }
}
