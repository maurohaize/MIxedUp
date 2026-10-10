using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Procedural flat-shaded meshes whose colours come from the 30-colour palette texture (one UV per triangle),
    /// the same trick the hand-made props use. Gives hills, ground, clouds and grass the same faceted look.
    /// </summary>
    public static class LowPoly
    {
        public const int PaletteSize = 30;

        // Palette indices (see Art/Textures/palette.png)
        // Olive shades only, like the sample: the facets get their contrast from lighting, not from colour.
        public static readonly int[] Grass = { 12, 12, 13, 11, 12, 13 };
        public static readonly int[] Meadow = { 13, 12, 11, 19 };
        public static readonly int[] Earth = { 13, 19, 11, 19 };
        public static readonly int[] Rock = { 19, 19, 26 };
        public const int Teal = 16, LightTeal = 29, Cream = 25, Wood = 9, DarkWood = 14, Bank = 21, Sand = 1;

        public static Vector2 PaletteUv(int index) =>
            new Vector2((Mathf.Clamp(index, 0, PaletteSize - 1) + 0.5f) / PaletteSize, 0.5f);

        public sealed class MeshBuilder
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector2> uvs = new List<Vector2>();
            readonly List<int> indices = new List<int>();

            public void Triangle(Vector3 a, Vector3 b, Vector3 c, int palette)
            {
                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                var uv = PaletteUv(palette);
                uvs.Add(uv); uvs.Add(uv); uvs.Add(uv);
                indices.Add(i); indices.Add(i + 1); indices.Add(i + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int palette)
            {
                Triangle(a, b, c, palette);
                Triangle(a, c, d, palette);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        static int Hash(int x, int z, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ z * 19349663 ^ seed * 83492791;
                h ^= h >> 13;
                h *= 1274126177;
                return h & 0x7fffffff;
            }
        }

        static int Pick(int[] set, int x, int z, int seed) => set[Hash(x, z, seed) % set.Length];

        // ------------------------------------------------------------- ground

        /// <summary>A flat patchwork of palette tiles (a few olive shades), facing up, at height y.</summary>
        public static Mesh Patchwork(string name, float x0, float z0, float x1, float z1, float y, float tile, int[] palette, int seed)
        {
            var b = new MeshBuilder();
            int nx = Mathf.CeilToInt((x1 - x0) / tile), nz = Mathf.CeilToInt((z1 - z0) / tile);
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float ax = x0 + i * tile, bx = Mathf.Min(x1, ax + tile);
                    float az = z0 + j * tile, bz = Mathf.Min(z1, az + tile);
                    b.Quad(new Vector3(ax, y, az), new Vector3(ax, y, bz), new Vector3(bx, y, bz), new Vector3(bx, y, az),
                        Pick(palette, i, j, seed));
                }
            }
            return b.ToMesh(name);
        }

        /// <summary>
        /// Faceted ground over a rectangle with rolling heights. Colour is picked per 4 m block from `palette`, except where
        /// `pathColor` returns a palette index (>= 0), which colours single tiles (trails, patches).
        /// </summary>
        public static Mesh Terrain(string name, float x0, float z0, float x1, float z1, float tile, int[] palette, int seed,
            System.Func<float, float, float> height, System.Func<float, float, int> pathColor, bool alternateDiagonal = true)
        {
            var b = new MeshBuilder();
            int nx = Mathf.CeilToInt((x1 - x0) / tile), nz = Mathf.CeilToInt((z1 - z0) / tile);
            int block = Mathf.Max(1, Mathf.RoundToInt(4f / tile));

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float ax = x0 + i * tile, bx = Mathf.Min(x1, ax + tile);
                    float az = z0 + j * tile, bz = Mathf.Min(z1, az + tile);
                    var a = new Vector3(ax, height(ax, az), az);
                    var c = new Vector3(ax, height(ax, bz), bz);
                    var d = new Vector3(bx, height(bx, bz), bz);
                    var e = new Vector3(bx, height(bx, az), az);

                    int color = pathColor != null ? pathColor((ax + bx) * 0.5f, (az + bz) * 0.5f) : -1;
                    if (color < 0) color = Pick(palette, i / block, j / block, seed);

                    // Alternate the diagonal so the facets do not all lean the same way.
                    if (!alternateDiagonal || (i + j) % 2 == 0)
                    {
                        b.Triangle(a, c, d, color);
                        b.Triangle(a, d, e, color);
                    }
                    else
                    {
                        b.Triangle(a, c, e, color);
                        b.Triangle(c, d, e, color);
                    }
                }
            }
            return b.ToMesh(name);
        }

        /// <summary>The river: bed, both vertical banks and a faceted water surface with a gentle chop.</summary>
        public static Mesh River(string name, float x0, float x1, float z0, float z1, float bedY, float waterY)
        {
            var b = new MeshBuilder();
            const float tile = 5f;
            int nx = Mathf.CeilToInt((x1 - x0) / tile);

            for (int i = 0; i < nx; i++)
            {
                float ax = x0 + i * tile, bx = Mathf.Min(x1, ax + tile);
                b.Quad(new Vector3(ax, bedY, z0), new Vector3(ax, bedY, z1), new Vector3(bx, bedY, z1), new Vector3(bx, bedY, z0),
                    Pick(Earth, i, 0, 5));
                // banks: the south bank faces north (+z), the north bank faces south (-z), both towards the water
                b.Quad(new Vector3(ax, bedY, z0), new Vector3(bx, bedY, z0), new Vector3(bx, 0f, z0), new Vector3(ax, 0f, z0), Bank);
                b.Quad(new Vector3(bx, bedY, z1), new Vector3(ax, bedY, z1), new Vector3(ax, 0f, z1), new Vector3(bx, 0f, z1), Bank);
            }
            return b.ToMesh(name);
        }

        /// <param name="accent">Palette index of the odd lighter facets (a darker teal gives a calmer sea, as at dusk).</param>
        public static Mesh Water(string name, float x0, float x1, float z0, float z1, float y, int accent = LightTeal)
        {
            var b = new MeshBuilder();
            const float tile = 2.5f;
            int nx = Mathf.CeilToInt((x1 - x0) / tile), nz = Mathf.CeilToInt((z1 - z0) / tile);

            float H(int i, int j) => y + (Hash(i, j, 11) % 100 / 100f - 0.5f) * 0.05f;
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float ax = x0 + i * tile, bx = Mathf.Min(x1, ax + tile);
                    float az = z0 + j * tile, bz = Mathf.Min(z1, az + tile);
                    var p00 = new Vector3(ax, H(i, j), az);
                    var p01 = new Vector3(ax, H(i, j + 1), bz);
                    var p11 = new Vector3(bx, H(i + 1, j + 1), bz);
                    var p10 = new Vector3(bx, H(i + 1, j), az);
                    b.Triangle(p00, p01, p11, (i * 5 + j * 3) % 7 == 0 ? accent : Teal);
                    b.Triangle(p00, p11, p10, (i * 3 + j * 5) % 9 == 0 ? accent : Teal);
                }
            }
            return b.ToMesh(name);
        }

        // -------------------------------------------------------------- hills

        static float Noise(float x, float z, float scale, float offset) =>
            Mathf.PerlinNoise(x * scale + offset, z * scale + offset * 0.7f);

        /// <summary>
        /// The mountains around the play area: a smooth height function plus the faceted surface that is actually drawn,
        /// so things can be stood on the visible triangles. `flatten` (0 = flat, 1 = untouched) calms the ground along the road.
        /// </summary>
        public sealed class HillField
        {
            public readonly Rect hole;
            public readonly float cell, halfExtent;
            public readonly int seed, n;
            /// <summary>Rectangles (on the grid lines) where another, finer mesh replaces the hills (the river valleys).</summary>
            public readonly List<Rect> cutouts = new List<Rect>();
            readonly float[,] heights;
            readonly float cx, cz;
            readonly System.Func<float, float, float> flatten;

            public HillField(Rect hole, float cell, float halfExtent, int seed, System.Func<float, float, float> flatten = null)
            {
                this.hole = hole;
                this.cell = cell;
                this.halfExtent = halfExtent;
                this.seed = seed;
                this.flatten = flatten;
                n = Mathf.RoundToInt(halfExtent * 2f / cell);
                cx = hole.center.x;
                cz = hole.center.y;
                heights = new float[n + 1, n + 1];
                for (int i = 0; i <= n; i++)
                    for (int j = 0; j <= n; j++)
                        heights[i, j] = Height(X(i), Z(j));
            }

            public float X(int i) => cx - halfExtent + i * cell;
            public float Z(int j) => cx - halfExtent + j * cell - cx + cz;
            public float Heights(int i, int j) => heights[i, j];

            public float Height(float px, float pz)
            {
                float dx = Mathf.Max(hole.xMin - px, 0f, px - hole.xMax);
                float dz = Mathf.Max(hole.yMin - pz, 0f, pz - hole.yMax);
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d <= 0f) return 0f;

                float calm = flatten != null ? flatten(px, pz) : 1f;
                float t = Mathf.SmoothStep(0f, 1f, d / 150f);
                float ridge = 1f - Mathf.Abs(Noise(px, pz, 0.009f, seed) * 2f - 1f);
                float broad = Noise(px, pz, 0.02f, seed * 3.1f);
                float bumps = Noise(px, pz, 0.08f, seed * 1.7f);
                float h = t * (3f + 52f * Mathf.Pow(ridge, 1.5f) * (0.5f + broad * 0.6f)) + bumps * 1.6f * t;
                return h * calm + Mathf.Min(d, 45f) * 0.05f;
            }

            /// <summary>Height of the drawn (triangulated) surface at a point; falls back to the smooth height outside the grid.</summary>
            public float Surface(float px, float pz)
            {
                float fx = (px - X(0)) / cell, fz = (pz - Z(0)) / cell;
                int i = Mathf.FloorToInt(fx), j = Mathf.FloorToInt(fz);
                if (i < 0 || j < 0 || i >= n || j >= n) return Height(px, pz);

                float u = fx - i, v = fz - j;
                float h00 = heights[i, j], h01 = heights[i, j + 1], h11 = heights[i + 1, j + 1], h10 = heights[i + 1, j];
                if ((i + j) % 2 == 0)
                    return v >= u ? h00 + v * (h01 - h00) + u * (h11 - h01) : h00 + u * (h10 - h00) + v * (h11 - h10);
                return u + v <= 1f ? h00 + u * (h10 - h00) + v * (h01 - h00) : h11 + (1f - u) * (h01 - h11) + (1f - v) * (h10 - h11);
            }

            public bool Inside(int i, int j)
            {
                float px = X(i), pz = Z(j);
                if (px >= hole.xMin - 0.01f && px <= hole.xMax + 0.01f && pz >= hole.yMin - 0.01f && pz <= hole.yMax + 0.01f) return true;
                foreach (var r in cutouts)
                    if (px >= r.xMin - 0.01f && px <= r.xMax + 0.01f && pz >= r.yMin - 0.01f && pz <= r.yMax + 0.01f) return true;
                return false;
            }
        }

        /// <summary>Which palette colours the hills use at each height (null = the olive meadow colours).</summary>
        public sealed class HillColors
        {
            public int[] low, mid, high, rock;
        }

        [System.ThreadStatic] static HillColors hillColors;

        /// <summary>Faceted mountains around a flat rectangular hole (the play area). Heights start at 0 at the hole's edge.</summary>
        public static Mesh Hills(string name, HillField field, HillColors colors = null)
        {
            hillColors = colors;
            try { return BuildHills(name, field); }
            finally { hillColors = null; }
        }

        static Mesh BuildHills(string name, HillField field)
        {
            var b = new MeshBuilder();
            int n = field.n;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (field.Inside(i, j) && field.Inside(i + 1, j) && field.Inside(i, j + 1) && field.Inside(i + 1, j + 1)) continue;

                    float x0 = field.X(i), x1 = field.X(i + 1);
                    float z0 = field.Z(j), z1 = field.Z(j + 1);
                    var p00 = new Vector3(x0, field.Heights(i, j), z0);
                    var p01 = new Vector3(x0, field.Heights(i, j + 1), z1);
                    var p11 = new Vector3(x1, field.Heights(i + 1, j + 1), z1);
                    var p10 = new Vector3(x1, field.Heights(i + 1, j), z0);

                    if ((i + j) % 2 == 0)
                    {
                        AddHillTriangle(b, p00, p01, p11, i, j, 0);
                        AddHillTriangle(b, p00, p11, p10, i, j, 1);
                    }
                    else
                    {
                        AddHillTriangle(b, p00, p01, p10, i, j, 2);
                        AddHillTriangle(b, p01, p11, p10, i, j, 3);
                    }
                }
            }
            return b.ToMesh(name);
        }

        static void AddHillTriangle(MeshBuilder b, Vector3 a, Vector3 c, Vector3 d, int i, int j, int salt)
        {
            float height = (a.y + c.y + d.y) / 3f;
            float steep = 1f - Mathf.Abs(Vector3.Cross(c - a, d - a).normalized.y);
            const int seed = 7;   // colour is chosen per 10 m block so facets read as big flat patches

            var colors = hillColors;
            int[] set;
            if (steep > 0.4f && height > 10f) set = colors != null ? colors.rock : Rock;
            else if (height < 12f) set = colors != null ? colors.low : Grass;
            else if (height < 28f) set = colors != null ? colors.mid : Meadow;
            else if (height < 44f) set = colors != null ? colors.high : Earth;
            else set = colors != null ? colors.rock : Rock;

            // Keep the winding facing up regardless of the diagonal used.
            int color = Pick(set, i / 2, j / 2, seed);
            if (Vector3.Cross(c - a, d - a).y < 0f) b.Triangle(a, d, c, color);
            else b.Triangle(a, c, d, color);
        }

        // ------------------------------------------------------------- clouds

        public static Mesh Cloud(string name, int seed)
        {
            var b = new MeshBuilder();
            var rng = new System.Random(seed);
            int puffs = 4 + rng.Next(3);
            for (int p = 0; p < puffs; p++)
            {
                float r = 5f + (float)rng.NextDouble() * 5f;
                var center = new Vector3((p - puffs * 0.5f) * 6.5f + (float)rng.NextDouble() * 3f, (float)rng.NextDouble() * 2.5f, (float)rng.NextDouble() * 4f - 2f);
                Icosphere(b, center, new Vector3(r * 1.25f, r * 0.7f, r), 1, 0.08f, Cream, seed + p);
            }
            return b.ToMesh(name);
        }

        public static Mesh Bush(string name, int seed, int palette)
        {
            var b = new MeshBuilder();
            Icosphere(b, new Vector3(0f, 0.55f, 0f), new Vector3(0.9f, 0.7f, 0.9f), 1, 0.16f, palette, seed);
            Icosphere(b, new Vector3(0.65f, 0.4f, 0.2f), new Vector3(0.55f, 0.45f, 0.55f), 1, 0.16f, palette, seed + 1);
            return b.ToMesh(name);
        }

        /// <summary>A few thin blades of grass.</summary>
        public static Mesh Tuft(string name, int seed, int palette)
        {
            var b = new MeshBuilder();
            var rng = new System.Random(seed);
            for (int i = 0; i < 6; i++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
                float h = 0.35f + (float)rng.NextDouble() * 0.35f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var side = new Vector3(-dir.z, 0f, dir.x) * 0.06f;
                var root = dir * (0.05f + (float)rng.NextDouble() * 0.12f);
                var tip = root + dir * 0.12f + Vector3.up * h;
                b.Triangle(root - side, tip, root + side, palette);
                b.Triangle(root + side, tip, root - side, palette);
            }
            return b.ToMesh(name);
        }

        public static Mesh Flower(string name, int seed, int petalPalette)
        {
            var b = new MeshBuilder();
            var rng = new System.Random(seed);
            float h = 0.35f + (float)rng.NextDouble() * 0.15f;
            var side = new Vector3(0.025f, 0f, 0f);
            b.Triangle(-side, new Vector3(0f, h, 0f), side, 12);
            b.Triangle(side, new Vector3(0f, h, 0f), -side, 12);
            Icosphere(b, new Vector3(0f, h + 0.05f, 0f), new Vector3(0.11f, 0.07f, 0.11f), 0, 0.1f, petalPalette, seed);
            return b.ToMesh(name);
        }

        // --------------------------------------------------------- icospheres

        static readonly float Phi = (1f + Mathf.Sqrt(5f)) / 2f;

        public static void Icosphere(MeshBuilder b, Vector3 center, Vector3 radii, int subdivisions, float jitter, int palette, int seed)
        {
            var verts = new List<Vector3>
            {
                new Vector3(-1, Phi, 0), new Vector3(1, Phi, 0), new Vector3(-1, -Phi, 0), new Vector3(1, -Phi, 0),
                new Vector3(0, -1, Phi), new Vector3(0, 1, Phi), new Vector3(0, -1, -Phi), new Vector3(0, 1, -Phi),
                new Vector3(Phi, 0, -1), new Vector3(Phi, 0, 1), new Vector3(-Phi, 0, -1), new Vector3(-Phi, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;

            var faces = new List<int[]>
            {
                new[] {0,11,5}, new[] {0,5,1}, new[] {0,1,7}, new[] {0,7,10}, new[] {0,10,11},
                new[] {1,5,9}, new[] {5,11,4}, new[] {11,10,2}, new[] {10,7,6}, new[] {7,1,8},
                new[] {3,9,4}, new[] {3,4,2}, new[] {3,2,6}, new[] {3,6,8}, new[] {3,8,9},
                new[] {4,9,5}, new[] {2,4,11}, new[] {6,2,10}, new[] {8,6,7}, new[] {9,8,1}
            };

            var cache = new Dictionary<long, int>();
            int Mid(int a, int c)
            {
                long key = a < c ? ((long)a << 32) + c : ((long)c << 32) + a;
                if (cache.TryGetValue(key, out int found)) return found;
                verts.Add(((verts[a] + verts[c]) * 0.5f).normalized);
                cache[key] = verts.Count - 1;
                return verts.Count - 1;
            }

            for (int s = 0; s < subdivisions; s++)
            {
                var next = new List<int[]>();
                foreach (var f in faces)
                {
                    int ab = Mid(f[0], f[1]), bc = Mid(f[1], f[2]), ca = Mid(f[2], f[0]);
                    next.Add(new[] { f[0], ab, ca });
                    next.Add(new[] { f[1], bc, ab });
                    next.Add(new[] { f[2], ca, bc });
                    next.Add(new[] { ab, bc, ca });
                }
                faces = next;
            }

            Vector3 Place(int index)
            {
                var d = verts[index];
                float k = 1f + (Hash(Mathf.RoundToInt(d.x * 1000f), Mathf.RoundToInt(d.y * 1000f) + Mathf.RoundToInt(d.z * 1000f) * 7, seed) % 1000 / 1000f - 0.5f) * 2f * jitter;
                return center + Vector3.Scale(d * k, radii);
            }

            foreach (var f in faces)
            {
                var a = Place(f[0]); var c = Place(f[1]); var d = Place(f[2]);
                // Outward facing: the triangle normal must point away from the centre.
                if (Vector3.Dot(Vector3.Cross(c - a, d - a), (a + c + d) / 3f - center) < 0f) b.Triangle(a, d, c, palette);
                else b.Triangle(a, c, d, palette);
            }
        }
    }
}
