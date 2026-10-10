using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// "Frozen Summit": a snowy mountain pass with a big frozen lake in the middle, pine woods, a bonfire camp, a power
    /// pylon, an ice ramp to a high ledge and the parkour tower, the crawl tunnel and the mushroom cliff of the meadow.
    /// A harder map: the ground is slippery and the hard boxes are really hard.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        // Almost all white snow, with a few pale-teal patches of shade (no tan squares: they read as a chessboard).
        static readonly int[] SnowTiles = { 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 29 };
        // White slopes with bare grey rock only where they are steep (one rock colour, so the cliffs are not a patchwork).
        static readonly LowPoly.HillColors SnowHills = new LowPoly.HillColors
        {
            low = new[] { 25 }, mid = new[] { 25 }, high = new[] { 25, 25, 25, 26 }, rock = new[] { 26 }
        };

        static void BuildSummitScene(GameAssets a, Mats m, ArtAssets art, Prefabs p) =>
            BuildMapScene(SummitScenePath, a, m, art, p, ctx => FillSummit(ctx));

        static void FillSummit(MapContext c)
        {
            var env = c.env;
            var m = c.m;
            var art = c.art;

            SetMood(art, "SkySummit",
                new Color(0.42f, 0.58f, 0.82f), new Color(0.88f, 0.92f, 0.97f), new Color(0.72f, 0.78f, 0.86f),
                new Color(1f, 0.97f, 0.92f), 1.25f, new Vector3(42f, -30f, 0f),
                new Color(0.7f, 0.8f, 0.95f), new Color(0.74f, 0.78f, 0.84f), new Color(0.46f, 0.5f, 0.58f));

            // ---------------------------------------------------------- ground
            FlatGround(env, "SnowField", -45f, -36f, 45f, 56f, 0f, 3f, SnowTiles, 4, art);
            MeshObject("Hills", env, MapHills("Map_SummitHills", 5, SnowHills), art.palette);
            c.truckPosition = new Vector3(-26f, 0f, -26f);
            c.noteSpots = new[]
            {
                c.truckPosition + new Vector3(-5.5f, 0f, -4.5f), c.truckPosition + new Vector3(6f, 0f, -9f), c.truckPosition + new Vector3(-7f, 0f, 4f)
            };

            // The frozen lake: very slippery, with a rocky islet in the middle.
            const float lakeX = 6f, lakeZ = -6f;
            Prim(PrimitiveType.Cube, "Lake", env, new Vector3(lakeX, 0.012f, lakeZ), new Vector3(36f, 0.024f, 24f), m.ice, false);
            HazardVolume("IceLakeVolume", env, new Vector3(lakeX, 0.6f, lakeZ), new Vector3(36f, 1.2f, 24f), Quaternion.identity, HazardType.Slippery);
            Prim(PrimitiveType.Cube, "Islet", env, new Vector3(lakeX, 0.17f, lakeZ), new Vector3(4.4f, 0.34f, 4.4f), m.stone, true, null, true);
            Prim(PrimitiveType.Cube, "IsletSnow", env, new Vector3(lakeX, 0.36f, lakeZ), new Vector3(4.1f, 0.06f, 4.1f), m.marker, false);
            for (int i = 0; i < 3; i++) Prim(PrimitiveType.Sphere, "IsletRock" + i, env, new Vector3(lakeX - 1.4f + i * 1.3f, 0.5f, lakeZ + 1.7f), Vector3.one * (0.6f + i * 0.15f), m.stone, true);

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

            // ------------------------------------------------- bonfire camp (hot box)
            var camp = new GameObject("BonfireCamp").transform;
            camp.SetParent(env, false);
            AddFireFx(camp, new Vector3(-30f, 0.35f, 14f), 1.5f);
            HazardSphere(camp, "FireZone", new Vector3(-30f, 0.35f, 14f), 1.25f, HazardType.Fire);
            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                Prim(PrimitiveType.Sphere, "FireStone" + i, camp, new Vector3(-30f + Mathf.Cos(angle) * 1.5f, 0.2f, 14f + Mathf.Sin(angle) * 1.5f), new Vector3(0.5f, 0.4f, 0.5f), m.stone, false);
            }
            Prim(PrimitiveType.Cube, "TentBase", camp, new Vector3(-33.5f, 0.9f, 18f), new Vector3(3f, 1.8f, 3f), m.woodDark, true, Quaternion.Euler(0f, 20f, 0f), true);
            Lamp(camp, art, new Vector3(-25f, 0f, 19f));

            // ------------------------------------------------- power pylon (electric box)
            var pylon = new GameObject("Pylon").transform;
            pylon.SetParent(env, false);
            Prim(PrimitiveType.Cube, "PylonLegA", pylon, new Vector3(31.2f, 3f, 15.4f), new Vector3(0.35f, 6f, 0.35f), m.woodDark, true, null, true);
            Prim(PrimitiveType.Cube, "PylonLegB", pylon, new Vector3(32.8f, 3f, 15.4f), new Vector3(0.35f, 6f, 0.35f), m.woodDark, true, null, true);
            Prim(PrimitiveType.Cube, "PylonBeam", pylon, new Vector3(32f, 5.6f, 15.4f), new Vector3(5f, 0.3f, 0.3f), m.woodDark, false);
            Lamp(pylon, art, new Vector3(28.5f, 0f, 11f));
            Lamp(pylon, art, new Vector3(34f, 0f, 11f));

            // ------------------------------------------------- toxic outcrop
            var outcrop = new GameObject("ToxicOutcrop").transform;
            outcrop.SetParent(env, false);
            foreach (var (x, z, s) in new[] { (-3.2f, 43.8f, 1.6f), (3.4f, 43f, 1.4f), (-2.4f, 38.6f, 1.1f), (3f, 38.9f, 1.2f) })
                PlaceProp(outcrop, art.rocks[(int)(Mathf.Abs(x * 3f)) % art.rocks.Length], new Vector3(x, 0f, z), x * 40f, s);
            Lamp(outcrop, art, new Vector3(0f, 0f, 45f));

            // ------------------------------------------------- the ice ramp (hard)
            var glacier = new GameObject("Glacier").transform;
            glacier.SetParent(env, false);
            Prim(PrimitiveType.Cube, "GlacierTop", glacier, new Vector3(-30f, 1.7f, 1.4f), new Vector3(6f, 3.4f, 6f), m.stone, true, null, true);
            Prim(PrimitiveType.Cube, "GlacierSnow", glacier, new Vector3(-30f, 3.43f, 1.4f), new Vector3(6.2f, 0.07f, 6.2f), m.marker, false);
            Ramp(glacier, "GlacierRamp", new Vector3(-30f, 0f, -11.5f), new Vector3(-30f, 3.4f, -1.6f), 3.4f, 0.4f, m.ice);
            HazardVolume("GlacierRampIce", glacier, new Vector3(-30f, 1.7f, -6.6f), new Vector3(3.4f, 2.6f, 10.5f), Quaternion.Euler(-19f, 0f, 0f), HazardType.Slippery);
            Lamp(glacier, art, new Vector3(-27.4f, 3.4f, 3.7f));
            c.Hard("Glacier", new Vector3(-30f, 3.45f, 1.6f));

            // a second islet far across the ice
            Prim(PrimitiveType.Cube, "FarIslet", env, new Vector3(22f, 0.17f, -14.5f), new Vector3(2.6f, 0.34f, 2.6f), m.stone, true, null, true);
            c.Hard("FarIslet", new Vector3(22f, 0.36f, -14.5f));

            // ------------------------------------------------- borrowed challenges
            var tower = BuildChallengeHolder(env, "TowerHolder", Vector3.zero, h => BuildTower(h, m, art));
            c.Hard("Tower", new Vector3(38f, 7.05f, -7f));
            var tunnel = BuildChallengeHolder(env, "TunnelHolder", Vector3.zero, h => BuildTunnel(h, m, art));
            c.Hard("Tunnel", new Vector3(-37f, 0f, 35.1f));
            var cliff = BuildChallengeHolder(env, "CliffHolder", Vector3.zero, h => BuildMushroomCliff(h, m, art));
            c.Hard("Cliff", new Vector3(35.5f, 6.85f, 50f));
            LinkStructure(tower.Find("ParkourTower"), c.points, "Spawn_Hard_Tower");
            LinkStructure(tunnel.Find("CrawlTunnel"), c.points, "Spawn_Hard_Tunnel");
            LinkStructure(cliff.Find("MushroomCliff"), c.points, "Spawn_Hard_Cliff");
            AddHint(c.points, "Spawn_Hard_Tunnel", "hintbox.tunnel", 9f);
            AddHint(c.points, "Spawn_Hard_Tower", "hintbox.tower", 10f);
            AddHint(c.points, "Spawn_Hard_Cliff", "hintbox.cliff", 10f);
            AddHint(c.points, "Spawn_Hard_Glacier", "hintbox.ice", 10f);
            AddHint(c.points, "Spawn_Hard_FarIslet", "hintbox.ice", 8f);

            // ------------------------------------------------- scenery
            var scenery = new GameObject("Scenery").transform;
            scenery.SetParent(env, false);
            var reserved = new System.Collections.Generic.List<(Vector2 centre, float radius)>
            {
                (new Vector2(lakeX, lakeZ), 21f), (new Vector2(-26f, -26f), 10f), (new Vector2(-30f, 14f), 8f), (new Vector2(-30f, 1.4f), 8f),
                (new Vector2(32f, 14f), 7f), (new Vector2(0f, 41f), 7f), (new Vector2(38f, -7f), 8f), (new Vector2(-37f, 33f), 8f),
                (new Vector2(33f, 50f), 9f), (new Vector2(22f, -14.5f), 4f)
            };
            foreach (var spot in c.points.GetComponentsInChildren<BoxSpawnPoint>())
                reserved.Add((new Vector2(spot.transform.position.x, spot.transform.position.z), 3.4f));

            bool Blocked(float x, float z)
            {
                foreach (var (centre, radius) in reserved)
                    if (Vector2.Distance(new Vector2(x, z), centre) < radius) return true;
                return false;
            }

            Scatter(90, 11, -43f, -33f, 43f, 54f, Blocked, (x, z, rng) => Pine(scenery, art, rng.Next(2), new Vector3(x, 0f, z), 0.9f + (float)rng.NextDouble() * 0.7f, true));
            Scatter(26, 12, -43f, -33f, 43f, 54f, Blocked, (x, z, rng) =>
            {
                var drift = MeshObject("Drift", scenery, snowDrifts[rng.Next(snowDrifts.Length)], art.palette, false);
                drift.transform.position = new Vector3(x, 0f, z);
                drift.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                drift.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.8f, (float)rng.NextDouble());
            });
            Scatter(18, 13, -43f, -33f, 43f, 54f, Blocked, (x, z, rng) =>
            {
                var crystal = MeshObject("Crystals", scenery, iceCrystals[rng.Next(iceCrystals.Length)], art.palette);
                crystal.transform.position = new Vector3(x, 0f, z);
                crystal.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                crystal.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.5f, (float)rng.NextDouble());
            });
            Scatter(14, 14, -43f, -33f, 43f, 54f, Blocked, (x, z, rng) =>
                PlaceProp(scenery, art.rocks[rng.Next(art.rocks.Length)], new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f, 0.8f + (float)rng.NextDouble()));

            foreach (var (x, z, yaw) in new[] { (-22f, -14f, 20f), (-8f, 18f, 200f), (14f, 30f, 120f), (-40f, 12f, 90f) })
                BuildSnowman(scenery, art.palette, new Vector3(x, 0f, z), yaw);

            AddSnowfall(env, new Vector3(0f, 14f, 10f), new Vector3(90f, 2f, 92f));
            AddTorchPost(env, c.truckPosition + new Vector3(-6f, 0f, -2f), 1.6f);
            AddTorchPost(env, c.truckPosition + new Vector3(7f, 0f, -2f), 1.6f);
            Lamp(env, art, c.truckPosition + new Vector3(0f, 0f, 5.5f));
        }
    }
}
