using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MixedUp.EditorTools;

namespace MixedUp.Tests
{
    /// <summary>Meshes whose faces point inwards vanish when seen from outside (you see through them): none may ship.</summary>
    public class MeshWindingTests
    {
        [Test]
        public void NoPrefabHasAPieceThatIsInsideOut()
        {
            var bad = new List<string>();
            int checkedMeshes = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null || filter.sharedMesh.vertexCount > 40000) continue;
                    checkedMeshes++;
                    int flipped = 0;
                    foreach (var part in MeshAudit.Components(filter.sharedMesh, filter.transform.localToWorldMatrix))
                        if (part.InsideOut) flipped++;
                    if (flipped > 0) bad.Add(prefab.name + "/" + filter.name + ": " + flipped + " pieces inside out");
                }
            }
            if (checkedMeshes == 0) Assert.Ignore("No prefabs yet. Run MixedUp > Build Prototype Scene first.");
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void GeneratedPropMeshesAreWoundOutwards()
        {
            var bad = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh Prop_", new[] { "Assets/_Project/Art/Meshes" }))
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
                if (mesh == null || mesh.name.Contains("Leaf") || mesh.name.Contains("Mud") || mesh.name.Contains("Lily")) continue;   // flat, double sided
                foreach (var part in MeshAudit.Components(mesh, Matrix4x4.identity))
                    if (part.closed && part.volume < 0f) bad.Add(mesh.name);
            }
            Assert.IsEmpty(bad, "inside-out props: " + string.Join(", ", bad));
        }
    }
}
