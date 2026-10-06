using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Logs the structure of model assets (children, mesh sizes, UV ranges) from batch mode.
    ///   Unity.exe -batchmode -projectPath P -executeMethod MixedUp.EditorTools.ModelInspector.Dump -inspect "Assets/a.fbx;Assets/b.fbx"
    /// </summary>
    public static class ModelInspector
    {
        public static void Dump()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-inspect");
            var paths = i >= 0 && i + 1 < args.Length ? args[i + 1].Split(';') : new string[0];

            var report = new StringBuilder();
            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) { report.AppendLine("MISSING " + path); continue; }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                report.AppendLine("== " + path);
                foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = filter.sharedMesh;
                    var renderer = filter.GetComponent<Renderer>();
                    var uv = mesh.uv;
                    string uvRange = uv.Length == 0 ? "no-uv"
                        : string.Format("u[{0:0.000},{1:0.000}] v[{2:0.000},{3:0.000}]",
                            uv.Min(p => p.x), uv.Max(p => p.x), uv.Min(p => p.y), uv.Max(p => p.y));
                    report.AppendLine(string.Format("  {0} mesh={1} verts={2} tris={3} subMeshes={4} worldCenter={5} worldSize={6} localScale={7} uv={8}",
                        filter.name, mesh.name, mesh.vertexCount, mesh.triangles.Length / 3, mesh.subMeshCount,
                        renderer.bounds.center.ToString("F2"), renderer.bounds.size.ToString("F2"),
                        filter.transform.lossyScale.ToString("F3"), uvRange));
                }
                UnityEngine.Object.DestroyImmediate(instance);
            }
            Debug.Log("[ModelInspector]\n" + report);
            EditorApplication.Exit(0);
        }
    }
}
