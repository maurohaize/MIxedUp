using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// The cartoon character from the reference drawing: a big round head with huge eyes, a trapezoid dress, floating
    /// oval hands and black boots, all with a thick ink outline. Skin colours the head and hands, clothes the dress.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        const string CharactersDir = ArtDir + "/Characters";

        const float HeadRadius = 0.37f;
        const float HeadCenterY = 1.17f;
        const float DressBaseY = 0.16f;

        sealed class CharacterParts
        {
            public Transform visual, body, handLeft, handRight, bootLeft, bootRight, carryAnchor;
            public Renderer head, dress, handLeftRenderer, handRightRenderer, face;
        }

        static Material CharacterMaterial(string name, string shaderName, System.Action<Material> setup)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = material == null;
            var shader = Shader.Find(shaderName);
            if (isNew) material = new Material(shader);
            else material.shader = shader;

            setup(material);
            if (isNew) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            return material;
        }

        static void CreateCharacterMaterials(Mats m)
        {
            m.boots = Mat("Boots", new Color(0.115f, 0.11f, 0.11f), 0.15f);
            m.outline = CharacterMaterial("Outline", "MixedUp/Outline", material =>
            {
                material.SetColor("_Color", new Color(0.05f, 0.04f, 0.04f));
                material.SetFloat("_Width", 0.03f);
                material.SetFloat("_DistanceScale", 0.4f);
            });

            propInk = CharacterMaterial("OutlineProps", "MixedUp/Outline", material =>
            {
                material.SetColor("_Color", new Color(0.05f, 0.04f, 0.04f));
                material.SetFloat("_Width", 0.034f);
                material.SetFloat("_DistanceScale", 0.5f);
                material.SetFloat("_SmoothNormals", 1f);
            });

            var face = AssetDatabase.LoadAssetAtPath<Texture2D>(CharactersDir + "/face.png");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(CharactersDir + "/face_atlas.png");
            m.face = CharacterMaterial("Face", "MixedUp/FaceAtlas", material =>
            {
                material.SetTexture("_MainTex", atlas != null ? atlas : face);
                material.SetVector("_Grid", atlas != null ? new Vector4(4f, 4f, 0f, 0f) : new Vector4(1f, 1f, 0f, 0f));
                material.SetVector("_Cell", Vector4.zero);
                material.SetColor("_Tint", Color.white);
            });
        }

        static GameObject AddOutline(GameObject part, Material outline)
        {
            var hull = new GameObject("Outline");
            hull.transform.SetParent(part.transform, false);
            hull.AddComponent<MeshFilter>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
            var renderer = hull.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = outline;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return hull;
        }

        /// <summary>A body part with an ink outline.</summary>
        static GameObject InkedPrim(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Material outline)
        {
            var part = Prim(type, name, parent, position, scale, material, false);
            AddOutline(part, outline);
            return part;
        }

        /// <summary>The visual hierarchy only: no gameplay components, so the settings preview can reuse it.</summary>
        static CharacterParts BuildCharacterModel(string name, Transform parent, Mats m)
        {
            var parts = new CharacterParts();

            parts.visual = new GameObject(name).transform;
            parts.visual.SetParent(parent, false);
            parts.body = new GameObject("Body").transform;
            parts.body.SetParent(parts.visual, false);
            var body = parts.body;

            var dressMesh = SaveMesh(CharacterMeshes.Dress("Character_Dress"));
            var dress = new GameObject("Dress");
            dress.transform.SetParent(body, false);
            dress.transform.localPosition = new Vector3(0f, DressBaseY, 0f);
            dress.AddComponent<MeshFilter>().sharedMesh = dressMesh;
            dress.AddComponent<MeshRenderer>().sharedMaterial = m.character;
            AddOutline(dress, m.outline);
            parts.dress = dress.GetComponent<Renderer>();

            var head = InkedPrim(PrimitiveType.Sphere, "Head", body, new Vector3(0f, HeadCenterY, 0f), Vector3.one * (HeadRadius * 2f), m.character, m.outline);
            parts.head = head.GetComponent<Renderer>();

            // Face: a patch of the head sphere, hovering a hair above the skin so it never z-fights with it.
            var faceMesh = SaveMesh(CharacterMeshes.FacePatch("Character_Face", 0.5f * 1.004f, 130f, 100f));
            var face = new GameObject("Face");
            face.transform.SetParent(head.transform, false);
            face.AddComponent<MeshFilter>().sharedMesh = faceMesh;
            var faceRenderer = face.AddComponent<MeshRenderer>();
            faceRenderer.sharedMaterial = m.face;
            parts.face = faceRenderer;
            faceRenderer.shadowCastingMode = ShadowCastingMode.Off;
            faceRenderer.receiveShadows = false;

            var handScale = new Vector3(0.2f, 0.26f, 0.14f);
            var handLeft = InkedPrim(PrimitiveType.Sphere, "HandLeft", body, new Vector3(-0.5f, 0.52f, 0.04f), handScale, m.character, m.outline);
            var handRight = InkedPrim(PrimitiveType.Sphere, "HandRight", body, new Vector3(0.5f, 0.52f, 0.04f), handScale, m.character, m.outline);
            parts.handLeft = handLeft.transform;
            parts.handRight = handRight.transform;
            parts.handLeftRenderer = handLeft.GetComponent<Renderer>();
            parts.handRightRenderer = handRight.GetComponent<Renderer>();

            var bootScale = new Vector3(0.3f, 0.23f, 0.4f);
            parts.bootLeft = InkedPrim(PrimitiveType.Sphere, "BootLeft", body, new Vector3(-0.17f, 0.115f, 0.05f), bootScale, m.boots, m.outline).transform;
            parts.bootRight = InkedPrim(PrimitiveType.Sphere, "BootRight", body, new Vector3(0.17f, 0.115f, 0.05f), bootScale, m.boots, m.outline).transform;

            // Where the carried boxes are held: front-right, so a camera behind the character can still see them.
            parts.carryAnchor = new GameObject("CarryAnchor").transform;
            parts.carryAnchor.SetParent(body, false);
            parts.carryAnchor.localPosition = new Vector3(0.52f, 0.34f, 0.5f);
            return parts;
        }

        static GameObject BuildCharacterPreview(Mats m, PlayerPalette palette)
        {
            var root = new GameObject("CharacterPreview");
            var parts = BuildCharacterModel("Visual", root.transform, m);
            AddAppearance(root, parts, palette, true);

            var idle = root.AddComponent<CharacterIdle>();
            idle.body = parts.body;
            idle.handLeft = parts.handLeft;
            idle.handRight = parts.handRight;
            return root;
        }

        static PlayerAppearance AddAppearance(GameObject root, CharacterParts parts, PlayerPalette palette, bool followSaved)
        {
            var appearance = root.AddComponent<PlayerAppearance>();
            appearance.skinRenderers = new[] { parts.head, parts.handLeftRenderer, parts.handRightRenderer };
            appearance.clothesRenderers = new[] { parts.dress };
            appearance.palette = palette;
            appearance.followSavedChoice = followSaved;
            appearance.skinColor = palette.Skin(CharacterCustomization.DefaultSkin);
            appearance.clothesColor = palette.Clothes(CharacterCustomization.DefaultClothes);
            return appearance;
        }

        /// <summary>Local = the controllable player; otherwise a teammate stand-in until multiplayer arrives.</summary>
        /// <summary>A small burst of icy mist that puffs up where a slab of ice forms.</summary>
        static ParticleSystem BuildFrostPuffs(Transform parent)
        {
            var frost = NewParticles("IceFrost", parent, fxSoft);
            var main = frost.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startColor = new Color(0.8f, 0.95f, 1f, 0.65f);
            main.gravityModifier = -0.04f;
            main.maxParticles = 80;
            var emission = frost.emission;
            emission.enabled = false;
            var shape = frost.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.8f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var fade = frost.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.2f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var size = frost.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            return frost;
        }

        static GameObject BuildCharacter(Mats m, PlayerPalette palette, bool local)
        {
            var root = new GameObject(local ? "Player" : "Teammate");
            var parts = BuildCharacterModel("Visual", root.transform, m);

            var inventory = root.AddComponent<PlayerInventory>();
            var status = root.AddComponent<PlayerStatus>();

            PlayerController controller = null;
            if (local)
            {
                var cc = root.AddComponent<CharacterController>();
                cc.height = 1.6f;
                cc.radius = 0.38f;
                cc.center = new Vector3(0f, 0.8f, 0f);
                cc.stepOffset = 0.45f;
                cc.slopeLimit = 50f;
                cc.skinWidth = 0.05f;

                controller = root.AddComponent<PlayerController>();
                controller.visual = parts.visual;
                root.AddComponent<PlayerInteractor>();
                root.AddComponent<PlayerPush>();
                var hug = root.AddComponent<PlayerHug>();
                hug.heartMaterial = fxHeart;
            }
            else
            {
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.height = 1.6f;
                capsule.radius = 0.38f;
                capsule.center = new Vector3(0f, 0.8f, 0f);
                root.AddComponent<TeammateDummy>();
            }

            var pass = root.AddComponent<PlayerPassTarget>();
            pass.allowTakeBack = !local;
            root.AddComponent<HazardSensor>();

            // Wading through the river with a frozen box leaves a trail of ice behind.
            var iceTrail = root.AddComponent<IceTrailEmitter>();
            iceTrail.slabPrefab = iceSlabPrefab != null ? iceSlabPrefab.GetComponent<IceSlab>() : null;
            iceTrail.frost = BuildFrostPuffs(root.transform);

            AddAppearance(root, parts, palette, local);

            var carry = root.AddComponent<CarriedBoxesView>();
            carry.status = status;
            carry.anchor = parts.carryAnchor;

            var expression = root.AddComponent<CharacterFace>();
            expression.faceRenderer = parts.face;

            var animator = root.AddComponent<PlayerAnimator>();
            animator.controller = controller;
            animator.status = status;
            animator.carry = carry;
            animator.body = parts.body;
            animator.handLeft = parts.handLeft;
            animator.handRight = parts.handRight;
            animator.bootLeft = parts.bootLeft;
            animator.bootRight = parts.bootRight;

            var labelGo = new GameObject("OverheadLabel");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.1f, 0f);
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
    }
}
