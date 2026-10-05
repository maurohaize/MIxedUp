using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixedUp.EditorTools
{
    public static partial class PrototypeBuilder
    {
        static void BuildScene(GameAssets a, Mats m, Prefabs p)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureLighting();

            var env = new GameObject("Environment").transform;
            BuildTerrain(env, m);
            BuildBridge(env, m);
            BuildIceHill(env, m);
            BuildBoundaries(env);
            BuildScenery(env, m);
            var truck = BuildTruck(env, m, a, p);

            var boxesRoot = new GameObject("Boxes").transform;
            PlaceBox(boxesRoot, p, a.normal, new Vector3(-10f, 0f, -24f));
            PlaceBox(boxesRoot, p, a.normal, new Vector3(10f, 0f, -20f));
            PlaceBox(boxesRoot, p, a.hot, new Vector3(6f, 0f, 30f));
            PlaceBox(boxesRoot, p, a.electric, new Vector3(30f, 0f, 38f));
            PlaceBox(boxesRoot, p, a.frozen, new Vector3(-32f, 5f, -6f));
            PlaceBox(boxesRoot, p, a.toxic, new Vector3(-18f, 0f, 42f));

            var players = new GameObject("Players").transform;
            SpawnCharacter(players, p.player, new Vector3(4f, 0.05f, -31f), a.palette, 0, 1);
            SpawnCharacter(players, p.teammate, new Vector3(-4f, 0.05f, -31f), a.palette, 0, 2);

            var managers = new GameObject("Managers");
            var game = managers.AddComponent<GameManager>();
            game.truck = truck;

            ConfigureCamera();
            BuildHud(a, truck);
            BuildEventSystem();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigureLighting()
        {
            var light = Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
                light.color = new Color(1f, 0.96f, 0.88f);
                light.intensity = 1.1f;
                light.shadows = LightShadows.Soft;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.7f);
        }

        static void ConfigureCamera()
        {
            var camera = Camera.main;
            if (camera == null) return;

            camera.gameObject.AddComponent<ThirdPersonCamera>();
            camera.transform.position = new Vector3(4f, 4f, -37f);
            camera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 250f;
        }

        // -------------------------------------------------------------- terrain

        static void BuildTerrain(Transform env, Mats m)
        {
            Prim(PrimitiveType.Cube, "GroundSouth", env, new Vector3(0f, -0.5f, -15f), new Vector3(90f, 1f, 40f), m.ground).isStatic = true;
            Prim(PrimitiveType.Cube, "GroundNorth", env, new Vector3(0f, -0.5f, 34f), new Vector3(90f, 1f, 42f), m.ground).isStatic = true;
            Prim(PrimitiveType.Cube, "RiverBed", env, new Vector3(0f, -0.9f, 9f), new Vector3(90f, 1f, 8f), m.riverBed).isStatic = true;
            Prim(PrimitiveType.Cube, "WaterSurface", env, new Vector3(0f, -0.17f, 9f), new Vector3(90f, 0.1f, 8f), m.water, false);
            Volume("WaterVolume", env, new Vector3(0f, -0.3f, 9f), new Vector3(90f, 0.5f, 8f), Quaternion.identity, HazardType.Water);
        }

        static void BuildBridge(Transform env, Mats m)
        {
            Prim(PrimitiveType.Cube, "BridgeDeck", env, new Vector3(-27f, 0.1f, 9f), new Vector3(5f, 0.2f, 9f), m.wood);
            Prim(PrimitiveType.Cube, "BridgeRailLeft", env, new Vector3(-29.4f, 0.55f, 9f), new Vector3(0.2f, 0.7f, 9f), m.wood);
            Prim(PrimitiveType.Cube, "BridgeRailRight", env, new Vector3(-24.6f, 0.55f, 9f), new Vector3(0.2f, 0.7f, 9f), m.wood);
        }

        /// <summary>A 5 m plateau reached by an icy ramp (slippery) or by a safe staircase on its east side.</summary>
        static void BuildIceHill(Transform env, Mats m)
        {
            const float plateauTop = 5f;
            const float width = 8f;

            Prim(PrimitiveType.Cube, "Plateau", env, new Vector3(-32f, plateauTop * 0.5f, -6f), new Vector3(10f, plateauTop, 10f), m.rock);

            var start = new Vector3(-32f, 0f, -25f);
            var end = new Vector3(-32f, plateauTop, -11f);
            var rotation = Quaternion.LookRotation((end - start).normalized, Vector3.up);
            float length = (end - start).magnitude;
            const float thickness = 0.5f;

            var center = (start + end) * 0.5f - rotation * Vector3.up * (thickness * 0.5f);
            Prim(PrimitiveType.Cube, "IceRamp", env, center, new Vector3(width, thickness, length), m.ice, true, rotation);

            var iceCenter = (start + end) * 0.5f + rotation * Vector3.up * 0.3f;
            Volume("IceVolume", env, iceCenter, new Vector3(width, 1f, length), rotation, HazardType.Slippery);

            const int steps = 12;
            float rise = plateauTop / steps;
            for (int i = 0; i < steps; i++)
            {
                float top = rise * (i + 1);
                float x = -27f + 1.2f * (steps - i) - 0.6f;
                Prim(PrimitiveType.Cube, "Step" + (i + 1), env, new Vector3(x, top * 0.5f, -6f), new Vector3(1.2f, top, 4f), m.wood);
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

        static void BuildScenery(Transform env, Mats m)
        {
            var scenery = new GameObject("Scenery").transform;
            scenery.SetParent(env, false);
            var rng = new System.Random(1234);

            // Forest around the toxic box (north-west bank); leave a clearing around the box itself.
            for (int i = 0; i < 34; i++)
            {
                float x = Mathf.Lerp(-40f, -5f, (float)rng.NextDouble());
                float z = Mathf.Lerp(24f, 52f, (float)rng.NextDouble());
                if (Vector2.Distance(new Vector2(x, z), new Vector2(-18f, 42f)) < 3.5f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(-27f, 16f)) < 6f) continue;
                Tree(scenery, m, new Vector3(x, 0f, z), Mathf.Lerp(0.8f, 1.4f, (float)rng.NextDouble()));
            }

            // Scattered trees on the other banks.
            for (int i = 0; i < 18; i++)
            {
                float x = Mathf.Lerp(8f, 42f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-30f, 0f, (float)rng.NextDouble());
                if (Mathf.Abs(x - 4f) < 3f && z < -26f) continue;
                Tree(scenery, m, new Vector3(x, 0f, z), Mathf.Lerp(0.8f, 1.3f, (float)rng.NextDouble()));
            }

            // Rock outcrop that hides the hot box.
            var hotSpot = new Vector3(6f, 0f, 30f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f + 20f;
                var offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 3.4f;
                Rock(scenery, m, hotSpot + offset, Mathf.Lerp(1.3f, 2f, (float)rng.NextDouble()), angle);
            }

            // Rocks and walls near the electric box on the far bank.
            for (int i = 0; i < 7; i++)
            {
                float x = Mathf.Lerp(18f, 40f, (float)rng.NextDouble());
                float z = Mathf.Lerp(20f, 50f, (float)rng.NextDouble());
                if (Vector2.Distance(new Vector2(x, z), new Vector2(30f, 38f)) < 3f) continue;
                Rock(scenery, m, new Vector3(x, 0f, z), Mathf.Lerp(1f, 2.2f, (float)rng.NextDouble()), (float)rng.NextDouble() * 360f);
            }
            Prim(PrimitiveType.Cube, "StoneWall1", scenery, new Vector3(24f, 0.9f, 33f), new Vector3(8f, 1.8f, 0.8f), m.rock);
            Prim(PrimitiveType.Cube, "StoneWall2", scenery, new Vector3(36f, 0.9f, 30f), new Vector3(0.8f, 1.8f, 7f), m.rock);

            // A few obstacles near the start.
            Rock(scenery, m, new Vector3(-16f, 0f, -14f), 1.8f, 30f);
            Rock(scenery, m, new Vector3(18f, 0f, -8f), 2.1f, 80f);
            Prim(PrimitiveType.Cube, "FenceLeft", scenery, new Vector3(-12f, 0.6f, -32f), new Vector3(0.3f, 1.2f, 6f), m.wood);
            Prim(PrimitiveType.Cube, "FenceRight", scenery, new Vector3(14f, 0.6f, -32f), new Vector3(0.3f, 1.2f, 6f), m.wood);

            Prim(PrimitiveType.Cube, "DeliveryPad", scenery, new Vector3(0f, 0.02f, -30.8f), new Vector3(6f, 0.04f, 3f), m.marker, false);
        }

        static void Tree(Transform parent, Mats m, Vector3 position, float scale)
        {
            var tree = new GameObject("Tree").transform;
            tree.SetParent(parent, false);
            tree.localPosition = position;
            Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 1.3f * scale, 0f), new Vector3(0.5f, 1.3f, 0.5f) * scale, m.trunk);
            Prim(PrimitiveType.Sphere, "Leaves", tree, new Vector3(0f, 3.4f * scale, 0f), Vector3.one * 3f * scale, m.leaves, false);
        }

        static void Rock(Transform parent, Mats m, Vector3 position, float scale, float yaw)
        {
            Prim(PrimitiveType.Sphere, "Rock", parent, position + Vector3.up * 0.4f * scale,
                new Vector3(2.2f, 1.4f, 1.8f) * scale * 0.7f, m.rock, true, Quaternion.Euler(0f, yaw, 0f));
        }

        static void Volume(string name, Transform parent, Vector3 position, Vector3 size, Quaternion rotation, HazardType type)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            go.AddComponent<HazardZone>().type = type;
        }

        // ---------------------------------------------------------------- truck

        static Truck BuildTruck(Transform env, Mats m, GameAssets a, Prefabs p)
        {
            var root = new GameObject("Truck");
            root.transform.position = new Vector3(0f, 0f, -26f);
            var t = root.transform;

            Prim(PrimitiveType.Cube, "Bed", t, new Vector3(0f, 1f, -0.8f), new Vector3(3.2f, 0.3f, 5.4f), m.truckBody);
            Prim(PrimitiveType.Cube, "RailLeft", t, new Vector3(-1.5f, 1.5f, -0.8f), new Vector3(0.2f, 0.7f, 5.4f), m.truckBody);
            Prim(PrimitiveType.Cube, "RailRight", t, new Vector3(1.5f, 1.5f, -0.8f), new Vector3(0.2f, 0.7f, 5.4f), m.truckBody);
            Prim(PrimitiveType.Cube, "RailFront", t, new Vector3(0f, 1.5f, 1.8f), new Vector3(3.2f, 0.7f, 0.2f), m.truckBody);
            Prim(PrimitiveType.Cube, "Cab", t, new Vector3(0f, 1.6f, 3.2f), new Vector3(3f, 1.9f, 2f), m.truckCab);
            Prim(PrimitiveType.Cube, "Windshield", t, new Vector3(0f, 2.0f, 4.22f), new Vector3(2.4f, 0.7f, 0.1f), m.glass, false);

            foreach (var zOffset in new[] { -2.5f, 2.7f })
            {
                foreach (var xSign in new[] { -1f, 1f })
                {
                    Prim(PrimitiveType.Cylinder, "Wheel", t, new Vector3(1.65f * xSign, 0.5f, zOffset),
                        new Vector3(1f, 0.18f, 1f), m.wheel, false, Quaternion.Euler(0f, 0f, 90f));
                }
            }

            var bedAnchor = new GameObject("BedAnchor").transform;
            bedAnchor.SetParent(t, false);
            bedAnchor.localPosition = new Vector3(0f, 1.15f, -1.8f);

            var delivery = new GameObject("DeliveryZone");
            delivery.transform.SetParent(t, false);
            delivery.transform.localPosition = new Vector3(0f, 0.5f, -4.8f);
            var zone = delivery.AddComponent<BoxCollider>();
            zone.size = new Vector3(6f, 3f, 3f);
            zone.isTrigger = true;

            var truck = root.AddComponent<Truck>();
            truck.order = a.order;
            truck.deliveryPoint = delivery.transform;
            truck.bedAnchor = bedAnchor;
            truck.bedCubePrefab = p.bedCube;
            return truck;
        }

        // ---------------------------------------------------------- placements

        static void PlaceBox(Transform parent, Prefabs p, BoxData data, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(p.box, parent);
            instance.name = "Box_" + data.id;
            instance.transform.position = position;
            SetRef(instance.GetComponent<BoxPickup>(), "data", data);
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
