using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MixedUp.EditorTools
{
    public static partial class PrototypeBuilder
    {
        /// <summary>Spots that must stay clear of scenery: boxes, notes, spawn.</summary>
        static readonly Vector2[] Reserved =
        {
            new Vector2(-10f, -24f), new Vector2(10f, -20f), new Vector2(6f, 30f), new Vector2(30f, 38f),
            new Vector2(-32f, -6f), new Vector2(-18f, 42f), new Vector2(-5.5f, -30.5f), new Vector2(-27f, 2.5f),
            new Vector2(-16f, 40f), new Vector2(4f, -31f), new Vector2(-4f, -31f), new Vector2(-5f, -12f)
        };

        static bool TuftOk(float x, float z) =>
            x > -43f && x < 43f && z > -34f && z < 54f
            && !(z > 3f && z < 15f)
            && !(x > -41f && x < -11f && z > -28f && z < 0f)
            && !(x > -4.5f && x < 4.5f && z > -37f && z < -22f)
            && !(x > 22f && x < 43f && z > -37f && z < -17f);

        static void BuildScene(GameAssets a, Mats m, ArtAssets art, Prefabs p)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureLighting(art);

            var env = new GameObject("Environment").transform;
            BuildTerrain(env, art);
            BuildBridge(env, m);
            BuildIceHill(env, m, art);
            BuildBoundaries(env);
            var truck = BuildTruck(env, m, a, art);
            BuildScenery(env, m, art);
            BuildSkyDecor(env, art);

            var boxesRoot = new GameObject("Boxes").transform;
            PlaceBox(boxesRoot, p, a.normal, new Vector3(-10f, 0f, -24f));
            PlaceBox(boxesRoot, p, a.normal, new Vector3(10f, 0f, -20f));
            PlaceBox(boxesRoot, p, a.hot, new Vector3(6f, 0f, 30f));
            PlaceBox(boxesRoot, p, a.electric, new Vector3(30f, 0f, 38f));
            PlaceBox(boxesRoot, p, a.frozen, new Vector3(-32f, 5f, -6f));
            PlaceBox(boxesRoot, p, a.toxic, new Vector3(-18f, 0f, 42f));

            var players = new GameObject("Players").transform;
            SpawnCharacter(players, p.player, new Vector3(4f, 0.05f, -31f), a.palette, CharacterCustomization.DefaultSkin, CharacterCustomization.DefaultClothes);
            SpawnCharacter(players, p.teammate, new Vector3(-4f, 0.05f, -31f), a.palette, 3, 9);

            var notes = new GameObject("Notes").transform;
            PlaceNote(notes, p.loreNote, a.rules, a.hot, a.frozen, new Vector3(-5.5f, 0f, -30.5f), "Note_HotFrozen");
            PlaceNote(notes, p.loreNote, a.rules, a.hot, a.electric, new Vector3(-27f, 0f, 2.5f), "Note_HotElectric");
            PlaceNote(notes, p.loreNote, a.rules, a.toxic, a.electric, new Vector3(-16f, 0f, 40f), "Note_ToxicElectric");

            var managers = new GameObject("Managers");
            var game = managers.AddComponent<GameManager>();
            game.truck = truck;
            var puzzle = managers.AddComponent<TruckPuzzleController>();
            puzzle.truck = truck;
            puzzle.rules = a.rules;

            ConfigureCamera();
            BuildHud(a, p, truck, puzzle);
            BuildEventSystem();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void ConfigureLighting(ArtAssets art)
        {
            var light = Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
                light.color = new Color(1f, 0.95f, 0.84f);
                light.intensity = 1.3f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.75f;
            }

            RenderSettings.skybox = art.sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.78f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.7f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.35f, 0.3f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.93f, 0.95f);
            RenderSettings.fogStartDistance = 130f;
            RenderSettings.fogEndDistance = 600f;

            var volumeObject = new GameObject("PostProcessing");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = art.post;
        }

        static void ConfigureCamera()
        {
            var camera = Camera.main;
            if (camera == null) return;

            camera.gameObject.AddComponent<ThirdPersonCamera>();
            camera.transform.position = new Vector3(4f, 4f, -37f);
            camera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 700f;
            camera.clearFlags = CameraClearFlags.Skybox;

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        // -------------------------------------------------------------- terrain

        static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (!shadows) renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.isStatic = true;
            return go;
        }

        static void ColliderBox(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<BoxCollider>().size = size;
            go.isStatic = true;
        }

        static void BuildTerrain(Transform env, ArtAssets art)
        {
            ColliderBox("GroundSouth", env, new Vector3(0f, -0.5f, -15f), new Vector3(90f, 1f, 40f));
            ColliderBox("GroundNorth", env, new Vector3(0f, -0.5f, 34f), new Vector3(90f, 1f, 42f));
            ColliderBox("RiverBed", env, new Vector3(0f, -0.9f, 9f), new Vector3(90f, 1f, 8f));

            MeshObject("GroundSouthVisual", env, art.groundSouth, art.palette);
            MeshObject("GroundNorthVisual", env, art.groundNorth, art.palette);
            MeshObject("RiverVisual", env, art.riverBed, art.palette);
            MeshObject("Water", env, art.water, art.paletteWater, false);
            MeshObject("Hills", env, art.hills, art.palette);

            HazardVolume("WaterVolume", env, new Vector3(0f, -0.3f, 9f), new Vector3(90f, 0.5f, 8f), Quaternion.identity, HazardType.Water);
        }

        static void BuildBridge(Transform env, Mats m)
        {
            Prim(PrimitiveType.Cube, "BridgeDeck", env, new Vector3(-27f, 0.1f, 9f), new Vector3(5f, 0.2f, 9f), m.wood);
            Prim(PrimitiveType.Cube, "BridgeRailLeft", env, new Vector3(-29.4f, 0.55f, 9f), new Vector3(0.2f, 0.7f, 9f), m.woodDark);
            Prim(PrimitiveType.Cube, "BridgeRailRight", env, new Vector3(-24.6f, 0.55f, 9f), new Vector3(0.2f, 0.7f, 9f), m.woodDark);
            for (int i = 0; i < 4; i++)
            {
                float z = 5f + i * 2.67f;
                Prim(PrimitiveType.Cube, "PostL" + i, env, new Vector3(-29.4f, 0.55f, z), new Vector3(0.3f, 1.1f, 0.3f), m.woodDark);
                Prim(PrimitiveType.Cube, "PostR" + i, env, new Vector3(-24.6f, 0.55f, z), new Vector3(0.3f, 1.1f, 0.3f), m.woodDark);
            }
        }

        /// <summary>A 5 m plateau reached by an icy ramp (slippery) or by a safe staircase on its east side.</summary>
        static void BuildIceHill(Transform env, Mats m, ArtAssets art)
        {
            const float plateauTop = 5f;
            const float width = 8f;

            Prim(PrimitiveType.Cube, "Plateau", env, new Vector3(-32f, plateauTop * 0.5f, -6f), new Vector3(10f, plateauTop, 10f), m.stone);

            var start = new Vector3(-32f, 0f, -25f);
            var end = new Vector3(-32f, plateauTop, -11f);
            var rotation = Quaternion.LookRotation((end - start).normalized, Vector3.up);
            float length = (end - start).magnitude;
            const float thickness = 0.5f;

            var center = (start + end) * 0.5f - rotation * Vector3.up * (thickness * 0.5f);
            Prim(PrimitiveType.Cube, "IceRamp", env, center, new Vector3(width, thickness, length), m.ice, true, rotation);

            var iceCenter = (start + end) * 0.5f + rotation * Vector3.up * 0.3f;
            HazardVolume("IceVolume", env, iceCenter, new Vector3(width, 1f, length), rotation, HazardType.Slippery);

            const int steps = 12;
            float rise = plateauTop / steps;
            for (int i = 0; i < steps; i++)
            {
                float top = rise * (i + 1);
                float x = -27f + 1.2f * (steps - i) - 0.6f;
                Prim(PrimitiveType.Cube, "Step" + (i + 1), env, new Vector3(x, top * 0.5f, -6f), new Vector3(1.2f, top, 4f), m.wood);
            }

            // Boulders around the foot of the plateau make it look like a natural outcrop.
            var rng = new System.Random(77);
            for (int i = 0; i < 9; i++)
            {
                float angle = (float)rng.NextDouble() * 360f;
                var spot = new Vector3(-32f, 0f, -6f) + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (6.8f + (float)rng.NextDouble() * 1.5f);
                if (spot.x > -29f && spot.z > -9f && spot.z < -3f) continue;
                if (spot.x > -37f && spot.x < -27f && spot.z < -11f && spot.z > -26f) continue;
                PlaceProp(env, art.rocks[rng.Next(art.rocks.Length)], spot, angle, 0.7f + (float)rng.NextDouble() * 0.6f);
            }
        }

        static void BuildBoundaries(Transform env)
        {
            Boundary(env, "WallEast", new Vector3(45.5f, 5f, 10f), new Vector3(1f, 12f, 95f));
            Boundary(env, "WallWest", new Vector3(-45.5f, 5f, 10f), new Vector3(1f, 12f, 95f));
            Boundary(env, "WallSouth", new Vector3(0f, 5f, -35.5f), new Vector3(92f, 12f, 1f));
            Boundary(env, "WallNorth", new Vector3(0f, 5f, 55.5f), new Vector3(92f, 12f, 1f));
        }

        static void Boundary(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<BoxCollider>().size = size;
        }

        static void HazardVolume(string name, Transform parent, Vector3 position, Vector3 size, Quaternion rotation, HazardType type)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            go.AddComponent<HazardZone>().type = type;
        }

        // -------------------------------------------------------------- scenery

        static bool Free(float x, float z, float margin = 0f)
        {
            if (x < -42f || x > 42f || z < -33f || z > 53f) return false;
            if (z > 3f && z < 15f) return false;                                   // river and banks
            if (x > -32f && x < -22f && z > -2f && z < 17f) return false;          // bridge approaches
            if (x > -41f && x < -8f && z > -28f && z < 0f) return false;           // ice hill and stairs
            if (x > -10f && x < 10f && z > -37f && z < -18f) return false;         // start and truck
            if (x > 22f && x < 43f && z > -37f && z < -17f) return false;          // warehouse
            foreach (var r in Reserved)
                if (Vector2.Distance(new Vector2(x, z), r) < 3.5f + margin) return false;
            return true;
        }

        static void PlaceProp(Transform parent, GameObject prefab, Vector3 position, float yaw, float scale, Material tint = null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * scale;
            if (tint != null)
                foreach (var r in instance.GetComponentsInChildren<Renderer>()) r.sharedMaterial = tint;
            SetStaticRecursively(instance);
        }

        static void SetStaticRecursively(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
        }

        static void BuildScenery(Transform env, Mats m, ArtAssets art)
        {
            var scenery = new GameObject("Scenery").transform;
            scenery.SetParent(env, false);
            var rng = new System.Random(1234);
            var tints = new[] { art.palette, art.palette, art.tintAutumn, art.tintGold, art.tintTeal };

            void Trees(float x0, float x1, float z0, float z1, int count)
            {
                int placed = 0;
                for (int attempt = 0; attempt < count * 8 && placed < count; attempt++)
                {
                    float x = Mathf.Lerp(x0, x1, (float)rng.NextDouble());
                    float z = Mathf.Lerp(z0, z1, (float)rng.NextDouble());
                    if (!Free(x, z, 0.5f)) continue;
                    PlaceProp(scenery, art.trees[rng.Next(art.trees.Length)], new Vector3(x, 0f, z),
                        (float)rng.NextDouble() * 360f, Mathf.Lerp(0.8f, 1.3f, (float)rng.NextDouble()), tints[rng.Next(tints.Length)]);
                    placed++;
                }
            }

            Trees(-41f, -5f, 22f, 52f, 40);   // the forest around the toxic box
            Trees(8f, 41f, -32f, 2f, 14);
            Trees(8f, 41f, 16f, 52f, 18);
            Trees(-41f, -9f, -32f, -28f, 5);

            // Rock ring that hides the hot box, plus a few more around the far bank.
            var hotSpot = new Vector3(6f, 0f, 30f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f + 20f;
                var spot = hotSpot + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 3.8f;
                int small = new[] { 0, 2, 3, 4 }[rng.Next(4)];
                PlaceProp(scenery, art.rocks[small], spot, angle, 0.9f + (float)rng.NextDouble() * 0.4f);
            }
            for (int i = 0; i < 16; i++)
            {
                float x = Mathf.Lerp(-41f, 41f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-32f, 52f, (float)rng.NextDouble());
                if (!Free(x, z, 2f)) continue;
                PlaceProp(scenery, art.rocks[rng.Next(art.rocks.Length)], new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f,
                    0.6f + (float)rng.NextDouble() * 0.8f);
            }

            // Stone walls near the electric box.
            Prim(PrimitiveType.Cube, "StoneWall1", scenery, new Vector3(24f, 0.7f, 33f), new Vector3(8f, 1.4f, 0.8f), m.stone);
            Prim(PrimitiveType.Cube, "StoneWall2", scenery, new Vector3(36f, 0.7f, 30f), new Vector3(0.8f, 1.4f, 7f), m.stone);

            // Warehouse landmark east of the start area.
            PlaceProp(scenery, art.warehouse, new Vector3(33f, 0f, -27f), 90f, 1f);

            BuildFence(scenery, m);

            // Bushes, grass and flowers: the small things that make the meadow feel alive.
            for (int i = 0; i < 34; i++)
            {
                float x = Mathf.Lerp(-41f, 41f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-32f, 52f, (float)rng.NextDouble());
                if (!Free(x, z, 0.5f)) continue;
                var bush = MeshObject("Bush", scenery, art.bushes[rng.Next(art.bushes.Length)], art.palette);
                bush.transform.position = new Vector3(x, 0f, z);
                bush.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                bush.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.5f, (float)rng.NextDouble());
            }

            for (int i = 0; i < 320; i++)
            {
                float x = Mathf.Lerp(-43f, 43f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-34f, 54f, (float)rng.NextDouble());
                if (!TuftOk(x, z)) continue;
                var tuft = MeshObject("Tuft", scenery, art.tufts[rng.Next(art.tufts.Length)], art.palette, false);
                tuft.transform.position = new Vector3(x, 0f, z);
                tuft.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                tuft.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.8f, (float)rng.NextDouble());
            }

            for (int i = 0; i < 70; i++)
            {
                float x = Mathf.Lerp(-43f, 43f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-34f, 54f, (float)rng.NextDouble());
                if (!TuftOk(x, z)) continue;
                var flower = MeshObject("Flower", scenery, art.flowers[rng.Next(art.flowers.Length)], art.palette, false);
                flower.transform.position = new Vector3(x, 0f, z);
                flower.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.6f, (float)rng.NextDouble());
            }
        }

        /// <summary>A wooden fence along the south edge, with a gap at the truck dock.</summary>
        static void BuildFence(Transform parent, Mats m)
        {
            for (float x = -43f; x <= 43f; x += 4f)
            {
                if (Mathf.Abs(x) < 5f) continue;
                Prim(PrimitiveType.Cube, "FencePost", parent, new Vector3(x, 0.6f, -34.4f), new Vector3(0.22f, 1.2f, 0.22f), m.woodDark);
                if (x + 4f <= 43f && !(x + 4f > -5f && x < 5f))
                {
                    Prim(PrimitiveType.Cube, "FenceRailHigh", parent, new Vector3(x + 2f, 0.95f, -34.4f), new Vector3(3.8f, 0.12f, 0.1f), m.wood, false);
                    Prim(PrimitiveType.Cube, "FenceRailLow", parent, new Vector3(x + 2f, 0.55f, -34.4f), new Vector3(3.8f, 0.12f, 0.1f), m.wood, false);
                }
            }
        }

        static void BuildSkyDecor(Transform env, ArtAssets art)
        {
            var clouds = new GameObject("Clouds").transform;
            clouds.SetParent(env, false);
            var rng = new System.Random(99);
            for (int i = 0; i < 14; i++)
            {
                float angle = (float)rng.NextDouble() * 360f;
                float distance = Mathf.Lerp(150f, 330f, (float)rng.NextDouble());
                var go = MeshObject("Cloud" + i, clouds, art.clouds[rng.Next(art.clouds.Length)], art.palette, false);
                go.transform.position = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance + Vector3.up * Mathf.Lerp(85f, 140f, (float)rng.NextDouble()) + new Vector3(0f, 0f, 10f);
                go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 3.2f, (float)rng.NextDouble());
            }
        }

        // ---------------------------------------------------------------- truck

        static Truck BuildTruck(Transform env, Mats m, GameAssets a, ArtAssets art)
        {
            var root = new GameObject("Truck");
            root.transform.position = new Vector3(0f, 0f, -26f);
            var t = root.transform;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(art.truck, t);
            model.name = "Model";

            // Loading dock behind the truck: delivered boxes are stacked here.
            Prim(PrimitiveType.Cube, "Dock", t, new Vector3(0f, 0.125f, -5.2f), new Vector3(5.2f, 0.25f, 4.2f), m.wood);
            Prim(PrimitiveType.Cube, "DockEdgeBack", t, new Vector3(0f, 0.2f, -7.3f), new Vector3(5.2f, 0.12f, 0.2f), m.woodDark, false);
            Prim(PrimitiveType.Cube, "DockEdgeLeft", t, new Vector3(-2.6f, 0.2f, -5.2f), new Vector3(0.2f, 0.12f, 4.2f), m.woodDark, false);
            Prim(PrimitiveType.Cube, "DockEdgeRight", t, new Vector3(2.6f, 0.2f, -5.2f), new Vector3(0.2f, 0.12f, 4.2f), m.woodDark, false);

            var bedAnchor = new GameObject("BedAnchor").transform;
            bedAnchor.SetParent(t, false);
            bedAnchor.localPosition = new Vector3(0f, 0.25f, -5.6f);

            var delivery = new GameObject("DeliveryZone");
            delivery.transform.SetParent(t, false);
            delivery.transform.localPosition = new Vector3(0f, 0.5f, -5.4f);
            var zone = delivery.AddComponent<BoxCollider>();
            zone.size = new Vector3(6.5f, 3f, 4.2f);
            zone.isTrigger = true;

            var truck = root.AddComponent<Truck>();
            truck.order = a.order;
            truck.deliveryPoint = delivery.transform;
            truck.bedAnchor = bedAnchor;
            return truck;
        }

        // ---------------------------------------------------------- placements

        static void PlaceBox(Transform parent, Prefabs p, BoxData data, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(p.box, parent);
            instance.name = "Box_" + data.id;
            instance.transform.position = position;

            var pickup = instance.GetComponent<BoxPickup>();
            SetRef(pickup, "data", data);
            PrefabUtility.InstantiatePrefab(data.worldPrefab, pickup.visual);
        }

        static void SpawnCharacter(Transform parent, GameObject prefab, Vector3 position, PlayerPalette palette, int skin, int clothes)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;

            var appearance = instance.GetComponent<PlayerAppearance>();
            SetColor(appearance, "skinColor", palette.Skin(skin));
            SetColor(appearance, "clothesColor", palette.Clothes(clothes));
        }
    }
}
