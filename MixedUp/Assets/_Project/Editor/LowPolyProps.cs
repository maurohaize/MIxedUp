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
                if (bottomCap) Tri(b, m, Vector3.zero, b0, b1, Vector3.down, palette);
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
            Frustum(b, lying, 0.55f, 0.55f, 1.0f, 9, Gold, Sand);
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
            Frustum(b, At(0f, 0f, 0f), 0.36f, 0.33f, 0.95f, 10, Wood, Tan);
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

        public static Mesh LampPost()
        {
            var b = new LowPoly.MeshBuilder();
            Frustum(b, At(0f, 0f, 0f), 0.14f, 0.1f, 0.2f, 8, Grey);
            Frustum(b, At(0f, 0.2f, 0f), 0.065f, 0.055f, 2.4f, 8, Espresso);
            Box(b, At(0f, 2.78f, 0f), new Vector3(0.42f, 0.42f, 0.42f), LightSand);
            Frustum(b, At(0f, 2.99f, 0f), 0.42f, 0.02f, 0.3f, 4, DarkRust, -1, false, Mathf.PI * 0.25f);
            return b.ToMesh("Prop_LampPost");
        }

        public static Mesh Log()
        {
            var b = new LowPoly.MeshBuilder();
            var lying = At(0f, 0.4f, 0f) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 90f)) * At(0f, -1.6f, 0f);
            Frustum(b, lying, 0.4f, 0.37f, 3.2f, 8, DarkWood, Tan);
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
            Frustum(b, lying, 0.3f, 0.3f, 6.4f, 8, Clay, DarkRust);
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
                Frustum(b, lean, 0.09f * scale, 0.08f * scale, 0.95f * scale, 6, DarkWood, Tan);
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
            return b.ToMesh("Prop_Lily" + seed);
        }
    }
}
