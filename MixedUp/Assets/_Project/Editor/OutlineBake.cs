using System.Collections.Generic;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Prepares meshes for the inked outline: the hull is pushed out along a smoothed normal (averaged over the vertices that
    /// share a position) stored in the tangent channel, and the mesh gets a second submesh with the same triangles so a
    /// renderer can draw it with the palette material and then with the outline material.
    /// </summary>
    public static class OutlineBake
    {
        public static bool IsBaked(Mesh mesh) => mesh != null && mesh.subMeshCount >= 2 && mesh.tangents != null && mesh.tangents.Length == mesh.vertexCount;

        /// <summary>Returns a baked copy (the original is untouched); an already baked mesh is returned as it is.</summary>
        public static Mesh Bake(Mesh source, string name = null)
        {
            if (IsBaked(source)) return source;

            var mesh = Object.Instantiate(source);
            mesh.name = name ?? source.name + "_Inked";
            Apply(mesh);
            return mesh;
        }

        /// <summary>Bakes in place (used for meshes that were just generated).</summary>
        public static void Apply(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            if (normals.Length != vertices.Length)
            {
                mesh.RecalculateNormals();
                normals = mesh.normals;
            }

            // Average the normals of every vertex at the same spot, weighting by triangle corner angle.
            var weld = new Dictionary<Vector3Int, Vector3>();
            Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 1000f), Mathf.RoundToInt(p.y * 1000f), Mathf.RoundToInt(p.z * 1000f));

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var indices = mesh.GetIndices(sub);
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    Vector3 a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]];
                    Vector3 face = Vector3.Cross(b - a, c - a);
                    if (face.sqrMagnitude < 1e-14f) continue;
                    face.Normalize();
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 p = vertices[indices[i + k]];
                        Vector3 e1 = vertices[indices[i + (k + 1) % 3]] - p, e2 = vertices[indices[i + (k + 2) % 3]] - p;
                        float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(e1.normalized, e2.normalized), -1f, 1f));
                        var key = Key(p);
                        weld[key] = (weld.TryGetValue(key, out var sum) ? sum : Vector3.zero) + face * angle;
                    }
                }
            }

            var tangents = new Vector4[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 smooth = weld.TryGetValue(Key(vertices[i]), out var sum) && sum.sqrMagnitude > 1e-10f ? sum.normalized : normals[i];
                tangents[i] = new Vector4(smooth.x, smooth.y, smooth.z, 1f);
            }
            mesh.tangents = tangents;

            if (mesh.subMeshCount < 2)
            {
                var all = mesh.GetIndices(0);
                mesh.subMeshCount = 2;
                mesh.SetIndices(all, MeshTopology.Triangles, 1);
            }
            mesh.RecalculateBounds();
        }
    }
}
