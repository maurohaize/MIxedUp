using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>Life on the river: splash and ripple effects for waders, and leaves carried along by the current.</summary>
    public static partial class PrototypeBuilder
    {
        static void BuildWaterLife(Transform env, ArtAssets art)
        {
            var life = new GameObject("WaterLife").transform;
            life.SetParent(env, false);

            var effects = life.gameObject.AddComponent<WaterEffects>();
            effects.ringMaterial = fxRing;
            effects.dropMaterial = fxSoft;
            effects.waterLevel = -0.1f;

            var leaves = new[] { SaveMesh(LowPolyProps.Leaf(0)), SaveMesh(LowPolyProps.Leaf(1)), SaveMesh(LowPolyProps.Leaf(2)), SaveMesh(LowPolyProps.Leaf(3)) };
            var rng = new System.Random(808);
            for (int i = 0; i < 16; i++)
            {
                float x = Mathf.Lerp(-42f, 42f, (float)rng.NextDouble());
                float z = Mathf.Lerp(6.2f, 11.8f, (float)rng.NextDouble());
                var leaf = MeshObject("Leaf", life, leaves[i % leaves.Length], art.palette, false);
                leaf.isStatic = false;
                leaf.transform.position = new Vector3(x, -0.1f, z);
                leaf.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                leaf.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 2f, (float)rng.NextDouble());
                var drift = leaf.AddComponent<Drifter>();
                drift.velocity = new Vector3(Mathf.Lerp(0.4f, 0.8f, (float)rng.NextDouble()), 0f, 0f);
                drift.spinDegrees = Mathf.Lerp(-18f, 18f, (float)rng.NextDouble());
                drift.minX = -43f;
                drift.maxX = 43f;
            }
        }
    }
}
