using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Builds the whole phase 1 prototype (data assets, materials, prefabs, level, HUD) from code,
    /// so the result is reproducible and easy to tweak. Run it from MixedUp > Build Phase 1 Prototype.
    /// Existing data assets are kept, so tuned numbers survive a rebuild.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        const string Root = "Assets/_Project";
        const string DataDir = Root + "/Data";
        const string MaterialsDir = Root + "/Materials";
        const string PrefabsDir = Root + "/Prefabs";
        const string IconsDir = Root + "/Art/UI/Boxes";
        const string ScenePath = "Assets/Scenes/Level_Prototype.unity";

        /// <summary>Ink material for the outline of the props (hull pushed along the baked smooth normals).</summary>
        static Material propInk;

        sealed class GameAssets
        {
            public BoxData normal, hot, electric, frozen, toxic;
            public BoxDatabase database;
            public OrderData order;
            public PlayerPalette palette;
            public CombinationRules rules;
        }

        /// <summary>Flat-colour materials for primitive shapes (planks, ramps). Textured props use the palette material.</summary>
        sealed class Mats
        {
            public Material character, wood, woodDark, stone, ice, marker, boots, outline, face, snow;
        }

        sealed class Prefabs
        {
            public GameObject box, player, teammate, preview, loreNote;
        }

        [MenuItem("MixedUp/Build Prototype Scene")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            EnsureFolder(DataDir + "/Effects");
            EnsureFolder(DataDir + "/Boxes");
            EnsureFolder(MaterialsDir);
            EnsureFolder(PrefabsDir);
            EnsureFolder("Assets/Scenes");

            ImportArt();
            ImportUi();
            UiFactory.Font = CreateUiFont();
            var assets = CreateData();
            var mats = CreateMaterials();
            var art = CreateArt(assets, mats);
            var prefabs = CreatePrefabs(assets, mats, art);
            BuildScene(assets, mats, art, prefabs);
            BuildMainMenuScene(assets, mats, art, prefabs);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MixedUp] Phase 1 prototype built: " + ScenePath);
        }

        // ---------------------------------------------------------------- data

        static GameAssets CreateData()
        {
            var heat = LoadOrCreate<HeatEffect>(DataDir + "/Effects/Effect_Heat.asset", e =>
            {
                e.displayNameKey = "effect.heat";
                e.hudColor = new Color(0.9f, 0.35f, 0.2f);
                e.overlay = EffectOverlay.Vignette;
            });
            var electric = LoadOrCreate<ElectricEffect>(DataDir + "/Effects/Effect_Electric.asset", e =>
            {
                e.displayNameKey = "effect.electric";
                e.hudColor = new Color(0.95f, 0.8f, 0.2f);
                e.overlay = EffectOverlay.None;
            });
            var frozen = LoadOrCreate<FrozenEffect>(DataDir + "/Effects/Effect_Frozen.asset", e =>
            {
                e.displayNameKey = "effect.frozen";
                e.hudColor = new Color(0.5f, 0.78f, 0.92f);
                e.overlay = EffectOverlay.Vignette;
            });
            var toxic = LoadOrCreate<ToxicEffect>(DataDir + "/Effects/Effect_Toxic.asset", e =>
            {
                e.displayNameKey = "effect.toxic";
                e.hudColor = new Color(0.45f, 0.65f, 0.25f);
                e.overlay = EffectOverlay.Fog;
            });

            var a = new GameAssets
            {
                normal = CreateBox("normal", new Color(0.76f, 0.6f, 0.4f), new BoxEffect[0]),
                hot = CreateBox("hot", new Color(0.86f, 0.36f, 0.25f), new BoxEffect[] { heat }),
                electric = CreateBox("electric", new Color(0.95f, 0.8f, 0.25f), new BoxEffect[] { electric }),
                frozen = CreateBox("frozen", new Color(0.6f, 0.82f, 0.92f), new BoxEffect[] { frozen }),
                toxic = CreateBox("toxic", new Color(0.5f, 0.7f, 0.3f), new BoxEffect[] { toxic })
            };

            a.database = LoadOrCreate<BoxDatabase>(DataDir + "/BoxDatabase.asset");
            a.database.boxes = new[] { a.normal, a.hot, a.electric, a.frozen, a.toxic };
            EditorUtility.SetDirty(a.database);

            a.order = LoadOrCreate<OrderData>(DataDir + "/Order_Prototype.asset", o =>
            {
                o.lines = new[]
                {
                    new OrderLine { box = a.hot, count = 1 },
                    new OrderLine { box = a.electric, count = 1 },
                    new OrderLine { box = a.frozen, count = 1 },
                    new OrderLine { box = a.toxic, count = 1 },
                    new OrderLine { box = a.normal, count = 2 }
                };
            });

            // The palette comes straight from the reference drawing and is not "tuned data": always rewrite it.
            a.palette = LoadOrCreate<PlayerPalette>(DataDir + "/PlayerPalette.asset");
            a.palette.skinTones = new[]
            {
                new Color32(0x85, 0x3a, 0x1a, 255), new Color32(0xaa, 0x5b, 0x36, 255), new Color32(0xd9, 0x8d, 0x5e, 255),
                new Color32(0xed, 0xb0, 0x7e, 255), new Color32(0xfa, 0xd7, 0xb1, 255)
            }.Select(c => (Color)c).ToArray();
            a.palette.clothesColors = new[]
            {
                new Color32(0xed, 0xdc, 0x52, 255), new Color32(0xad, 0xd5, 0x5f, 255), new Color32(0x57, 0xc8, 0x86, 255),
                new Color32(0x00, 0xba, 0xae, 255), new Color32(0x2a, 0x7b, 0x9b, 255), new Color32(0x3c, 0x3e, 0x6b, 255),
                new Color32(0x52, 0x18, 0x49, 255), new Color32(0x91, 0x0b, 0x3f, 255), new Color32(0xc6, 0x04, 0x1f, 255),
                new Color32(0xfe, 0x57, 0x32, 255)
            }.Select(c => (Color)c).ToArray();
            EditorUtility.SetDirty(a.palette);

            a.rules = CreateRules(a);
            return a;
        }

        static BoxData CreateBox(string id, Color color, BoxEffect[] effects)
        {
            string path = DataDir + "/Boxes/Box_" + char.ToUpper(id[0]) + id.Substring(1) + ".asset";
            var box = LoadOrCreate<BoxData>(path, b =>
            {
                b.id = id;
                b.nameKey = "box." + id + ".name";
                b.descriptionKey = "box." + id + ".desc";
                b.color = color;
                b.effects = effects;
            });

            if (box.icon == null)
            {
                box.icon = LoadIcon(id);
                EditorUtility.SetDirty(box);
            }
            return box;
        }

        static Sprite LoadIcon(string id)
        {
            string path = IconsDir + "/" + id + ".png";
            if (!File.Exists(path)) return null;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------ materials

        static Mats CreateMaterials()
        {
            var m = new Mats
            {
                character = Mat("Character", Color.white, 0.05f),
                wood = Mat("Wood", new Color(0.58f, 0.44f, 0.32f)),
                woodDark = Mat("WoodDark", new Color(0.5f, 0.37f, 0.21f)),
                stone = Mat("Stone", new Color(0.52f, 0.5f, 0.44f)),
                ice = Mat("Ice", new Color(0.6f, 0.8f, 0.86f), 0.7f),
                marker = Mat("Marker", new Color(0.87f, 0.73f, 0.53f)),
                snow = Mat("Snow", new Color(0.96f, 0.95f, 0.93f), 0.25f)
            };
            CreateCharacterMaterials(m);
            return m;
        }

        static Material Mat(string name, Color color, float smoothness = 0.05f)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = material == null;
            if (isNew) material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            if (isNew) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            return material;
        }

        // -------------------------------------------------------------- helpers

        static T LoadOrCreate<T>(string path, System.Action<T> init = null) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool collider = true, Quaternion? rotation = null, bool ink = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (ink && type == PrimitiveType.Cube) InkCube(go, material);
            return go;
        }

        static Mesh inkedCube;

        /// <summary>Gives a primitive cube the inked outline: a copy of the cube mesh with smooth normals and a second submesh.</summary>
        static void InkCube(GameObject go, Material material)
        {
            if (propInk == null) return;
            if (inkedCube == null)
            {
                var builtin = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                inkedCube = SaveMesh(OutlineBake.Bake(builtin, "Prim_Cube_Inked"));
            }
            go.GetComponent<MeshFilter>().sharedMesh = inkedCube;
            go.GetComponent<Renderer>().sharedMaterials = new[] { material, propInk };
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetColor(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
