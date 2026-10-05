using System.IO;
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

        sealed class GameAssets
        {
            public BoxData normal, hot, electric, frozen, toxic;
            public BoxDatabase database;
            public OrderData order;
            public PlayerPalette palette;
        }

        sealed class Mats
        {
            public Material ground, riverBed, water, ice, rock, wood, trunk, leaves;
            public Material truckBody, truckCab, wheel, glass, character, box, tape, marker;
        }

        sealed class Prefabs
        {
            public GameObject box, player, teammate, bedCube;
        }

        [MenuItem("MixedUp/Build Phase 1 Prototype")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            EnsureFolder(DataDir + "/Effects");
            EnsureFolder(DataDir + "/Boxes");
            EnsureFolder(MaterialsDir);
            EnsureFolder(PrefabsDir);
            EnsureFolder("Assets/Scenes");

            var assets = CreateData();
            var mats = CreateMaterials();
            var prefabs = CreatePrefabs(mats);
            BuildScene(assets, mats, prefabs);

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

            a.palette = LoadOrCreate<PlayerPalette>(DataDir + "/PlayerPalette.asset", p =>
            {
                p.skinTones = new[]
                {
                    new Color(0.72f, 0.72f, 0.74f), new Color(0.96f, 0.80f, 0.69f), new Color(0.87f, 0.68f, 0.50f),
                    new Color(0.65f, 0.45f, 0.32f), new Color(0.40f, 0.28f, 0.20f)
                };
                p.clothesColors = new[]
                {
                    new Color(0.82f, 0.32f, 0.30f), new Color(0.35f, 0.50f, 0.80f), new Color(0.40f, 0.65f, 0.40f),
                    new Color(0.92f, 0.80f, 0.35f), new Color(0.60f, 0.45f, 0.75f), new Color(0.92f, 0.60f, 0.30f),
                    new Color(0.92f, 0.60f, 0.70f), new Color(0.93f, 0.92f, 0.88f), new Color(0.20f, 0.20f, 0.22f)
                };
            });
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

        static Mats CreateMaterials() => new Mats
        {
            ground = Mat("Ground", new Color(0.52f, 0.62f, 0.42f)),
            riverBed = Mat("RiverBed", new Color(0.55f, 0.5f, 0.4f)),
            water = Mat("Water", new Color(0.42f, 0.6f, 0.72f), 0.6f),
            ice = Mat("Ice", new Color(0.72f, 0.86f, 0.92f), 0.7f),
            rock = Mat("Rock", new Color(0.6f, 0.58f, 0.55f)),
            wood = Mat("Wood", new Color(0.55f, 0.4f, 0.28f)),
            trunk = Mat("Trunk", new Color(0.4f, 0.3f, 0.22f)),
            leaves = Mat("Leaves", new Color(0.4f, 0.52f, 0.35f)),
            truckBody = Mat("TruckBody", new Color(0.85f, 0.78f, 0.6f)),
            truckCab = Mat("TruckCab", new Color(0.75f, 0.35f, 0.3f)),
            wheel = Mat("Wheel", new Color(0.2f, 0.2f, 0.22f)),
            glass = Mat("Glass", new Color(0.3f, 0.36f, 0.42f), 0.5f),
            character = Mat("Character", Color.white, 0.05f),
            box = Mat("Box", new Color(0.76f, 0.6f, 0.4f)),
            tape = Mat("Tape", new Color(0.45f, 0.33f, 0.22f)),
            marker = Mat("Marker", new Color(0.93f, 0.85f, 0.45f))
        };

        static Material Mat(string name, Color color, float smoothness = 0.05f)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, path);
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
            Material material, bool collider = true, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
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
