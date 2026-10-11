using UnityEngine;
using UnityEngine.Rendering;

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
            light.range = 10f;
            light.intensity = 4.2f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            lightObject.AddComponent<FlickerLight>().baseIntensity = 4.2f;

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

            // Tips shown when you get close to a hard box.
            AddHint(points, "Spawn_Hard_Tunnel", "hintbox.tunnel", 9f);
            AddHint(points, "Spawn_Hard_Tower", "hintbox.tower", 10f);
            AddHint(points, "Spawn_Hard_Cliff", "hintbox.cliff", 10f);
            AddHint(points, "Spawn_Hard_Sweeper", "hintbox.sweeper", 11f);
            AddHint(points, "Spawn_Hard_IceRamp", "hintbox.ice", 9f);

            // Lights and flags of the out-of-the-way spots follow the boxes of the game mode.
            LinkArea("Environment/Challenges/ParkourTower", points, "Spawn_Hard_Tower");
            LinkArea("Environment/Challenges/CrawlTunnel", points, "Spawn_Hard_Tunnel");
            LinkArea("Environment/Challenges/MushroomCliff", points, "Spawn_Hard_Cliff");
        }

        static void AddHint(Transform points, string spawnName, string key, float radius)
        {
            var spawn = points.Find(spawnName);
            if (spawn == null) return;
            var hint = spawn.gameObject.AddComponent<AreaHint>();
            hint.hintKey = key;
            hint.radius = radius;
            hint.point = spawn.GetComponent<BoxSpawnPoint>();
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
            managers.gameObject.AddComponent<LightShadowBudget>();
            managers.gameObject.AddComponent<AchievementTracker>();
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

        /// <summary>Which map a borrowed challenge structure is dressed for: the same shape, a look that belongs there.</summary>
        enum MapTheme { Meadow, Snow, Harbour }

        static readonly int[] MeadowRock = { LowPolyKit.Grey, LowPolyKit.Grey, LowPolyKit.Stone };
        static readonly int[] SnowRock = { LowPolyKit.Grey, LowPolyKit.Grey };

        /// <summary>
        /// A spiral of wooden ledges round a pillar, 7 m up, with a box on top. One mistake means a nasty fall. In the meadow the
        /// pillar is a needle of mossy rock, on the summit an icy one, and in the harbour it is the old lighthouse under repair:
        /// the ledges are the scaffolding boards.
        /// </summary>
        static void BuildTower(Transform parent, Mats m, ArtAssets art, MapTheme theme = MapTheme.Meadow)
        {
            var tower = new GameObject("ParkourTower").transform;
            tower.SetParent(parent, false);
            var centre = new Vector3(38f, 0f, -7f);

            HideVisual(Prim(PrimitiveType.Cube, "Pillar", tower, centre + new Vector3(0f, 3.4f, 0f), new Vector3(1.4f, 6.8f, 1.4f), m.stone, true));
            HideVisual(Prim(PrimitiveType.Cube, "TopLedge", tower, centre + new Vector3(0f, 6.85f, 0f), new Vector3(3.2f, 0.3f, 3.2f), m.wood, true));

            var b = new LowPoly.MeshBuilder();
            bool snow = theme == MapTheme.Snow;
            const int ledges = 8;
            for (int i = 0; i < ledges; i++)
            {
                float angle = i * 48f * Mathf.Deg2Rad;
                float height = 0.7f + i * 0.78f;
                var at = centre + new Vector3(Mathf.Cos(angle) * 3.7f, height - 0.15f, Mathf.Sin(angle) * 3.7f);
                var rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                HideVisual(Prim(PrimitiveType.Cube, "Ledge" + (i + 1), tower, at, new Vector3(2.3f, 0.3f, 2.3f), m.wood, true, rotation));

                var local = at - centre;
                LowPolyKit.Planks(b, local + Vector3.up * 0.15f, new Vector2(2.3f, 2.3f), 0.3f, rotation, i * 7 + 1, snow);
                // two brackets tie each board to the pillar, so it does not float
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var across = new Vector3(-dir.z, 0f, dir.x);
                float pillar = theme == MapTheme.Harbour ? Mathf.Lerp(1.15f, 0.92f, height / 6.6f) : 0.75f;
                foreach (float side in new[] { -0.55f, 0.55f })
                {
                    LowPolyKit.Beam(b, dir * pillar + across * side * 0.6f + Vector3.up * (height - 1.15f), local + across * side - dir * 0.9f + Vector3.up * -0.12f, 0.12f, LowPolyKit.DarkWood);
                    LowPolyKit.Beam(b, dir * pillar + across * side * 0.6f + Vector3.up * (height - 0.25f), local + across * side - dir * 0.9f + Vector3.up * -0.12f, 0.1f, LowPolyKit.DarkWood);
                }
            }

            if (theme == MapTheme.Harbour)
            {
                var body = MeshObject("Lighthouse", tower, Kit("Kit_LighthouseBody", () => LowPolyKit.LighthouseBody("l", (ledges - 1) * 48f)), art.palette);
                body.transform.position = centre;
                var room = MeshObject("LanternRoom", tower, Kit("Kit_LanternRoom", () => LowPolyKit.LanternRoom("r")), art.palette);
                room.transform.position = centre + Vector3.up * 7f;
                EnsureKitMaterials();
                var glass = Prim(PrimitiveType.Cylinder, "LanternGlass", tower, centre + Vector3.up * (7f + LowPolyKit.LanternGlassMid), new Vector3(1.55f, 0.58f, 1.55f), lanternGlass, false);
                glass.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            else
            {
                LowPolyKit.RockBlock(b, Vector3.zero, new Vector3(1.4f, 6.8f, 1.4f), 0.32f, 11, snow ? SnowRock : MeadowRock, snow ? LowPolyKit.Snow : LowPolyKit.Moss);
                LowPolyKit.FootRocks(b, Vector3.zero, new Vector2(0.75f, 0.75f), 6, 0.55f, 13, snow ? SnowRock : MeadowRock);
                LowPolyKit.Planks(b, new Vector3(0f, 7.0f, 0f), new Vector2(3.2f, 3.2f), 0.3f, Quaternion.identity, 99, snow);
                if (snow) LowPolyKit.Icicles(b, new Vector3(0f, 6.85f - 0.25f, 0f), new Vector3(3.0f, 0.3f, 3.0f), 14, 5);
                else
                    for (int i = 0; i < 7; i++)   // ivy climbing the rock
                        LowPolyKit.Blob(b, new Vector3(Mathf.Cos(i * 2.1f) * 0.85f, 0.8f + i * 0.85f, Mathf.Sin(i * 2.1f) * 0.85f), new Vector3(0.35f, 0.5f, 0.35f), i % 2 == 0 ? LowPolyKit.Moss : LowPolyKit.Olive, i, 1, 0.2f);
            }
            var dressingMesh = MeshObject("TowerDressing", tower, Kit("Kit_Tower_" + theme, () => b.ToMesh("t")), art.palette);
            dressingMesh.transform.position = centre;

            // a flag and a lantern at the top so it can be seen from afar
            var dressing = new GameObject("Dressing").transform;
            dressing.SetParent(tower, false);
            var flag = MeshObject("TowerFlag", dressing, flags[0], art.palette);
            flag.transform.position = centre + new Vector3(1.2f, 7.0f, 1.2f);
            if (theme != MapTheme.Harbour)
            {
                var lamp = MeshObject("TowerLamp", dressing, lampMesh, art.palette);
                lamp.transform.position = centre + new Vector3(-1.1f, 7.0f, -1.1f);
                AddLampLight(lamp.transform, new Vector3(0f, 2.78f, 0f), 10f, 2.4f);
            }
            tower.gameObject.AddComponent<SpawnAreaLights>().dressing = dressing.gameObject;
        }

        /// <summary>
        /// A low tunnel under a mound: only a crouching player fits through. The box lies at the dead end. In the meadow the mound
        /// is grassy; on the summit it is a snowy rock with the timber frame, rails and cart of an old mine.
        /// </summary>
        static void BuildTunnel(Transform parent, Mats m, ArtAssets art, MapTheme theme = MapTheme.Meadow)
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
                HideVisual(Prim(PrimitiveType.Cube, name, tunnel, c + centre, size, m.grass, true));
            var mound = theme == MapTheme.Snow
                ? Kit("Kit_MineMound", () => LowPolyKit.Recolor(LowPolyProps.TunnelMound(), "m", (LowPolyKit.Moss, LowPolyKit.Grey), (LowPolyKit.Olive, LowPolyKit.Snow), (LowPolyKit.Gold, LowPolyKit.Snow)))
                : SaveInkedMesh(LowPolyProps.TunnelMound());
            var moundVisual = MeshObject("MoundVisual", tunnel, mound, art.palette);
            moundVisual.transform.position = c;

            if (theme == MapTheme.Snow)
            {
                var frame = MeshObject("MineFrame", tunnel, Kit("Kit_MineFrame", () => LowPolyKit.MineFrame("f", 1.0f, 1.4f)), art.palette);
                frame.transform.position = c + new Vector3(0f, 0f, -4.18f);
                WorldText(tunnel, "sign.mine", c + new Vector3(0f, 2.12f, -4.37f), Quaternion.identity, new Vector2(1.4f, 0.42f), 3f, UiFactory.Ink, -1, 0.8f);
                var rails = new LowPoly.MeshBuilder();
                LowPolyKit.Rails(rails, new Vector3(0f, 0f, 3.2f), new Vector3(0f, 0f, -8.5f));
                MeshObject("Rails", tunnel, Kit("Kit_MineRails", () => rails.ToMesh("r")), art.palette).transform.position = c;
                var cart = Piece(tunnel, "MineCart", Kit("Kit_MineCart", () => LowPolyKit.MineCart("c", LowPolyKit.Gold)), art.palette, c + new Vector3(0f, 0.12f, -7.6f), 0f, 1f, new Vector3(1.5f, 1.0f, 1.5f));
                cart.transform.rotation = Quaternion.Euler(0f, 4f, 0f);
            }

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

        /// <summary>
        /// Two ledges one above the other, each reached from the one below by a bounce pad. The box is on the top one. Rocks with
        /// mushrooms in the meadow, icy rocks with frosted mushrooms on the summit, stacked containers with inflatable buoys in the harbour.
        /// </summary>
        static void BuildMushroomCliff(Transform parent, Mats m, ArtAssets art, MapTheme theme = MapTheme.Meadow)
        {
            var cliff = new GameObject("MushroomCliff").transform;
            cliff.SetParent(parent, false);

            HideVisual(Prim(PrimitiveType.Cube, "LedgeLow", cliff, new Vector3(30f, 1.8f, 50f), new Vector3(5f, 3.6f, 5f), m.stone, true));
            HideVisual(Prim(PrimitiveType.Cube, "LedgeHigh", cliff, new Vector3(35.5f, 3.4f, 50f), new Vector3(4.2f, 6.8f, 4.2f), m.stone, true));
            Mesh low, high;
            if (theme == MapTheme.Harbour)
            {
                int[] colours = { LowPolyKit.Brick, LowPolyKit.Teal, LowPolyKit.Gold, LowPolyKit.Navy, LowPolyKit.Rust };
                low = Kit("Kit_ContainersLow", () => LowPolyKit.ContainerStack("c", new Vector3(5f, 3.6f, 5f), 2, colours, 4));
                high = Kit("Kit_ContainersHigh", () => LowPolyKit.ContainerStack("c", new Vector3(4.2f, 6.8f, 4.2f), 3, colours, 9));
            }
            else
            {
                bool snow = theme == MapTheme.Snow;
                int[] rock = snow ? SnowRock : MeadowRock;
                int top = snow ? LowPolyKit.Snow : LowPolyKit.Grass;
                low = Kit("Kit_CliffLow_" + theme, () => LowPolyKit.Rock("r", new Vector3(5f, 3.6f, 5f), 0.5f, 21, rock, top, true, snow));
                high = Kit("Kit_CliffHigh_" + theme, () => LowPolyKit.Rock("r", new Vector3(4.2f, 6.8f, 4.2f), 0.5f, 37, rock, top, true, snow));
            }
            MeshObject("LedgeLowVisual", cliff, low, art.palette).transform.position = new Vector3(30f, 0f, 50f);
            MeshObject("LedgeHighVisual", cliff, high, art.palette).transform.position = new Vector3(35.5f, 0f, 50f);

            AddBounce(cliff, new Vector3(30f, GroundHeight(30f, 44.5f), 44.5f), art, theme);
            AddBounce(cliff, new Vector3(30.2f, 3.6f, 51.5f), art, theme);

            var dressing = new GameObject("Dressing").transform;
            dressing.SetParent(cliff, false);
            var flag = MeshObject("CliffFlag", dressing, flags[1], art.palette);
            flag.transform.position = new Vector3(36.6f, 6.9f, 51.2f);
            var lamp = MeshObject("CliffLamp", dressing, lampMesh, art.palette);
            lamp.transform.position = new Vector3(34.4f, 6.9f, 48.9f);
            AddLampLight(lamp.transform, new Vector3(0f, 2.78f, 0f), 10f, 2.4f);
            cliff.gameObject.AddComponent<SpawnAreaLights>().dressing = dressing.gameObject;
        }

        static void AddBounce(Transform parent, Vector3 position, ArtAssets art, MapTheme theme = MapTheme.Meadow)
        {
            var pad = theme == MapTheme.Harbour
                ? MeshObject("BounceBuoy", parent, Kit("Kit_BounceBuoy", () => LowPolyKit.BounceBuoy("b")), art.palette)
                : theme == MapTheme.Snow
                    ? MeshObject("BounceMushroom", parent, Kit("Kit_FrostMushroom", () => LowPolyKit.Recolor(LowPolyProps.Mushroom("m"), "m", (LowPolyKit.Brick, LowPolyKit.Teal), (LowPolyKit.Salmon, LowPolyKit.LightTeal))), art.palette)
                    : MeshObject("BounceMushroom", parent, mushroomMesh, art.palette);
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
