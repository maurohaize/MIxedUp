using UnityEditor;
using UnityEngine;

namespace MixedUp.EditorTools
{
    public static partial class PrototypeBuilder
    {
        static Prefabs CreatePrefabs(GameAssets a, Mats m, ArtAssets art)
        {
            iceSlabPrefab = SavePrefab(BuildIceSlab(art), "IceSlab");
            return new Prefabs
            {
                box = SavePrefab(BuildBoxPickup(), "BoxPickup"),
                player = SavePrefab(BuildCharacter(m, a.palette, true), "Player"),
                teammate = SavePrefab(BuildCharacter(m, a.palette, false), "Teammate"),
                preview = SavePrefab(BuildCharacterPreview(m, a.palette), "CharacterPreview"),
                loreNote = SavePrefab(BuildLoreNote(art), "LoreNote")
            };
        }

        static GameObject SavePrefab(GameObject temp, string name)
        {
            var asset = PrefabUtility.SaveAsPrefabAsset(temp, PrefabsDir + "/" + name + ".prefab");
            Object.DestroyImmediate(temp);
            return asset;
        }

        /// <summary>The pickup is an empty shell; the level builder places the textured model (BoxData.worldPrefab) inside Visual.</summary>
        static GameObject BuildBoxPickup()
        {
            var root = new GameObject("BoxPickup");
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.55f, 0f);
            collider.size = new Vector3(1.1f, 1.1f, 1.1f);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.localPosition = new Vector3(0f, 0.05f, 0f);

            var pickup = root.AddComponent<BoxPickup>();
            pickup.visual = visual;
            return root;
        }
    }
}
