using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixedUp.EditorTools
{
    /// <summary>Smooth procedural meshes for the cartoon character: the rounded trapezoid dress and the face patch.</summary>
    public static class CharacterMeshes
    {
        /// <summary>
        /// The dress/tunic: a trapezoid that is wide at the hem and narrow at the shoulders, with softly rounded
        /// corners (a lathe of a smooth profile). Origin at the centre of the bottom, 0.675 m tall.
        /// </summary>
        public static Mesh Dress(string name, float depthScale = 0.82f, int segments = 40)
        {
            var control = new[]
            {
                new Vector2(0.00f, 0.000f), new Vector2(0.22f, 0.000f), new Vector2(0.325f, 0.012f), new Vector2(0.357f, 0.065f),
                new Vector2(0.335f, 0.19f), new Vector2(0.29f, 0.36f), new Vector2(0.242f, 0.51f), new Vector2(0.21f, 0.605f),
                new Vector2(0.175f, 0.652f), new Vector2(0.10f, 0.672f), new Vector2(0.00f, 0.678f)
            };
            return Lathe(name, Smooth(control, 5), segments, depthScale);
        }

        static List<Vector2> Smooth(Vector2[] control, int perSegment)
        {
            var padded = new List<Vector2> { control[0] };
            padded.AddRange(control);
            padded.Add(control[control.Length - 1]);

            var result = new List<Vector2>();
            for (int i = 1; i < padded.Count - 2; i++)
            {
                Vector2 p0 = padded[i - 1], p1 = padded[i], p2 = padded[i + 1], p3 = padded[i + 2];
                for (int s = 0; s < perSegment; s++)
                {
                    float t = s / (float)perSegment, t2 = t * t, t3 = t2 * t;
                    result.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            result.Add(control[control.Length - 1]);
            return result;
        }

        static Mesh Lathe(string name, List<Vector2> profile, int segments, float depthScale)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            float height = profile[profile.Count - 1].y;

            for (int i = 0; i < profile.Count; i++)
            {
                var prev = profile[Mathf.Max(0, i - 1)];
                var next = profile[Mathf.Min(profile.Count - 1, i + 1)];
                var tangent = (next - prev).normalized;
                var normal2 = new Vector2(tangent.y, -tangent.x);   // outward for a profile that runs bottom -> top
                if (i == 0) normal2 = new Vector2(0f, -1f);
                if (i == profile.Count - 1) normal2 = new Vector2(0f, 1f);

                for (int s = 0; s <= segments; s++)
                {
                    float angle = s / (float)segments * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                    vertices.Add(new Vector3(profile[i].x * cos, profile[i].y, profile[i].x * sin * depthScale));
                    // Inverse-transpose of the depth squash keeps the shading correct on the ellipse.
                    normals.Add(new Vector3(normal2.x * cos, normal2.y, normal2.x * sin / depthScale).normalized);
                    uvs.Add(new Vector2(s / (float)segments, profile[i].y / height));
                }
            }

            var triangles = new List<int>();
            int ring = segments + 1;
            for (int i = 0; i < profile.Count - 1; i++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = i * ring + s, b = a + 1, c = a + ring, d = c + 1;
                    AddOutward(triangles, vertices, normals, a, c, b);
                    AddOutward(triangles, vertices, normals, b, c, d);
                }
            }
            return Build(name, vertices, normals, uvs, triangles);
        }

        /// <summary>
        /// A patch of a sphere facing +Z, covering azimuth +-azDegrees/2 and elevation +-elDegrees/2, UV-mapped linearly
        /// by angle. The face texture is drawn the same way (see Tools/generate_character_art.py).
        /// </summary>
        public static Mesh FacePatch(string name, float radius, float azDegrees, float elDegrees, int columns = 48, int rows = 36)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();

            for (int j = 0; j <= rows; j++)
            {
                float v = j / (float)rows;
                float el = Mathf.Lerp(-elDegrees * 0.5f, elDegrees * 0.5f, v) * Mathf.Deg2Rad;
                for (int i = 0; i <= columns; i++)
                {
                    float u = i / (float)columns;
                    float az = Mathf.Lerp(-azDegrees * 0.5f, azDegrees * 0.5f, u) * Mathf.Deg2Rad;
                    var n = new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el));
                    vertices.Add(n * radius);
                    normals.Add(n);
                    uvs.Add(new Vector2(u, v));
                }
            }

            var triangles = new List<int>();
            int row = columns + 1;
            for (int j = 0; j < rows; j++)
            {
                for (int i = 0; i < columns; i++)
                {
                    int a = j * row + i, b = a + 1, c = a + row, d = c + 1;
                    AddOutward(triangles, vertices, normals, a, c, b);
                    AddOutward(triangles, vertices, normals, b, c, d);
                }
            }
            return Build(name, vertices, normals, uvs, triangles);
        }

        /// <summary>Adds a triangle wound so that it faces the same way as the vertex normals.</summary>
        static void AddOutward(List<int> triangles, List<Vector3> vertices, List<Vector3> normals, int a, int b, int c)
        {
            var face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            var wanted = normals[a] + normals[b] + normals[c];
            if (Vector3.Dot(face, wanted) >= 0f) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
            else { triangles.Add(a); triangles.Add(c); triangles.Add(b); }
        }

        static Mesh Build(string name, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
