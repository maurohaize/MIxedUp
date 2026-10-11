using System.Collections.Generic;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// "Frozen Summit": a snowy mountain pass round a big frozen lake. Gravel paths link the base camp, the mountain hut, the
    /// old mine, the power pylon, the chemical dump, an icy lookout and a chair lift up the mountain; a road climbs in from the
    /// valley to where the truck parked. A harder map: the ice is slippery and the hard boxes are really hard.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        // White slopes with bare grey rock only where they are steep (one rock colour, so the cliffs are not a patchwork).
        static readonly LowPoly.HillColors SnowHills = new LowPoly.HillColors
        {
            low = new[] { 25 }, mid = new[] { 25 }, high = new[] { 25, 25, 25, 26 }, rock = new[] { 26 }
        };

        static readonly Vector2 SummitLakeCentre = new Vector2(6f, -6f);

        /// <summary>The shore of the frozen lake, round from the north (a convex blob that reaches out to the far islet).</summary>
        static readonly Vector2[] SummitLake =
        {
            V(6f, 6.5f), V(13f, 5.5f), V(19f, 3f), V(23.5f, -1.5f), V(25.5f, -7f), V(25f, -13f), V(23f, -17.5f), V(17.5f, -19.5f),
            V(10f, -19.8f), V(2f, -19.2f), V(-5f, -18f), V(-10.5f, -15f), V(-12.5f, -9f), V(-11.5f, -2.5f), V(-7.5f, 2.5f), V(-1f, 5.5f)
        };

        /// <summary>The gravel paths between the places (painted on the snow, kept clear of trees).</summary>
        static readonly Vector2[][] SummitTrails =
        {
            new[] { V(-26f, -21f), V(-22f, -16f), V(-17f, -12f) },                                                              // truck -> crossroads
            new[] { V(-17f, -12f), V(-23f, -13.6f), V(-30f, -13.2f) },                                                          // -> glacier ramp
            new[] { V(-17f, -12f), V(-16.5f, -3f), V(-19f, 5f), V(-24f, 10.5f) },                                               // -> base camp
            new[] { V(-27.5f, 19.5f), V(-24.5f, 26f), V(-20.5f, 33f), V(-20f, 40.5f) },                                         // camp -> hut
            new[] { V(-17f, -12f), V(-15f, -19f), V(-8f, -23f), V(2f, -23.5f), V(12f, -23.5f), V(22f, -24.5f), V(30f, -22f), V(34f, -16f), V(35f, -12f) },
            new[] { V(-16.5f, -3f), V(-12f, 4f), V(-6f, 9.5f), V(0f, 12f), V(8f, 10.5f), V(16f, 9f), V(23f, 11f), V(27.5f, 12f) },
            new[] { V(0f, 12f), V(2f, 20f), V(0f, 28f), V(-3f, 33.5f), V(-1f, 37.5f) },                                          // -> chemical dump
            new[] { V(2f, 20f), V(11f, 22.5f), V(15.5f, 30f), V(18f, 38f), V(24f, 43f), V(29f, 44.5f) },                         // -> cliff
            new[] { V(-20f, 40.5f), V(-27f, 37f), V(-33f, 32f), V(-37f, 27.5f) },                                               // hut -> mine
            new[] { V(27.5f, 12f), V(32f, 6f), V(34f, 0f), V(34.5f, -1.5f) },                                                   // pylon -> lookout
            new[] { V(18f, 38f), V(14f, 46f), V(12f, 50f) }                                                                     // -> chair lift
        };

        /// <summary>The road from the valley to the truck (it enters through the arch at the south edge).</summary>
        static readonly Vector2[] SummitRoad =
        {
            V(-26f, -33.5f), V(-26f, -46f), V(-21f, -60f), V(-28f, -76f), V(-23f, -94f), V(-31f, -114f), V(-27f, -136f), V(-35f, -160f)
        };

        static float SummitTrailDistance(float x, float z)
        {
            float best = float.MaxValue;
            foreach (var trail in SummitTrails) best = Mathf.Min(best, DistanceToLine(trail, x, z));
            return best;
        }

        static bool InPolygon(IList<Vector2> polygon, float x, float z)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i], b = polygon[j];
                if ((a.y > z) != (b.y > z) && x < (b.x - a.x) * (z - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        static void BuildSummitScene(GameAssets a, Mats m, ArtAssets art, Prefabs p) =>
            BuildMapScene(SummitScenePath, a, m, art, p, ctx => FillSummit(ctx));

        static void FillSummit(MapContext c)
        {
            EnsureKitMaterials();
            var env = c.env;
            var m = c.m;
            var art = c.art;
            var pal = art.palette;

            SetMood(art, "SkySummit",
                new Color(0.42f, 0.58f, 0.82f), new Color(0.88f, 0.92f, 0.97f), new Color(0.72f, 0.78f, 0.86f),
                new Color(1f, 0.97f, 0.92f), 1.25f, new Vector3(42f, -30f, 0f),
                new Color(0.7f, 0.8f, 0.95f), new Color(0.74f, 0.78f, 0.84f), new Color(0.46f, 0.5f, 0.58f));

            // ---------------------------------------------------------- ground: snow with a faint relief, paths trodden into it
            var lakeLoop = LowPolyKit.SmoothLoop(SummitLake, 1.6f);
            ColliderBox("SnowFieldCollider", env, new Vector3(0f, -0.5f, 10f), new Vector3(90f, 1f, 92f));
            float SnowHeight(float x, float z)
            {
                if (InPolygon(SummitLake, x, z)) return -0.01f;
                float away = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 3f, SummitTrailDistance(x, z)));
                return Mathf.PerlinNoise(x * 0.11f + 3f, z * 0.11f + 9f) * 0.09f * away;
            }
            int SnowPath(float x, float z)
            {
                float d = SummitTrailDistance(x, z);
                if (d > 1.5f) return -1;
                int hash = (Mathf.FloorToInt(x) * 31 + Mathf.FloorToInt(z) * 17) & 3;
                return d < 0.8f ? (hash == 0 ? 20 : 2) : (hash < 2 ? 2 : 25);
            }
            MeshObject("SnowField", env, SaveMesh(LowPoly.Terrain("Map_SummitSnow", -45f, -36f, 45f, 56f, 1f, new[] { 25 }, 4, SnowHeight, SnowPath)), pal);

            var field = MapHillField(5, SummitRoad);
            MeshObject("Hills", env, SaveMesh(LowPoly.Hills("Map_SummitHills", field, SnowHills)), pal);
            c.truckPosition = new Vector3(-26f, 0f, -26f);
            c.noteSpots = new[]
            {
                c.truckPosition + new Vector3(-5.5f, 0f, -4.5f), c.truckPosition + new Vector3(6f, 0f, -9f), c.truckPosition + new Vector3(-7f, 0f, 4f)
            };

            // ---------------------------------------------------------- the frozen lake: glossy ice, snowy shore, a rocky islet
            var lake = new GameObject("FrozenLake").transform;
            lake.SetParent(env, false);
            MeshObject("Ice", lake, SaveMesh(LowPolyKit.LakeIce("Map_SummitLakeIce", lakeLoop, SummitLakeCentre, 0.025f, 3)), fxIce != null ? fxIce : pal, false);
            MeshObject("Shore", lake, Kit("Kit_SummitLakeShore", () => LowPolyKit.LakeShore("s", lakeLoop, SummitLakeCentre, 7)), pal);
            var volume = new GameObject("IceLakeVolume");
            volume.transform.SetParent(lake, false);
            var trigger = volume.AddComponent<MeshCollider>();
            trigger.sharedMesh = PrismMesh("Collider_SummitLake", SummitLake, 0f, 1.2f);
            trigger.convex = true;
            trigger.isTrigger = true;
            volume.AddComponent<HazardZone>().type = HazardType.Slippery;

            const float lakeX = 6f, lakeZ = -6f;
            HideVisual(Prim(PrimitiveType.Cube, "Islet", lake, new Vector3(lakeX, 0.17f, lakeZ), new Vector3(4.4f, 0.34f, 4.4f), m.stone, true));
            MeshObject("IsletRock", lake, Kit("Kit_SummitIslet", () =>
            {
                var b = new LowPoly.MeshBuilder();
                LowPolyKit.RockBlock(b, Vector3.zero, new Vector3(4.4f, 0.34f, 4.4f), 0.35f, 5, SnowRock, LowPolyKit.Snow, 0.12f);
                LowPolyKit.Blob(b, new Vector3(-1.5f, 0.45f, 1.6f), new Vector3(0.7f, 0.55f, 0.6f), LowPolyKit.Grey, 1, 1, 0.15f);
                LowPolyKit.Blob(b, new Vector3(-0.6f, 0.4f, 1.9f), new Vector3(0.5f, 0.4f, 0.5f), LowPolyKit.Stone, 2, 1, 0.15f);
                LowPolyKit.Blob(b, new Vector3(-1.6f, 0.82f, 1.55f), new Vector3(0.5f, 0.14f, 0.45f), LowPolyKit.Snow, 3, 1, 0.1f);
                return b.ToMesh("i");
            }), pal).transform.position = new Vector3(lakeX, 0f, lakeZ);
            var isletRocks = new GameObject("IsletRocksCollider");
            isletRocks.transform.SetParent(lake, false);
            isletRocks.transform.position = new Vector3(lakeX - 1.1f, 0.6f, lakeZ + 1.7f);
            isletRocks.AddComponent<BoxCollider>().size = new Vector3(1.8f, 0.6f, 1.1f);
            Pine(lake, art, 1, new Vector3(lakeX + 1.6f, 0.34f, lakeZ - 1.4f), 0.55f, true);

            HideVisual(Prim(PrimitiveType.Cube, "FarIslet", lake, new Vector3(22f, 0.17f, -14.5f), new Vector3(2.6f, 0.34f, 2.6f), m.stone, true));
            MeshObject("FarIsletRock", lake, Kit("Kit_SummitFarIslet", () => LowPolyKit.Rock("f", new Vector3(2.6f, 0.34f, 2.6f), 0.3f, 8, SnowRock, LowPolyKit.Snow, false)), pal)
                .transform.position = new Vector3(22f, 0f, -14.5f);

            // ------------------------------------------------- the boxes' spots
            c.Classic("Normal1", "normal", new Vector3(-36f, 0f, -18f));
            c.Classic("Normal2", "normal", new Vector3(-14f, 0f, -20f));
            c.Classic("Hot", "hot", new Vector3(-26.5f, 0f, 16f));
            c.Classic("Electric", "electric", new Vector3(30f, 0f, 13f));
            c.Classic("Frozen", "frozen", new Vector3(lakeX, 0.36f, lakeZ));
            c.Classic("Toxic", "toxic", new Vector3(0f, 0f, 41f));
            foreach (var spot in new[]
            {
                new Vector2(-35f, 3f), new Vector2(-18f, 28f), new Vector2(12f, 22f), new Vector2(26f, -24f),
                new Vector2(36f, -22f), new Vector2(-6f, 34f), new Vector2(18f, 42f), new Vector2(-36f, 46f), new Vector2(-6f, 10f)
            })
                c.Easy(new Vector3(spot.x, 0f, spot.y));

            // ------------------------------------------------- the glacier: an icy rock with a slippery ramp (hard)
            var glacier = new GameObject("Glacier").transform;
            glacier.SetParent(env, false);
            HideVisual(Prim(PrimitiveType.Cube, "GlacierTop", glacier, new Vector3(-30f, 1.7f, 1.4f), new Vector3(6f, 3.4f, 6f), m.stone, true));
            MeshObject("GlacierRock", glacier, Kit("Kit_SummitGlacier", () => LowPolyKit.Rock("g", new Vector3(6f, 3.4f, 6f), 0.5f, 17, SnowRock, LowPolyKit.Snow, true, true)), pal)
                .transform.position = new Vector3(-30f, 0f, 1.4f);
            Ramp(glacier, "GlacierRamp", new Vector3(-30f, 0f, -11.5f), new Vector3(-30f, 3.4f, -1.6f), 3.4f, 0.4f, m.ice);
            HazardVolume("GlacierRampIce", glacier, new Vector3(-30f, 1.7f, -6.6f), new Vector3(3.4f, 2.6f, 10.5f), Quaternion.Euler(-19f, 0f, 0f), HazardType.Slippery);
            foreach (var (x, z, s) in new[] { (-32.4f, -9.5f, 1.1f), (-27.6f, -8f, 0.9f), (-32.2f, -5f, 1.3f), (-27.8f, -4f, 1.0f) })
            {
                var drift = MeshObject("Drift", glacier, snowDrifts[(int)(Mathf.Abs(x * z)) % snowDrifts.Length], pal, false);
                drift.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, x * 31f, 0f));
                drift.transform.localScale = Vector3.one * s;
            }
            Lamp(glacier, art, new Vector3(-27.4f, 3.4f, 3.7f));
            c.Hard("Glacier", new Vector3(-30f, 3.45f, 1.6f));
            c.Hard("FarIslet", new Vector3(22f, 0.36f, -14.5f));

            // ------------------------------------------------- borrowed challenges, dressed for the snow
            var tower = BuildChallengeHolder(env, "TowerHolder", Vector3.zero, h => BuildTower(h, m, art, MapTheme.Snow));
            c.Hard("Tower", new Vector3(38f, 7.05f, -7f));
            var tunnel = BuildChallengeHolder(env, "TunnelHolder", Vector3.zero, h => BuildTunnel(h, m, art, MapTheme.Snow));
            c.Hard("Tunnel", new Vector3(-37f, 0f, 35.1f));
            var cliff = BuildChallengeHolder(env, "CliffHolder", Vector3.zero, h => BuildMushroomCliff(h, m, art, MapTheme.Snow));
            c.Hard("Cliff", new Vector3(35.5f, 6.85f, 50f));
            LinkStructure(tower.Find("ParkourTower"), c.points, "Spawn_Hard_Tower");
            LinkStructure(tunnel.Find("CrawlTunnel"), c.points, "Spawn_Hard_Tunnel");
            LinkStructure(cliff.Find("MushroomCliff"), c.points, "Spawn_Hard_Cliff");
            AddHint(c.points, "Spawn_Hard_Tunnel", "hintbox.tunnel", 9f);
            AddHint(c.points, "Spawn_Hard_Tower", "hintbox.tower", 10f);
            AddHint(c.points, "Spawn_Hard_Cliff", "hintbox.cliff", 10f);
            AddHint(c.points, "Spawn_Hard_Glacier", "hintbox.ice", 10f);
            AddHint(c.points, "Spawn_Hard_FarIslet", "hintbox.ice", 8f);

            // ------------------------------------------------- the places
            var reserved = new List<(Vector2 centre, float radius)>
            {
                (new Vector2(-28.5f, 15.5f), 8.5f), (new Vector2(-30f, -4f), 9f), (new Vector2(30f, 15f), 7.5f), (new Vector2(0f, 42.5f), 6.5f),
                (new Vector2(38f, -7f), 7.5f), (new Vector2(-37f, 30f), 7.5f), (new Vector2(33f, 49f), 8.5f), (new Vector2(-26f, -28f), 9f),
                (new Vector2(-26f, -34f), 4f), (new Vector2(22f, -14.5f), 3f)
            };
            foreach (var spot in c.points.GetComponentsInChildren<BoxSpawnPoint>())
                reserved.Add((new Vector2(spot.transform.position.x, spot.transform.position.z), 3.4f));
            AddSummitPlaces(c, field, reserved);

            bool Blocked(float x, float z)
            {
                if (InPolygon(SummitLake, x, z)) return true;
                foreach (var q in SummitLake) if (Vector2.Distance(new Vector2(x, z), q) < 2.2f) return true;
                if (SummitTrailDistance(x, z) < 2.4f) return true;
                foreach (var (centre, radius) in reserved)
                    if (Vector2.Distance(new Vector2(x, z), centre) < radius) return true;
                return false;
            }

            // ------------------------------------------------- pine groves, drifts, crystals and rocks
            var scenery = new GameObject("Scenery").transform;
            scenery.SetParent(env, false);
            Scatter(110, 11, -43f, -34f, 43f, 54f, (x, z) => Blocked(x, z) || Mathf.PerlinNoise(x * 0.07f + 4f, z * 0.07f + 1f) < 0.42f,
                (x, z, rng) => Pine(scenery, art, rng.Next(2), new Vector3(x, 0f, z), 0.85f + (float)rng.NextDouble() * 0.75f, true));
            Scatter(16, 15, -43f, -34f, 43f, 54f, Blocked,
                (x, z, rng) => Pine(scenery, art, rng.Next(2), new Vector3(x, 0f, z), 0.7f + (float)rng.NextDouble() * 0.5f, true));
            Scatter(30, 12, -43f, -34f, 43f, 54f, Blocked, (x, z, rng) =>
            {
                var drift = MeshObject("Drift", scenery, snowDrifts[rng.Next(snowDrifts.Length)], pal, false);
                drift.transform.position = new Vector3(x, 0f, z);
                drift.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                drift.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.9f, (float)rng.NextDouble());
            });
            Scatter(16, 13, -43f, -34f, 43f, 54f, Blocked, (x, z, rng) =>
            {
                var crystal = MeshObject("Crystals", scenery, iceCrystals[rng.Next(iceCrystals.Length)], pal);
                crystal.transform.position = new Vector3(x, 0f, z);
                crystal.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                crystal.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.5f, (float)rng.NextDouble());
            });
            Scatter(14, 14, -43f, -34f, 43f, 54f, Blocked, (x, z, rng) =>
                PlaceProp(scenery, art.rocks[rng.Next(art.rocks.Length)], new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f, 0.8f + (float)rng.NextDouble()));
            var bushes = new[] { Kit("Kit_SnowBush1", () => LowPolyKit.SnowBush("b", 3)), Kit("Kit_SnowBush2", () => LowPolyKit.SnowBush("b", 8)) };
            Scatter(40, 16, -43f, -34f, 43f, 54f, Blocked, (x, z, rng) =>
            {
                var bush = MeshObject("Bush", scenery, bushes[rng.Next(bushes.Length)], pal);
                bush.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
                bush.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.5f, (float)rng.NextDouble());
            });

            foreach (var (x, z, yaw) in new[] { (-8f, 18f, 200f), (9f, 29f, 120f), (-40f, 12f, 90f), (-10f, -29f, 20f) })
                BuildSnowman(scenery, pal, new Vector3(x, 0f, z), yaw);

            // ------------------------------------------------- outside: the road in, snowy forest, chalets
            MapRoad(env, SummitRoad, field, art, "Summit", 2, true);
            EntranceArch(env, new Vector3(-26f, 0f, -34.6f), 0f, 3.6f, "map.summit.name", true, art);
            var smoothRoad = Smooth(SummitRoad, 2f);
            HillDecor(env, field, art, 77, 420, 110, (x, z) => DistanceToLine(smoothRoad, x, z) < 5.5f || (Mathf.Abs(x - 12f) < 4f && z > 50f),
                (parent, at, rng) => Pine(parent, art, rng.Next(2), at + Vector3.down * 0.15f, Mathf.Lerp(1.0f, 1.9f, (float)rng.NextDouble()), false));
            var outside = env.Find("Outside");
            foreach (var (x, z, yaw) in new[] { (-40f, -47f, 20f), (-12f, -50f, -30f), (-44f, -66f, 70f), (58f, 18f, -90f), (-60f, 30f, 100f) })
                Cottage(outside, field, new Vector3(x, 0f, z), yaw, 4.2f, 3.6f, LowPolyKit.Wood, LowPolyKit.Brick, true, true, art);

            AddSnowfall(env, new Vector3(0f, 14f, 10f), new Vector3(90f, 2f, 92f));
            AddTorchPost(env, c.truckPosition + new Vector3(-6f, 0f, -2f), 1.6f);
            AddTorchPost(env, c.truckPosition + new Vector3(7f, 0f, -2f), 1.6f);
            Lamp(env, art, c.truckPosition + new Vector3(-3.5f, 0f, 5.5f));
        }

        /// <summary>The huts, signs, props and secrets that give the summit's places a reason to be there.</summary>
        static void AddSummitPlaces(MapContext c, LowPoly.HillField field, List<(Vector2 centre, float radius)> reserved)
        {
            var env = c.env;
            var m = c.m;
            var art = c.art;
            var pal = art.palette;
            var places = new GameObject("Details").transform;
            places.SetParent(env, false);

            // ---- the way in: where you are and where things are
            Notice(places, new Vector3(-19.4f, 0f, -18.6f), new Vector3(-25f, 0f, -28f), "notice.summit.start", m);
            DirectionSign(places, new Vector3(-18.6f, 0f, -9.6f), m,
                ("sign.lake", new Vector3(6f, 0f, -6f)), ("sign.glacier", new Vector3(-30f, 0f, -4f)),
                ("sign.camp", new Vector3(-30f, 0f, 14f)), ("sign.cabin", new Vector3(-20f, 0f, 46f)));
            DirectionSign(places, new Vector3(4f, 0f, 16f), m,
                ("sign.tower", new Vector3(36f, 0f, -4f)), ("sign.mine", new Vector3(-37f, 0f, 29f)), ("sign.camp", new Vector3(-30f, 0f, 14f)));
            reserved.Add((new Vector2(-18.6f, -9.6f), 2.5f));
            reserved.Add((new Vector2(4f, 16f), 2.5f));

            // ---- base camp: a fire ring, log benches, two tents, firewood, a drying line with socks
            var camp = new Vector3(-30f, 0f, 14f);
            var campRoot = new GameObject("BaseCamp").transform;
            campRoot.SetParent(places, false);
            Piece(campRoot, "Campfire", Kit("Kit_Campfire", () => LowPolyProps.Campfire("c", 1.15f)), pal, camp, 0f);
            AddFireFx(campRoot, camp + new Vector3(0f, 0.3f, 0f), 1.5f);
            HazardSphere(campRoot, "FireZone", camp + new Vector3(0f, 0.35f, 0f), 1.25f, HazardType.Fire);
            var log = Kit("Kit_Log", () => LowPolyProps.Log());
            foreach (float angle in new[] { 100f, 210f, 320f })
            {
                float rad = angle * Mathf.Deg2Rad;
                Piece(campRoot, "Bench", log, pal, camp + new Vector3(Mathf.Cos(rad) * 3.0f, 0f, Mathf.Sin(rad) * 3.0f), 90f - angle, 0.68f, new Vector3(3.1f, 0.8f, 0.8f));
            }
            var tentCentre = new Vector3(-33.5f, 0f, 18f);
            Piece(campRoot, "Tent", Kit("Kit_Tent", () => LowPolyProps.Tent()), pal, tentCentre, 200f, 1.25f, new Vector3(2.5f, 1.8f, 2.5f));
            Piece(campRoot, "TentTeal", Kit("Kit_TentTeal", () => LowPolyKit.Recolor(LowPolyProps.Tent(), "t", (LowPolyKit.Clay, LowPolyKit.Teal), (LowPolyKit.Brown, LowPolyKit.DeepTeal))), pal,
                new Vector3(-35.5f, 0f, 11f), 120f, 1.05f, new Vector3(2.2f, 1.6f, 2.2f));
            AddSecret(places, "tent", tentCentre + new Vector3(-0.8f, 1f, -2.2f), "secret.tent", "prompt.secret", 1.4f);
            Piece(campRoot, "Firewood", Kit("Kit_WoodShelterSnow", () => LowPolyKit.WoodShelter("w", true)), pal, new Vector3(-34.5f, 0f, 20.5f), 160f, 1f, new Vector3(2.6f, 2f, 1.2f));
            var rackBase = new Vector3(-25.5f, 0f, 20.8f);
            Prim(PrimitiveType.Cube, "RackPostA", campRoot, rackBase + new Vector3(-1.4f, 0.95f, 0f), new Vector3(0.12f, 1.9f, 0.12f), m.woodDark, true, null, true);
            Prim(PrimitiveType.Cube, "RackPostB", campRoot, rackBase + new Vector3(1.4f, 0.95f, 0f), new Vector3(0.12f, 1.9f, 0.12f), m.woodDark, true, null, true);
            Prim(PrimitiveType.Cube, "RackLine", campRoot, rackBase + new Vector3(0f, 1.82f, 0f), new Vector3(2.9f, 0.04f, 0.04f), coalBlack, false);
            var clothMats = new[] { clothRed, creamPaint, m.ice, clothRed, fishBlue };
            for (int i = 0; i < 5; i++)
                Prim(PrimitiveType.Cube, "Sock" + i, campRoot, rackBase + new Vector3(-1.1f + i * 0.55f, 1.5f, 0.02f), new Vector3(0.3f, 0.6f, 0.03f), clothMats[i], false, Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 6f : -5f)));
            Piece(campRoot, "Skis", Kit("Kit_Skis_Rust", () => LowPolyKit.Skis("s", LowPolyKit.Brick)), pal, new Vector3(-23.6f, 0f, 18.2f), 60f);
            Prim(PrimitiveType.Cylinder, "CookingPot", campRoot, camp + new Vector3(1.6f, 0.2f, -1.4f), new Vector3(0.5f, 0.2f, 0.5f), coalBlack, true);
            Lamp(campRoot, art, new Vector3(-24.6f, 0f, 12.6f));
            Notice(places, new Vector3(-22.6f, 0f, 12.2f), new Vector3(-17f, 0f, 2f), "notice.summit.camp", m);

            // ---- the mountain hut: log walls, a smoking chimney, lit windows; a sled, firewood, skis
            var hutAt = new Vector3(-20f, 0f, 46.5f);
            var hutRoot = new GameObject("MountainHut").transform;
            hutRoot.SetParent(places, false);
            var hut = Piece(hutRoot, "Cabin", Kit("Kit_SummitHut", () => LowPolyKit.Cabin("h", 5.6f, 4.2f, 2.6f, LowPolyKit.Wood, LowPolyKit.Brick, LowPolyKit.Snow, true, true, true)),
                pal, hutAt, 180f, 1f, new Vector3(6.0f, 3.2f, 4.6f));
            GlowWindows(hut.transform, 5.6f, 4.2f, 2.6f, true);
            ChimneySmoke(hutRoot, hut.transform.TransformPoint(new Vector3(5.6f * 0.27f, 2.6f + 4.2f * 0.42f * 1.05f + 0.8f, -4.2f * 0.2f)));
            AddWallLantern(hut.transform, new Vector3(-0.88f, 2.2f, 2.28f), Vector3.forward, 9f, 2.0f);
            reserved.Add((new Vector2(hutAt.x, hutAt.z), 6f));
            AddSecret(places, "cabin", hutAt + new Vector3(0f, 1f, -3.0f), "secret.cabin", "prompt.knock", 1.5f);
            Notice(places, new Vector3(-15.6f, 0f, 41.6f), new Vector3(-20f, 0f, 32f), "notice.summit.cabin", m);
            Piece(hutRoot, "Firewood", Kit("Kit_WoodShelterSnow", () => LowPolyKit.WoodShelter("w", true)), pal, hutAt + new Vector3(4.3f, 0f, 0.4f), 90f, 1f, new Vector3(2.6f, 2f, 1.2f));
            Piece(hutRoot, "Bench", Kit("Kit_Bench", () => LowPolyKit.Bench("b")), pal, hutAt + new Vector3(1.9f, 0f, -2.9f), 180f, 1f, new Vector3(1.7f, 0.5f, 0.45f));
            Piece(hutRoot, "SkisA", Kit("Kit_Skis_Teal", () => LowPolyKit.Skis("s", LowPolyKit.Teal)), pal, hutAt + new Vector3(-3.4f, 0f, -2.6f), 160f);
            Piece(hutRoot, "SkisB", Kit("Kit_Skis_Gold", () => LowPolyKit.Skis("s", LowPolyKit.Gold)), pal, hutAt + new Vector3(-4.1f, 0f, -2.2f), 200f);
            Lamp(hutRoot, art, hutAt + new Vector3(-4.6f, 0f, -4.4f));
            FenceRun(hutRoot, m, hutAt + new Vector3(-6.4f, 0f, -1.2f), hutAt + new Vector3(-6.4f, 0f, 4.2f));
            FenceRun(hutRoot, m, hutAt + new Vector3(-6.4f, 0f, 4.2f), hutAt + new Vector3(6.6f, 0f, 4.2f));
            FenceRun(hutRoot, m, hutAt + new Vector3(6.6f, 0f, 4.2f), hutAt + new Vector3(6.6f, 0f, -1.6f));

            var sled = new GameObject("Sled").transform;
            sled.SetParent(hutRoot, false);
            sled.SetPositionAndRotation(hutAt + new Vector3(-6.2f, 0f, -3.4f), Quaternion.Euler(0f, 25f, 0f));
            foreach (float side in new[] { 1f, -1f })
                Prim(PrimitiveType.Cube, "Skid", sled, new Vector3(side * 0.35f, 0.09f, 0f), new Vector3(0.1f, 0.1f, 2.2f), m.woodDark, false, null, true);
            Prim(PrimitiveType.Cube, "Seat", sled, new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 0.1f, 1.6f), clothRed, true, null, true);
            Prim(PrimitiveType.Cube, "Rope", sled, new Vector3(0f, 0.16f, 1.4f), new Vector3(0.05f, 0.05f, 0.8f), creamPaint, false);
            AddSecret(places, "sled", sled.position + new Vector3(0f, 0.6f, 0f), "secret.sled", "prompt.secret", 1.3f, sled);

            // ---- the power pylon by the electric box, a transformer and the cables over the hills
            var pylonRoot = new GameObject("Pylon").transform;
            pylonRoot.SetParent(places, false);
            var pylonAt = new Vector3(33.5f, 0f, 17.5f);
            Piece(pylonRoot, "PowerPylon", Kit("Kit_PowerPylon", () => LowPolyKit.PowerPylon("p")), pal, pylonAt, 70f, 1f, new Vector3(2.5f, 8.2f, 2.5f));
            Piece(pylonRoot, "Transformer", Kit("Kit_Transformer", () => LowPolyKit.Transformer("t")), pal, new Vector3(26.2f, 0f, 16.8f), 200f, 1f, new Vector3(1.9f, 1.9f, 1.4f));
            var armTip = Quaternion.Euler(0f, 70f, 0f) * new Vector3(2.45f, 0f, 0f);
            var farPylon = new Vector3(64f, 0f, 29f);
            farPylon.y = field.Surface(farPylon.x, farPylon.z);
            Piece(pylonRoot, "FarPylon", Kit("Kit_PowerPylon", () => LowPolyKit.PowerPylon("p")), pal, farPylon, 70f);
            foreach (float side in new[] { 1f, -1f })
            {
                var from = pylonAt + armTip * side + Vector3.up * 7.75f;
                var to = farPylon + armTip * side + Vector3.up * 7.75f;
                Sag(pylonRoot, from, to, 1.6f);
                Sag(pylonRoot, from, new Vector3(26.2f, 1.75f, 16.8f), 0.6f);
            }
            Lamp(pylonRoot, art, new Vector3(28.6f, 0f, 10.6f));
            Notice(places, new Vector3(25.4f, 0f, 9.4f), new Vector3(16f, 0f, 9f), "notice.summit.pylon", m);

            // ---- the chemical dump: leaking drums, a green puddle, a warning post
            var dump = new GameObject("ToxicDump").transform;
            dump.SetParent(places, false);
            var drum = Kit("Kit_DrumToxic", () => LowPolyKit.Drum("d", LowPolyKit.Moss, LowPolyKit.Gold));
            var drumRust = Kit("Kit_DrumRust", () => LowPolyKit.Drum("d", LowPolyKit.Rust, LowPolyKit.Gold));
            foreach (var (x, z, yaw, rusty) in new[] { (3.4f, 44.6f, 10f, false), (4.3f, 43.7f, 60f, true), (4.1f, 45.5f, 20f, false), (-3.6f, 45.4f, 80f, true) })
                Piece(dump, "Drum", rusty ? drumRust : drum, pal, new Vector3(x, 0f, z), yaw, 1f, new Vector3(0.7f, 0.95f, 0.7f));
            var lying = Piece(dump, "DrumLying", drum, pal, new Vector3(-2.2f, 0.33f, 44.4f), 0f);
            lying.transform.rotation = Quaternion.Euler(0f, 30f, 90f);
            var puddle = Prim(PrimitiveType.Cylinder, "Puddle", dump, new Vector3(-1.4f, 0.03f, 44.6f), new Vector3(2.4f, 0.01f, 1.6f), toxicGlow, false);
            puddle.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var (x, z, s) in new[] { (-4.2f, 47.6f, 1.5f), (2.8f, 47.9f, 1.3f), (6.2f, 41.2f, 1.1f) })
                PlaceProp(dump, art.rocks[(int)Mathf.Abs(x * 3f) % art.rocks.Length], new Vector3(x, 0f, z), x * 40f, s);
            Lamp(dump, art, new Vector3(0.2f, 0f, 47.4f));
            Notice(places, new Vector3(4.4f, 0f, 35f), new Vector3(0f, 0f, 28f), "notice.summit.outcrop", m);

            // ---- the other places say what they are
            Notice(places, new Vector3(-5.6f, 0f, -21f), new Vector3(-8f, 0f, -27f), "notice.summit.lake", m);
            Notice(places, new Vector3(-34.2f, 0f, -12.6f), new Vector3(-24f, 0f, -14f), "notice.summit.glacier", m);
            Notice(places, new Vector3(31.4f, 0f, -3.6f), new Vector3(32f, 0f, 6f), "notice.summit.tower", m);
            Notice(places, new Vector3(-32.6f, 0f, 26.8f), new Vector3(-28f, 0f, 35f), "notice.summit.mine", m);
            Torch(places, new Vector3(-35.2f, 0f, 27.6f));
            Torch(places, new Vector3(-38.8f, 0f, 27.6f));

            // ---- ice fishing on the lake, frozen ducks, thin-ice poles, gusts that sweep the east shore
            var fishing = new Vector3(13f, 0f, -13.5f);
            Prim(PrimitiveType.Cylinder, "IceHole", places, fishing + new Vector3(0f, 0.036f, 0f), new Vector3(1.5f, 0.012f, 1.5f), darkWater, false);
            Prim(PrimitiveType.Cylinder, "Bucket", places, fishing + new Vector3(1.4f, 0.25f, 0.3f), new Vector3(0.5f, 0.25f, 0.5f), m.woodDark, true);
            Prim(PrimitiveType.Cylinder, "Stool", places, fishing + new Vector3(-1.5f, 0.2f, -0.3f), new Vector3(0.55f, 0.2f, 0.55f), m.wood, true);
            Beam(places, "Rod", fishing + new Vector3(-1.5f, 0.75f, -0.3f), fishing + new Vector3(-0.1f, 0.1f, 0.4f), 0.05f, m.woodDark, false);
            AddSecret(places, "fish", fishing + new Vector3(0f, 0.5f, 0f), "secret.fish", "prompt.fish", 1.6f);
            Notice(places, fishing + new Vector3(2.6f, 0f, -2.2f), fishing + new Vector3(0f, 0f, -9f), "notice.summit.lakeshore", m);

            foreach (var (x, z, yaw) in new[] { (-7f, -14f, 40f), (21f, -16.5f, 210f), (18f, 1f, 120f) })
                AddDuck(places, new Vector3(x, 0.02f, z), yaw, pal);
            for (int i = 0; i < SummitLake.Length; i += 3)
            {
                var q = SummitLake[i];
                var outward = (q - SummitLakeCentre).normalized;
                var at = q + outward * 1.2f;
                if (SummitTrailDistance(at.x, at.y) < 2f) continue;
                Prim(PrimitiveType.Cube, "IcePole", places, new Vector3(at.x, 0.6f, at.y), new Vector3(0.09f, 1.2f, 0.09f), i % 2 == 0 ? clothRed : creamPaint, false, null, true);
            }
            AddGust(places, new Vector3(27f, 1.5f, -16f), new Vector3(-1f, 0f, 0.3f), new Vector3(5f, 2.5f, 4f));

            // ---- an igloo with a very lost penguin
            var iglooAt = new Vector3(-40.5f, 0f, -7f);
            var igloo = Piece(places, "Igloo", Kit("Kit_Igloo", () => LowPolyKit.Igloo("i")), pal, iglooAt, 90f);
            var dome = igloo.AddComponent<SphereCollider>();
            dome.center = Vector3.zero;
            dome.radius = 1.85f;
            var penguin = Piece(places, "Penguin", Kit("Kit_Penguin", () => LowPolyKit.Penguin("p")), pal, iglooAt + new Vector3(3.4f, 0f, 0.6f), 110f);
            penguin.isStatic = false;
            AddSecret(places, "igloo", iglooAt + new Vector3(3.3f, 0.8f, 0f), "secret.igloo", "prompt.wave", 1.6f, penguin.transform);
            reserved.Add((new Vector2(iglooAt.x, iglooAt.z), 4.5f));

            // ---- the chair lift up the mountain: a base station inside the map, pylons and chairs on the slope outside
            BuildChairLift(places, field, art, new Vector3(12f, 0f, 52.6f));
            reserved.Add((new Vector2(12f, 52.6f), 5f));

            // ---- the yeti: a trail of footprints leads to a very shy pile of fur peeking from behind a rock
            var yetiAt = new Vector3(-40.5f, 0f, 52.5f);
            for (int i = 0; i < 11; i++)
            {
                float t = i / 10f;
                var along = Vector3.Lerp(new Vector3(-26f, 0f, 41f), yetiAt + new Vector3(3.2f, 0f, -3f), t);
                float yaw = YawToward(new Vector3(-26f, 0f, 41f), yetiAt);
                var sideStep = Quaternion.Euler(0f, yaw, 0f) * Vector3.right * (i % 2 == 0 ? 0.45f : -0.45f);
                Prim(PrimitiveType.Sphere, "Print" + i, places, along + sideStep + new Vector3(0f, 0.06f, 0f), new Vector3(0.55f, 0.03f, 1.0f),
                    m.ice, false, Quaternion.Euler(0f, yaw, 0f));
            }
            var yeti = new GameObject("Yeti").transform;
            yeti.SetParent(places, false);
            yeti.SetPositionAndRotation(yetiAt, Quaternion.Euler(0f, YawToward(yetiAt, new Vector3(0f, 0f, 10f)), 0f));
            Prim(PrimitiveType.Sphere, "Body", yeti, new Vector3(0f, 1.15f, 0f), new Vector3(2.5f, 2.3f, 2.2f), furWhite, true);
            var head = Prim(PrimitiveType.Sphere, "Head", yeti, new Vector3(0f, 2.8f, 0.35f), new Vector3(1.5f, 1.4f, 1.4f), furWhite, false);
            foreach (float side in new[] { 1f, -1f })
            {
                Prim(PrimitiveType.Sphere, "Eye", yeti, new Vector3(side * 0.32f, 2.95f, 1.02f), Vector3.one * 0.3f, creamPaint, false);
                Prim(PrimitiveType.Sphere, "Pupil", yeti, new Vector3(side * 0.32f, 2.95f, 1.15f), Vector3.one * 0.13f, coalBlack, false);
                Prim(PrimitiveType.Sphere, "Arm", yeti, new Vector3(side * 1.25f, 1.25f, 0.5f), new Vector3(0.85f, 1.4f, 0.85f), furWhite, false);
            }
            Prim(PrimitiveType.Sphere, "Nose", yeti, new Vector3(0f, 2.72f, 1.1f), new Vector3(0.28f, 0.22f, 0.22f), m.ice, false);
            AddSecret(places, "yeti", yetiAt + new Vector3(0f, 1.2f, 2.6f), "secret.yeti", "prompt.wave", 1.8f, head.transform);
            PlaceProp(places, art.rocks[1 % art.rocks.Length], yetiAt + new Vector3(2.2f, 0f, 1.6f), 40f, 1.8f);
            reserved.Add((new Vector2(yetiAt.x, yetiAt.z), 5f));

            // ---- lamps along the paths
            PathLamps(places, art, SummitTrails, 15f, (x, z) =>
            {
                foreach (var (centre, radius) in reserved)
                    if (Vector2.Distance(new Vector2(x, z), centre) < radius * 0.7f) return true;
                return InPolygon(SummitLake, x, z);
            });
        }

        /// <summary>A real torch planted by a path (a stick with a flame).</summary>
        static void Torch(Transform parent, Vector3 foot) => AddTorchPost(parent, foot, 1.5f);

        /// <summary>A sagging cable between two points.</summary>
        static void Sag(Transform parent, Vector3 from, Vector3 to, float sag)
        {
            const int segments = 6;
            Vector3 Point(float t) => Vector3.Lerp(from, to, t) + Vector3.down * sag * 4f * t * (1f - t);
            for (int i = 0; i < segments; i++)
                Beam(parent, "Cable", Point(i / (float)segments), Point((i + 1f) / segments), 0.05f, coalBlack, false);
        }

        /// <summary>Lamp posts along paths every `spacing` metres, on alternate sides, except where `skip` says no.</summary>
        static void PathLamps(Transform parent, ArtAssets art, Vector2[][] trails, float spacing, System.Func<float, float, bool> skip)
        {
            var lamps = new GameObject("PathLamps").transform;
            lamps.SetParent(parent, false);
            int count = 0;
            var placed = new List<Vector2>();
            foreach (var trail in trails)
            {
                float carried = spacing * 0.5f;
                for (int i = 0; i + 1 < trail.Length; i++)
                {
                    Vector2 a = trail[i], b = trail[i + 1], dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                    float length = Vector2.Distance(a, b);
                    float d = carried;
                    for (; d < length; d += spacing)
                    {
                        Vector2 p = a + dir * d + side * (count % 2 == 0 ? 2.2f : -2.2f);
                        if (p.x < -43f || p.x > 43f || p.y < -34f || p.y > 54f || skip(p.x, p.y)) continue;
                        bool near = false;
                        foreach (var q in placed) if (Vector2.Distance(p, q) < 7f) near = true;
                        if (near) continue;
                        placed.Add(p);
                        count++;
                        var lamp = MeshObject("Lamp", lamps, lampMesh, art.palette);
                        lamp.transform.position = new Vector3(p.x, 0f, p.y);
                        var capsule = lamp.AddComponent<CapsuleCollider>();
                        capsule.center = new Vector3(0f, 1.5f, 0f);
                        capsule.radius = 0.12f;
                        capsule.height = 3f;
                        AddLampLight(lamp.transform, new Vector3(0f, 2.78f, 0f));
                    }
                    carried = d - length;
                }
            }
        }

        /// <summary>
        /// A chair lift: a base station by the north edge of the map with a bull wheel and a roof, and outside the walls a line of
        /// pylons climbing the mountain with two cables and chairs hanging from them.
        /// </summary>
        static void BuildChairLift(Transform parent, LowPoly.HillField field, ArtAssets art, Vector3 station)
        {
            var root = new GameObject("ChairLift").transform;
            root.SetParent(parent, false);
            var stationMesh = Kit("Kit_LiftStation", () =>
            {
                var b = new LowPoly.MeshBuilder();
                LowPolyKit.Box(b, new Vector3(0f, 0.2f, 0f), new Vector3(5.2f, 0.4f, 4.2f), LowPolyKit.Stone, LowPolyKit.Grey);
                foreach (var (x, z) in new[] { (-2.2f, -1.7f), (2.2f, -1.7f), (-2.2f, 1.7f), (2.2f, 1.7f) })
                    LowPolyKit.Box(b, new Vector3(x, 2.2f, z), new Vector3(0.3f, 3.6f, 0.3f), LowPolyKit.DarkWood);
                LowPolyKit.Box(b, new Vector3(0f, 4.15f, 0f), new Vector3(5.8f, 0.3f, 4.8f), LowPolyKit.DarkWood, LowPolyKit.Snow);
                LowPolyKit.Box(b, new Vector3(0f, 4.42f, 0f), new Vector3(5.6f, 0.24f, 4.6f), LowPolyKit.Snow, LowPolyKit.Snow);
                LowPolyKit.Cylinder(b, new Vector3(0f, 3.5f, 0f), 1.4f, 1.4f, 0.3f, 14, LowPolyKit.Brick, LowPolyKit.Grey);
                LowPolyKit.Cylinder(b, new Vector3(0f, 2.8f, 0f), 0.2f, 0.2f, 0.75f, 8, LowPolyKit.Grey);
                LowPolyKit.Box(b, new Vector3(-1.4f, 1.0f, -1.2f), new Vector3(1.6f, 1.6f, 1.2f), LowPolyKit.Gold, LowPolyKit.Snow);
                LowPolyKit.Box(b, new Vector3(-1.4f, 1.1f, -0.58f), new Vector3(1.0f, 0.6f, 0.05f), LowPolyKit.Navy);
                return b.ToMesh("s");
            });
            Piece(root, "LiftStation", stationMesh, art.palette, station, 0f, 1f, new Vector3(5.2f, 0.4f, 4.2f));
            foreach (var (x, z) in new[] { (-2.2f, -1.7f), (2.2f, -1.7f), (-2.2f, 1.7f), (2.2f, 1.7f) })
            {
                var post = new GameObject("PostCollider");
                post.transform.SetParent(root, false);
                post.transform.position = station + new Vector3(x, 2f, z);
                post.AddComponent<BoxCollider>().size = new Vector3(0.35f, 4f, 0.35f);
            }

            var chair = Kit("Kit_LiftChair", () =>
            {
                var b = new LowPoly.MeshBuilder();
                LowPolyKit.Box(b, new Vector3(0f, -1.6f, 0f), new Vector3(0.06f, 1.6f, 0.06f), LowPolyKit.Black);
                LowPolyKit.Box(b, new Vector3(0f, -2.42f, 0f), new Vector3(1.3f, 0.1f, 0.5f), LowPolyKit.Brick);
                LowPolyKit.Box(b, new Vector3(0f, -2.15f, -0.24f), new Vector3(1.3f, 0.5f, 0.08f), LowPolyKit.Brick);
                LowPolyKit.Box(b, new Vector3(0f, -2.0f, 0.3f), new Vector3(1.3f, 0.05f, 0.05f), LowPolyKit.Black);
                return b.ToMesh("c");
            });
            var pylon = Kit("Kit_LiftPylon", () =>
            {
                var b = new LowPoly.MeshBuilder();
                LowPolyKit.Cylinder(b, Vector3.zero, 0.45f, 0.3f, 8.5f, 8, LowPolyKit.Grey, LowPolyKit.Grey);
                LowPolyKit.Box(b, new Vector3(0f, 8.4f, 0f), new Vector3(3.4f, 0.3f, 0.5f), LowPolyKit.Grey);
                foreach (float x in new[] { -1.4f, 1.4f }) LowPolyKit.Cylinder(b, new Vector3(x, 8.0f, 0f), 0.22f, 0.22f, 0.3f, 8, LowPolyKit.Black, LowPolyKit.Black, Quaternion.Euler(90f, 0f, 0f));
                return b.ToMesh("p");
            });

            var stops = new List<Vector3> { station + new Vector3(0f, 3.65f, 0f) };
            for (float z = station.z + 16f; z < station.z + 120f; z += 17f)
            {
                float y = field.Surface(station.x, z);
                Piece(root, "LiftPylon", pylon, art.palette, new Vector3(station.x, y - 0.2f, z), 0f);
                stops.Add(new Vector3(station.x, y + 8.2f, z));
            }
            foreach (float side in new[] { -1.4f, 1.4f })
            {
                for (int i = 0; i + 1 < stops.Count; i++)
                {
                    var a = stops[i] + new Vector3(side, 0f, 0f);
                    var b = stops[i + 1] + new Vector3(side, 0f, 0f);
                    Beam(root, "LiftCable", a, b, 0.06f, coalBlack, false);
                    for (float t = 0.25f; t < 1f; t += 0.5f)
                    {
                        var at = Vector3.Lerp(a, b, t);
                        var seat = MeshObject("Chair", root, chair, art.palette);
                        seat.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, side < 0f ? 0f : 180f, 0f));
                    }
                }
            }
        }
    }
}
