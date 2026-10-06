using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Finds meshes whose triangles face inwards (they vanish when seen from outside with back-face culling, so you see through
    /// them). A closed solid has a positive signed volume when its triangles are wound counter-clockwise seen from outside.
    /// </summary>
    public static class MeshAudit
    {
        public struct Report
        {
            public string name;
            public int triangles;
            public float inwardShare;
            public float signedVolume;
            /// <summary>Share of triangles whose outward ray crosses the mesh an odd number of times: they face into a solid.</summary>
            public float oddShare;
            public override string ToString() => name + ": " + triangles + " tris, " + Mathf.RoundToInt(inwardShare * 100f) + "% facing inwards, volume " + signedVolume.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)
                                                 + ", odd " + Mathf.RoundToInt(oddShare * 100f) + "%";
        }

        /// <summary>Share of triangles whose normal points towards the mesh centre, measured in the given local-to-world matrix.</summary>
        public static Report Analyze(string name, Mesh mesh, Matrix4x4 toWorld)
        {
            var vertices = mesh.vertices;
            var report = new Report { name = name };
            var centre = Vector3.zero;
            foreach (var v in vertices) centre += toWorld.MultiplyPoint3x4(v);
            if (vertices.Length > 0) centre /= vertices.Length;

            bool mirrored = toWorld.determinant < 0f;
            int inward = 0, total = 0;
            float volume = 0f;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var indices = mesh.GetIndices(sub);
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    Vector3 a = toWorld.MultiplyPoint3x4(vertices[indices[i]]);
                    Vector3 b = toWorld.MultiplyPoint3x4(vertices[indices[i + 1]]);
                    Vector3 c = toWorld.MultiplyPoint3x4(vertices[indices[i + 2]]);
                    if (mirrored) (b, c) = (c, b);

                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    if (normal.sqrMagnitude < 1e-12f) continue;
                    total++;
                    Vector3 middle = (a + b + c) / 3f;
                    if (Vector3.Dot(normal, middle - centre) < 0f) inward++;
                    volume += Vector3.Dot(a - centre, Vector3.Cross(b - centre, c - centre)) / 6f;
                }
            }
            if (total > 0 && total <= 3000) report.oddShare = OddShare(mesh, vertices, toWorld, mirrored);
            report.triangles = total;
            report.inwardShare = total == 0 ? 0f : inward / (float)total;
            report.signedVolume = volume;
            return report;
        }

        public struct ComponentInfo
        {
            public int triangles;
            public bool closed;
            public float volume;
            public Vector3 centre;
            /// <summary>Volume of the box around the piece, to judge whether a signed volume is meaningful.</summary>
            public float boxVolume;
            public bool InsideOut => volume < 0f && (closed || -volume > 0.08f * boxVolume);
        }

        /// <summary>Splits a mesh into welded pieces and reports each closed piece's signed volume (negative = inside out).</summary>
        public static List<ComponentInfo> Components(Mesh mesh, Matrix4x4 toWorld) => Components(mesh, toWorld, null);

        /// <summary>`members` (optional) receives, for every component, the global triangle numbers (submeshes in order) it contains.</summary>
        public static List<ComponentInfo> Components(Mesh mesh, Matrix4x4 toWorld, List<List<int>> members)
        {
            var vertices = mesh.vertices;
            var weld = new Dictionary<Vector3Int, int>();
            var vertexIds = new int[vertices.Length];
            // Points closer than a ten-thousandth of the size of the piece are the same point (imported models can be tiny in their own units).
            var worldBounds = new Bounds(toWorld.MultiplyPoint3x4(vertices.Length > 0 ? vertices[0] : Vector3.zero), Vector3.zero);
            foreach (var v in vertices) worldBounds.Encapsulate(toWorld.MultiplyPoint3x4(v));
            float resolution = 1f / Mathf.Max(1e-6f, worldBounds.size.magnitude * 1e-4f);
            for (int i = 0; i < vertices.Length; i++)
            {
                var p = toWorld.MultiplyPoint3x4(vertices[i]);
                var key = new Vector3Int(Mathf.RoundToInt(p.x * resolution), Mathf.RoundToInt(p.y * resolution), Mathf.RoundToInt(p.z * resolution));
                if (!weld.TryGetValue(key, out int id))
                {
                    id = weld.Count;
                    weld[key] = id;
                }
                vertexIds[i] = id;
            }

            bool mirrored = toWorld.determinant < 0f;
            var tris = new List<int[]>();
            // A mesh baked for the outline repeats its triangles in a second submesh: look at the first one only.
            int submeshes = OutlineBake.IsBaked(mesh) ? 1 : mesh.subMeshCount;
            for (int sub = 0; sub < submeshes; sub++)
            {
                var indices = mesh.GetIndices(sub);
                for (int i = 0; i + 2 < indices.Length; i += 3)
                    tris.Add(mirrored ? new[] { indices[i], indices[i + 2], indices[i + 1], tris.Count } : new[] { indices[i], indices[i + 1], indices[i + 2], tris.Count });
            }

            // Pieces are groups of triangles that share edges (not just corner points): a leaf blob that merely touches a branch is
            // its own piece, so its orientation can be judged on its own.
            var parent = new int[tris.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            var edgeOwner = new Dictionary<long, int>();
            for (int ti = 0; ti < tris.Count; ti++)
            {
                var t = tris[ti];
                for (int k = 0; k < 3; k++)
                {
                    int u = vertexIds[t[k]], v = vertexIds[t[(k + 1) % 3]];
                    long key = u < v ? ((long)u << 32) | (uint)v : ((long)v << 32) | (uint)u;
                    if (edgeOwner.TryGetValue(key, out int other)) parent[Find(ti)] = Find(other);
                    else edgeOwner[key] = ti;
                }
            }

            var groups = new Dictionary<int, List<int[]>>();
            for (int ti = 0; ti < tris.Count; ti++)
            {
                int root = Find(ti);
                if (!groups.TryGetValue(root, out var list)) groups[root] = list = new List<int[]>();
                list.Add(tris[ti]);
            }

            var result = new List<ComponentInfo>();
            foreach (var group in groups.Values)
            {
                var edges = new Dictionary<long, int>();
                var centre = Vector3.zero;
                int count = 0;
                foreach (var t in group)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int u = vertexIds[t[k]], v = vertexIds[t[(k + 1) % 3]];
                        long key = u < v ? ((long)u << 32) | (uint)v : ((long)v << 32) | (uint)u;
                        edges[key] = edges.TryGetValue(key, out int n) ? n + 1 : 1;
                        centre += toWorld.MultiplyPoint3x4(vertices[t[k]]);
                        count++;
                    }
                }
                centre /= Mathf.Max(1, count);

                bool closed = true;
                foreach (var n in edges.Values)
                    if (n != 2) { closed = false; break; }

                float volume = 0f;
                var box = new Bounds(toWorld.MultiplyPoint3x4(vertices[group[0][0]]), Vector3.zero);
                foreach (var t in group)
                    for (int k = 0; k < 3; k++) box.Encapsulate(toWorld.MultiplyPoint3x4(vertices[t[k]]));
                foreach (var t in group)
                {
                    Vector3 a = toWorld.MultiplyPoint3x4(vertices[t[0]]) - centre;
                    Vector3 b = toWorld.MultiplyPoint3x4(vertices[t[1]]) - centre;
                    Vector3 c = toWorld.MultiplyPoint3x4(vertices[t[2]]) - centre;
                    volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
                }
                result.Add(new ComponentInfo { triangles = group.Count, closed = closed, volume = volume, centre = centre, boxVolume = box.size.x * box.size.y * box.size.z });
                if (members != null)
                {
                    var list = new List<int>();
                    foreach (var t in group) list.Add(t[3]);
                    members.Add(list);
                }
            }
            return result;
        }

        static float OddShare(Mesh mesh, Vector3[] vertices, Matrix4x4 toWorld, bool mirrored)
        {
            var tris = new List<(Vector3 a, Vector3 b, Vector3 c)>();
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var indices = mesh.GetIndices(sub);
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    Vector3 a = toWorld.MultiplyPoint3x4(vertices[indices[i]]);
                    Vector3 b = toWorld.MultiplyPoint3x4(vertices[indices[i + 1]]);
                    Vector3 c = toWorld.MultiplyPoint3x4(vertices[indices[i + 2]]);
                    if (mirrored) (b, c) = (c, b);
                    if (Vector3.Cross(b - a, c - a).sqrMagnitude > 1e-12f) tris.Add((a, b, c));
                }
            }

            int odd = 0;
            for (int i = 0; i < tris.Count; i++)
            {
                var t = tris[i];
                Vector3 normal = Vector3.Cross(t.b - t.a, t.c - t.a).normalized;
                Vector3 origin = (t.a + t.b + t.c) / 3f + normal * 1e-3f;
                int hits = 0;
                for (int j = 0; j < tris.Count; j++)
                {
                    if (j == i) continue;
                    if (RayHits(origin, normal, tris[j].a, tris[j].b, tris[j].c)) hits++;
                }
                if (hits % 2 == 1) odd++;
            }
            return tris.Count == 0 ? 0f : odd / (float)tris.Count;
        }

        /// <summary>Moller-Trumbore, counting only hits in front of the origin, from either side.</summary>
        static bool RayHits(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 e1 = b - a, e2 = c - a;
            Vector3 p = Vector3.Cross(direction, e2);
            float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-9f) return false;
            float inv = 1f / det;
            Vector3 tv = origin - a;
            float u = Vector3.Dot(tv, p) * inv;
            if (u < 0f || u > 1f) return false;
            Vector3 q = Vector3.Cross(tv, e1);
            float v = Vector3.Dot(direction, q) * inv;
            if (v < 0f || u + v > 1f) return false;
            return Vector3.Dot(e2, q) * inv > 1e-4f;
        }

        public static List<Report> AuditPrefabs(string folder)
        {
            var reports = new List<Report>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null) continue;
                    // The prefab root sits at the origin, so the child's local-to-world matrix is its matrix inside the prefab.
                    reports.Add(Analyze(prefab.name + "/" + filter.name, filter.sharedMesh, filter.transform.localToWorldMatrix));
                }
            }
            return reports;
        }

        public static List<Report> AuditMeshes(string folder)
        {
            var reports = new List<Report>();
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh != null) reports.Add(Analyze(mesh.name, mesh, Matrix4x4.identity));
            }
            return reports;
        }

        /// <summary>
        /// Returns a copy of the mesh in which every closed piece that is inside out (so you would see through it) is turned
        /// the right way round, or null when the mesh is already fine. `toWorld` is the matrix the mesh is rendered with.
        /// </summary>
        /// <summary>
        /// Unit normal of a triangle. Imported models are often tiny in their own units (centimetres of a hundredth of a metre), and
        /// Vector3.normalized gives zero for vectors shorter than 1e-5: scale up first.
        /// </summary>
        public static Vector3 FaceNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 cross = Vector3.Cross((b - a) * 1000f, (c - a) * 1000f);
            return cross.sqrMagnitude < 1e-12f ? Vector3.up : cross.normalized;
        }

        public static Mesh FixInsideOut(Mesh mesh, Matrix4x4 toWorld)
        {
            var members = new List<List<int>>();
            var parts = Components(mesh, toWorld, members);

            var flip = new HashSet<int>();
            for (int i = 0; i < parts.Count; i++)
            {
                if (!parts[i].InsideOut) continue;
                foreach (var t in members[i]) flip.Add(t);
            }
            if (flip.Count == 0) return null;

            var copy = Object.Instantiate(mesh);
            copy.name = mesh.name + "_Fixed";
            var normals = copy.normals;
            var vertices = copy.vertices;
            int number = 0;
            for (int sub = 0; sub < copy.subMeshCount; sub++)
            {
                var indices = copy.GetIndices(sub);
                for (int i = 0; i + 2 < indices.Length; i += 3, number++)
                {
                    if (!flip.Contains(number)) continue;
                    (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                    if (normals.Length == vertices.Length)
                    {
                        // Flat normal of the corrected face, pointing outwards (a mirrored transform turns the winding around once more).
                        Vector3 face = FaceNormal(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]);
                        if (toWorld.determinant < 0f) face = -face;
                        for (int k = 0; k < 3; k++) normals[indices[i + k]] = face;
                    }
                }
                copy.SetIndices(indices, copy.GetTopology(sub), sub);
            }
            if (normals.Length == vertices.Length) copy.normals = normals;
            copy.RecalculateBounds();
            return copy;
        }

        static string Components(string name, Mesh mesh, Matrix4x4 matrix)
        {
            var parts = Components(mesh, matrix);
            int closed = 0, flipped = 0, open = 0;
            foreach (var p in parts)
            {
                if (!p.closed) open++; else closed++;
                if (p.InsideOut) flipped++;
            }
            return "COMP " + name + ": " + parts.Count + " parts, " + closed + " closed, " + flipped + " inside-out, " + open + " open";
        }

        /// <summary>Which palette colour each piece of a model uses (a black one shows up as a black blob).</summary>
        public static void TreeColours()
        {
            var text = new StringBuilder();
            foreach (var name in new[] { "Tree1", "Tree2", "Tree3", "Tree4" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/" + name + ".prefab");
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh;
                    var uvs = mesh.uv;
                    var vertices = mesh.vertices;
                    var counts = new Dictionary<int, int>();
                    for (int i = 0; i < uvs.Length; i++)
                    {
                        int cell = Mathf.FloorToInt(uvs[i].x * 30f);
                        counts[cell] = counts.TryGetValue(cell, out int n) ? n + 1 : 1;
                    }
                    var parts = new StringBuilder();
                    foreach (var pair in counts) parts.Append(pair.Key + ":" + pair.Value + " ");
                    var normals = mesh.normals;
                    int zero = 0, inward = 0;
                    var zeroByCell = new Dictionary<int, int>();
                    var centre = mesh.bounds.center;
                    for (int i = 0; i < normals.Length; i++)
                    {
                        if (normals[i].sqrMagnitude < 0.5f)
                        {
                            zero++;
                            int cell = Mathf.FloorToInt(uvs[i].x * 30f);
                            zeroByCell[cell] = zeroByCell.TryGetValue(cell, out int zn) ? zn + 1 : 1;
                        }
                        else if (Vector3.Dot(normals[i], vertices[i] - centre) < 0f) inward++;
                    }
                    foreach (var pair in zeroByCell) parts.Append(" zeroInCell" + pair.Key + "=" + pair.Value);
                    parts.Append(" zeroNormals " + zero + " inwardNormals " + inward + " of " + normals.Length + " transform scale " + filter.transform.lossyScale.ToString("0.00"));
                    text.AppendLine("TREECOL " + name + " mesh " + mesh.name + " verts " + vertices.Length + " cells " + parts);
                }
            }
            Debug.Log("[MixedUp] Tree colours" + System.Environment.NewLine + text);
        }

        [MenuItem("MixedUp/Audit Mesh Winding")]
        public static void Run()
        {
            var text = new StringBuilder();
            foreach (var r in AuditMeshes("Assets/_Project/Art/Meshes")) text.AppendLine("MESH " + r);
            foreach (var r in AuditPrefabs("Assets/_Project/Prefabs")) text.AppendLine("PREFAB " + r);
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/_Project/Art/Meshes" }))
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
                if (mesh != null && mesh.vertexCount < 40000) text.AppendLine(Components(mesh.name, mesh, Matrix4x4.identity));
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null) text.AppendLine(Components(prefab.name + "/" + filter.name, filter.sharedMesh, filter.transform.localToWorldMatrix));
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                text.AppendLine("BOUNDS " + prefab.name + ": min y " + bounds.min.y.ToString("0.00", inv) + " max y " + bounds.max.y.ToString("0.00", inv)
                                + " size " + bounds.size.x.ToString("0.00", inv) + "x" + bounds.size.z.ToString("0.00", inv) + " renderers " + renderers.Length);
            }
            Debug.Log("[MixedUp] Mesh audit\n" + text);
        }
    }
}
