using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MixedUp.EditorTools
{
    public static partial class PrototypeBuilder
    {
        const string ArtDir = Root + "/Art";
        const string ModelsDir = ArtDir + "/Models";
        const string TexturesDir = ArtDir + "/Textures";
        const string MeshesDir = ArtDir + "/Meshes";
        const string OverlaysDir = ArtDir + "/UI/Overlays";

        sealed class ArtAssets
        {
            public Material palette, paletteWater, sky;
            public Material tintAutumn, tintGold, tintTeal;
            public readonly Dictionary<string, GameObject> boxWorld = new Dictionary<string, GameObject>();
            public GameObject truck, warehouse, cartel;
            public GameObject[] trees, rocks;
            public Mesh groundSouth, groundNorth, riverBed, water, hills;
            public LowPoly.HillField hillField;
            public Mesh[] clouds, tufts, flowers, bushes;
            public VolumeProfile post;
        }

        // ------------------------------------------------------------ importing

        static void ImportArt()
        {
            AssetDatabase.Refresh();

            foreach (var path in Directory.GetFiles(ModelsDir, "*.fbx", SearchOption.AllDirectories))
                ConfigureModel(path.Replace('\\', '/'));

            ConfigureTexture(TexturesDir + "/palette.png", FilterMode.Point, mips: false, uncompressed: true, npotNone: true);
            foreach (var path in Directory.GetFiles(TexturesDir, "box_*.*").Where(p => !p.EndsWith(".meta")))
                ConfigureTexture(path.Replace('\\', '/'), FilterMode.Bilinear, mips: true, uncompressed: false, npotNone: false);
            ConfigureTexture(CharactersDir + "/face.png", FilterMode.Bilinear, mips: true, uncompressed: true, npotNone: false);
            foreach (var path in Directory.GetFiles(OverlaysDir, "overlay_*.png"))
                ConfigureTexture(path.Replace('\\', '/'), FilterMode.Bilinear, mips: false, uncompressed: false, npotNone: false);

            AssetDatabase.Refresh();
        }

        static void ConfigureModel(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return;

            bool dirty = importer.materialImportMode != ModelImporterMaterialImportMode.None
                         || importer.importCameras || importer.importLights || importer.importAnimation
                         || importer.meshCompression != ModelImporterMeshCompression.Off;
            if (!dirty) return;

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.SaveAndReimport();
        }

        static void ConfigureTexture(string path, FilterMode filter, bool mips, bool uncompressed, bool npotNone)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = filter;
            importer.mipmapEnabled = mips;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            if (uncompressed) importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (npotNone) importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------ building

        static ArtAssets CreateArt(GameAssets a, Mats m)
        {
            var art = new ArtAssets();
            var paletteTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesDir + "/palette.png");

            art.palette = TexMat("Palette", paletteTex, Color.white, 0.03f);
            art.paletteWater = TexMat("PaletteWater", paletteTex, Color.white, 0.55f);
            art.tintAutumn = TexMat("PaletteAutumn", paletteTex, new Color(1.3f, 0.85f, 0.72f), 0.03f);
            art.tintGold = TexMat("PaletteGold", paletteTex, new Color(1.32f, 1.18f, 0.7f), 0.03f);
            art.tintTeal = TexMat("PaletteTeal", paletteTex, new Color(0.82f, 1.08f, 1.12f), 0.03f);

            CreateBoxModels(a, art);
            CreateProps(art, m);
            CreateMeshes(art);
            CreateFx(art);
            CreateSky(art);
            CreatePostProcessing(art);
            AssignEffectOverlays(a);
            ConfigureRenderPipelineAssets();
            return art;
        }

        static Material TexMat(string name, Texture texture, Color tint, float smoothness)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = material == null;
            if (isNew) material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            if (isNew) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            return material;
        }

        static void CreateBoxModels(GameAssets a, ArtAssets art)
        {
            string[] ids = { "normal", "hot", "electric", "frozen", "toxic" };
            string[] textures = { "box_normal.jpg", "box_hot.jpg", "box_electric.jpg", "box_frozen.png", "box_toxic.jpg" };
            var boxes = new[] { a.normal, a.hot, a.electric, a.frozen, a.toxic };

            for (int i = 0; i < ids.Length; i++)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesDir + "/" + textures[i]);
                var material = TexMat("Box_" + char.ToUpper(ids[i][0]) + ids[i].Substring(1), tex, Color.white, 0.12f);
                var prefab = MakeModelPrefab(ModelsDir + "/Boxes/box_" + ids[i] + ".fbx", null, "BoxModel_" + ids[i], material, 1.05f, SizeMode.Height, 0f);
                art.boxWorld[ids[i]] = prefab;
                boxes[i].worldPrefab = prefab;
                EditorUtility.SetDirty(boxes[i]);
            }
        }

        /// <summary>A wooden signpost with a paper notice. Built from primitives: the hand-made sign model is UV-mapped to a texture we do not use.</summary>
        static GameObject BuildSignPrefab(Mats m)
        {
            var root = new GameObject("SignModel");
            var t = root.transform;
            Prim(PrimitiveType.Cube, "Post", t, new Vector3(0f, 0.9f, 0f), new Vector3(0.14f, 1.8f, 0.14f), m.woodDark, false);
            Prim(PrimitiveType.Cube, "Board", t, new Vector3(0f, 1.65f, 0f), new Vector3(1.25f, 0.8f, 0.1f), m.wood, false);
            foreach (var z in new[] { 0.07f, -0.07f })
            {
                Prim(PrimitiveType.Cube, "Paper", t, new Vector3(0f, 1.65f, z), new Vector3(0.95f, 0.58f, 0.03f), m.marker, false);
                Prim(PrimitiveType.Cube, "Line1", t, new Vector3(0f, 1.82f, z * 1.35f), new Vector3(0.7f, 0.05f, 0.01f), m.woodDark, false);
                Prim(PrimitiveType.Cube, "Line2", t, new Vector3(0f, 1.68f, z * 1.35f), new Vector3(0.7f, 0.05f, 0.01f), m.woodDark, false);
                Prim(PrimitiveType.Cube, "Line3", t, new Vector3(-0.15f, 1.54f, z * 1.35f), new Vector3(0.4f, 0.05f, 0.01f), m.woodDark, false);
            }
            return SavePrefab(root, "SignModel");
        }

        static void CreateProps(ArtAssets art, Mats m)
        {
            string trees = ModelsDir + "/Props/arboles.fbx";
            string[] treeNodes = { "Icosphere.003", "Icosphere.005", "Icosphere.006", "Icosphere.011" };
            art.trees = new GameObject[treeNodes.Length];
            for (int i = 0; i < treeNodes.Length; i++)
                art.trees[i] = MakeModelPrefab(trees, treeNodes[i], "Tree" + (i + 1), art.palette, 0.34f, SizeMode.Factor, 0f, ColliderKind.Trunk);

            string rocks = ModelsDir + "/Props/piedras.fbx";
            string[] rockNodes = { "Icosphere", "Icosphere.001", "Icosphere.002", "Icosphere.004", "Icosphere.007", "Icosphere.008", "Icosphere.009" };
            art.rocks = new GameObject[rockNodes.Length];
            for (int i = 0; i < rockNodes.Length; i++)
                art.rocks[i] = MakeModelPrefab(rocks, rockNodes[i], "Rock" + (i + 1), art.palette, 0.5f, SizeMode.Factor, 0f, ColliderKind.Box);

            art.truck = MakeModelPrefab(ModelsDir + "/Props/camion.fbx", null, "TruckModel", art.palette, 0.56f, SizeMode.Factor, 180f, ColliderKind.Box);
            art.warehouse = MakeModelPrefab(ModelsDir + "/Props/biltegia.fbx", null, "Warehouse", art.palette, 0.5f, SizeMode.Factor, 0f, ColliderKind.Box);
            art.cartel = BuildSignPrefab(m);
        }

        enum SizeMode { Height, Factor }
        enum ColliderKind { None, Box, Trunk }

        /// <summary>
        /// Builds a prefab from a model: keeps one named mesh node (or all of them), applies the material,
        /// scales it, moves its base centre to the origin and turns it by yaw degrees.
        /// </summary>
        static GameObject MakeModelPrefab(string fbxPath, string nodeName, string prefabName, Material material,
            float size, SizeMode mode, float yaw, ColliderKind collider = ColliderKind.None)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var filters = instance.GetComponentsInChildren<MeshFilter>();
            if (nodeName != null)
            {
                foreach (var f in filters)
                    if (f.name != nodeName) Object.DestroyImmediate(f.gameObject);
            }

            // Some hand-made models have parts whose faces point inwards: you would see through them. Turn those round.
            foreach (var f in instance.GetComponentsInChildren<MeshFilter>())
            {
                if (f.sharedMesh == null) continue;
                var fixedMesh = MeshAudit.FixInsideOut(f.sharedMesh, f.transform.localToWorldMatrix);
                if (fixedMesh == null) continue;
                fixedMesh.name = prefabName + "_" + f.name.Replace('.', '_') + "_Fixed";
                f.sharedMesh = SaveMesh(fixedMesh);
            }

            var renderers = instance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.sharedMaterial = material;

            // Every model gets the same inked outline as the character: bake smooth normals and draw the hull as a second material.
            if (propInk != null)
            {
                foreach (var f in instance.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = f.GetComponent<Renderer>();
                    if (f.sharedMesh == null || renderer == null) continue;
                    f.sharedMesh = SaveMesh(OutlineBake.Bake(f.sharedMesh, prefabName + "_" + f.name.Replace('.', '_') + "_Inked"));
                    renderer.sharedMaterials = new[] { material, propInk };
                }
            }

            var bounds = TightBounds(instance.transform);
            float scale = mode == SizeMode.Height ? size / bounds.size.y : size;

            var root = new GameObject(prefabName);
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // The holder carries the scale and the offset, so models whose root node is the mesh itself
            // (and therefore carries its own position) keep their transform intact.
            var anchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var holder = new GameObject("Holder").transform;
            holder.SetParent(pivot, false);
            holder.localScale = Vector3.one * scale;
            holder.localPosition = -anchor * scale;
            instance.transform.SetParent(holder, false);

            // World-space bounds after the transform, relative to the prefab root at the origin.
            var finalBounds = TightBounds(root.transform);

            if (collider == ColliderKind.Box)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = finalBounds.center;
                box.size = finalBounds.size * 0.9f;
            }
            else if (collider == ColliderKind.Trunk)
            {
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.radius = Mathf.Min(0.45f, finalBounds.size.x * 0.12f);
                capsule.height = Mathf.Min(3.2f, finalBounds.size.y * 0.5f);
                capsule.center = new Vector3(finalBounds.center.x, capsule.height * 0.5f, finalBounds.center.z);
            }

            return SavePrefab(root, prefabName);
        }

        /// <summary>
        /// The box around the real vertices. Renderer.bounds is the box of the rotated mesh bounds, which is looser than the mesh
        /// whenever a node is rotated: standing a rock on that box leaves it hovering above the ground.
        /// </summary>
        static Bounds TightBounds(Transform root)
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || filter.GetComponent<Renderer>() == null) continue;
                var matrix = filter.transform.localToWorldMatrix;
                foreach (var v in filter.sharedMesh.vertices)
                {
                    var p = matrix.MultiplyPoint3x4(v);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return bounds;
        }

        // --------------------------------------------------------------- meshes

        /// <summary>Saves a generated mesh prepared for the inked outline (smooth normals and a second submesh).</summary>
        static Mesh SaveInkedMesh(Mesh mesh)
        {
            OutlineBake.Apply(mesh);
            return SaveMesh(mesh);
        }

        static Mesh SaveMesh(Mesh mesh)
        {
            EnsureFolder(MeshesDir);
            string path = MeshesDir + "/" + mesh.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void CreateMeshes(ArtAssets art)
        {
            art.groundSouth = SaveMesh(LowPoly.Terrain("Ground_South", -45f, -35f, 45f, 5f, 1f, LowPoly.Grass, 1, GroundHeight, TrailColor));
            art.groundNorth = SaveMesh(LowPoly.Terrain("Ground_North", -45f, 13f, 45f, 55f, 1f, LowPoly.Grass, 2, GroundHeight, TrailColor));
            art.riverBed = SaveMesh(LowPoly.River("River_Bed", -45f, 45f, 5f, 13f, -0.4f, -0.12f));
            art.water = SaveMesh(LowPoly.Water("River_Water", -45f, 45f, 5f, 13f, -0.12f));
            art.hillField = new LowPoly.HillField(new Rect(-45f, -35f, 90f, 90f), 5f, 300f, 7, RoadCalm);
            art.hills = SaveMesh(LowPoly.Hills("Hills", art.hillField));

            art.clouds = new[]
            {
                SaveMesh(LowPoly.Cloud("Cloud_1", 3)), SaveMesh(LowPoly.Cloud("Cloud_2", 8)), SaveMesh(LowPoly.Cloud("Cloud_3", 21))
            };
            art.tufts = new[]
            {
                SaveMesh(LowPoly.Tuft("Tuft_1", 4, 12)), SaveMesh(LowPoly.Tuft("Tuft_2", 9, 18)), SaveMesh(LowPoly.Tuft("Tuft_3", 15, 11))
            };
            art.flowers = new[]
            {
                SaveMesh(LowPoly.Flower("Flower_Pink", 2, 24)), SaveMesh(LowPoly.Flower("Flower_Cream", 5, 2)),
                SaveMesh(LowPoly.Flower("Flower_Blue", 7, 29))
            };
            art.bushes = new[]
            {
                SaveInkedMesh(LowPoly.Bush("Bush_Olive", 6, 12)), SaveInkedMesh(LowPoly.Bush("Bush_Green", 12, 18))
            };
        }

        // ------------------------------------------------------ sky and lighting

        static void CreateSky(ArtAssets art)
        {
            string path = MaterialsDir + "/Sky.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = material == null;
            if (isNew) material = new Material(Shader.Find("MixedUp/GradientSky"));

            material.SetColor("_TopColor", new Color(0.30f, 0.60f, 0.88f));
            material.SetColor("_HorizonColor", new Color(0.74f, 0.93f, 0.95f));
            material.SetColor("_BottomColor", new Color(0.72f, 0.84f, 0.80f));
            material.SetFloat("_Exponent", 0.65f);
            if (isNew) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            art.sky = material;
        }

        static void CreatePostProcessing(ArtAssets art)
        {
            string path = ArtDir + "/PostProcessing.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null)
            {
                art.post = existing;
                return;
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.22f);
            bloom.scatter.Override(0.6f);

            var colors = profile.Add<ColorAdjustments>(true);
            colors.postExposure.Override(0.15f);
            colors.contrast.Override(8f);
            colors.saturation.Override(10f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.5f);

            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            art.post = profile;
        }

        static void AssignEffectOverlays(GameAssets a)
        {
            void Assign(string effectPath, string overlayFile, EffectOverlay? mode = null)
            {
                var effect = AssetDatabase.LoadAssetAtPath<BoxEffect>(effectPath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(OverlaysDir + "/" + overlayFile);
                if (effect == null || texture == null) return;
                effect.screenOverlay = texture;
                if (mode.HasValue) effect.overlay = mode.Value;
                EditorUtility.SetDirty(effect);
            }

            // Every box has its own hand-drawn frame around the screen (the toxic one also keeps its fog).
            Assign(DataDir + "/Effects/Effect_Heat.asset", "overlay_hot_frame.png", EffectOverlay.Vignette);
            Assign(DataDir + "/Effects/Effect_Frozen.asset", "overlay_frozen.png", EffectOverlay.Vignette);
            Assign(DataDir + "/Effects/Effect_Electric.asset", "overlay_electric.png", EffectOverlay.Vignette);
            Assign(DataDir + "/Effects/Effect_Toxic.asset", "overlay_toxic_frame.png", EffectOverlay.Fog);
        }

        static void ConfigureRenderPipelineAssets()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                asset.msaaSampleCount = 4;
                asset.shadowDistance = 90f;
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
