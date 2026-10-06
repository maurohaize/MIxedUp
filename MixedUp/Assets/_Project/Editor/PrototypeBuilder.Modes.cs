using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Everything the game modes need on the map: the spawn points for boxes (the six of the classic puzzle, plenty of ordinary
    /// ones and the hard-to-reach ones), the challenge structures (tower, tunnel, mushroom cliff, island) and the director.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        static GameObject beaconPrefab;

        /// <summary>Extra ordinary spots for the random modes (x, z). Scenery stays away from them (see Reserved).</summary>
        static readonly Vector2[] EasySpots =
        {
            new Vector2(16f, -14f), new Vector2(-2f, -8f), new Vector2(36f, -5f), new Vector2(30f, -12f), new Vector2(-28f, 27f),
            new Vector2(-8f, 22f), new Vector2(28f, 47f), new Vector2(40f, 22f), new Vector2(-38f, 47f), new Vector2(14f, 17f),
            new Vector2(-4f, 34f), new Vector2(36f, 44f)
        };

        /// <summary>The hard spots, also kept clear of scenery. Their structures are built in BuildChallenges.</summary>
        static readonly Vector2[] ChallengeAreas =
        {
            new Vector2(38f, -7f), new Vector2(-37f, 33f), new Vector2(32f, 50f)
        };

        static GameObject BuildBeacon()
        {
            var root = new GameObject("BoxBeacon");
            var lightObject = new GameObject("Glow");
            lightObject.transform.SetParent(root.transform, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 3.2f;
            light.shadows = LightShadows.None;
            lightObject.AddComponent<FlickerLight>().baseIntensity = 3.2f;

            var halo = AddHalo(root.transform, new Vector3(0f, 0.6f, 0f), 4.2f, new Color(1f, 1f, 1f, 0.6f));
            var beacon = root.AddComponent<BoxBeacon>();
            beacon.glow = light;
            beacon.halo = halo.GetComponent<Renderer>();
            return root;
        }

        static BoxSpawnPoint SpawnPoint(Transform parent, string name, Vector3 position, string classicBox = null, bool hard = false, bool original = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var point = go.AddComponent<BoxSpawnPoint>();
            point.classicBoxId = classicBox;
            point.hard = hard;
            point.original = original;
            return point;
        }

        static void BuildSpawnPoints(Transform root)
        {
            var points = new GameObject("SpawnPoints").transform;
            points.SetParent(root, false);

            // The six boxes of the classic puzzle.
            SpawnPoint(points, "Spawn_Classic_Normal1", new Vector3(-10f, GroundHeight(-10f, -24f), -24f), "normal", false, true);
            SpawnPoint(points, "Spawn_Classic_Normal2", new Vector3(10f, GroundHeight(10f, -20f), -20f), "normal", false, true);
            SpawnPoint(points, "Spawn_Classic_Hot", new Vector3(6f, GroundHeight(6f, 30f), 30f), "hot", false, true);
            SpawnPoint(points, "Spawn_Classic_Electric", new Vector3(30f, GroundHeight(30f, 38f), 38f), "electric", false, true);
            SpawnPoint(points, "Spawn_Classic_Frozen", new Vector3(-32f, 5f, -6f), "frozen", false, true);
            SpawnPoint(points, "Spawn_Classic_Toxic", new Vector3(-18f, 2.4f, 43f), "toxic", false, true);

            for (int i = 0; i < EasySpots.Length; i++)
                SpawnPoint(points, "Spawn_Easy" + (i + 1).ToString("00"), new Vector3(EasySpots[i].x, GroundHeight(EasySpots[i].x, EasySpots[i].y), EasySpots[i].y));

            // An islet in the river, reached by stepping stones.
            SpawnPoint(points, "Spawn_Island", new Vector3(3f, 0.15f, 9f));

            // Hard spots.
            SpawnPoint(points, "Spawn_Hard_Tower", new Vector3(38f, 7.05f, -7f), null, true);
            SpawnPoint(points, "Spawn_Hard_Tunnel", new Vector3(-37f, 0f, 35.1f), null, true);
            SpawnPoint(points, "Spawn_Hard_Cliff", new Vector3(35.5f, 6.85f, 50f), null, true);
            SpawnPoint(points, "Spawn_Hard_Sweeper", new Vector3(24.4f, GroundHeight(24.4f, 26f), 26f), null, true);
            SpawnPoint(points, "Spawn_Hard_IceRamp", new Vector3(-32f, 2.52f, -18f), null, true);

            // Lights and flags of the out-of-the-way spots follow the boxes of the game mode.
            LinkArea("Environment/Challenges/ParkourTower", points, "Spawn_Hard_Tower");
            LinkArea("Environment/Challenges/CrawlTunnel", points, "Spawn_Hard_Tunnel");
            LinkArea("Environment/Challenges/MushroomCliff", points, "Spawn_Hard_Cliff");
        }

        static void LinkArea(string path, Transform points, params string[] spawnNames)
        {
            var area = GameObject.Find(path);
            var lights = area != null ? area.GetComponent<SpawnAreaLights>() : null;
            if (lights == null) return;
            var found = new System.Collections.Generic.List<BoxSpawnPoint>();
            foreach (var name in spawnNames)
            {
                var t = points.Find(name);
                if (t != null) found.Add(t.GetComponent<BoxSpawnPoint>());
            }
            lights.points = found.ToArray();
        }

        static void BuildDirector(Transform managers, GameAssets a, Prefabs p, Truck truck)
        {
            var director = managers.gameObject.AddComponent<LevelDirector>();
            director.truck = truck;
            director.rules = a.rules;
            director.boxKinds = a.database.boxes;
            director.boxPickupPrefab = p.box;
            director.beaconPrefab = beaconPrefab;
            director.teammatePrefab = p.teammate;
            director.palette = a.palette;

            var night = managers.gameObject.AddComponent<NightLighting>();
            director.night = night;
        }

        // ----------------------------------------------------------- challenges

        static void BuildChallenges(Transform env, Mats m, ArtAssets art)
        {
            var group = new GameObject("Challenges").transform;
            group.SetParent(env, false);
            BuildTower(group, m, art);
            BuildTunnel(group, m, art);
            BuildMushroomCliff(group, m, art);
            BuildIsland(group, m, art);
        }

        /// <summary>A spiral of wooden ledges round a stone pillar, 7 m up, with a box on top. One mistake means a nasty fall.</summary>
        static void BuildTower(Transform parent, Mats m, ArtAssets art)
        {
            var tower = new GameObject("ParkourTower").transform;
            tower.SetParent(parent, false);
            var centre = new Vector3(38f, 0f, -7f);

            Prim(PrimitiveType.Cube, "Pillar", tower, centre + new Vector3(0f, 3.4f, 0f), new Vector3(1.4f, 6.8f, 1.4f), m.stone, true, null, true);
            Prim(PrimitiveType.Cube, "TopLedge", tower, centre + new Vector3(0f, 6.85f, 0f), new Vector3(3.2f, 0.3f, 3.2f), m.wood, true, null, true);
            Prim(PrimitiveType.Cube, "TopSnow", tower, centre + new Vector3(0f, 7.03f, 0f), new Vector3(2.8f, 0.06f, 2.8f), m.marker, false);

            const int ledges = 8;
            for (int i = 0; i < ledges; i++)
            {
                float angle = i * 48f * Mathf.Deg2Rad;
                float height = 0.7f + i * 0.78f;
                var at = centre + new Vector3(Mathf.Cos(angle) * 3.7f, height - 0.15f, Mathf.Sin(angle) * 3.7f);
                Prim(PrimitiveType.Cube, "Ledge" + (i + 1), tower, at, new Vector3(2.3f, 0.3f, 2.3f), i % 2 == 0 ? m.wood : m.woodDark, true, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f), true);
            }

            // a flag and a lantern at the top so it can be seen from afar
            var dressing = new GameObject("Dressing").transform;
            dressing.SetParent(tower, false);
            var flag = MeshObject("TowerFlag", dressing, flags[0], art.palette);
            flag.transform.position = centre + new Vector3(1.2f, 7.0f, 1.2f);
            var lamp = MeshObject("TowerLamp", dressing, lampMesh, art.palette);
            lamp.transform.position = centre + new Vector3(-1.1f, 7.0f, -1.1f);
            AddLampLight(lamp.transform, new Vector3(0f, 2.78f, 0f), 10f, 2.4f);
            tower.gameObject.AddComponent<SpawnAreaLights>().dressing = dressing.gameObject;
        }

        /// <summary>A low tunnel under a grassy mound: only a crouching player fits through. The box lies at the dead end.</summary>
        static void BuildTunnel(Transform parent, Mats m, ArtAssets art)
        {
            var tunnel = new GameObject("CrawlTunnel").transform;
            tunnel.SetParent(parent, false);
            var c = new Vector3(-37f, 0f, 33f);

            Prim(PrimitiveType.Cube, "WallLeft", tunnel, c + new Vector3(-1.45f, 0.7f, 0f), new Vector3(0.9f, 1.4f, 7.4f), m.stone, true, null, true);
            Prim(PrimitiveType.Cube, "WallRight", tunnel, c + new Vector3(1.45f, 0.7f, 0f), new Vector3(0.9f, 1.4f, 7.4f), m.stone, true, null, true);
            Prim(PrimitiveType.Cube, "Roof", tunnel, c + new Vector3(0f, 1.575f, 0f), new Vector3(3.8f, 0.35f, 7.4f), m.stone, true, null, true);   // clearance 1.4 m
            Prim(PrimitiveType.Cube, "BackWall", tunnel, c + new Vector3(0f, 0.8f, 3.85f), new Vector3(3.8f, 1.6f, 0.4f), m.stone, true, null, true);
            // The collision of the mound is two boxes; what you see is a lumpy turf dome.
            foreach (var (name, centre, size) in new[] { ("Mound", new Vector3(0f, 2.3f, 0f), new Vector3(4.6f, 1.4f, 7.8f)), ("MoundTop", new Vector3(0f, 3.1f, 0.2f), new Vector3(3.4f, 0.9f, 6.2f)) })
            {
                var box = Prim(PrimitiveType.Cube, name, tunnel, c + centre, size, m.grass, true);
                Object.DestroyImmediate(box.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(box.GetComponent<MeshFilter>());
            }
            var moundVisual = MeshObject("MoundVisual", tunnel, SaveInkedMesh(LowPolyProps.TunnelMound()), art.palette);
            moundVisual.transform.position = c;

            // lanterns hanging from the roof of the passage; they only burn when a box lies in here
            var dressing = new GameObject("Dressing").transform;
            dressing.SetParent(tunnel, false);
            AddHangingLantern(dressing, c + new Vector3(0f, 1.4f, -0.4f), 0.3f, 7f, 1.8f);
            AddHangingLantern(dressing, c + new Vector3(0f, 1.4f, 2.6f), 0.3f, 6f, 1.6f);
            AddTorchPost(dressing, c + new Vector3(-1.9f, 0f, -3.9f), 1.6f);
            AddTorchPost(dressing, c + new Vector3(1.9f, 0f, -3.9f), 1.6f);
            tunnel.gameObject.AddComponent<SpawnAreaLights>().dressing = dressing.gameObject;
            // boulders and a signpost at the entrance
            PlaceProp(tunnel, art.rocks[1], c + new Vector3(-3.1f, 0f, -3.4f), 30f, 1.1f);
            PlaceProp(tunnel, art.rocks[5], c + new Vector3(3.2f, 0f, -3.1f), 160f, 1.0f);
            PlaceProp(tunnel, art.cartel, c + new Vector3(2.3f, 0f, -4.4f), 200f, 1f);
        }

        /// <summary>Two ledges one above the other, each reached from the one below by a bounce mushroom. The box is on the top one.</summary>
        static void BuildMushroomCliff(Transform parent, Mats m, ArtAssets art)
        {
            var cliff = new GameObject("MushroomCliff").transform;
            cliff.SetParent(parent, false);

            Prim(PrimitiveType.Cube, "LedgeLow", cliff, new Vector3(30f, 1.8f, 50f), new Vector3(5f, 3.6f, 5f), m.stone, true, null, true);
            Prim(PrimitiveType.Cube, "LedgeHigh", cliff, new Vector3(35.5f, 3.4f, 50f), new Vector3(4.2f, 6.8f, 4.2f), m.stone, true, null, true);
            Prim(PrimitiveType.Cube, "LedgeLowGrass", cliff, new Vector3(30f, 3.65f, 50f), new Vector3(5.1f, 0.12f, 5.1f), m.grass, false);
            Prim(PrimitiveType.Cube, "LedgeHighGrass", cliff, new Vector3(35.5f, 6.85f, 50f), new Vector3(4.3f, 0.12f, 4.3f), m.grass, false);

            AddBounce(cliff, new Vector3(30f, GroundHeight(30f, 44.5f), 44.5f), art);
            AddBounce(cliff, new Vector3(30.2f, 3.6f, 51.5f), art);

            var dressing = new GameObject("Dressing").transform;
            dressing.SetParent(cliff, false);
            var flag = MeshObject("CliffFlag", dressing, flags[1], art.palette);
            flag.transform.position = new Vector3(36.6f, 6.9f, 51.2f);
            var lamp = MeshObject("CliffLamp", dressing, lampMesh, art.palette);
            lamp.transform.position = new Vector3(34.4f, 6.9f, 48.9f);
            AddLampLight(lamp.transform, new Vector3(0f, 2.78f, 0f), 10f, 2.4f);
            cliff.gameObject.AddComponent<SpawnAreaLights>().dressing = dressing.gameObject;
        }

        static void AddBounce(Transform parent, Vector3 position, ArtAssets art)
        {
            var pad = MeshObject("BounceMushroom", parent, mushroomMesh, art.palette);
            pad.transform.position = position;
            pad.isStatic = false;
            var bounce = pad.AddComponent<BouncePad>();
            bounce.launchSpeed = 14.5f;
            bounce.radius = 0.95f;
            bounce.capHeight = 1.1f;
            bounce.squashed = pad.transform;
        }

        /// <summary>A small sandy islet in the river with a bush, reached by three stepping stones from the south bank.</summary>
        static void BuildIsland(Transform parent, Mats m, ArtAssets art)
        {
            var island = new GameObject("Islet").transform;
            island.SetParent(parent, false);

            Prim(PrimitiveType.Cube, "IsletSand", island, new Vector3(3f, -0.1f, 9f), new Vector3(3.4f, 0.5f, 3.4f), m.sandy, true, null, true);
            Prim(PrimitiveType.Cube, "IsletSandTop", island, new Vector3(3f, 0.16f, 9f), new Vector3(3.1f, 0.05f, 3.1f), m.sandy, false);
            var bush = MeshObject("IsletBush", island, art.bushes[0], art.palette);
            bush.transform.position = new Vector3(3.9f, 0.15f, 9.9f);
            bush.transform.localScale = Vector3.one * 1.1f;

            foreach (var (x, z) in new[] { (2.1f, 6.3f), (3.6f, 4.9f) })
                Prim(PrimitiveType.Cube, "IsletStone", island, new Vector3(x, -0.12f, z), new Vector3(1.3f, 0.5f, 1.3f), m.stone, true, null, true);
        }
    }
}
