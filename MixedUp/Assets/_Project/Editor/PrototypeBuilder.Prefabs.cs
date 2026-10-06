using UnityEditor;
using UnityEngine;
using TMPro;

namespace MixedUp.EditorTools
{
    public static partial class PrototypeBuilder
    {
        static Prefabs CreatePrefabs(Mats m, ArtAssets art)
        {
            return new Prefabs
            {
                box = SavePrefab(BuildBoxPickup(), "BoxPickup"),
                player = SavePrefab(BuildCharacter(m, true), "Player"),
                teammate = SavePrefab(BuildCharacter(m, false), "Teammate"),
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

        /// <summary>Primitive-built character: grey simple body, round head, no face. Local = controllable player.</summary>
        static GameObject BuildCharacter(Mats m, bool local)
        {
            var root = new GameObject(local ? "Player" : "Teammate");

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var body = new GameObject("Body").transform;
            body.SetParent(visual, false);

            var torso = Prim(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.32f, 0.35f), m.character, false);
            var head = Prim(PrimitiveType.Sphere, "Head", body, new Vector3(0f, 1.7f, 0f), Vector3.one * 0.5f, m.character, false);
            var armL = Limb("ArmLeft", body, new Vector3(-0.34f, 1.35f, 0f), new Vector3(0.14f, 0.3f, 0.14f), -0.28f, m, out var armLRenderer);
            var armR = Limb("ArmRight", body, new Vector3(0.34f, 1.35f, 0f), new Vector3(0.14f, 0.3f, 0.14f), -0.28f, m, out var armRRenderer);
            var legL = Limb("LegLeft", body, new Vector3(-0.14f, 0.8f, 0f), new Vector3(0.17f, 0.4f, 0.17f), -0.4f, m, out var legLRenderer);
            var legR = Limb("LegRight", body, new Vector3(0.14f, 0.8f, 0f), new Vector3(0.17f, 0.4f, 0.17f), -0.4f, m, out var legRRenderer);

            // Delivery worker's backpack.
            Prim(PrimitiveType.Cube, "Backpack", body, new Vector3(0f, 1.1f, -0.27f), new Vector3(0.44f, 0.52f, 0.22f), m.wood, false);
            Prim(PrimitiveType.Cube, "BackpackFlap", body, new Vector3(0f, 1.3f, -0.28f), new Vector3(0.46f, 0.14f, 0.24f), m.woodDark, false);

            var inventory = root.AddComponent<PlayerInventory>();
            var status = root.AddComponent<PlayerStatus>();

            PlayerController controller = null;
            if (local)
            {
                var cc = root.AddComponent<CharacterController>();
                cc.height = 1.95f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 0.975f, 0f);
                cc.stepOffset = 0.45f;
                cc.slopeLimit = 50f;
                cc.skinWidth = 0.05f;

                controller = root.AddComponent<PlayerController>();
                controller.visual = visual;
                root.AddComponent<PlayerInteractor>();
            }
            else
            {
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.height = 1.95f;
                capsule.radius = 0.35f;
                capsule.center = new Vector3(0f, 0.975f, 0f);
                root.AddComponent<TeammateDummy>();
            }

            var pass = root.AddComponent<PlayerPassTarget>();
            pass.allowTakeBack = !local;
            root.AddComponent<HazardSensor>();

            var appearance = root.AddComponent<PlayerAppearance>();
            appearance.skinRenderers = new[]
            {
                head.GetComponent<Renderer>(), armLRenderer, armRRenderer, legLRenderer, legRRenderer
            };
            appearance.clothesRenderers = new[] { torso.GetComponent<Renderer>() };

            var animator = root.AddComponent<PlayerAnimator>();
            animator.controller = controller;
            animator.status = status;
            animator.body = body;
            animator.armLeft = armL;
            animator.armRight = armR;
            animator.legLeft = legL;
            animator.legRight = legR;

            var labelGo = new GameObject("OverheadLabel");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            var text = labelGo.AddComponent<TextMeshPro>();
            text.fontSize = 1.7f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = UiFactory.Ink;
            text.fontStyle = FontStyles.Bold;
            text.rectTransform.sizeDelta = new Vector2(6f, 3f);
            text.text = string.Empty;

            var overhead = root.AddComponent<OverheadLabel>();
            overhead.text = text;
            overhead.status = status;
            overhead.controller = controller;
            overhead.displayNameKey = "ui.teammate";

            return root;
        }

        static Transform Limb(string name, Transform parent, Vector3 pivotPosition, Vector3 scale, float centerOffsetY,
            Mats m, out Renderer renderer)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = pivotPosition;

            var mesh = Prim(PrimitiveType.Capsule, name + "Mesh", pivot, new Vector3(0f, centerOffsetY, 0f), scale, m.character, false);
            renderer = mesh.GetComponent<Renderer>();
            return pivot;
        }
    }
}
