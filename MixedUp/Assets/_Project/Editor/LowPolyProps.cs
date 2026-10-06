using System.Collections.Generic;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Flat-shaded building blocks (boxes, frustums, roofs, blobs) and the props assembled from them: hay bales, barrels,
    /// a windmill, a farmhouse, mushrooms... Colours are palette indices, exactly like the hand-made models.
    /// </summary>
    public static class LowPolyProps
    {
        // Palette indices (see Art/Textures/palette.png)
        const int Gold = 0, Sand = 1, LightSand = 2, Clay = 3, Rust = 4, DarkRust = 5, Brown = 6, MudA = 15, MudB = 21,
            Wood = 9, Olive = 11, DarkWood = 14, Espresso = 15, DeepTeal = 17, Moss = 18, Stone = 19, Tan = 20, Cocoa = 21,
            Brick = 22, Salmon = 23, Cream = 25, Grey = 26, Navy = 28, LightTeal = 29;

        public static Matrix4x4 At(float x, float y, float z, float yaw = 0f) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f), Vector3.one);

        public static Matrix4x4 At(Vector3 position, Quaternion rotation) => Matrix4x4.TRS(position, rotation, Vector3.one);

        // ------------------------------------------------------------- shapes

        static void Face(LowPoly.MeshBuilder b, Matrix4x4 m, Vector3 a, Vector3 c, Vector3 d, Vector3 e, Vector3 outward, int palette)
        {
            Vector3 p0 = m.MultiplyPoint3x4(a), p1 = m.MultiplyPoint3x4(c), p2 = m.MultiplyPoint3x4(d), p3 = m.MultiplyPoint3x4(e);
            Vector3 hint = m.MultiplyVector(outward);
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), hint) >= 0f) b.Quad(p0, p1, p2, p3, palette);
            else b.Quad(p3, p2, p1, p0, palette);
        }

        static void Tri(LowPoly.MeshBuilder b, Matrix4x4 m, Vector3 a, Vector3 c, Vector3 d, Vector3 outward, int palette)
        {
            Vector3 p0 = m.MultiplyPoint3x4(a), p1 = m.MultiplyPoint3x4(c), p2 = m.MultiplyPoint3x4(d);
            Vector3 hint = m.MultiplyVector(outward);
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), hint) >= 0f) b.Triangle(p0, p1, p2, palette);
            else b.Triangle(p2, p1, p0, palette);
        }

        /// <summary>A box centred on the matrix origin. The top may use another colour.</summary>
        public static void Box(LowPoly.MeshBuilder b, Matrix4x4 m, Vector3 size, int palette, int topPalette = -1)
        {
            Vector3 h = size * 0.5f;
            Vector3 v000 = new Vector3(-h.x, -h.y, -h.z), v100 = new Vector3(h.x, -h.y, -h.z), v110 = new Vector3(h.x, h.y, -h.z), v010 = new Vector3(-h.x, h.y, -h.z);
            Vector3 v001 = new Vector3(-h.x, -h.y, h.z), v101 = new Vector3(h.x, -h.y, h.z), v111 = new Vector3(h.x, h.y, h.z), v011 = new Vector3(-h.x, h.y, h.z);
            Face(b, m, v000, v010, v110, v100, Vector3.back, palette);
            Face(b, m, v001, v101, v111, v011, Vector3.forward, palette);
            Face(b, m, v000, v001, v011, v010, Vector3.left, palette);
            Face(b, m, v100, v110, v111, v101, Vector3.right, palette);
            Face(b, m, v010, v011, v111, v110, Vector3.up, topPalette >= 0 ? topPalette : palette);
            Face(b, m, v000, v100, v101, v001, Vector3.down, palette);
        }

        /// <summary>A prism/frustum standing on the matrix origin (y up). rTop = 0 makes a cone or pyramid.</summary>
        public static void Frustum(LowPoly.MeshBuilder b, Matrix4x4 m, float rBottom, float rTop, float height, int sides, int palette,
            int capPalette = -1, bool bottomCap = false, float twist = 0f)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f + twist, a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f + twist;
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * rBottom, 0f, Mathf.Sin(a0) * rBottom);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * rBottom, 0f, Mathf.Sin(a1) * rBottom);
                Vector3 t0 = new Vector3(Mathf.Cos(a0) * rTop, height, Mathf.Sin(a0) * rTop);
                Vector3 t1 = new Vector3(Mathf.Cos(a1) * rTop, height, Mathf.Sin(a1) * rTop);
                float mid = (a0 + a1) * 0.5f;
                Vector3 radial = new Vector3(Mathf.Cos(mid), (rBottom - rTop) / Mathf.Max(0.001f, height), Mathf.Sin(mid));

                if (rTop <= 0.0001f) Tri(b, m, b0, t0, b1, radial, palette);
                else Face(b, m, b0, t0, t1, b1, radial, palette);

                if (rTop > 0.0001f) Tri(b, m, new Vector3(0f, height, 0f), t1, t0, Vector3.up, capPalette >= 0 ? capPalette : palette);
                if (bottomCap) Tri(b, m, Vector3.zero, b0, b1, Vector3.down, capPalette >= 0 ? capPalette : palette);
            }
        }

        /// <summary>A gable roof over a width x depth footprint (ridge along the depth), sitting on the matrix origin.</summary>
        public static void GableRoof(LowPoly.MeshBuilder b, Matrix4x4 m, float width, float depth, float rise, float overhang, int palette, int gablePalette)
        {
            float w = width * 0.5f + overhang, d = depth * 0.5f + overhang;
            Vector3 l0 = new Vector3(-w, 0f, -d), l1 = new Vector3(-w, 0f, d), r0 = new Vector3(w, 0f, -d), r1 = new Vector3(w, 0f, d);
            Vector3 t0 = new Vector3(0f, rise, -d), t1 = new Vector3(0f, rise, d);
            Face(b, m, l0, t0, t1, l1, new Vector3(-rise, w, 0f), palette);
            Face(b, m, r0, r1, t1, t0, new Vector3(rise, w, 0f), palette);
            Tri(b, m, l0, r0, t0, Vector3.back, gablePalette);
            Tri(b, m, l1, t1, r1, Vector3.forward, gablePalette);
            Face(b, m, l0, l1, r1, r0, Vector3.down, gablePalette);
        }

        public static void Blob(LowPoly.MeshBuilder b, Vector3 center, Vector3 radii, int subdivisions, float jitter, int palette, int seed) =>
            LowPoly.Icosphere(b, center, radii, subdivisions, jitter, palette, seed);

        // -------------------------------------------------------------- props

        public static Mesh HayBale()
        {
            var b = new LowPoly.MeshBuilder();
            var lying = At(0f, 0.55f, 0f) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 90f)) * At(0f, -0.5f, 0f);
            Frustum(b, lying, 0.55f, 0.55f, 1.0f, 9, Gold, Sand, true);
            // twine bands
            foreach (var y in new[] { 0.22f, 0.78f })
                Frustum(b, lying * At(0f, y - 0.03f, 0f), 0.575f, 0.575f, 0.06f, 9, DarkWood);
            return b.ToMesh("Prop_HayBale");
        }

        public static Mesh Haystack()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 1.5f, 1.35f, 1.2f, 9, Gold);
            Frustum(b, At(0f, 1.2f, 0f), 1.35f, 0.15f, 1.5f, 9, Sand);
            return b.ToMesh("Prop_Haystack");
        }

        public static Mesh Barrel()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 0.36f, 0.33f, 0.95f, 10, Wood, Tan, true);
            foreach (var y in new[] { 0.16f, 0.72f })
                Frustum(b, At(0f, y, 0f), 0.375f, 0.375f, 0.07f, 10, DarkWood);
            return b.ToMesh("Prop_Barrel");
        }

        public static Mesh Crate()
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, At(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f), Wood, Tan);
            foreach (var (x, z) in new[] { (-0.42f, -0.42f), (0.42f, -0.42f), (-0.42f, 0.42f), (0.42f, 0.42f) })
                Box(b, At(x, 0.45f, z), new Vector3(0.13f, 0.94f, 0.13f), DarkWood);
            Box(b, At(0f, 0.45f, 0f), new Vector3(0.96f, 0.12f, 0.96f), DarkWood);
            return b.ToMesh("Prop_Crate");
        }

        /// <summary>A lamp post whose lantern is an open cage: the glowing bulb (LampBulb) is added in the scene and shows through.</summary>
        public static Mesh LampPost()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 0.14f, 0.1f, 0.2f, 8, Grey);
            Frustum(b, At(0f, 0.2f, 0f), 0.065f, 0.055f, 2.4f, 8, Espresso);
            Box(b, At(0f, 2.62f, 0f), new Vector3(0.36f, 0.06f, 0.36f), Espresso);            // lantern floor
            foreach (var (x, z) in new[] { (-0.17f, -0.17f), (0.17f, -0.17f), (-0.17f, 0.17f), (0.17f, 0.17f) })
                Box(b, At(x, 2.8f, z), new Vector3(0.04f, 0.34f, 0.04f), Espresso);            // cage posts
            Frustum(b, At(0f, 2.97f, 0f), 0.42f, 0.02f, 0.32f, 4, DarkRust, -1, false, Mathf.PI * 0.25f);
            return b.ToMesh("Prop_LampPost");
        }

        public static Mesh Log()
        {
            var b = new LowPoly.MeshBuilder();
            var lying = At(0f, 0.4f, 0f) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 90f)) * At(0f, -1.6f, 0f);
            Frustum(b, lying, 0.4f, 0.37f, 3.2f, 8, DarkWood, Tan, true);
            Frustum(b, lying * At(0f, 3.19f, 0f), 0.29f, 0.29f, 0.03f, 8, Cocoa);
            return b.ToMesh("Prop_Log");
        }

        /// <summary>The bounce pad: a big red toadstool with cream spots.</summary>
        public static Mesh Mushroom(string name = "Prop_Mushroom")
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 0.34f, 0.25f, 0.8f, 8, Cream);
            Blob(b, new Vector3(0f, 0.95f, 0f), new Vector3(1.05f, 0.62f, 1.05f), 1, 0.05f, Brick, 3);
            // flatten the underside with a salmon disc
            Frustum(b, At(0f, 0.74f, 0f), 0.8f, 0.55f, 0.09f, 9, Salmon);
            foreach (var (x, y, z, r) in new[] { (0.42f, 1.42f, 0.22f, 0.15f), (-0.34f, 1.44f, 0.32f, 0.13f), (0.0f, 1.52f, -0.4f, 0.14f), (-0.1f, 1.56f, 0.05f, 0.1f), (0.5f, 1.22f, -0.3f, 0.12f) })
                Blob(b, new Vector3(x, y, z), new Vector3(r, r * 0.55f, r), 0, 0.02f, Cream, 7);
            return b.ToMesh(name);
        }

        public static Mesh SweeperPost()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 1.0f, 0.85f, 0.25f, 10, Stone, Grey);
            Frustum(b, At(0f, 0.25f, 0f), 0.42f, 0.34f, 1.0f, 9, DarkWood, Tan);
            return b.ToMesh("Prop_SweeperPost");
        }

        /// <summary>The spinning log: 6.4 m long along X, with warning stripes.</summary>
        public static Mesh SweeperArm()
        {
            var b = new LowPoly.MeshBuilder();
            var lying = At(0f, 0.55f, 0f) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 90f)) * At(0f, -3.2f, 0f);
            Frustum(b, lying, 0.3f, 0.3f, 6.4f, 8, Clay, DarkRust, true);
            foreach (var x in new[] { -2.6f, -1.3f, 0f, 1.3f, 2.6f })
                Frustum(b, lying * At(0f, x + 3.2f - 0.1f, 0f), 0.325f, 0.325f, 0.2f, 8, Cream);
            Frustum(b, At(0f, 0.2f, 0f), 0.38f, 0.3f, 0.7f, 8, DarkWood);
            return b.ToMesh("Prop_SweeperArm");
        }

        public static Mesh Tent()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 1.9f, 0.02f, 1.8f, 4, Clay, -1, false, Mathf.PI * 0.25f);
            // a darker front flap
            Tri(b, At(0f, 0f, 0f), new Vector3(-0.55f, 0.01f, 1.3f), new Vector3(0.55f, 0.01f, 1.3f), new Vector3(0f, 1.15f, 0.78f), Vector3.forward, Brown);
            return b.ToMesh("Prop_Tent");
        }

        public static Mesh Campfire(string name, float scale)
        {
            var b = new LowPoly.MeshBuilder();
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                Blob(b, new Vector3(Mathf.Cos(a) * 0.8f, 0.12f, Mathf.Sin(a) * 0.8f) * scale, new Vector3(0.2f, 0.16f, 0.2f) * scale, 0, 0.18f, i % 2 == 0 ? Grey : Stone, 40 + i);
            }
            for (int i = 0; i < 4; i++)
            {
                var lean = At(0f, 0.1f * scale, 0f, i * 90f + 20f) * Matrix4x4.Translate(new Vector3(0f, 0f, 0.5f * scale)) * Matrix4x4.Rotate(Quaternion.Euler(-62f, 0f, 0f));
                Frustum(b, lean, 0.09f * scale, 0.08f * scale, 0.95f * scale, 6, DarkWood, Tan, true);
            }
            Frustum(b, At(0f, 0.12f * scale, 0f), 0.46f * scale, 0.02f, 1.5f * scale, 5, Clay);
            Frustum(b, At(0.16f * scale, 0.12f * scale, 0.06f * scale), 0.3f * scale, 0.02f, 1.2f * scale, 5, Brick, -1, false, 0.5f);
            Frustum(b, At(-0.12f * scale, 0.12f * scale, -0.12f * scale), 0.32f * scale, 0.02f, 1.25f * scale, 5, Gold, -1, false, 1.1f);
            Frustum(b, At(0f, 0.14f * scale, 0f), 0.22f * scale, 0.02f, 0.95f * scale, 5, LightSand);
            return b.ToMesh(name);
        }

        /// <summary>An irregular puddle of thick mud, a hair above the ground.</summary>
        public static Mesh MudPatch(float radius, int seed)
        {
            var b = new LowPoly.MeshBuilder();
            var rng = new System.Random(seed);
            const int segments = 16;
            var rim = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                float r = radius * (0.72f + (float)rng.NextDouble() * 0.38f);
                rim[i] = new Vector3(Mathf.Cos(a) * r, 0.03f, Mathf.Sin(a) * r);
            }
            for (int i = 0; i < segments; i++)
            {
                var c = new Vector3((float)(rng.NextDouble() - 0.5) * 0.4f, 0.03f, (float)(rng.NextDouble() - 0.5) * 0.4f);
                b.Triangle(c, rim[(i + 1) % segments], rim[i], i % 4 == 0 ? MudB : MudA);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = (float)rng.NextDouble() * radius * 0.55f;
                Blob(b, new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r), new Vector3(0.14f, 0.07f, 0.14f), 0, 0.1f, 21, seed + i);
            }
            return b.ToMesh("Prop_Mud" + seed);
        }

        public static Mesh StepStone(int seed)
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 0.95f, 0.8f, 0.55f, 7, Stone, Grey, true, seed * 0.7f);
            return b.ToMesh("Prop_StepStone" + seed);
        }

        public static Mesh Windmill()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 2.15f, 2.0f, 1.3f, 8, Grey, Stone, true);
            Frustum(b, At(0f, 1.3f, 0f), 1.95f, 1.3f, 4.9f, 8, Cream, Tan);
            Frustum(b, At(0f, 6.2f, 0f), 1.75f, 0.12f, 2.1f, 8, DarkRust, -1, false);
            // door and windows
            Box(b, At(0f, 1.2f, 1.95f), new Vector3(0.9f, 1.6f, 0.2f), DarkWood);
            foreach (var y in new[] { 3.2f, 4.7f })
                Box(b, At(0f, y, 1.52f), new Vector3(0.55f, 0.65f, 0.2f), Navy);
            Frustum(b, At(0f, 5.1f, 1.2f) * Matrix4x4.Rotate(Quaternion.Euler(90f, 0f, 0f)), 0.22f, 0.22f, 1.3f, 6, Espresso);
            return b.ToMesh("Prop_Windmill");
        }

        /// <summary>The sails: a hub and four arms with canvas, built in the XY plane around the origin (axis along Z).</summary>
        public static Mesh WindmillBlades()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, -0.3f) * Matrix4x4.Rotate(Quaternion.Euler(90f, 0f, 0f)), 0.45f, 0.3f, 0.8f, 8, DarkWood, Tan);
            for (int i = 0; i < 4; i++)
            {
                var arm = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, i * 90f + 12f));
                Box(b, arm * At(0f, 2.2f, 0.15f), new Vector3(0.22f, 4.4f, 0.16f), Espresso);
                Box(b, arm * At(0.5f, 2.7f, 0.1f), new Vector3(0.95f, 2.5f, 0.06f), Cream);
                Box(b, arm * At(0.5f, 2.7f, 0.14f), new Vector3(0.95f, 0.07f, 0.04f), DarkWood);
            }
            return b.ToMesh("Prop_WindmillBlades");
        }

        public static Mesh Farmhouse()
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, At(0f, 1.5f, 0f), new Vector3(6.4f, 3f, 4.8f), Tan);
            Box(b, At(0f, 0.2f, 0f), new Vector3(6.6f, 0.4f, 5f), Stone);
            GableRoof(b, At(0f, 3f, 0f), 6.4f, 4.8f, 2.0f, 0.55f, Brick, Tan);
            Box(b, At(-1.8f, 4.3f, -1.2f), new Vector3(0.7f, 2.0f, 0.7f), Grey);
            Box(b, At(1.2f, 1.1f, 2.42f), new Vector3(1.1f, 2.1f, 0.12f), DarkWood);
            foreach (var x in new[] { -1.9f, 2.5f })
                Box(b, At(x, 1.8f, 2.42f), new Vector3(0.95f, 0.95f, 0.12f), LightTeal);
            foreach (var x in new[] { -1.9f, 2.5f })
                Box(b, At(x, 1.8f, 2.46f), new Vector3(1.1f, 0.12f, 0.06f), Espresso);
            foreach (var z in new[] { -0.8f, 0.9f })
                Box(b, At(3.22f, 1.8f, z), new Vector3(0.12f, 0.95f, 0.95f), LightTeal);
            // porch
            Box(b, At(1.2f, 0.12f, 3.1f), new Vector3(2.2f, 0.24f, 1.4f), Wood);
            return b.ToMesh("Prop_Farmhouse");
        }

        public static Mesh Barn()
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, At(0f, 2.1f, 0f), new Vector3(7.6f, 4.2f, 6.2f), DarkRust);
            GableRoof(b, At(0f, 4.2f, 0f), 7.6f, 6.2f, 2.6f, 0.5f, Espresso, DarkRust);
            Box(b, At(0f, 1.5f, 3.12f), new Vector3(3.0f, 3.0f, 0.14f), Cream);
            Box(b, At(0f, 1.5f, 3.2f), new Vector3(0.18f, 3.0f, 0.06f), Espresso);
            Box(b, At(-0.75f, 1.5f, 3.2f), new Vector3(0.08f, 3.0f, 0.06f) * 1f, Espresso);
            Box(b, At(0f, 4.9f, 3.12f), new Vector3(1.1f, 1.1f, 0.14f), Cream);
            return b.ToMesh("Prop_Barn");
        }

        public static Mesh Well()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 1.0f, 0.95f, 0.9f, 9, Grey, Stone);
            Frustum(b, At(0f, 0.9f, 0f), 0.7f, 0.7f, 0.04f, 9, DeepTeal);
            foreach (var x in new[] { -0.85f, 0.85f })
                Box(b, At(x, 1.75f, 0f), new Vector3(0.14f, 1.9f, 0.14f), DarkWood);
            Box(b, At(0f, 2.55f, 0f), new Vector3(0.14f, 0.14f, 0.14f), DarkWood);
            GableRoof(b, At(0f, 2.65f, 0f), 1.9f, 1.5f, 0.8f, 0.25f, Brick, Tan);
            return b.ToMesh("Prop_Well");
        }

        public static Mesh Scarecrow()
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, At(0f, 1.0f, 0f), new Vector3(0.1f, 2.0f, 0.1f), DarkWood);
            Box(b, At(0f, 1.55f, 0f), new Vector3(1.5f, 0.1f, 0.1f), DarkWood);
            Box(b, At(0f, 1.3f, 0f), new Vector3(0.55f, 0.7f, 0.28f), Navy);
            Blob(b, new Vector3(0f, 2.0f, 0f), new Vector3(0.25f, 0.25f, 0.25f), 1, 0.05f, Sand, 5);
            Frustum(b, At(0f, 2.18f, 0f), 0.42f, 0.42f, 0.04f, 8, Brown);
            Frustum(b, At(0f, 2.2f, 0f), 0.24f, 0.18f, 0.28f, 8, Brown);
            return b.ToMesh("Prop_Scarecrow");
        }

        /// <summary>A ploughed furrow with young plants in a row.</summary>
        public static Mesh CropRow()
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, At(0f, 0.12f, 0f), new Vector3(6.2f, 0.24f, 0.9f), Espresso, Cocoa);
            for (int i = 0; i < 12; i++)
            {
                float x = -2.75f + i * 0.5f;
                Frustum(b, At(x, 0.22f, 0f), 0.16f, 0.02f, 0.5f + (i % 3) * 0.06f, 4, i % 2 == 0 ? Olive : Moss, -1, false, i * 0.6f);
            }
            return b.ToMesh("Prop_CropRow");
        }

        public static Mesh Lily(int seed)
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 0.5f, 0.46f, 0.04f, 8, Moss, Olive, false, seed);
            // a little notch and a flower on some pads
            if (seed % 2 == 0) Blob(b, new Vector3(0.1f, 0.09f, 0.05f), new Vector3(0.1f, 0.07f, 0.1f), 0, 0.1f, Salmon, seed);
            return b.ToMesh("Prop_Lily" + seed);
        }

        /// <summary>Reeds and cattails growing in a clump.</summary>
        public static Mesh Reeds(int seed)
        {
            var b = new LowPoly.MeshBuilder();
            var rng = new System.Random(seed * 13);
            for (int i = 0; i < 7; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f, spread = (float)rng.NextDouble() * 0.3f;
                float height = 1.1f + (float)rng.NextDouble() * 0.9f;
                var at = At(new Vector3(Mathf.Cos(angle) * spread, 0f, Mathf.Sin(angle) * spread),
                    Quaternion.Euler((float)(rng.NextDouble() - 0.5) * 22f, (float)rng.NextDouble() * 360f, (float)(rng.NextDouble() - 0.5) * 22f));
                Frustum(b, at, 0.035f, 0.012f, height, 4, i % 2 == 0 ? Olive : Moss);
                if (i % 3 == 0) Frustum(b, at * Matrix4x4.Translate(new Vector3(0f, height * 0.72f, 0f)), 0.06f, 0.05f, 0.3f, 5, Brown, Cocoa);
            }
            return b.ToMesh("Prop_Reeds" + seed);
        }

        /// <summary>
        /// A rocky cave mouth: the opening faces +x (where the river flows), 9 m wide and 5 m tall, filled with darkness and
        /// framed by boulders and stalactites. The mesh stands on the origin.
        /// </summary>
        public static Mesh CaveArch()
        {
            var b = new LowPoly.MeshBuilder();
            var rng = new System.Random(21);
            Box(b, At(-2.2f, 2.4f, 0f), new Vector3(4.4f, 4.8f, 8.6f), 27);                         // the dark inside
            foreach (var side in new[] { -1f, 1f })
            {
                for (int i = 0; i < 4; i++)
                    Blob(b, new Vector3(0.2f + (float)rng.NextDouble() * 0.5f, 0.9f + i * 1.3f, side * (4.6f + (float)rng.NextDouble() * 0.3f)),
                        new Vector3(1.6f, 1.5f, 1.4f) * (0.9f + (float)rng.NextDouble() * 0.4f), 1, 0.16f, (i + (side > 0 ? 1 : 0)) % 2 == 0 ? Stone : Grey, 3 + i);
                for (int i = 0; i < 3; i++)
                    Blob(b, new Vector3(-1.5f - i * 1.3f, 1.5f + i * 1.8f, side * (5.4f + i * 0.7f)), new Vector3(2.8f, 3.0f, 2.6f), 1, 0.18f, i % 2 == 0 ? Grey : Stone, 9 + i);
            }
            for (int i = 0; i < 7; i++)
                Blob(b, new Vector3(0.1f + (float)rng.NextDouble() * 0.6f, 5.0f + (float)rng.NextDouble() * 0.4f, -3.9f + i * 1.3f),
                    new Vector3(1.5f, 1.1f, 1.0f), 1, 0.15f, i % 2 == 0 ? Stone : Grey, 20 + i);
            for (int i = 0; i < 4; i++)
                Blob(b, new Vector3(-2.5f, 6.0f + i * 1.2f, -4f + i * 2.6f), new Vector3(3.6f, 2.2f, 2.8f), 1, 0.16f, Grey, 30 + i);
            for (int i = 0; i < 9; i++)
            {
                float z = -3.6f + i * 0.9f;
                Frustum(b, At(0.4f, 4.6f, z, 0f) * Matrix4x4.Rotate(Quaternion.Euler(180f, 0f, 0f)), 0.14f, 0.01f, 0.45f + (float)rng.NextDouble() * 0.8f, 5, i % 2 == 0 ? Stone : Grey);
            }
            // moss on the lintel and vines hanging over the mouth
            for (int i = 0; i < 6; i++)
                Blob(b, new Vector3(0.2f + (float)rng.NextDouble() * 0.5f, 5.7f + (float)rng.NextDouble() * 0.3f, -3.6f + i * 1.45f), new Vector3(1.1f, 0.45f, 0.9f), 1, 0.12f, i % 2 == 0 ? Olive : Moss, 40 + i);
            for (int i = 0; i < 11; i++)
            {
                float z = -4.1f + i * 0.82f + (float)rng.NextDouble() * 0.3f;
                float length = 0.7f + (float)rng.NextDouble() * 1.5f;
                Box(b, At(0.55f, 5.35f - length * 0.5f, z), new Vector3(0.1f, length, 0.14f), i % 3 == 0 ? Moss : Olive);
            }
            return b.ToMesh("Prop_CaveArch");
        }

        /// <summary>
        /// The grassy mound over the crawl tunnel (about 4.6 x 7.8 m, 3.6 m tall, origin at the ground): a lumpy dome of turf with
        /// stones showing through, flat stone lips round the entrance on the -Z side and tufts of grass on top.
        /// </summary>
        public static Mesh TunnelMound()
        {
            var b = new LowPoly.MeshBuilder();
            var rng = new System.Random(77);
            Blob(b, new Vector3(0f, 1.4f, 0f), new Vector3(2.45f, 2.1f, 4.1f), 2, 0.12f, Moss, 5);
            Blob(b, new Vector3(0f, 2.0f, 0.2f), new Vector3(2.0f, 1.7f, 3.5f), 2, 0.14f, Olive, 6);
            for (int i = 0; i < 9; i++)
            {
                float z = -3.2f + i * 0.8f;
                float side = i % 2 == 0 ? 1f : -1f;
                Blob(b, new Vector3(side * (1.6f + (float)rng.NextDouble() * 0.5f), 0.6f + (float)rng.NextDouble() * 1.4f, z), new Vector3(0.8f, 0.7f, 0.8f) * (0.8f + (float)rng.NextDouble() * 0.5f), 1, 0.2f, i % 3 == 0 ? Stone : Grey, 10 + i);
            }
            // the lip of the entrance: two stone jambs and a lintel
            Box(b, At(-1.5f, 0.75f, -3.8f), new Vector3(0.6f, 1.5f, 0.5f), Stone);
            Box(b, At(1.5f, 0.75f, -3.8f), new Vector3(0.6f, 1.5f, 0.5f), Stone);
            Box(b, At(0f, 1.62f, -3.8f), new Vector3(3.8f, 0.3f, 0.6f), Grey);
            // tufts and little flowers on top
            for (int i = 0; i < 12; i++)
            {
                float x = ((float)rng.NextDouble() - 0.5f) * 2.4f, z = ((float)rng.NextDouble() - 0.5f) * 5.4f;
                float top = 3.15f - (x * x) * 0.25f - (z * z) * 0.03f;
                Frustum(b, At(x, top - 0.25f, z), 0.1f, 0.01f, 0.4f + (float)rng.NextDouble() * 0.3f, 4, i % 3 == 0 ? Gold : Olive, -1, false, i);
            }
            return b.ToMesh("Prop_TunnelMound");
        }

        /// <summary>A traffic cone: an orange pyramid with a cream stripe on a square base.</summary>
        public static Mesh Cone()
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, At(0f, 0.025f, 0f), new Vector3(0.5f, 0.05f, 0.5f), Rust);
            Frustum(b, At(0f, 0.05f, 0f), 0.2f, 0.05f, 0.62f, 8, Brick, Rust);
            Frustum(b, At(0f, 0.3f, 0f), 0.133f, 0.108f, 0.1f, 8, Cream);
            return b.ToMesh("Prop_Cone");
        }

        /// <summary>A drifting leaf for the river: small, flat and pointed.</summary>
        public static Mesh Leaf(int seed)
        {
            var b = new LowPoly.MeshBuilder();
            int[] colours = { Gold, Rust, Olive, Clay };
            int colour = colours[seed % colours.Length];
            Vector3 tip = new Vector3(0f, 0.01f, 0.2f), tail = new Vector3(0f, 0.01f, -0.18f);
            Vector3 left = new Vector3(-0.1f, 0.02f, 0f), right = new Vector3(0.1f, 0.02f, 0f);
            b.Triangle(tail, left, tip, colour);
            b.Triangle(tail, tip, right, colour);
            b.Triangle(tail, tip, left, colour);
            b.Triangle(tail, right, tip, colour);
            return b.ToMesh("Prop_Leaf" + seed);
        }

        // ------------------------------------------------------------ the ice hill

        const int Snow = 25, Ice = 29, Teal = 16;

        /// <summary>
        /// The rocky plateau of the ice hill: a 10 x 10 m stone block whose flat top sits at y = 5, wearing a thick cap of snow
        /// with icicles hanging from its lip and a few frozen boulders at the corners.
        /// </summary>
        public static Mesh IcePlateau()
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, At(0f, 1.0f, 0f), new Vector3(10.6f, 2.0f, 10.6f), Stone);
            Box(b, At(0f, 3.3f, 0f), new Vector3(9.8f, 2.6f, 9.8f), Grey);
            // chunky rock facets on the corners and sides
            var rng = new System.Random(5);
            for (int i = 0; i < 14; i++)
            {
                float angle = i / 14f * Mathf.PI * 2f;
                float r = 5.1f + (float)rng.NextDouble() * 0.3f;
                Blob(b, new Vector3(Mathf.Cos(angle) * r * 0.97f, 1.4f + (float)rng.NextDouble() * 2.4f, Mathf.Sin(angle) * r * 0.97f),
                    new Vector3(0.9f, 1.2f, 0.9f) * (0.8f + (float)rng.NextDouble() * 0.5f), 1, 0.16f, i % 2 == 0 ? Stone : Grey, 11 + i);
            }

            // the snow cap, with its top exactly at the walkable height
            Box(b, At(0f, 4.8f, 0f), new Vector3(10.5f, 0.4f, 10.5f), Snow);
            // a lip of snow lumps round the edge (kept below the walking surface)
            for (int i = 0; i < 26; i++)
            {
                float t = i / 26f * 4f;
                int side = Mathf.FloorToInt(t);
                float along = (t - side) * 10.4f - 5.2f;
                Vector3 p = side == 0 ? new Vector3(along, 4.78f, 5.25f) : side == 1 ? new Vector3(5.25f, 4.78f, -along)
                          : side == 2 ? new Vector3(-along, 4.78f, -5.25f) : new Vector3(-5.25f, 4.78f, along);
                Blob(b, p, new Vector3(0.55f, 0.28f, 0.55f) * (0.8f + (float)rng.NextDouble() * 0.5f), 0, 0.14f, Snow, 40 + i);
            }

            // icicles under the lip
            for (int i = 0; i < 34; i++)
            {
                float t = (i + (float)rng.NextDouble() * 0.6f) / 34f * 4f;
                int side = Mathf.FloorToInt(t) % 4;
                float along = (t - Mathf.Floor(t)) * 9.8f - 4.9f;
                Vector3 p = side == 0 ? new Vector3(along, 4.62f, 5.2f) : side == 1 ? new Vector3(5.2f, 4.62f, -along)
                          : side == 2 ? new Vector3(-along, 4.62f, -5.2f) : new Vector3(-5.2f, 4.62f, along);
                float length = 0.45f + (float)rng.NextDouble() * 0.9f;
                Frustum(b, At(p, Quaternion.Euler(180f, 0f, 0f)), 0.09f + (float)rng.NextDouble() * 0.05f, 0.01f, length, 5, i % 3 == 0 ? Snow : Ice);
            }
            return b.ToMesh("Prop_IcePlateau");
        }

        /// <summary>A pine tree dusted with snow, about 5 m tall.</summary>
        public static Mesh SnowPine(int seed)
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 0.24f, 0.17f, 1.3f, 6, DarkWood);
            for (int tier = 0; tier < 4; tier++)
            {
                float r = 1.55f - tier * 0.34f, y = 0.9f + tier * 0.95f;
                Frustum(b, At(0f, y, 0f), r, 0.06f, 1.5f, 7, tier % 2 == 0 ? DeepTeal : Moss, -1, false, tier * 0.5f + seed);
                // the snow lying on this tier
                Frustum(b, At(0f, y + 0.55f, 0f), r * 0.66f + 0.03f, 0.05f, 1.0f, 7, Snow, -1, false, tier * 0.5f + seed);
            }
            return b.ToMesh("Prop_SnowPine" + seed);
        }

        /// <summary>The two lower snowballs with scarf, arms and buttons; stands on the origin.</summary>
        public static Mesh SnowmanBody()
        {
            var b = new LowPoly.MeshBuilder();
            Blob(b, new Vector3(0f, 0.5f, 0f), new Vector3(0.62f, 0.55f, 0.62f), 1, 0.05f, Snow, 3);
            Blob(b, new Vector3(0f, 1.25f, 0f), new Vector3(0.46f, 0.42f, 0.46f), 1, 0.05f, Snow, 4);
            Frustum(b, At(0f, 1.5f, 0f), 0.4f, 0.4f, 0.1f, 8, Brick);                      // scarf
            Box(b, At(0.22f, 1.35f, 0.05f), new Vector3(0.1f, 0.4f, 0.1f), Brick);          // its hanging end
            for (int i = 0; i < 3; i++) Blob(b, new Vector3(0f, 1.0f + i * 0.17f, 0.4f), new Vector3(0.05f, 0.05f, 0.05f), 0, 0.05f, 27, 9 + i);
            Box(b, At(-0.68f, 1.45f, 0f) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 35f)), new Vector3(0.7f, 0.06f, 0.06f), DarkWood);
            Box(b, At(0.68f, 1.45f, 0f) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -35f)), new Vector3(0.7f, 0.06f, 0.06f), DarkWood);
            return b.ToMesh("Prop_SnowmanBody");
        }

        /// <summary>The head with eyes and a carrot nose (+z), centred on the origin so it can roll.</summary>
        public static Mesh SnowmanHead()
        {
            var b = new LowPoly.MeshBuilder();
            Blob(b, Vector3.zero, new Vector3(0.34f, 0.32f, 0.34f), 1, 0.05f, Snow, 5);
            Frustum(b, At(0f, -0.04f, 0.28f) * Matrix4x4.Rotate(Quaternion.Euler(90f, 0f, 0f)), 0.05f, 0.01f, 0.32f, 4, Rust);
            Blob(b, new Vector3(-0.12f, 0.09f, 0.28f), new Vector3(0.04f, 0.04f, 0.04f), 0, 0.05f, 27, 7);
            Blob(b, new Vector3(0.12f, 0.09f, 0.28f), new Vector3(0.04f, 0.04f, 0.04f), 0, 0.05f, 27, 8);
            return b.ToMesh("Prop_SnowmanHead");
        }

        /// <summary>A top hat standing on the origin.</summary>
        public static Mesh SnowmanHat()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 0.42f, 0.42f, 0.04f, 8, Navy);
            Frustum(b, At(0f, 0.02f, 0f), 0.25f, 0.21f, 0.36f, 8, Navy);
            return b.ToMesh("Prop_SnowmanHat");
        }

        /// <summary>A raft: 3 x 3 m of planks bound with rope, the top of the deck at y = 0.05.</summary>
        public static Mesh Raft()
        {
            var b = new LowPoly.MeshBuilder();
            for (int i = 0; i < 6; i++)
                Box(b, At(-1.25f + i * 0.5f, -0.05f, 0f), new Vector3(0.46f, 0.2f, 3.0f), i % 2 == 0 ? Wood : Tan);
            foreach (var z in new[] { -1.1f, 1.1f })
                Box(b, At(0f, -0.2f, z), new Vector3(3.1f, 0.14f, 0.2f), DarkWood);
            foreach (var (x, z) in new[] { (-1.45f, -1.45f), (1.45f, -1.45f), (-1.45f, 1.45f), (1.45f, 1.45f) })
                Box(b, At(x, 0.3f, z), new Vector3(0.12f, 0.7f, 0.12f), DarkWood);
            Box(b, At(0f, 0.55f, -1.45f), new Vector3(3.0f, 0.07f, 0.07f), Tan);
            Box(b, At(0f, 0.55f, 1.45f), new Vector3(3.0f, 0.07f, 0.07f), Tan);
            return b.ToMesh("Prop_Raft");
        }

        /// <summary>A rubber duck floating on its origin (waterline at y = 0).</summary>
        public static Mesh Duck()
        {
            var b = new LowPoly.MeshBuilder();
            Blob(b, new Vector3(0f, 0.16f, 0f), new Vector3(0.3f, 0.22f, 0.36f), 1, 0.04f, Gold, 21);
            Blob(b, new Vector3(0f, 0.45f, 0.18f), new Vector3(0.18f, 0.18f, 0.18f), 1, 0.03f, Gold, 22);
            Frustum(b, At(0f, 0.43f, 0.34f) * Matrix4x4.Rotate(Quaternion.Euler(90f, 0f, 0f)), 0.09f, 0.05f, 0.16f, 5, Rust);
            Blob(b, new Vector3(-0.09f, 0.5f, 0.3f), new Vector3(0.03f, 0.03f, 0.03f), 0, 0.03f, 27, 23);
            Blob(b, new Vector3(0.09f, 0.5f, 0.3f), new Vector3(0.03f, 0.03f, 0.03f), 0, 0.03f, 27, 24);
            Blob(b, new Vector3(0f, 0.2f, -0.33f), new Vector3(0.1f, 0.09f, 0.12f), 0, 0.04f, Gold, 25);
            return b.ToMesh("Prop_Duck");
        }

        /// <summary>A cluster of ice crystals growing out of the ground.</summary>
        public static Mesh IceCrystals(int seed)
        {
            var b = new LowPoly.MeshBuilder();
            var rng = new System.Random(seed);
            int count = 4 + rng.Next(3);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f, spread = i == 0 ? 0f : 0.15f + (float)rng.NextDouble() * 0.3f;
                var lean = Quaternion.Euler((float)(rng.NextDouble() - 0.5) * 36f, (float)rng.NextDouble() * 360f, (float)(rng.NextDouble() - 0.5) * 36f);
                float height = i == 0 ? 1.4f : 0.5f + (float)rng.NextDouble() * 0.8f;
                float width = 0.13f + (float)rng.NextDouble() * 0.1f;
                var at = At(new Vector3(Mathf.Cos(angle) * spread, 0f, Mathf.Sin(angle) * spread), lean);
                Frustum(b, at, width, width * 0.6f, height * 0.75f, 6, i % 2 == 0 ? Ice : Teal, Snow);
                Frustum(b, at * Matrix4x4.Translate(new Vector3(0f, height * 0.75f, 0f)), width * 0.6f, 0.01f, height * 0.25f, 6, Snow);
            }
            return b.ToMesh("Prop_IceCrystals" + seed);
        }

        /// <summary>A soft mound of snow.</summary>
        public static Mesh SnowDrift(int seed)
        {
            var b = new LowPoly.MeshBuilder();
            Blob(b, new Vector3(0f, 0.12f, 0f), new Vector3(1.3f, 0.5f, 1.0f), 1, 0.12f, Snow, seed);
            Blob(b, new Vector3(0.8f, 0.1f, 0.3f), new Vector3(0.7f, 0.32f, 0.6f), 1, 0.12f, Snow, seed + 1);
            return b.ToMesh("Prop_SnowDrift" + seed);
        }

        /// <summary>A pennant on a pole.</summary>
        public static Mesh Flag(int pennantColour)
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, At(0f, 1.3f, 0f), new Vector3(0.09f, 2.6f, 0.09f), DarkWood);
            Vector3 top = new Vector3(0f, 2.5f, 0f), low = new Vector3(0f, 1.95f, 0f), tip = new Vector3(0.8f, 2.28f, 0.06f);
            b.Triangle(top, low, tip, pennantColour);
            b.Triangle(top, tip, low, pennantColour);
            Blob(b, new Vector3(0f, 2.62f, 0f), new Vector3(0.08f, 0.08f, 0.08f), 0, 0.05f, Gold, 2);
            return b.ToMesh("Prop_Flag" + pennantColour);
        }

        /// <summary>One slab of the ice a frozen box leaves in the river: 1.8 m square, its top at y = 0.02.</summary>
        public static Mesh IceSlabMesh()
        {
            var b = new LowPoly.MeshBuilder();
            Box(b, At(0f, -0.16f, 0f), new Vector3(1.8f, 0.36f, 1.8f), Ice, Snow);
            // frosty bumps on top and a ragged edge
            var rng = new System.Random(3);
            for (int i = 0; i < 7; i++)
                Blob(b, new Vector3((float)(rng.NextDouble() - 0.5) * 1.5f, 0.0f, (float)(rng.NextDouble() - 0.5) * 1.5f), new Vector3(0.25f, 0.05f, 0.25f), 0, 0.1f, i % 2 == 0 ? Snow : Ice, 20 + i);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 20f;
                var at = At(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.01f, 0.95f), Quaternion.Euler(0f, angle, 0f));
                Frustum(b, at, 0.1f, 0.01f, 0.28f + i * 0.04f, 5, Ice);
            }
            return b.ToMesh("Prop_IceSlab");
        }

        /// <summary>
        /// A floe of ice for the trail a frozen box leaves in the river: an eight-sided slab, 2 m across, its top at y = 0.02,
        /// with a frosted raised middle and a ring of little crystals round the edge.
        /// </summary>
        public static Mesh IceFloe()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, -0.34f, 0f), 0.78f, 0.98f, 0.36f, 8, Teal, Ice, false, 0.2f);
            Frustum(b, At(0f, 0.02f, 0f), 0.86f, 0.74f, 0.05f, 8, Ice, Snow, false, 0.2f);
            Frustum(b, At(0f, 0.07f, 0f), 0.5f, 0.4f, 0.04f, 8, Snow, Snow, false, 0.45f);
            var rng = new System.Random(8);
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * 360f + (float)rng.NextDouble() * 25f;
                var at = At(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.04f, 0.78f), Quaternion.Euler(0f, angle, 0f));
                Frustum(b, at, 0.09f, 0.01f, 0.18f + (float)rng.NextDouble() * 0.2f, 5, i % 2 == 0 ? Ice : Snow);
            }
            return b.ToMesh("Prop_IceFloe");
        }

        /// <summary>The glowing part of a lamp: a small lantern bulb to be drawn with an emissive material.</summary>
        public static Mesh LampBulb()
        {
            var b = new LowPoly.MeshBuilder();
            Blob(b, Vector3.zero, new Vector3(0.17f, 0.2f, 0.17f), 1, 0.03f, Gold, 1);
            return b.ToMesh("Prop_LampBulb");
        }

    }
}
