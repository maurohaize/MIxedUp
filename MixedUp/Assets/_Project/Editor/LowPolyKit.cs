using System.Collections.Generic;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// More flat-shaded props, mostly for the two extra maps and the challenge structures: cabins, rock blocks, a lighthouse,
    /// pier decks, boats, shipping containers, a power pylon, mine gear, an igloo... Same trick as LowPolyProps: every face takes
    /// one cell of the 30-colour palette, so they all share one material and get the inked outline.
    /// </summary>
    public static class LowPolyKit
    {
        public const int Gold = 0, Sand = 1, LightSand = 2, Clay = 3, Rust = 4, DarkRust = 5, Brown = 6, Maroon = 7, Mauve = 8, Wood = 9,
            Khaki = 10, Olive = 11, Grass = 12, OliveBrown = 13, DarkWood = 14, Espresso = 15, Teal = 16, DeepTeal = 17, Moss = 18,
            Stone = 19, Tan = 20, Cocoa = 21, Brick = 22, Salmon = 23, Pink = 24, Snow = 25, Grey = 26, Black = 27, Navy = 28, LightTeal = 29;

        static int Hash(int a, int b, int seed)
        {
            unchecked
            {
                int h = a * 73856093 ^ b * 19349663 ^ seed * 83492791;
                h ^= h >> 13;
                h *= 1274126177;
                return h & 0x7fffffff;
            }
        }

        static float Rand(int a, int b, int seed) => Hash(a, b, seed) % 10000 / 10000f;

        // ------------------------------------------------------------- shapes

        static void Tri(LowPoly.MeshBuilder b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 outward, int palette)
        {
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), outward) >= 0f) b.Triangle(p0, p1, p2, palette);
            else b.Triangle(p2, p1, p0, palette);
        }

        static void Quad(LowPoly.MeshBuilder b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 outward, int palette)
        {
            Tri(b, p0, p1, p2, outward, palette);
            Tri(b, p0, p2, p3, outward, palette);
        }

        public static void Box(LowPoly.MeshBuilder b, Vector3 centre, Vector3 size, int palette, int top = -1, Quaternion? rotation = null) =>
            LowPolyProps.Box(b, Matrix4x4.TRS(centre, rotation ?? Quaternion.identity, Vector3.one), size, palette, top);

        public static void Cylinder(LowPoly.MeshBuilder b, Vector3 foot, float rBottom, float rTop, float height, int sides, int palette, int cap = -1,
            Quaternion? rotation = null, bool bottomCap = false, float twist = 0f) =>
            LowPolyProps.Frustum(b, Matrix4x4.TRS(foot, rotation ?? Quaternion.identity, Vector3.one), rBottom, rTop, height, sides, palette, cap, bottomCap, twist);

        public static void Blob(LowPoly.MeshBuilder b, Vector3 centre, Vector3 radii, int palette, int seed, int subdivisions = 1, float jitter = 0.1f) =>
            LowPoly.Icosphere(b, centre, radii, subdivisions, jitter, palette, seed);

        /// <summary>A square beam between two points.</summary>
        public static void Beam(LowPoly.MeshBuilder b, Vector3 from, Vector3 to, float thickness, int palette)
        {
            Vector3 along = to - from;
            if (along.sqrMagnitude < 1e-6f) return;
            var up = Mathf.Abs(along.normalized.y) > 0.98f ? Vector3.forward : Vector3.up;
            Box(b, (from + to) * 0.5f, new Vector3(thickness, thickness, along.magnitude), palette, -1, Quaternion.LookRotation(along.normalized, up));
        }

        /// <summary>A slab with thickness under the quad a-b-c-d (given in order round its top face).</summary>
        static void Slab(LowPoly.MeshBuilder b, Vector3 a, Vector3 c, Vector3 d, Vector3 e, float thickness, int top, int edge)
        {
            Vector3 normal = Vector3.Cross(c - a, e - a).normalized;
            if (normal.y < 0f) normal = -normal;
            Vector3 down = -normal * thickness;
            Quad(b, a, c, d, e, normal, top);
            Quad(b, a + down, c + down, d + down, e + down, -normal, edge);
            Vector3 centre = (a + c + d + e) * 0.25f;
            var ring = new[] { a, c, d, e };
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = ring[i], q = ring[(i + 1) % 4];
                Vector3 outward = ((p + q) * 0.5f - centre);
                outward -= Vector3.Dot(outward, normal) * normal;
                Quad(b, p, q, q + down, p + down, outward, edge);
            }
        }

        // ------------------------------------------------------------ rock

        /// <summary>
        /// A block of rock standing on `foot`, size.x by size.z and size.y tall, whose sides bulge outwards (never inwards) by up
        /// to `bulge`, so it can dress a box collider of the same size. The flat top keeps exactly the height of the box and wears
        /// a band of `top` (grass, snow, wood...).
        /// </summary>
        public static void RockBlock(LowPoly.MeshBuilder b, Vector3 foot, Vector3 size, float bulge, int seed, int[] rock, int top, float lip = 0.24f)
        {
            float hx = size.x * 0.5f, hz = size.z * 0.5f, h = size.y;

            // The ring round the block, corner by corner, without repeating the corners; every point knows which way is out
            // and along which side it may slide.
            var ring = new List<Vector2>();
            var outs = new List<Vector2>();
            var along = new List<Vector2>();
            var corner = new List<bool>();
            float step = 0.8f;
            void Side(Vector2 from, Vector2 to, Vector2 normal)
            {
                int n = Mathf.Max(2, Mathf.RoundToInt(Vector2.Distance(from, to) / 0.8f));
                step = Vector2.Distance(from, to) / n;
                for (int i = 0; i < n; i++)
                {
                    ring.Add(Vector2.Lerp(from, to, i / (float)n));
                    outs.Add(i == 0 ? (normal + new Vector2(-normal.y, normal.x) * -1f).normalized : normal);
                    along.Add((to - from).normalized);
                    corner.Add(i == 0);
                }
            }
            Side(new Vector2(-hx, -hz), new Vector2(hx, -hz), Vector2.down);
            Side(new Vector2(hx, -hz), new Vector2(hx, hz), Vector2.right);
            Side(new Vector2(hx, hz), new Vector2(-hx, hz), Vector2.up);
            Side(new Vector2(-hx, hz), new Vector2(-hx, -hz), Vector2.left);
            float slide = Mathf.Min(step, Mathf.Min(hx, hz)) * 0.28f;

            int rows = Mathf.Max(1, Mathf.RoundToInt((h - lip) / 0.75f));
            var levels = new List<float>();
            for (int j = 0; j <= rows; j++) levels.Add((h - lip) * j / rows);
            levels.Add(h);
            int last = levels.Count - 1;

            Vector3 P(int k, int j)
            {
                k = (k % ring.Count + ring.Count) % ring.Count;
                // every vertex juts out its own way, never inwards, so the faces break into the facets of a crag
                float amount = bulge * (0.15f + 0.85f * Rand(k, j, seed));
                if (corner[k]) amount *= 1.25f;
                if (j == 0) amount = bulge * (0.7f + 0.6f * Rand(k, 99, seed));
                if (j == last) amount = bulge * 0.45f;
                if (j == last - 1) amount = Mathf.Max(amount, bulge * 0.7f);
                var o = outs[k] * amount;
                if (!corner[k] && j < last) o += along[k] * (Rand(k, j + 50, seed) - 0.5f) * 2f * slide;
                float y = levels[j];
                if (j > 0 && j < last - 1) y += (Rand(k, j + 7, seed) - 0.5f) * 0.32f;
                return foot + new Vector3(ring[k].x + o.x, y, ring[k].y + o.y);
            }

            for (int k = 0; k < ring.Count; k++)
            {
                for (int j = 0; j < last; j++)
                {
                    Vector3 a = P(k, j), c = P(k + 1, j), d = P(k + 1, j + 1), e = P(k, j + 1);
                    Vector3 mid = (a + c + d + e) * 0.25f - foot;
                    var outward = new Vector3(mid.x, 0f, mid.z);
                    // The faces turned towards the sky hold snow or grass; the rest is one rock colour with the odd band of another.
                    int colour = j == last - 1 ? top : Hash(j, k / 4, seed) % 4 == 0 ? rock[1 + Hash(j, k / 4 + 1, seed) % (rock.Length - 1)] : rock[0];
                    Vector3 n1 = Vector3.Cross(c - a, d - a).normalized, n2 = Vector3.Cross(d - a, e - a).normalized;
                    if (Vector3.Dot(n1, outward) < 0f) n1 = -n1;
                    if (Vector3.Dot(n2, outward) < 0f) n2 = -n2;
                    Tri(b, a, c, d, outward, n1.y > 0.45f ? top : colour);
                    Tri(b, a, d, e, outward, n2.y > 0.45f ? top : colour);
                }
            }

            Vector3 centre = foot + new Vector3(0f, h, 0f);
            for (int k = 0; k < ring.Count; k++)
                Tri(b, centre, P(k, last), P(k + 1, last), Vector3.up, top);
        }

        /// <summary>Boulders round the foot of something, so it seems to grow out of the ground.</summary>
        public static void FootRocks(LowPoly.MeshBuilder b, Vector3 foot, Vector2 halfSize, int count, float size, int seed, int[] rock)
        {
            for (int i = 0; i < count; i++)
            {
                float t = (i + Rand(i, 1, seed) * 0.6f) / count;
                float perimeter = 2f * (halfSize.x + halfSize.y);
                float s = t * perimeter * 2f;
                Vector2 p;
                if (s < 2f * halfSize.x) p = new Vector2(-halfSize.x + s, -halfSize.y);
                else if ((s -= 2f * halfSize.x) < 2f * halfSize.y) p = new Vector2(halfSize.x, -halfSize.y + s);
                else if ((s -= 2f * halfSize.y) < 2f * halfSize.x) p = new Vector2(halfSize.x - s, halfSize.y);
                else { s -= 2f * halfSize.x; p = new Vector2(-halfSize.x, halfSize.y - Mathf.Min(s, 2f * halfSize.y)); }
                float r = size * (0.6f + Rand(i, 2, seed) * 0.7f);
                Blob(b, foot + new Vector3(p.x * 1.08f, r * 0.35f, p.y * 1.08f), new Vector3(r, r * 0.75f, r), rock[i % rock.Length], seed + i, 1, 0.16f);
            }
        }

        /// <summary>Icicles hanging under the lip of a block of the given size.</summary>
        public static void Icicles(LowPoly.MeshBuilder b, Vector3 foot, Vector3 size, int count, int seed)
        {
            float hx = size.x * 0.5f + 0.1f, hz = size.z * 0.5f + 0.1f;
            for (int i = 0; i < count; i++)
            {
                float t = (i + Rand(i, 3, seed) * 0.7f) / count * 4f;
                int side = Mathf.FloorToInt(t) % 4;
                float along = (t - Mathf.Floor(t)) * 2f - 1f;
                Vector3 p = side == 0 ? new Vector3(along * hx, 0f, -hz) : side == 1 ? new Vector3(hx, 0f, along * hz)
                          : side == 2 ? new Vector3(-along * hx, 0f, hz) : new Vector3(-hx, 0f, -along * hz);
                float length = 0.35f + Rand(i, 4, seed) * 0.8f;
                Cylinder(b, foot + p + new Vector3(0f, size.y - 0.2f, 0f), 0.09f, 0.01f, length, 5, i % 3 == 0 ? Snow : LightTeal, -1, Quaternion.Euler(180f, 0f, 0f));
            }
        }

        public static Mesh Rock(string name, Vector3 size, float bulge, int seed, int[] rock, int top, bool footRocks = true, bool icicles = false)
        {
            var b = new LowPoly.MeshBuilder();
            RockBlock(b, Vector3.zero, size, bulge, seed, rock, top);
            if (footRocks) FootRocks(b, Vector3.zero, new Vector2(size.x * 0.5f, size.z * 0.5f), Mathf.RoundToInt((size.x + size.z) * 0.6f), 0.5f, seed + 50, rock);
            if (icicles) Icicles(b, Vector3.zero, size, Mathf.RoundToInt((size.x + size.z) * 2.2f), seed + 80);
            return b.ToMesh(name);
        }

        // ---------------------------------------------------------- buildings

        /// <summary>Where the windows of a Cabin are (local centre, outward normal), so glowing panes can be laid over them.</summary>
        public static List<(Vector3 centre, Vector3 normal)> CabinWindows(float w, float d, float h)
        {
            var list = new List<(Vector3, Vector3)>();
            float y = Mathf.Min(h * 0.58f, 1.45f);
            if (w >= 3.6f) { list.Add((new Vector3(-w * 0.3f, y, d * 0.5f), Vector3.forward)); list.Add((new Vector3(w * 0.3f, y, d * 0.5f), Vector3.forward)); }
            else list.Add((new Vector3(w * 0.28f, y, d * 0.5f), Vector3.forward));
            list.Add((new Vector3(w * 0.5f, y, 0f), Vector3.right));
            list.Add((new Vector3(-w * 0.5f, y, 0f), Vector3.left));
            return list;
        }

        /// <summary>
        /// A small building: w wide (x), d deep (z), walls h tall, the door on the +z face, a gabled roof with its ridge along x.
        /// Log walls for a mountain hut, plain walls with corner posts otherwise; a cap of snow on the roof when `snow`.
        /// </summary>
        public static Mesh Cabin(string name, float w, float d, float h, int wall, int roof, int trim, bool logs, bool snow, bool chimney, float rise = -1f)
        {
            var b = new LowPoly.MeshBuilder();
            if (rise < 0f) rise = d * 0.42f;

            // walls
            Box(b, new Vector3(0f, 0.12f, 0f), new Vector3(w + 0.2f, 0.24f, d + 0.2f), Stone, Grey);
            if (logs)
            {
                Box(b, new Vector3(0f, h * 0.5f, 0f), new Vector3(w - 0.12f, h, d - 0.12f), Espresso);
                int rows = Mathf.Max(3, Mathf.RoundToInt(h / 0.34f));
                float step = h / rows;
                for (int r = 0; r < rows; r++)
                {
                    float y = step * (r + 0.5f);
                    int colour = r % 2 == 0 ? wall : DarkWood;
                    Box(b, new Vector3(0f, y, d * 0.5f), new Vector3(w + 0.36f, step * 0.94f, 0.3f), colour);
                    Box(b, new Vector3(0f, y, -d * 0.5f), new Vector3(w + 0.36f, step * 0.94f, 0.3f), colour);
                    float ys = Mathf.Min(y + step * 0.5f, h - step * 0.5f);
                    Box(b, new Vector3(w * 0.5f, ys, 0f), new Vector3(0.3f, step * 0.94f, d + 0.36f), r % 2 == 0 ? DarkWood : wall);
                    Box(b, new Vector3(-w * 0.5f, ys, 0f), new Vector3(0.3f, step * 0.94f, d + 0.36f), r % 2 == 0 ? DarkWood : wall);
                }
            }
            else
            {
                Box(b, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall);
                foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                    Box(b, new Vector3(x * w * 0.5f, h * 0.5f, z * d * 0.5f), new Vector3(0.22f, h + 0.02f, 0.22f), trim);
                Box(b, new Vector3(0f, h - 0.08f, d * 0.5f), new Vector3(w + 0.1f, 0.16f, 0.12f), trim);
                Box(b, new Vector3(0f, h - 0.08f, -d * 0.5f), new Vector3(w + 0.1f, 0.16f, 0.12f), trim);
            }

            // gables, filled with boards
            float ridge = h + rise;
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * w * 0.5f;
                Tri(b, new Vector3(x, h, -d * 0.5f), new Vector3(x, h, d * 0.5f), new Vector3(x, ridge, 0f), new Vector3(side, 0f, 0f), logs ? Wood : wall);
            }

            // the roof: two thick slabs meeting at the ridge, with an overhang
            const float overhang = 0.45f;
            float slope = rise / (d * 0.5f);
            float eaveY = h - overhang * slope;
            float ex = w * 0.5f + overhang * 0.8f, ez = d * 0.5f + overhang;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 e0 = new Vector3(-ex, eaveY, side * ez), e1 = new Vector3(ex, eaveY, side * ez);
                Vector3 r1 = new Vector3(ex, ridge + 0.02f, 0f), r0 = new Vector3(-ex, ridge + 0.02f, 0f);
                Vector3 lift = new Vector3(0f, 0.17f, 0f);
                Slab(b, e0 + lift, e1 + lift, r1 + lift, r0 + lift, 0.17f, roof, roof == Brick ? DarkRust : Espresso);
                if (snow)
                {
                    Vector3 s = new Vector3(0f, 0.29f, 0f), shrink = new Vector3(0f, 0f, -side * 0.12f);
                    Slab(b, e0 + s + shrink + new Vector3(0.12f, 0f, 0f), e1 + s + shrink - new Vector3(0.12f, 0f, 0f), r1 + s - new Vector3(0.12f, 0f, 0f), r0 + s + new Vector3(0.12f, 0f, 0f), 0.13f, Snow, Snow);
                }
            }
            Box(b, new Vector3(0f, ridge + 0.2f, 0f), new Vector3(w + overhang * 1.6f + 0.1f, 0.14f, 0.24f), snow ? Snow : trim);

            if (chimney)
            {
                Box(b, new Vector3(w * 0.27f, h + rise * 0.55f, -d * 0.2f), new Vector3(0.6f, rise + 1.1f, 0.6f), Stone, Grey);
                Box(b, new Vector3(w * 0.27f, h + rise * 1.05f + 0.6f, -d * 0.2f), new Vector3(0.74f, 0.14f, 0.74f), Grey, snow ? Snow : Grey);
            }

            // door, step and window frames (the panes are drawn dark; a glowing pane may be laid over them)
            float face = d * 0.5f + (logs ? 0.16f : 0f);
            Box(b, new Vector3(0f, 1.0f, face + 0.06f), new Vector3(1.0f, 2.0f, 0.1f), DarkWood);
            Box(b, new Vector3(0f, 2.06f, face + 0.08f), new Vector3(1.25f, 0.14f, 0.12f), trim);
            Box(b, new Vector3(-0.56f, 1.0f, face + 0.08f), new Vector3(0.12f, 2.0f, 0.12f), trim);
            Box(b, new Vector3(0.56f, 1.0f, face + 0.08f), new Vector3(0.12f, 2.0f, 0.12f), trim);
            Box(b, new Vector3(0.28f, 1.0f, face + 0.13f), new Vector3(0.1f, 0.1f, 0.06f), Gold);
            Box(b, new Vector3(0f, 0.1f, face + 0.5f), new Vector3(1.5f, 0.2f, 0.8f), Stone, Grey);
            foreach (var (centre, normal) in CabinWindows(w, d, h))
            {
                var rotation = Quaternion.LookRotation(normal);
                Vector3 at = centre + normal * (logs ? 0.17f : 0.03f);
                Box(b, at, new Vector3(0.86f, 0.66f, 0.06f), Navy, -1, rotation);
                Box(b, at + rotation * new Vector3(0f, 0.37f, 0.03f), new Vector3(1.04f, 0.1f, 0.1f), trim, -1, rotation);
                Box(b, at + rotation * new Vector3(0f, -0.37f, 0.06f), new Vector3(1.08f, 0.1f, 0.16f), trim, -1, rotation);
                Box(b, at + rotation * new Vector3(-0.48f, 0f, 0.03f), new Vector3(0.1f, 0.74f, 0.1f), trim, -1, rotation);
                Box(b, at + rotation * new Vector3(0.48f, 0f, 0.03f), new Vector3(0.1f, 0.74f, 0.1f), trim, -1, rotation);
                Box(b, at + rotation * new Vector3(0f, 0f, 0.04f), new Vector3(0.06f, 0.66f, 0.04f), trim, -1, rotation);
            }
            return b.ToMesh(name);
        }

        /// <summary>A big shed with a slate roof and wide double doors on the +z face (a warehouse).</summary>
        public static Mesh Shed(string name, float w, float d, float h, int wall, int roof, int door)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, 0.15f, 0f), new Vector3(w + 0.3f, 0.3f, d + 0.3f), Stone, Grey);
            Box(b, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall);
            // vertical boards
            for (float x = -w * 0.5f + 0.5f; x < w * 0.5f; x += 0.9f)
            {
                Box(b, new Vector3(x, h * 0.5f, -d * 0.5f - 0.04f), new Vector3(0.12f, h, 0.08f), DarkWood);
                if (Mathf.Abs(x) > 1.7f) Box(b, new Vector3(x, h * 0.5f, d * 0.5f + 0.04f), new Vector3(0.12f, h, 0.08f), DarkWood);
            }
            float rise = d * 0.3f;
            foreach (float side in new[] { -1f, 1f })
                Tri(b, new Vector3(side * w * 0.5f, h, -d * 0.5f), new Vector3(side * w * 0.5f, h, d * 0.5f), new Vector3(side * w * 0.5f, h + rise, 0f), new Vector3(side, 0f, 0f), wall);
            float ex = w * 0.5f + 0.35f, ez = d * 0.5f + 0.4f, eaveY = h - 0.4f * rise / (d * 0.5f);
            foreach (float side in new[] { -1f, 1f })
                Slab(b, new Vector3(-ex, eaveY + 0.16f, side * ez), new Vector3(ex, eaveY + 0.16f, side * ez), new Vector3(ex, h + rise + 0.18f, 0f), new Vector3(-ex, h + rise + 0.18f, 0f), 0.16f, roof, Espresso);
            // the double doors, a sign plate and a lamp bracket
            Box(b, new Vector3(-0.8f, 1.4f, d * 0.5f + 0.06f), new Vector3(1.55f, 2.8f, 0.1f), door);
            Box(b, new Vector3(0.8f, 1.4f, d * 0.5f + 0.06f), new Vector3(1.55f, 2.8f, 0.1f), door);
            foreach (float x in new[] { -0.8f, 0.8f })
            {
                Beam(b, new Vector3(x - 0.7f, 0.2f, d * 0.5f + 0.13f), new Vector3(x + 0.7f, 2.6f, d * 0.5f + 0.13f), 0.1f, DarkWood);
                Box(b, new Vector3(x, 2.65f, d * 0.5f + 0.13f), new Vector3(1.5f, 0.12f, 0.08f), DarkWood);
                Box(b, new Vector3(x, 0.15f, d * 0.5f + 0.13f), new Vector3(1.5f, 0.12f, 0.08f), DarkWood);
            }
            Box(b, new Vector3(0f, 3.05f, d * 0.5f + 0.02f), new Vector3(3.6f, 0.2f, 0.2f), DarkWood);
            Box(b, new Vector3(0f, h - 0.55f, d * 0.5f + 0.08f), new Vector3(1.6f, 0.6f, 0.06f), Snow);
            return b.ToMesh(name);
        }

        // ----------------------------------------------------------- lighthouse

        /// <summary>
        /// The body of a lighthouse round a 1.4 m square core, striped red and white, 7 m tall up to an eight-sided gallery whose top
        /// is at y = 7. The railing round the gallery leaves a gap facing `gapAngle` (degrees round y, from +x towards +z).
        /// </summary>
        public static Mesh LighthouseBody(string name, float gapAngle)
        {
            var b = new LowPoly.MeshBuilder();
            Cylinder(b, Vector3.zero, 1.55f, 1.45f, 0.5f, 10, Stone, Grey);
            const int bands = 6;
            float y = 0.5f;
            for (int i = 0; i < bands; i++)
            {
                float t0 = i / (float)bands, t1 = (i + 1) / (float)bands;
                float h = (6.65f - 0.5f) / bands;
                Cylinder(b, new Vector3(0f, y, 0f), Mathf.Lerp(1.22f, 0.95f, t0), Mathf.Lerp(1.22f, 0.95f, t1), h, 10, i % 2 == 0 ? Snow : Brick);
                y += h;
            }
            // the door and little windows going up
            Box(b, new Vector3(0f, 0.95f, 1.2f), new Vector3(0.75f, 1.5f, 0.2f), DarkWood);
            Box(b, new Vector3(0f, 1.75f, 1.22f), new Vector3(0.95f, 0.12f, 0.22f), Grey);
            for (int i = 0; i < 4; i++)
            {
                float a = (90f + i * 100f) * Mathf.Deg2Rad, r = Mathf.Lerp(1.2f, 0.98f, (2.2f + i * 1.15f) / 6.6f);
                var at = new Vector3(Mathf.Cos(a) * r, 2.2f + i * 1.15f, Mathf.Sin(a) * r);
                Box(b, at, new Vector3(0.34f, 0.46f, 0.34f), Navy, -1, Quaternion.LookRotation(new Vector3(at.x, 0f, at.z)));
            }
            // the gallery: a ring of brackets, the floor (wide enough to cover the 3.2 m square ledge) and a railing with a gap
            for (int i = 0; i < 8; i++)
            {
                float a = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                Beam(b, new Vector3(Mathf.Cos(a) * 0.95f, 5.7f, Mathf.Sin(a) * 0.95f), new Vector3(Mathf.Cos(a) * 2.05f, 6.68f, Mathf.Sin(a) * 2.05f), 0.14f, Grey);
            }
            Cylinder(b, new Vector3(0f, 6.7f, 0f), 2.25f, 2.3f, 0.3f, 8, Grey, Navy, null, true);
            for (int i = 0; i < 16; i++)
            {
                float deg = i * 22.5f;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, gapAngle)) < 30f) continue;
                float a = deg * Mathf.Deg2Rad, a2 = (deg + 22.5f) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 2.18f, 7.0f, Mathf.Sin(a) * 2.18f);
                Box(b, p + new Vector3(0f, 0.45f, 0f), new Vector3(0.07f, 0.9f, 0.07f), Black);
                if (Mathf.Abs(Mathf.DeltaAngle(deg + 22.5f, gapAngle)) < 30f) continue;
                var q = new Vector3(Mathf.Cos(a2) * 2.18f, 7.0f, Mathf.Sin(a2) * 2.18f);
                Beam(b, p + new Vector3(0f, 0.88f, 0f), q + new Vector3(0f, 0.88f, 0f), 0.07f, Black);
            }
            return b.ToMesh(name);
        }

        /// <summary>The height (above the gallery floor) of the middle of the lantern's glass.</summary>
        public const float LanternGlassMid = 3.4f;

        /// <summary>
        /// The lantern room over the gallery, high on four slim posts so whoever climbs up still fits under it: a dark frame,
        /// a red cap and a gilt ball. The glass itself (a glowing cylinder) is added in the scene.
        /// </summary>
        public static Mesh LanternRoom(string name)
        {
            var b = new LowPoly.MeshBuilder();
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                Box(b, new Vector3(x * 1.05f, 1.35f, z * 1.05f), new Vector3(0.1f, 2.7f, 0.1f), Black);
            Cylinder(b, new Vector3(0f, 2.65f, 0f), 1.3f, 1.3f, 0.18f, 8, Black, Grey, null, true, Mathf.PI / 8f);
            for (int i = 0; i < 8; i++)
            {
                float a = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                Box(b, new Vector3(Mathf.Cos(a) * 0.84f, 3.41f, Mathf.Sin(a) * 0.84f), new Vector3(0.07f, 1.17f, 0.07f), Black);
            }
            Cylinder(b, new Vector3(0f, 4.0f, 0f), 1.15f, 1.15f, 0.12f, 8, Black, Black, null, true, Mathf.PI / 8f);
            Cylinder(b, new Vector3(0f, 4.12f, 0f), 1.15f, 0.12f, 0.95f, 8, Brick, -1, null, false, Mathf.PI / 8f);
            Blob(b, new Vector3(0f, 5.17f, 0f), new Vector3(0.17f, 0.17f, 0.17f), Gold, 3, 1, 0.02f);
            Box(b, new Vector3(0f, 5.55f, 0f), new Vector3(0.05f, 0.6f, 0.05f), Black);
            return b.ToMesh(name);
        }

        // ---------------------------------------------------------- harbour

        /// <summary>A pier deck along +z (length) and x (width), its top at y = 0, planked, on posts that go down `depth` metres.</summary>
        public static Mesh PierDeck(string name, float length, float width, float depth, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            int planks = Mathf.Max(1, Mathf.RoundToInt(length / 0.52f));
            float step = length / planks;
            for (int i = 0; i < planks; i++)
            {
                float z = -length * 0.5f + step * (i + 0.5f);
                int colour = Hash(i, 1, seed) % 5 == 0 ? Tan : Hash(i, 2, seed) % 4 == 0 ? Khaki : Wood;
                float jitter = (Rand(i, 3, seed) - 0.5f) * 0.18f;
                Box(b, new Vector3(jitter, -0.06f, z), new Vector3(width + 0.1f, 0.12f, step * 0.9f), colour, -1);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Box(b, new Vector3(side * (width * 0.5f - 0.3f), -0.22f, 0f), new Vector3(0.22f, 0.2f, length), DarkWood);
                Box(b, new Vector3(side * (width * 0.5f + 0.02f), -0.12f, 0f), new Vector3(0.12f, 0.24f, length + 0.04f), DarkWood);
            }
            int posts = Mathf.Max(1, Mathf.RoundToInt(length / 2.6f));
            for (int i = 0; i <= posts; i++)
            {
                float z = Mathf.Lerp(-length * 0.5f + 0.2f, length * 0.5f - 0.2f, i / (float)posts);
                foreach (float side in new[] { -1f, 1f })
                {
                    bool bollard = (i == 0 || i == posts) || i % 3 == 0;
                    float top = bollard ? 0.32f : -0.02f;
                    Cylinder(b, new Vector3(side * (width * 0.5f + 0.06f), -depth, z), 0.17f, 0.15f, depth + top, 6, DarkWood, Espresso);
                }
            }
            return b.ToMesh(name);
        }

        /// <summary>A moored boat along +z (bow forward), its deck at y = 0.35 above the waterline at 0.</summary>
        public static Mesh Boat(string name, float length, float beam, int hull, int stripe, bool cabin, bool mast, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            float L = length * 0.5f, W = beam * 0.5f;
            // the outline of the deck, round from the stern: left corner, right corner, right side, bow, left side
            var ring = new List<Vector2>
            {
                new Vector2(-W * 0.86f, -L), new Vector2(W * 0.86f, -L), new Vector2(W, -L * 0.35f), new Vector2(W * 0.82f, L * 0.4f),
                new Vector2(0f, L), new Vector2(-W * 0.82f, L * 0.4f), new Vector2(-W, -L * 0.35f)
            };
            float top = 0.38f, band = 0.12f, keel = -0.55f;
            Vector3 At(Vector2 p, float y, float shrink) => new Vector3(p.x * shrink, y, p.y * (y < 0f ? 0.92f : 1f));
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 p = ring[i], q = ring[(i + 1) % ring.Count];
                Vector3 outward = new Vector3((p.x + q.x) * 0.5f, 0f, (p.y + q.y) * 0.5f);
                Quad(b, At(p, top, 1f), At(q, top, 1f), At(q, band, 0.97f), At(p, band, 0.97f), outward, stripe);
                Quad(b, At(p, band, 0.97f), At(q, band, 0.97f), At(q, keel, 0.42f), At(p, keel, 0.42f), outward + Vector3.down * 0.4f, hull);
                Tri(b, Vector3.up * keel, At(p, keel, 0.42f), At(q, keel, 0.42f), Vector3.down, hull);
                Tri(b, new Vector3(0f, top - 0.08f, 0f), At(p, top - 0.08f, 0.94f), At(q, top - 0.08f, 0.94f), Vector3.up, Wood);
            }
            // gunwale
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 p = ring[i], q = ring[(i + 1) % ring.Count];
                Beam(b, new Vector3(p.x, top + 0.04f, p.y), new Vector3(q.x, top + 0.04f, q.y), 0.1f, DarkWood);
            }
            if (cabin)
            {
                Box(b, new Vector3(0f, top + 0.5f, -L * 0.22f), new Vector3(beam * 0.62f, 1.0f, length * 0.3f), Snow);
                Box(b, new Vector3(0f, top + 1.06f, -L * 0.22f), new Vector3(beam * 0.7f, 0.12f, length * 0.34f), stripe);
                foreach (float side in new[] { -1f, 1f })
                    Box(b, new Vector3(side * beam * 0.31f, top + 0.62f, -L * 0.22f), new Vector3(0.05f, 0.32f, length * 0.18f), Navy);
                Box(b, new Vector3(0f, top + 0.62f, -L * 0.22f + length * 0.15f), new Vector3(beam * 0.4f, 0.32f, 0.05f), Navy);
            }
            else
            {
                for (int i = 0; i < 2; i++)
                    Box(b, new Vector3(0f, top - 0.05f, -L * 0.3f + i * L * 0.6f), new Vector3(beam * 0.8f, 0.08f, 0.32f), DarkWood);
            }
            if (mast)
            {
                Cylinder(b, new Vector3(0f, top, L * 0.15f), 0.08f, 0.06f, 3.4f, 6, DarkWood);
                Beam(b, new Vector3(0f, top + 1.2f, L * 0.15f), new Vector3(0f, top + 1.25f, -L * 0.7f), 0.08f, DarkWood);
                Cylinder(b, new Vector3(0f, top + 1.38f, L * 0.1f), 0.2f, 0.16f, L * 0.75f, 6, Snow, Snow, Quaternion.Euler(90f, 180f, 0f));
                b.Triangle(new Vector3(0f, top + 3.35f, L * 0.15f), new Vector3(0f, top + 3.0f, L * 0.15f), new Vector3(0f, top + 3.18f, L * 0.15f - 0.55f), stripe);
                b.Triangle(new Vector3(0f, top + 3.0f, L * 0.15f), new Vector3(0f, top + 3.35f, L * 0.15f), new Vector3(0f, top + 3.18f, L * 0.15f - 0.55f), stripe);
            }
            // a fender or two on the sides
            foreach (float side in new[] { -1f, 1f })
                Cylinder(b, new Vector3(side * (W + 0.05f), top - 0.5f, -L * 0.1f), 0.12f, 0.12f, 0.4f, 6, Snow, Snow);
            return b.ToMesh(name);
        }

        /// <summary>A shipping container lying along z, standing on `foot`.</summary>
        public static void Container(LowPoly.MeshBuilder b, Vector3 foot, Vector3 size, int colour, int seed, Quaternion rotation)
        {
            Vector3 centre = foot + rotation * new Vector3(0f, size.y * 0.5f, 0f);
            Box(b, centre, size - new Vector3(0.06f, 0.02f, 0.06f), colour, colour, rotation);
            float hx = size.x * 0.5f, hz = size.z * 0.5f;
            int ribs = Mathf.Max(3, Mathf.RoundToInt(size.z / 0.5f));
            for (int i = 1; i < ribs; i++)
            {
                float z = -hz + size.z * i / ribs;
                foreach (float side in new[] { -1f, 1f })
                    Box(b, foot + rotation * new Vector3(side * hx, size.y * 0.5f, z), new Vector3(0.06f, size.y * 0.86f, 0.14f), colour, -1, rotation);
            }
            // doors with their locking bars on the +z end, frames round both ends
            foreach (float side in new[] { -1f, 1f })
            {
                Box(b, foot + rotation * new Vector3(side * hx * 0.5f, size.y * 0.5f, hz + 0.01f), new Vector3(hx * 0.92f, size.y * 0.86f, 0.06f), colour, -1, rotation);
                for (int k = 0; k < 2; k++)
                    Box(b, foot + rotation * new Vector3(side * hx * (0.25f + k * 0.5f), size.y * 0.5f, hz + 0.06f), new Vector3(0.05f, size.y * 0.8f, 0.05f), Grey, -1, rotation);
                Box(b, foot + rotation * new Vector3(0f, side > 0 ? size.y - 0.05f : 0.05f, hz), new Vector3(size.x, 0.1f, 0.1f), Grey, -1, rotation);
                Box(b, foot + rotation * new Vector3(0f, side > 0 ? size.y - 0.05f : 0.05f, -hz), new Vector3(size.x, 0.1f, 0.1f), Grey, -1, rotation);
            }
            Box(b, foot + rotation * new Vector3(0f, size.y * 0.72f, -hz - 0.01f), new Vector3(size.x * 0.5f, size.y * 0.18f, 0.03f), Snow, -1, rotation);
        }

        /// <summary>A block of containers filling exactly `size` (so it dresses a box collider of that size), a few levels tall.</summary>
        public static Mesh ContainerStack(string name, Vector3 size, int levels, int[] colours, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            float h = size.y / levels;
            for (int level = 0; level < levels; level++)
            {
                bool alongX = level % 2 == 1;
                float across = alongX ? size.z : size.x, along = alongX ? size.x : size.z;
                int count = across > 3.2f ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    float offset = -across * 0.5f + across * (i + 0.5f) / count;
                    var rotation = Quaternion.Euler(0f, alongX ? 90f : 0f, 0f);
                    Vector3 foot = alongX ? new Vector3(0f, level * h, offset) : new Vector3(offset, level * h, 0f);
                    Container(b, foot, new Vector3(across / count, h, along), colours[Hash(level, i, seed) % colours.Length], seed + level * 7 + i, rotation);
                }
            }
            return b.ToMesh(name);
        }

        /// <summary>A pile of wooden crates filling `size` (on `foot`): crates about a metre tall, each with a dark frame and a brace.</summary>
        public static void CrateBlock(LowPoly.MeshBuilder b, Vector3 foot, Vector3 size, int seed)
        {
            int levels = Mathf.Max(1, Mathf.RoundToInt(size.y / 1.2f));
            float h = size.y / levels;
            for (int level = 0; level < levels; level++)
            {
                Vector3 c = foot + new Vector3(0f, h * (level + 0.5f), 0f);
                float shrink = level == 0 ? 0f : 0.04f * (Hash(level, 1, seed) % 3);
                Vector3 s = new Vector3(size.x - shrink, h - 0.02f, size.z - shrink);
                Box(b, c, s, Hash(level, 2, seed) % 3 == 0 ? Tan : Wood, Tan);
                float hx = s.x * 0.5f, hy = s.y * 0.5f, hz = s.z * 0.5f;
                foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                    Box(b, c + new Vector3(x * hx, 0f, z * hz), new Vector3(0.14f, s.y + 0.02f, 0.14f), DarkWood);
                foreach (float y in new[] { -1f, 1f })
                {
                    Box(b, c + new Vector3(0f, y * hy, -hz), new Vector3(s.x, 0.12f, 0.1f), DarkWood);
                    Box(b, c + new Vector3(0f, y * hy, hz), new Vector3(s.x, 0.12f, 0.1f), DarkWood);
                    Box(b, c + new Vector3(-hx, y * hy, 0f), new Vector3(0.1f, 0.12f, s.z), DarkWood);
                    Box(b, c + new Vector3(hx, y * hy, 0f), new Vector3(0.1f, 0.12f, s.z), DarkWood);
                }
                Beam(b, c + new Vector3(-hx + 0.1f, -hy + 0.1f, -hz - 0.03f), c + new Vector3(hx - 0.1f, hy - 0.1f, -hz - 0.03f), 0.1f, DarkWood);
                Beam(b, c + new Vector3(-hx + 0.1f, -hy + 0.1f, hz + 0.03f), c + new Vector3(hx - 0.1f, hy - 0.1f, hz + 0.03f), 0.1f, DarkWood);
                Box(b, c + new Vector3(0f, 0.1f, -hz - 0.05f), new Vector3(0.5f, 0.3f, 0.02f), Snow);
            }
        }

        /// <summary>A seagull gliding (wings out along x, beak towards +z).</summary>
        public static Mesh Gull(string name)
        {
            var b = new LowPoly.MeshBuilder();
            Blob(b, Vector3.zero, new Vector3(0.12f, 0.11f, 0.32f), Snow, 1, 1, 0.03f);
            Blob(b, new Vector3(0f, 0.08f, 0.26f), new Vector3(0.09f, 0.09f, 0.1f), Snow, 2, 0, 0.03f);
            Cylinder(b, new Vector3(0f, 0.07f, 0.34f), 0.03f, 0.005f, 0.12f, 4, Gold, -1, Quaternion.Euler(90f, 0f, 0f));
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 root0 = new Vector3(side * 0.08f, 0.03f, 0.12f), root1 = new Vector3(side * 0.08f, 0.03f, -0.1f);
                Vector3 elbow = new Vector3(side * 0.42f, 0.14f, 0.05f), tip = new Vector3(side * 0.85f, 0.05f, -0.12f);
                Tri(b, root0, elbow, root1, Vector3.up, Snow);
                Tri(b, root0, root1, elbow, Vector3.down, Snow);
                Tri(b, elbow, tip, root1, Vector3.up, Grey);
                Tri(b, elbow, root1, tip, Vector3.down, Grey);
            }
            b.Triangle(new Vector3(0f, 0.02f, -0.28f), new Vector3(-0.1f, 0.03f, -0.42f), new Vector3(0.1f, 0.03f, -0.42f), Grey);
            b.Triangle(new Vector3(0f, 0.02f, -0.28f), new Vector3(0.1f, 0.03f, -0.42f), new Vector3(-0.1f, 0.03f, -0.42f), Grey);
            return b.ToMesh(name);
        }

        /// <summary>The steel hull of a barge under a deck `size` (x, z) whose top is at y = 0: rusty sides down into the water.</summary>
        public static Mesh BargeHull(string name, Vector2 size)
        {
            var b = new LowPoly.MeshBuilder();
            float hx = size.x * 0.5f + 0.25f, hz = size.y * 0.5f + 0.25f;
            Box(b, new Vector3(0f, -0.45f, 0f), new Vector3(hx * 2f, 0.6f, hz * 2f), Rust, DarkRust);
            Box(b, new Vector3(0f, -1.0f, 0f), new Vector3(hx * 2f - 0.3f, 0.6f, hz * 2f - 0.3f), Black);
            Box(b, new Vector3(0f, -0.21f, 0f), new Vector3(hx * 2f + 0.1f, 0.16f, hz * 2f + 0.1f), Grey, Grey);
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i < 4; i++)
                    Cylinder(b, new Vector3(side * (hx + 0.08f), -0.45f, -hz + 1.2f + i * (hz * 2f - 2.4f) / 3f), 0.22f, 0.22f, 0.25f, 8, Black, Black, Quaternion.Euler(0f, 0f, 90f), true);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                Cylinder(b, new Vector3(x * (hx - 0.4f), 0f, z * (hz - 0.4f)), 0.16f, 0.13f, 0.4f, 8, Black, Grey);
            return b.ToMesh(name);
        }

        /// <summary>A big striped inflatable float that bounces people up: its top is 1.1 m up, 0.95 m across the middle.</summary>
        public static Mesh BounceBuoy(string name)
        {
            var b = new LowPoly.MeshBuilder();
            float[] ys = { 0f, 0.22f, 0.5f, 0.78f, 1.0f, 1.1f };
            float[] rs = { 0.5f, 0.84f, 0.96f, 0.9f, 0.62f, 0.25f };
            for (int i = 0; i + 1 < ys.Length; i++)
                Cylinder(b, new Vector3(0f, ys[i], 0f), rs[i], rs[i + 1], ys[i + 1] - ys[i], 12, i % 2 == 0 ? Brick : Snow, i == ys.Length - 2 ? Brick : -1);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                Blob(b, new Vector3(Mathf.Cos(a) * 0.95f, 0.5f, Mathf.Sin(a) * 0.95f), new Vector3(0.1f, 0.1f, 0.1f), Tan, i, 0, 0.05f);
            }
            Cylinder(b, new Vector3(0f, 0.47f, 0f), 0.985f, 0.985f, 0.07f, 12, Tan);
            return b.ToMesh(name);
        }

        public static Mesh Brazier(string name)
        {
            var b = new LowPoly.MeshBuilder();
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 30f) * Mathf.Deg2Rad;
                Beam(b, new Vector3(Mathf.Cos(a) * 0.55f, 0f, Mathf.Sin(a) * 0.55f), new Vector3(Mathf.Cos(a) * 0.25f, 0.8f, Mathf.Sin(a) * 0.25f), 0.07f, Black);
            }
            Cylinder(b, new Vector3(0f, 0.72f, 0f), 0.32f, 0.62f, 0.42f, 9, Black, -1, null, true);
            Cylinder(b, new Vector3(0f, 1.12f, 0f), 0.64f, 0.64f, 0.06f, 9, Grey);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f * Mathf.Deg2Rad, r = 0.15f + (i % 3) * 0.13f;
                Blob(b, new Vector3(Mathf.Cos(a) * r, 1.08f, Mathf.Sin(a) * r), new Vector3(0.14f, 0.1f, 0.14f), i % 2 == 0 ? Rust : DarkRust, i, 0, 0.2f);
            }
            return b.ToMesh(name);
        }

        public static Mesh Generator(string name)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, 0.08f, 0f), new Vector3(1.9f, 0.16f, 1.4f), Black);
            Box(b, new Vector3(0f, 0.75f, 0f), new Vector3(1.6f, 1.2f, 1.2f), Gold, Grey);
            for (int i = 0; i < 5; i++)
                Box(b, new Vector3(-0.5f + i * 0.25f, 0.8f, 0.61f), new Vector3(0.1f, 0.7f, 0.04f), Black);
            Box(b, new Vector3(0f, 0.25f, 0.61f), new Vector3(1.6f, 0.14f, 0.03f), Black);
            Box(b, new Vector3(0.55f, 0.95f, 0.62f), new Vector3(0.36f, 0.36f, 0.03f), Snow);
            Box(b, new Vector3(0.55f, 0.95f, 0.64f), new Vector3(0.08f, 0.24f, 0.02f), Black, -1, Quaternion.Euler(0f, 0f, 25f));
            Cylinder(b, new Vector3(-0.55f, 1.35f, -0.3f), 0.1f, 0.1f, 0.8f, 6, Grey, Black);
            // the coil on top between two insulators
            Cylinder(b, new Vector3(0.25f, 1.35f, 0f), 0.24f, 0.24f, 0.55f, 10, Clay, Rust);
            for (int i = 0; i < 4; i++) Cylinder(b, new Vector3(0.25f, 1.42f + i * 0.13f, 0f), 0.27f, 0.27f, 0.05f, 10, Rust);
            foreach (float x in new[] { -0.2f, 0.7f })
            {
                Cylinder(b, new Vector3(x, 1.35f, 0f), 0.08f, 0.06f, 0.32f, 6, LightTeal);
                for (int i = 0; i < 3; i++) Cylinder(b, new Vector3(x, 1.4f + i * 0.09f, 0f), 0.11f, 0.11f, 0.03f, 6, LightTeal);
            }
            // a cable drum
            Cylinder(b, new Vector3(-1.25f, 0.45f, 0.2f), 0.4f, 0.4f, 0.12f, 10, Wood, Wood, Quaternion.Euler(0f, 0f, 90f), true);
            Cylinder(b, new Vector3(-1.53f, 0.45f, 0.2f), 0.4f, 0.4f, 0.12f, 10, Wood, Wood, Quaternion.Euler(0f, 0f, 90f), true);
            Cylinder(b, new Vector3(-1.41f, 0.45f, 0.2f), 0.3f, 0.3f, 0.28f, 10, Black, Black, Quaternion.Euler(0f, 0f, 90f));
            return b.ToMesh(name);
        }

        /// <summary>A dockside crane: a stone pedestal, a cab, a lattice jib along +z whose tip is 8 m up, and a counterweight.</summary>
        public static Mesh Crane(string name)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, 0.4f, 0f), new Vector3(2.4f, 0.8f, 2.4f), Stone, Grey);
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Beam(b, new Vector3(Mathf.Cos(a) * 1.2f, 0.8f, Mathf.Sin(a) * 1.2f), new Vector3(Mathf.Cos(a) * 0.35f, 5.6f, Mathf.Sin(a) * 0.35f), 0.2f, Navy);
            }
            for (int level = 0; level < 4; level++)
            {
                float y = 1.6f + level * 1.1f, r = Mathf.Lerp(1.05f, 0.42f, (y - 0.8f) / 4.8f);
                for (int i = 0; i < 4; i++)
                {
                    float a = (45f + i * 90f) * Mathf.Deg2Rad, a2 = (45f + (i + 1) * 90f) * Mathf.Deg2Rad;
                    Beam(b, new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), new Vector3(Mathf.Cos(a2) * r, y, Mathf.Sin(a2) * r), 0.09f, Navy);
                }
            }
            Box(b, new Vector3(0f, 6.2f, -0.2f), new Vector3(1.7f, 1.3f, 2.0f), Gold, Grey);
            Box(b, new Vector3(0f, 6.35f, 0.81f), new Vector3(1.3f, 0.6f, 0.05f), Navy);
            Box(b, new Vector3(0f, 6.95f, -0.2f), new Vector3(1.9f, 0.15f, 2.2f), Grey);
            // the jib: two chords and the zigzag between them
            Vector3 root = new Vector3(0f, 6.4f, 0.8f), tip = new Vector3(0f, 8.2f, 9.6f);
            foreach (float side in new[] { -0.32f, 0.32f })
            {
                Beam(b, root + new Vector3(side, 0f, 0f), tip + new Vector3(side * 0.4f, 0f, 0f), 0.14f, Navy);
                Beam(b, root + new Vector3(side, 0.7f, 0f), tip + new Vector3(side * 0.4f, 0.25f, 0f), 0.1f, Navy);
            }
            for (int i = 0; i < 8; i++)
            {
                float t0 = i / 8f, t1 = (i + 1) / 8f;
                Vector3 a = Vector3.Lerp(root, tip, t0), c = Vector3.Lerp(root + Vector3.up * 0.7f, tip + Vector3.up * 0.25f, t1);
                Beam(b, a, c, 0.07f, Navy);
            }
            Box(b, new Vector3(0f, 7.0f, -2.2f), new Vector3(1.5f, 1.2f, 1.2f), Grey, Stone);
            Beam(b, new Vector3(0f, 7.1f, -1.0f), new Vector3(0f, 9.4f, 0.2f), 0.12f, Navy);
            Beam(b, new Vector3(0f, 9.4f, 0.2f), tip + new Vector3(0f, 0.25f, 0f), 0.05f, Black);
            Beam(b, new Vector3(0f, 9.4f, 0.2f), new Vector3(0f, 7.5f, -2.2f), 0.05f, Black);
            Box(b, tip + new Vector3(0f, 0.1f, 0f), new Vector3(0.5f, 0.4f, 0.5f), Grey);
            return b.ToMesh(name);
        }

        /// <summary>A market stall: a counter with crates of fish, two posts and a striped awning (open side towards +z).</summary>
        public static Mesh MarketStall(string name, int stripe, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1.0f, 0.9f), Wood, Tan);
            for (int i = 0; i < 4; i++) Box(b, new Vector3(-0.9f + i * 0.6f, 0.5f, 0.46f), new Vector3(0.08f, 0.9f, 0.04f), DarkWood);
            foreach (float x in new[] { -1.12f, 1.12f })
                Box(b, new Vector3(x, 1.15f, -0.45f), new Vector3(0.1f, 2.3f, 0.1f), DarkWood);
            foreach (float x in new[] { -1.12f, 1.12f })
                Box(b, new Vector3(x, 0.95f, 0.42f), new Vector3(0.08f, 1.9f, 0.08f), DarkWood);
            var tilt = Quaternion.Euler(16f, 0f, 0f);
            for (int i = 0; i < 6; i++)
                Box(b, new Vector3(-1.25f + i * 0.5f + 0.25f, 2.12f, 0f), new Vector3(0.5f, 0.06f, 1.45f), i % 2 == 0 ? stripe : Snow, -1, tilt);
            for (int i = 0; i < 6; i++)
                Box(b, new Vector3(-1.25f + i * 0.5f + 0.25f, 1.86f, 0.74f), new Vector3(0.5f, 0.24f, 0.04f), i % 2 == 0 ? stripe : Snow);
            // crates of fish and ice on the counter
            for (int c = 0; c < 2; c++)
            {
                float x = -0.6f + c * 1.2f;
                Box(b, new Vector3(x, 1.08f, 0.05f), new Vector3(0.95f, 0.16f, 0.6f), Tan, LightTeal);
                for (int f = 0; f < 3; f++)
                {
                    float fx = x - 0.3f + f * 0.3f;
                    Blob(b, new Vector3(fx, 1.2f, 0.05f), new Vector3(0.1f, 0.06f, 0.24f), (f + c + seed) % 3 == 0 ? Grey : LightTeal, f + c * 3 + seed, 0, 0.05f);
                    b.Triangle(new Vector3(fx, 1.2f, 0.27f), new Vector3(fx - 0.08f, 1.24f, 0.36f), new Vector3(fx + 0.08f, 1.24f, 0.36f), Grey);
                    b.Triangle(new Vector3(fx, 1.2f, 0.27f), new Vector3(fx + 0.08f, 1.24f, 0.36f), new Vector3(fx - 0.08f, 1.24f, 0.36f), Grey);
                }
            }
            Box(b, new Vector3(0.9f, 1.25f, -0.25f), new Vector3(0.36f, 0.4f, 0.04f), Snow);
            return b.ToMesh(name);
        }

        /// <summary>A drying rack for nets: two posts, a beam and a net draped over it with floats along the top.</summary>
        public static Mesh NetRack(string name, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            foreach (float x in new[] { -1.4f, 1.4f })
                Box(b, new Vector3(x, 0.95f, 0f), new Vector3(0.12f, 1.9f, 0.12f), DarkWood);
            Box(b, new Vector3(0f, 1.85f, 0f), new Vector3(3.1f, 0.1f, 0.12f), DarkWood);
            for (int i = 0; i < 6; i++)
            {
                float x0 = -1.25f + i * 0.42f, x1 = x0 + 0.42f;
                float sag0 = 0.15f * Mathf.Sin(i * 1.3f + seed), sag1 = 0.15f * Mathf.Sin((i + 1) * 1.3f + seed);
                foreach (float side in new[] { -1f, 1f })
                {
                    var a = new Vector3(x0, 1.82f, 0.02f * side);
                    var c = new Vector3(x1, 1.82f, 0.02f * side);
                    var d = new Vector3(x1, 0.45f + sag1, 0.28f * side);
                    var e = new Vector3(x0, 0.45f + sag0, 0.28f * side);
                    Quad(b, a, c, d, e, new Vector3(0f, 0.2f, side), i % 2 == 0 ? DeepTeal : Teal);
                }
                Blob(b, new Vector3(x0 + 0.21f, 1.92f, 0f), new Vector3(0.09f, 0.09f, 0.09f), i % 2 == 0 ? Gold : Rust, i + seed, 0, 0.05f);
            }
            return b.ToMesh(name);
        }

        public static Mesh RopeCoil(string name)
        {
            var b = new LowPoly.MeshBuilder();
            for (int i = 0; i < 4; i++) Cylinder(b, new Vector3(0f, i * 0.08f, 0f), 0.42f - i * 0.02f, 0.42f - i * 0.02f, 0.08f, 10, i % 2 == 0 ? Tan : Sand, Cocoa);
            return b.ToMesh(name);
        }

        public static Mesh Bollard(string name)
        {
            var b = new LowPoly.MeshBuilder();
            Cylinder(b, Vector3.zero, 0.26f, 0.22f, 0.55f, 8, Black, Grey);
            Cylinder(b, new Vector3(0f, 0.55f, 0f), 0.33f, 0.3f, 0.14f, 8, Black, Black);
            return b.ToMesh(name);
        }

        /// <summary>A ring buoy on a post, facing +z.</summary>
        public static Mesh Lifebuoy(string name)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, 0.8f, 0f), new Vector3(0.14f, 1.6f, 0.14f), DarkWood);
            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) * 45f * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 0.3f, 1.2f + Mathf.Sin(a) * 0.3f, 0.12f);
                Box(b, p, new Vector3(0.13f, 0.27f, 0.13f), i % 2 == 0 ? Brick : Snow, -1, Quaternion.Euler(0f, 0f, (i + 0.5f) * 45f));
            }
            return b.ToMesh(name);
        }

        public static Mesh Anchor(string name)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, 1.0f, 0f), new Vector3(0.16f, 1.8f, 0.16f), Black);
            Box(b, new Vector3(0f, 1.7f, 0f), new Vector3(1.0f, 0.13f, 0.13f), Black);
            Cylinder(b, new Vector3(0f, 1.95f, -0.06f), 0.16f, 0.16f, 0.12f, 8, Black, Black, Quaternion.Euler(90f, 0f, 0f));
            Beam(b, new Vector3(0f, 0.16f, 0f), new Vector3(-0.62f, 0.62f, 0f), 0.15f, Black);
            Beam(b, new Vector3(0f, 0.16f, 0f), new Vector3(0.62f, 0.62f, 0f), 0.15f, Black);
            Cylinder(b, new Vector3(-0.66f, 0.55f, 0f), 0.14f, 0.01f, 0.3f, 4, Black);
            Cylinder(b, new Vector3(0.66f, 0.55f, 0f), 0.14f, 0.01f, 0.3f, 4, Black);
            return b.ToMesh(name);
        }

        /// <summary>A round café table with a little lamp and two stools.</summary>
        public static Mesh CafeTable(string name, int cloth)
        {
            var b = new LowPoly.MeshBuilder();
            Cylinder(b, Vector3.zero, 0.25f, 0.06f, 0.1f, 8, Black);
            Cylinder(b, new Vector3(0f, 0.1f, 0f), 0.05f, 0.05f, 0.62f, 6, Black);
            Cylinder(b, new Vector3(0f, 0.72f, 0f), 0.48f, 0.5f, 0.06f, 10, cloth, cloth);
            Cylinder(b, new Vector3(0.12f, 0.78f, 0.05f), 0.06f, 0.05f, 0.2f, 6, LightTeal, Snow);
            foreach (float side in new[] { -1f, 1f })
            {
                Cylinder(b, new Vector3(side * 0.8f, 0f, 0f), 0.04f, 0.04f, 0.42f, 5, DarkWood);
                Cylinder(b, new Vector3(side * 0.8f, 0.42f, 0f), 0.2f, 0.2f, 0.07f, 8, Wood, Tan);
            }
            return b.ToMesh(name);
        }

        public static Mesh Bench(string name)
        {
            var b = new LowPoly.MeshBuilder();
            foreach (float x in new[] { -0.7f, 0.7f })
                Box(b, new Vector3(x, 0.22f, 0f), new Vector3(0.1f, 0.44f, 0.42f), DarkWood);
            for (int i = 0; i < 3; i++) Box(b, new Vector3(0f, 0.47f, -0.15f + i * 0.15f), new Vector3(1.7f, 0.06f, 0.13f), Wood, Tan);
            for (int i = 0; i < 2; i++) Box(b, new Vector3(0f, 0.75f + i * 0.2f, -0.24f), new Vector3(1.7f, 0.12f, 0.05f), Wood);
            foreach (float x in new[] { -0.7f, 0.7f }) Box(b, new Vector3(x, 0.72f, -0.24f), new Vector3(0.08f, 0.6f, 0.06f), DarkWood);
            return b.ToMesh(name);
        }

        /// <summary>A fishing trap: a little wooden cage with a net bag.</summary>
        public static Mesh LobsterTrap(string name)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, 0.04f, 0f), new Vector3(0.8f, 0.08f, 0.55f), DarkWood);
            foreach (var (x, z) in new[] { (-0.37f, -0.24f), (0.37f, -0.24f), (-0.37f, 0.24f), (0.37f, 0.24f) })
                Box(b, new Vector3(x, 0.25f, z), new Vector3(0.06f, 0.42f, 0.06f), Wood);
            Box(b, new Vector3(0f, 0.43f, 0f), new Vector3(0.8f, 0.06f, 0.55f), Wood);
            Blob(b, new Vector3(0f, 0.25f, 0f), new Vector3(0.33f, 0.16f, 0.2f), Teal, 4, 1, 0.1f);
            return b.ToMesh(name);
        }

        // ------------------------------------------------------------- summit

        /// <summary>A closed outline made smooth (Catmull-Rom), with about `step` metres between points.</summary>
        public static List<Vector2> SmoothLoop(IList<Vector2> loop, float step)
        {
            var points = new List<Vector2>();
            int n = loop.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = loop[(i - 1 + n) % n], p1 = loop[i], p2 = loop[(i + 1) % n], p3 = loop[(i + 2) % n];
                int k = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / step));
                for (int s = 0; s < k; s++)
                {
                    float t = s / (float)k, t2 = t * t, t3 = t2 * t;
                    points.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            return points;
        }

        /// <summary>
        /// The ice of a frozen lake inside a closed `outline` (seen from `centre`), a hair above the ground: rings of clear and frosted
        /// ice, a few dark cracks. Meant for the glossy ice material.
        /// </summary>
        public static Mesh LakeIce(string name, IList<Vector2> outline, Vector2 centre, float y, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            float[] rings = { 0f, 0.3f, 0.55f, 0.78f, 0.92f, 1f };
            Vector3 P(int i, int r)
            {
                var o = outline[i % outline.Count];
                var p = Vector2.Lerp(centre, o, rings[r]);
                if (r > 0 && r < rings.Length - 1)
                    p += new Vector2(Rand(i, r, seed) - 0.5f, Rand(i, r + 9, seed) - 0.5f) * 0.9f;
                return new Vector3(p.x, y, p.y);
            }
            for (int i = 0; i < outline.Count; i++)
            {
                for (int r = 0; r + 1 < rings.Length; r++)
                {
                    // clear ice with big frosty patches; the frost thickens towards the shore
                    int colour = Hash(i / 4, r / 2, seed) % (r >= rings.Length - 2 ? 2 : 5) == 0 ? Snow : LightTeal;
                    if (r == 0) Tri(b, P(i, 0), P(i + 1, 1), P(i, 1), Vector3.up, colour);
                    else Quad(b, P(i, r), P(i, r + 1), P(i + 1, r + 1), P(i + 1, r), Vector3.up, colour);
                }
            }
            // cracks: thin dark zigzags running out from a few points
            for (int c = 0; c < 7; c++)
            {
                int start = Hash(c, 1, seed) % outline.Count;
                Vector2 from = Vector2.Lerp(centre, outline[start], 0.2f + Rand(c, 2, seed) * 0.4f);
                Vector2 dir = (outline[start] - centre).normalized;
                Vector2 p = from;
                int segments = 3 + Hash(c, 3, seed) % 3;
                for (int s = 0; s < segments; s++)
                {
                    float turn = (Rand(c, s + 10, seed) - 0.5f) * 1.2f;
                    var d = new Vector2(dir.x * Mathf.Cos(turn) - dir.y * Mathf.Sin(turn), dir.x * Mathf.Sin(turn) + dir.y * Mathf.Cos(turn));
                    Vector2 q = p + d * (1.2f + Rand(c, s + 20, seed) * 1.4f);
                    Vector2 side = new Vector2(-d.y, d.x) * 0.05f;
                    Quad(b, new Vector3(p.x - side.x, y + 0.004f, p.y - side.y), new Vector3(q.x - side.x, y + 0.004f, q.y - side.y),
                        new Vector3(q.x + side.x, y + 0.004f, q.y + side.y), new Vector3(p.x + side.x, y + 0.004f, p.y + side.y), Vector3.up, DeepTeal);
                    p = q;
                }
            }
            return b.ToMesh(name);
        }

        /// <summary>The shore of a frozen lake: lumps of snow and a few stones half over the edge of the ice.</summary>
        public static Mesh LakeShore(string name, IList<Vector2> outline, Vector2 centre, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            for (int i = 0; i < outline.Count; i++)
            {
                if (Hash(i, 1, seed) % 3 == 0) continue;
                Vector2 o = outline[i], dir = (o - centre).normalized, along = new Vector2(-dir.y, dir.x);
                Vector2 p = o + dir * (0.2f + Rand(i, 2, seed) * 0.9f) + along * (Rand(i, 6, seed) - 0.5f) * 1.2f;
                float r = 0.5f + Rand(i, 3, seed) * Rand(i, 7, seed) * 1.6f;
                bool stone = Hash(i, 4, seed) % 6 == 0;
                Blob(b, new Vector3(p.x, stone ? 0.12f : -0.05f, p.y), stone ? new Vector3(r * 0.6f, r * 0.45f, r * 0.6f) : new Vector3(r, r * 0.38f, r * 0.85f),
                    stone ? (Hash(i, 5, seed) % 2 == 0 ? Grey : Stone) : Snow, seed + i, 1, 0.12f);
            }
            return b.ToMesh(name);
        }

        /// <summary>A steel power pylon about 8 m tall with two cross-arms and glass insulators (its arms run along x).</summary>
        public static Mesh PowerPylon(string name)
        {
            var b = new LowPoly.MeshBuilder();
            const float height = 8.2f;
            Vector3 Leg(int i, float y)
            {
                float t = y / height, r = Mathf.Lerp(1.15f, 0.35f, t);
                float sx = i == 0 || i == 3 ? -1f : 1f, sz = i < 2 ? -1f : 1f;
                return new Vector3(sx * r, y, sz * r);
            }
            for (int i = 0; i < 4; i++)
            {
                Box(b, Leg(i, 0f) + new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.3f, 0.5f), Stone, Grey);
                Beam(b, Leg(i, 0f), Leg(i, height), 0.13f, Grey);
            }
            for (int level = 0; level < 4; level++)
            {
                float y0 = 0.4f + level * 1.8f, y1 = y0 + 1.8f;
                for (int i = 0; i < 4; i++)
                {
                    int j = (i + 1) % 4;
                    Beam(b, Leg(i, y0), Leg(j, y1), 0.06f, Grey);
                    Beam(b, Leg(j, y0), Leg(i, y1), 0.06f, Grey);
                    Beam(b, Leg(i, y1), Leg(j, y1), 0.07f, Grey);
                }
            }
            foreach (var (y, half) in new[] { (height - 0.1f, 2.6f), (height - 1.9f, 1.9f) })
            {
                Beam(b, new Vector3(-half, y, 0f), new Vector3(half, y, 0f), 0.14f, Grey);
                Beam(b, new Vector3(-half, y, 0f), new Vector3(-0.3f, y + 0.8f, 0f), 0.06f, Grey);
                Beam(b, new Vector3(half, y, 0f), new Vector3(0.3f, y + 0.8f, 0f), 0.06f, Grey);
                foreach (float x in new[] { -half + 0.15f, half - 0.15f })
                    for (int k = 0; k < 4; k++)
                        Cylinder(b, new Vector3(x, y - 0.12f - k * 0.13f, 0f), 0.13f, 0.09f, 0.08f, 7, LightTeal, Teal);
            }
            Cylinder(b, new Vector3(0f, height, 0f), 0.4f, 0.02f, 0.9f, 4, Grey, -1, null, false, Mathf.PI * 0.25f);
            // a warning plate on a leg
            Box(b, Leg(3, 1.7f) + new Vector3(0f, 0f, 0.1f), new Vector3(0.5f, 0.5f, 0.04f), Gold, -1, Quaternion.Euler(0f, 0f, 45f));
            Box(b, Leg(3, 1.7f) + new Vector3(0f, 0f, 0.13f), new Vector3(0.06f, 0.3f, 0.02f), Black, -1, Quaternion.Euler(0f, 0f, 20f));
            return b.ToMesh(name);
        }

        /// <summary>A transformer box with cooling fins and insulators on the lid.</summary>
        public static Mesh Transformer(string name)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, 0.1f, 0f), new Vector3(1.9f, 0.2f, 1.4f), Stone, Grey);
            Box(b, new Vector3(0f, 0.85f, 0f), new Vector3(1.4f, 1.3f, 1.0f), Teal, Grey);
            for (int i = 0; i < 6; i++)
                foreach (float side in new[] { -1f, 1f })
                    Box(b, new Vector3(-0.55f + i * 0.22f, 0.85f, side * 0.56f), new Vector3(0.08f, 1.05f, 0.14f), DeepTeal);
            for (int i = 0; i < 3; i++)
            {
                Cylinder(b, new Vector3(-0.45f + i * 0.45f, 1.5f, 0f), 0.08f, 0.06f, 0.45f, 6, LightTeal);
                for (int k = 0; k < 3; k++) Cylinder(b, new Vector3(-0.45f + i * 0.45f, 1.56f + k * 0.12f, 0f), 0.13f, 0.13f, 0.04f, 6, LightTeal);
            }
            Box(b, new Vector3(0f, 0.95f, 0.52f), new Vector3(0.46f, 0.46f, 0.03f), Gold, -1, Quaternion.Euler(0f, 0f, 45f));
            Box(b, new Vector3(0f, 0.95f, 0.54f), new Vector3(0.06f, 0.3f, 0.02f), Black, -1, Quaternion.Euler(0f, 0f, 20f));
            return b.ToMesh(name);
        }

        /// <summary>The timber frame round a mine entrance: posts either side of an opening `halfWidth` wide and `clearance` high, a lintel and a blank board above.</summary>
        public static Mesh MineFrame(string name, float halfWidth, float clearance)
        {
            var b = new LowPoly.MeshBuilder();
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * (halfWidth + 0.18f);
                Box(b, new Vector3(x, clearance * 0.5f, 0f), new Vector3(0.34f, clearance, 0.34f), DarkWood, Tan);
                Beam(b, new Vector3(x, clearance * 0.62f, -0.12f), new Vector3(side * (halfWidth - 0.3f), clearance + 0.02f, -0.12f), 0.13f, Wood);
            }
            Box(b, new Vector3(0f, clearance + 0.18f, 0f), new Vector3(halfWidth * 2f + 1.1f, 0.36f, 0.42f), DarkWood, Tan);
            Box(b, new Vector3(0f, clearance + 0.72f, -0.12f), new Vector3(1.5f, 0.52f, 0.08f), Wood);
            return b.ToMesh(name);
        }

        /// <summary>
        /// A square wooden ledge `thickness` deep whose top is at `top` (planks on two bearers), turned by `rotation`; with a
        /// layer of snow on it when `snow`.
        /// </summary>
        public static void Planks(LowPoly.MeshBuilder b, Vector3 top, Vector2 size, float thickness, Quaternion rotation, int seed, bool snow)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt(size.y / 0.46f));
            float step = size.y / n, plank = Mathf.Min(0.14f, thickness * 0.5f);
            float lift = snow ? -0.03f : 0f;
            for (int i = 0; i < n; i++)
            {
                float z = -size.y * 0.5f + step * (i + 0.5f);
                int colour = Hash(i, 1, seed) % 4 == 0 ? Tan : Hash(i, 2, seed) % 3 == 0 ? Khaki : Wood;
                Box(b, top + rotation * new Vector3((Rand(i, 3, seed) - 0.5f) * 0.12f, lift - plank * 0.5f, z), new Vector3(size.x + 0.06f, plank, step * 0.9f), colour, -1, rotation);
            }
            foreach (float side in new[] { -1f, 1f })
                Box(b, top + rotation * new Vector3(side * size.x * 0.32f, lift - plank - (thickness - plank) * 0.5f, 0f), new Vector3(0.2f, thickness - plank, size.y - 0.1f), DarkWood, -1, rotation);
            if (snow)
                Box(b, top + rotation * new Vector3(0f, -0.005f, 0f), new Vector3(size.x - 0.12f, 0.05f, size.y - 0.12f), Snow, Snow, rotation);
        }

        /// <summary>A copy of a palette mesh with some colours swapped (e.g. the grassy tunnel mound made rocky and snowy).</summary>
        public static Mesh Recolor(Mesh source, string name, params (int from, int to)[] swaps)
        {
            var mesh = Object.Instantiate(source);
            mesh.name = name;
            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);
            for (int i = 0; i < uvs.Count; i++)
            {
                int index = Mathf.Clamp(Mathf.FloorToInt(uvs[i].x * LowPoly.PaletteSize), 0, LowPoly.PaletteSize - 1);
                foreach (var (from, to) in swaps)
                    if (index == from) { uvs[i] = LowPoly.PaletteUv(to); break; }
            }
            mesh.SetUVs(0, uvs);
            return mesh;
        }

        /// <summary>Two rails on sleepers from `from` to `to` (on the ground).</summary>
        public static void Rails(LowPoly.MeshBuilder b, Vector3 from, Vector3 to)
        {
            Vector3 along = to - from;
            var rotation = Quaternion.LookRotation(along.normalized);
            int sleepers = Mathf.Max(2, Mathf.RoundToInt(along.magnitude / 0.7f));
            for (int i = 0; i <= sleepers; i++)
                Box(b, Vector3.Lerp(from, to, i / (float)sleepers) + Vector3.up * 0.05f, new Vector3(1.2f, 0.1f, 0.24f), DarkWood, -1, rotation);
            foreach (float side in new[] { -0.38f, 0.38f })
                Beam(b, from + rotation * new Vector3(side, 0.14f, 0f), to + rotation * new Vector3(side, 0.14f, 0f), 0.08f, Grey);
        }

        public static Mesh MineCart(string name, int load)
        {
            var b = new LowPoly.MeshBuilder();
            Cylinder(b, new Vector3(0f, 0.3f, 0f), 0.55f, 0.75f, 0.65f, 4, Rust, -1, null, true, Mathf.PI * 0.25f);
            Cylinder(b, new Vector3(0f, 0.92f, 0f), 0.78f, 0.78f, 0.06f, 4, Grey, -1, null, false, Mathf.PI * 0.25f);
            foreach (float x in new[] { -0.38f, 0.38f })
                foreach (float z in new[] { -0.35f, 0.35f })
                    Cylinder(b, new Vector3(x + Mathf.Sign(x) * 0.08f, 0.2f, z), 0.2f, 0.2f, 0.08f, 8, Black, Black, Quaternion.Euler(0f, 0f, 90f), true);
            for (int i = 0; i < 6; i++)
                Blob(b, new Vector3(-0.25f + (i % 3) * 0.25f, 0.95f, -0.15f + (i / 3) * 0.3f), new Vector3(0.18f, 0.14f, 0.18f), i % 2 == 0 ? load : Stone, i + 9, 0, 0.2f);
            return b.ToMesh(name);
        }

        /// <summary>A steel drum: `body` colour with a `band` of hazard paint.</summary>
        public static Mesh Drum(string name, int body, int band)
        {
            var b = new LowPoly.MeshBuilder();
            Cylinder(b, Vector3.zero, 0.33f, 0.33f, 0.92f, 10, body, Grey, null, true);
            foreach (float y in new[] { 0.2f, 0.66f }) Cylinder(b, new Vector3(0f, y, 0f), 0.345f, 0.345f, 0.06f, 10, Grey);
            Cylinder(b, new Vector3(0f, 0.36f, 0f), 0.338f, 0.338f, 0.22f, 10, band);
            Cylinder(b, new Vector3(0.12f, 0.92f, 0.08f), 0.06f, 0.06f, 0.04f, 6, Grey, Grey);
            return b.ToMesh(name);
        }

        /// <summary>An igloo (entrance to +z): a dome of snow blocks with a short tunnel.</summary>
        public static Mesh Igloo(string name)
        {
            var b = new LowPoly.MeshBuilder();
            const int rings = 5;
            for (int r = 0; r < rings; r++)
            {
                float a0 = r / (float)rings * Mathf.PI * 0.5f, a1 = (r + 1) / (float)rings * Mathf.PI * 0.5f;
                float y0 = Mathf.Sin(a0) * 1.7f, y1 = Mathf.Sin(a1) * 1.7f;
                float r0 = Mathf.Cos(a0) * 2.0f, r1 = Mathf.Max(0.001f, Mathf.Cos(a1) * 2.0f);
                Cylinder(b, new Vector3(0f, y0, 0f), r0, r1 < 0.05f ? 0f : r1, y1 - y0, 12, r % 2 == 0 ? Snow : LightTeal, Snow, null, false, r * 0.26f);
            }
            Box(b, new Vector3(0f, 0.55f, 2.0f), new Vector3(1.3f, 1.1f, 1.4f), Snow);
            Cylinder(b, new Vector3(0f, 1.1f, 1.3f), 0.65f, 0.65f, 1.4f, 8, Snow, Snow, Quaternion.Euler(90f, 0f, 0f));
            Box(b, new Vector3(0f, 0.5f, 2.71f), new Vector3(0.75f, 0.95f, 0.04f), Black);
            return b.ToMesh(name);
        }

        /// <summary>A penguin, about 0.9 m tall, looking towards +z.</summary>
        public static Mesh Penguin(string name)
        {
            var b = new LowPoly.MeshBuilder();
            Blob(b, new Vector3(0f, 0.42f, 0f), new Vector3(0.3f, 0.42f, 0.27f), Navy, 1, 1, 0.04f);
            Blob(b, new Vector3(0f, 0.38f, 0.1f), new Vector3(0.22f, 0.33f, 0.2f), Snow, 2, 1, 0.03f);
            Blob(b, new Vector3(0f, 0.82f, 0.02f), new Vector3(0.19f, 0.18f, 0.19f), Navy, 3, 1, 0.03f);
            Cylinder(b, new Vector3(0f, 0.8f, 0.18f), 0.06f, 0.01f, 0.16f, 4, Gold, -1, Quaternion.Euler(90f, 0f, 0f));
            foreach (float side in new[] { -1f, 1f })
            {
                Blob(b, new Vector3(side * 0.08f, 0.87f, 0.16f), new Vector3(0.03f, 0.03f, 0.03f), Snow, 5, 0, 0.02f);
                Blob(b, new Vector3(side * 0.3f, 0.45f, 0f), new Vector3(0.05f, 0.24f, 0.12f), Navy, 6, 0, 0.05f);
                Box(b, new Vector3(side * 0.1f, 0.03f, 0.08f), new Vector3(0.12f, 0.05f, 0.2f), Gold);
            }
            return b.ToMesh(name);
        }

        /// <summary>A pair of skis and poles stuck upright in the snow.</summary>
        public static Mesh Skis(string name, int colour)
        {
            var b = new LowPoly.MeshBuilder();
            foreach (float x in new[] { -0.12f, 0.12f })
            {
                Box(b, new Vector3(x, 0.8f, 0f), new Vector3(0.1f, 1.7f, 0.03f), colour, -1, Quaternion.Euler(0f, 0f, x * 25f));
                Box(b, new Vector3(x * 1.4f, 1.62f, 0.03f), new Vector3(0.1f, 0.12f, 0.06f), colour, -1, Quaternion.Euler(-30f, 0f, x * 25f));
            }
            foreach (float x in new[] { -0.45f, 0.45f })
            {
                Box(b, new Vector3(x, 0.65f, -0.2f), new Vector3(0.03f, 1.3f, 0.03f), Black, -1, Quaternion.Euler(0f, 0f, -x * 18f));
                Cylinder(b, new Vector3(x - x * 0.02f, 0.15f, -0.2f), 0.08f, 0.08f, 0.02f, 6, Black);
            }
            return b.ToMesh(name);
        }

        /// <summary>
        /// A dry-stone wall along x (`length` long, `height` tall, `depth` thick, standing on the origin): courses of uneven stones,
        /// a row of capstones and moss creeping over the top.
        /// </summary>
        public static Mesh StoneWall(string name, float length, float height, float depth, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, height * 0.45f, 0f), new Vector3(length - 0.1f, height * 0.9f, depth * 0.8f), Espresso);
            int courses = Mathf.Max(2, Mathf.RoundToInt(height / 0.35f));
            float course = height / courses;
            for (int c = 0; c < courses; c++)
            {
                float x = -length * 0.5f + (c % 2) * 0.3f;
                int i = 0;
                while (x < length * 0.5f - 0.05f)
                {
                    float w = Mathf.Min(0.45f + Rand(c, i, seed) * 0.5f, length * 0.5f - x);
                    float hgt = course * (0.85f + Rand(c, i + 40, seed) * 0.2f);
                    float d = depth * (0.92f + Rand(c, i + 80, seed) * 0.16f);
                    int colour = Hash(c, i, seed) % 5 == 0 ? Stone : Hash(c, i, seed + 1) % 4 == 0 ? Mauve : Grey;
                    Box(b, new Vector3(x + w * 0.5f, course * (c + 0.5f), (Rand(c, i + 120, seed) - 0.5f) * 0.08f), new Vector3(w - 0.04f, hgt, d), colour, -1,
                        Quaternion.Euler(0f, (Rand(c, i + 160, seed) - 0.5f) * 6f, (Rand(c, i + 200, seed) - 0.5f) * 5f));
                    x += w;
                    i++;
                }
            }
            for (float x = -length * 0.5f + 0.3f; x < length * 0.5f; x += 0.55f)
                Box(b, new Vector3(x, height + 0.06f, 0f), new Vector3(0.5f, 0.14f, depth * 1.08f), Grey, Stone, Quaternion.Euler(0f, 0f, (Rand((int)(x * 10f), 3, seed) - 0.5f) * 14f));
            for (int i = 0; i < Mathf.RoundToInt(length / 1.4f); i++)
            {
                float x = -length * 0.5f + (i + 0.5f) * length / Mathf.RoundToInt(length / 1.4f) + (Rand(i, 5, seed) - 0.5f) * 0.6f;
                Blob(b, new Vector3(x, height + 0.12f, (Rand(i, 6, seed) - 0.5f) * depth * 0.4f), new Vector3(0.45f + Rand(i, 7, seed) * 0.3f, 0.14f, depth * 0.55f), i % 2 == 0 ? Moss : Olive, seed + i, 1, 0.15f);
            }
            return b.ToMesh(name);
        }

        /// <summary>A dark evergreen bush wearing a cap of snow.</summary>
        public static Mesh SnowBush(string name, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            Blob(b, new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.6f, 0.8f), DeepTeal, seed, 1, 0.16f);
            Blob(b, new Vector3(0.55f, 0.35f, 0.2f), new Vector3(0.5f, 0.42f, 0.5f), Moss, seed + 1, 1, 0.16f);
            Blob(b, new Vector3(0.05f, 0.86f, 0f), new Vector3(0.62f, 0.22f, 0.6f), Snow, seed + 2, 1, 0.12f);
            Blob(b, new Vector3(0.6f, 0.66f, 0.2f), new Vector3(0.38f, 0.15f, 0.38f), Snow, seed + 3, 1, 0.12f);
            return b.ToMesh(name);
        }

        /// <summary>A stack of firewood under a little lean-to roof.</summary>
        public static Mesh WoodShelter(string name, bool snow)
        {
            var b = new LowPoly.MeshBuilder();
            foreach (float x in new[] { -1.1f, 1.1f })
                foreach (float z in new[] { -0.45f, 0.45f })
                    Box(b, new Vector3(x, 0.8f + (z < 0 ? 0.15f : 0f), z), new Vector3(0.12f, 1.6f + (z < 0 ? 0.3f : 0f), 0.12f), DarkWood);
            Slab(b, new Vector3(-1.3f, 1.95f, -0.75f), new Vector3(1.3f, 1.95f, -0.75f), new Vector3(1.3f, 1.65f, 0.75f), new Vector3(-1.3f, 1.65f, 0.75f), 0.1f, snow ? Snow : Brick, DarkWood);
            for (int row = 0; row < 4; row++)
                for (int i = 0; i < 6 - (row % 2); i++)
                    Cylinder(b, new Vector3(-0.95f + i * 0.38f + (row % 2) * 0.19f, 0.17f + row * 0.32f, -0.4f), 0.16f, 0.16f, 0.8f, 6, i % 2 == 0 ? Wood : Tan, Tan, Quaternion.Euler(90f, 0f, 0f), true);
            return b.ToMesh(name);
        }

        /// <summary>A board of signs to hold a notice (two legs behind the board, a little roof).</summary>
        public static Mesh NoticeFrame(string name, float width, float height, float centreY)
        {
            var b = new LowPoly.MeshBuilder();
            float top = centreY + height * 0.5f;
            foreach (float x in new[] { -width * 0.5f + 0.1f, width * 0.5f - 0.1f })
                Box(b, new Vector3(x, (top + 0.12f) * 0.5f, -0.09f), new Vector3(0.12f, top + 0.12f, 0.12f), DarkWood, Tan);
            Box(b, new Vector3(0f, centreY, -0.02f), new Vector3(width, height, 0.07f), Wood);
            Box(b, new Vector3(0f, centreY + height * 0.5f - 0.04f, 0.02f), new Vector3(width + 0.04f, 0.08f, 0.04f), DarkWood);
            Box(b, new Vector3(0f, centreY - height * 0.5f + 0.04f, 0.02f), new Vector3(width + 0.04f, 0.08f, 0.04f), DarkWood);
            Box(b, new Vector3(-width * 0.5f + 0.04f, centreY, 0.02f), new Vector3(0.08f, height, 0.04f), DarkWood);
            Box(b, new Vector3(width * 0.5f - 0.04f, centreY, 0.02f), new Vector3(0.08f, height, 0.04f), DarkWood);
            Slab(b, new Vector3(-width * 0.5f - 0.15f, top + 0.12f, 0.2f), new Vector3(width * 0.5f + 0.15f, top + 0.12f, 0.2f),
                new Vector3(width * 0.5f + 0.15f, top + 0.3f, -0.2f), new Vector3(-width * 0.5f - 0.15f, top + 0.3f, -0.2f), 0.06f, Brick, DarkWood);
            return b.ToMesh(name);
        }

        /// <summary>The post of a direction sign with a pointed cap (the arms are added one by one).</summary>
        public static Mesh SignPost(string name, float height)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.16f, height, 0.16f), DarkWood, Tan);
            Cylinder(b, new Vector3(0f, height, 0f), 0.16f, 0.01f, 0.22f, 4, Brick, -1, null, false, Mathf.PI * 0.25f);
            Blob(b, new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.12f, 0.28f), Stone, 3, 0, 0.2f);
            return b.ToMesh(name);
        }

        /// <summary>An arrow plank pointing along +z, its centre half-way along it: 2 m long and 0.42 m tall.</summary>
        public static Mesh ArrowPlank(string name, int colour)
        {
            var b = new LowPoly.MeshBuilder();
            const float length = 2.0f, height = 0.42f, thick = 0.07f;
            Box(b, new Vector3(0f, 0f, 0f), new Vector3(thick, height, length - 0.3f), colour);
            // the arrow head
            float z0 = length * 0.5f - 0.15f, z1 = length * 0.5f + 0.12f;
            foreach (float side in new[] { -1f, 1f })
            {
                Tri(b, new Vector3(side * thick * 0.5f, height * 0.5f, z0), new Vector3(side * thick * 0.5f, -height * 0.5f, z0), new Vector3(side * thick * 0.5f, 0f, z1), new Vector3(side, 0f, 0f), colour);
            }
            Quad(b, new Vector3(-thick * 0.5f, height * 0.5f, z0), new Vector3(thick * 0.5f, height * 0.5f, z0), new Vector3(thick * 0.5f, 0f, z1), new Vector3(-thick * 0.5f, 0f, z1), new Vector3(0f, 1f, 1f), colour);
            Quad(b, new Vector3(-thick * 0.5f, -height * 0.5f, z0), new Vector3(thick * 0.5f, -height * 0.5f, z0), new Vector3(thick * 0.5f, 0f, z1), new Vector3(-thick * 0.5f, 0f, z1), new Vector3(0f, -1f, 1f), colour);
            // a nail head at the post
            Box(b, new Vector3(0f, 0f, -length * 0.5f + 0.2f), new Vector3(thick + 0.03f, 0.08f, 0.08f), Grey);
            return b.ToMesh(name);
        }

        /// <summary>A giant tree stump (the meadow's raised platform): `radius` wide, `height` tall, flat top, roots round the foot.</summary>
        public static Mesh Stump(string name, float radius, float height, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            Cylinder(b, Vector3.zero, radius * 1.08f, radius, height - 0.12f, 12, Brown, -1, null, false, seed);
            Cylinder(b, new Vector3(0f, height - 0.12f, 0f), radius, radius * 0.98f, 0.12f, 12, Tan, Sand, null, false, seed);
            for (int ring = 1; ring < 4; ring++)
                Cylinder(b, new Vector3(0f, height - 0.005f, 0f), radius * ring / 4f, radius * ring / 4f, 0.012f, 12, ring % 2 == 0 ? Sand : Tan, ring % 2 == 0 ? Sand : Tan, null, false, seed);
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 7f * 360f + Rand(i, 1, seed) * 30f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Beam(b, dir * radius * 0.8f + Vector3.up * 0.9f, dir * (radius + 1.1f + Rand(i, 2, seed) * 0.5f) + Vector3.up * 0.05f, 0.42f, i % 2 == 0 ? Brown : Espresso);
            }
            for (int i = 0; i < 9; i++)
            {
                float a = (i * 40f + 15f) * Mathf.Deg2Rad, y = 0.4f + Rand(i, 3, seed) * (height - 0.9f);
                var at = new Vector3(Mathf.Cos(a) * (radius + 0.05f), y, Mathf.Sin(a) * (radius + 0.05f));
                Box(b, at, new Vector3(0.12f, 0.5f + Rand(i, 4, seed) * 0.6f, 0.12f), Espresso, -1, Quaternion.LookRotation(new Vector3(at.x, 0f, at.z)));
            }
            // shelf fungi and moss on the bark
            for (int i = 0; i < 5; i++)
            {
                float a = (i * 70f + 40f) * Mathf.Deg2Rad, y = 0.6f + i * 0.33f;
                Cylinder(b, new Vector3(Mathf.Cos(a) * (radius + 0.12f), y, Mathf.Sin(a) * (radius + 0.12f)), 0.32f, 0.2f, 0.1f, 7, i % 2 == 0 ? Sand : Clay, Tan);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 10f) * Mathf.Deg2Rad;
                Blob(b, new Vector3(Mathf.Cos(a) * radius * 1.04f, 0.3f, Mathf.Sin(a) * radius * 1.04f), new Vector3(0.6f, 0.35f, 0.6f), i % 2 == 0 ? Moss : Olive, i + seed, 1, 0.15f);
            }
            return b.ToMesh(name);
        }
    }
}
