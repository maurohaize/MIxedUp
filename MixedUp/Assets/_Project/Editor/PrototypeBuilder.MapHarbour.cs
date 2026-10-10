using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// "Dusk Harbour": a quay on the south bank and a wide bay of shallow water to the north, crossed by wooden piers. The
    /// lanterns are lit, a raft ferries people to the far island and a gangway leads to a barge. Water and electricity are
    /// never far from each other here, so it is the map for the "night" and "challenge" modes.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        // Warm sandy flagstones of nearly one tone: the dusk light does the rest (the old mix of olive and grey made a chessboard).
        static readonly int[] QuayTiles = { 20, 20, 2, 20, 1, 20, 20, 2 };
        static readonly int[] SeaBedTiles = { 13, 19, 11, 19 };

        static void BuildHarbourScene(GameAssets a, Mats m, ArtAssets art, Prefabs p) =>
            BuildMapScene(HarbourScenePath, a, m, art, p, ctx => FillHarbour(ctx));

        /// <summary>A wooden pier deck whose top is at y = 0.25, on posts that stand in the water.</summary>
        static void Pier(Transform parent, string name, float x0, float z0, float x1, float z1, Mats m)
        {
            Prim(PrimitiveType.Cube, name, parent, new Vector3((x0 + x1) * 0.5f, 0.125f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.25f, z1 - z0), m.wood, true, null, true);
            for (float x = x0 + 0.3f; x <= x1 - 0.2f; x += 3.6f)
            {
                Prim(PrimitiveType.Cube, name + "PostN", parent, new Vector3(x, 0.15f, z0 + 0.15f), new Vector3(0.3f, 1.1f, 0.3f), m.woodDark, false);
                Prim(PrimitiveType.Cube, name + "PostS", parent, new Vector3(x, 0.15f, z1 - 0.15f), new Vector3(0.3f, 1.1f, 0.3f), m.woodDark, false);
            }
        }

        static void FillHarbour(MapContext c)
        {
            var env = c.env;
            var m = c.m;
            var art = c.art;

            SetMood(art, "SkyDusk",
                new Color(0.2f, 0.22f, 0.45f), new Color(1f, 0.6f, 0.38f), new Color(0.3f, 0.24f, 0.32f),
                new Color(1f, 0.7f, 0.48f), 1.05f, new Vector3(16f, -55f, 0f),
                new Color(0.5f, 0.46f, 0.66f), new Color(0.62f, 0.5f, 0.5f), new Color(0.3f, 0.26f, 0.3f));

            // ------------------------------------------------------ quay and bay
            FlatGround(env, "Quay", -45f, -36f, 45f, -8f, 0f, 3f, QuayTiles, 8, art);
            ColliderBox("SeaBedCollider", env, new Vector3(0f, -0.9f, 23.5f), new Vector3(90f, 1f, 63f));
            MeshObject("SeaBed", env, SaveMesh(LowPoly.Patchwork("Map_SeaBed", -45f, -8f, 45f, 55f, -0.4f, 5f, SeaBedTiles, 3)), art.palette, false);
            Prim(PrimitiveType.Cube, "QuayWall", env, new Vector3(0f, -0.2f, -8.05f), new Vector3(90f, 0.4f, 0.3f), m.stone, false);
            MeshObject("Water", env, SaveMesh(LowPoly.Water("Map_HarbourWater", -45f, 45f, -8f, 55f, -0.3f, 17)),
                fxLake != null ? fxLake : fxWater != null ? fxWater : art.paletteWater, false);
            HazardVolume("WaterVolume", env, new Vector3(0f, -0.3f, 23.5f), new Vector3(90f, 0.5f, 63f), Quaternion.identity, HazardType.Water);
            // Their own mountains: the meadow's have river valleys cut into them, which showed up here as a floating arch.
            MeshObject("Hills", env, MapHills("Map_HarbourHills", 9), art.palette);

            var life = new GameObject("WaterLife").transform;
            life.SetParent(env, false);
            var effects = life.gameObject.AddComponent<WaterEffects>();
            effects.ringMaterial = fxRing;
            effects.dropMaterial = fxSoft;
            effects.waterLevel = -0.3f;

            c.truckPosition = new Vector3(0f, 0f, -26f);
            c.noteSpots = new[] { new Vector3(-5.5f, 0f, -30.5f), new Vector3(7f, 0f, -14f), new Vector3(-8f, 0f, -12f) };

            // ------------------------------------------------------ piers
            var piers = new GameObject("Piers").transform;
            piers.SetParent(env, false);
            Pier(piers, "MainPier", -3f, -8f, 3f, 26f, m);
            Pier(piers, "EastPier", 3f, 8f, 23f, 12f, m);
            Pier(piers, "WestPier", -22f, 13f, -3f, 19f, m);
            Pier(piers, "TeePier", -9f, 26f, 9f, 30f, m);
            Pier(piers, "IslandPier", 27f, 17f, 37f, 27f, m);
            Pier(piers, "BargeDeck", -23f, 33f, -13f, 39f, m);
            Prim(PrimitiveType.Cube, "Gangway", piers, new Vector3(-18f, 0.125f, 26f), new Vector3(2f, 0.25f, 14f), m.wood, true, null, true);
            Prim(PrimitiveType.Cube, "BargeCabin", piers, new Vector3(-21f, 1.1f, 36f), new Vector3(3.4f, 1.7f, 3f), m.woodDark, true, null, true);

            // The ferry: a raft that shuttles between the end of the east pier and the island.
            var raft = new GameObject("Raft");
            raft.transform.SetParent(piers, false);
            raft.transform.position = new Vector3(25f, 0f, 10f);
            Prim(PrimitiveType.Cube, "Deck", raft.transform, new Vector3(0f, -0.05f, 0f), new Vector3(3f, 0.2f, 3f), m.wood, false, null, true);
            var raftCollider = raft.AddComponent<BoxCollider>();
            raftCollider.center = new Vector3(0f, -0.05f, 0f);
            raftCollider.size = new Vector3(3f, 0.2f, 3f);
            raft.AddComponent<Rigidbody>().isKinematic = true;
            var ferry = raft.AddComponent<MovingRaft>();
            ferry.pointA = new Vector3(25f, 0f, 10f);
            ferry.pointB = new Vector3(25f, 0f, 20f);
            ferry.speed = 1.8f;
            ferry.pause = 3f;
            var raftLamp = Lamp(raft.transform, art, Vector3.zero, 9f, 1.8f);
            raftLamp.transform.localPosition = new Vector3(1.2f, 0.05f, 1.2f);
            raftLamp.transform.localScale = Vector3.one * 0.7f;

            // ------------------------------------------------------ the boxes' spots
            c.Classic("Normal1", "normal", new Vector3(-14f, 0f, -24f));
            c.Classic("Normal2", "normal", new Vector3(12f, 0f, -20f));
            c.Classic("Hot", "hot", new Vector3(-21f, 0.25f, 16f));
            c.Classic("Electric", "electric", new Vector3(0f, 0.25f, 28f));
            c.Classic("Frozen", "frozen", new Vector3(32f, 0.25f, 22f));
            c.Classic("Toxic", "toxic", new Vector3(-18f, 0.25f, 36f));
            foreach (var spot in new[]
            {
                new Vector3(-32f, 0f, -26f), new Vector3(24f, 0f, -14f), new Vector3(-8f, 0f, -14f), new Vector3(30f, 0f, -28f),
                new Vector3(0f, 0.25f, 4f), new Vector3(14f, 0.25f, 10f), new Vector3(-12f, 0.25f, 16f), new Vector3(0f, 0.25f, 21f), new Vector3(7f, 0.25f, 28f)
            })
                c.Easy(spot);

            // ------------------------------------------------------ the brazier (hot box) and the generator (electric box)
            var brazier = new GameObject("Brazier").transform;
            brazier.SetParent(env, false);
            Prim(PrimitiveType.Cylinder, "Bowl", brazier, new Vector3(-16f, 0.5f, 16f), new Vector3(1.1f, 0.25f, 1.1f), m.stone, false);
            Prim(PrimitiveType.Cube, "Stand", brazier, new Vector3(-16f, 0.4f, 16f), new Vector3(0.3f, 0.3f, 0.3f), m.woodDark, false);
            AddFireFx(brazier, new Vector3(-16f, 0.7f, 16f), 1.2f);
            HazardSphere(brazier, "FireZone", new Vector3(-16f, 0.8f, 16f), 1.2f, HazardType.Fire);

            var generator = new GameObject("Generator").transform;
            generator.SetParent(env, false);
            Prim(PrimitiveType.Cube, "Body", generator, new Vector3(3.2f, 0.85f, 28.4f), new Vector3(1.6f, 1.2f, 1.2f), m.stone, true, null, true);
            Prim(PrimitiveType.Cube, "Coil", generator, new Vector3(3.2f, 1.7f, 28.4f), new Vector3(0.5f, 0.5f, 0.5f), m.marker, false);
            Lamp(generator, art, new Vector3(-3.3f, 0.25f, 28f));

            // ------------------------------------------------------ the crate stack (hard)
            var stack = new GameObject("CrateStack").transform;
            stack.SetParent(env, false);
            Prim(PrimitiveType.Cube, "CrateStep", stack, new Vector3(14.2f, 0.3f, -30f), new Vector3(1.5f, 0.6f, 1.6f), m.woodDark, true, null, true);
            Prim(PrimitiveType.Cube, "Crate1", stack, new Vector3(16f, 0.6f, -30f), new Vector3(2.2f, 1.2f, 2.4f), m.wood, true, null, true);
            Prim(PrimitiveType.Cube, "Crate2", stack, new Vector3(18.3f, 1.2f, -30f), new Vector3(2.2f, 2.4f, 2.4f), m.woodDark, true, null, true);
            Prim(PrimitiveType.Cube, "Crate3", stack, new Vector3(20.6f, 1.8f, -30f), new Vector3(2.2f, 3.6f, 2.4f), m.wood, true, null, true);
            Lamp(stack, art, new Vector3(22.2f, 3.6f, -29f), 10f, 2.4f);
            c.Hard("CrateStack", new Vector3(20.6f, 3.65f, -30f));
            c.Hard("IslandFar", new Vector3(35.5f, 0.25f, 25.5f));

            // ------------------------------------------------------ borrowed challenges: lighthouse tower and the mushroom cliff
            var tower = BuildChallengeHolder(env, "TowerHolder", new Vector3(0f, 0f, -12f), h => BuildTower(h, m, art));
            c.Hard("Tower", new Vector3(38f, 7.05f, -19f));
            var cliff = BuildChallengeHolder(env, "CliffHolder", new Vector3(-70f, 0f, -62f), h => BuildMushroomCliff(h, m, art));
            c.Hard("Cliff", new Vector3(-34.5f, 6.85f, -12f));
            LinkStructure(tower.Find("ParkourTower"), c.points, "Spawn_Hard_Tower");
            LinkStructure(cliff.Find("MushroomCliff"), c.points, "Spawn_Hard_Cliff");
            AddHint(c.points, "Spawn_Hard_Tower", "hintbox.tower", 10f);
            AddHint(c.points, "Spawn_Hard_Cliff", "hintbox.cliff", 10f);
            AddHint(c.points, "Spawn_Hard_CrateStack", "hintbox.crates", 9f);
            AddHint(c.points, "Spawn_Hard_IslandFar", "hintbox.raft", 12f);

            // ------------------------------------------------------ lanterns, crates and boats
            var props = new GameObject("Props").transform;
            props.SetParent(env, false);
            foreach (float z in new[] { 0f, 10f, 20f })
            {
                Lamp(props, art, new Vector3(-2.6f, 0.25f, z));
                Lamp(props, art, new Vector3(2.6f, 0.25f, z + 4f));
            }
            Lamp(props, art, new Vector3(13f, 0.25f, 11.2f));
            Lamp(props, art, new Vector3(-12f, 0.25f, 18.2f));
            Lamp(props, art, new Vector3(32f, 0.25f, 18f), 10f, 2.4f);
            Lamp(props, art, new Vector3(-23f, 0.25f, 34f), 10f, 2.4f);
            foreach (var x in new[] { -30f, -20f, 20f, 30f }) Lamp(props, art, new Vector3(x, 0f, -12f));
            AddTorchPost(props, new Vector3(-5f, 0f, -22f), 1.6f);
            AddTorchPost(props, new Vector3(5f, 0f, -22f), 1.6f);

            var crateMesh = SaveInkedMesh(LowPolyProps.Crate());
            var barrelMesh = SaveInkedMesh(LowPolyProps.Barrel());
            foreach (var (x, z, yaw) in new[] { (-2.2f, 2f, 10f), (2.1f, 14.5f, 80f), (-1.5f, 23f, 40f), (8f, 11.2f, 0f), (-8f, 18.2f, 30f), (31f, 25f, 15f), (-14f, 34.5f, 70f) })
            {
                float y = 0.25f;
                Prop(props, "Crate", crateMesh, art.palette, x, z, yaw, 1f, Solid.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.95f), y);
                if (x > 0f) Prop(props, "Barrel", barrelMesh, art.palette, x + 1.2f, z + 0.4f, 0f, 1f, Solid.Capsule, new Vector3(0f, 0.475f, 0f), new Vector3(0.4f, 0.95f, 0f), y);
            }
            foreach (var (x, z, yaw) in new[] { (-28f, -18f, 20f), (-30f, -30f, 100f), (26f, -22f, 60f), (34f, -30f, 150f), (-12f, -30f, 10f) })
                Prop(props, "Crate", crateMesh, art.palette, x, z, yaw, 1.1f, Solid.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.95f), 0f);

            // moored boats: a hull, a little cabin and a mast. Just for the view; the water is shallow.
            foreach (var (x, z, yaw) in new[] { (-9f, 6f, 90f), (9f, 18f, 80f), (-30f, 24f, 20f), (22f, 38f, 100f), (8f, 44f, 70f) })
            {
                var boat = new GameObject("Boat").transform;
                boat.SetParent(props, false);
                boat.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, yaw, 0f));
                Prim(PrimitiveType.Cube, "Hull", boat, new Vector3(0f, -0.15f, 0f), new Vector3(1.6f, 0.7f, 4.6f), m.woodDark, false, null, true);
                Prim(PrimitiveType.Cube, "Cabin", boat, new Vector3(0f, 0.45f, -0.6f), new Vector3(1.1f, 0.6f, 1.4f), m.wood, false, null, true);
                Prim(PrimitiveType.Cube, "Mast", boat, new Vector3(0f, 1.3f, 0.9f), new Vector3(0.12f, 2.6f, 0.12f), m.woodDark, false);
            }

            Scatter(14, 21, -43f, -33f, 43f, -10f, (x, z) => Mathf.Abs(x) < 8f && z > -33f || (x > 12f && x < 24f && z < -27f) || z > -13f || NearSpawnPoint(c, x, z, 3.2f), (x, z, rng) =>
                PlaceProp(props, art.rocks[rng.Next(art.rocks.Length)], new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f, 0.7f + (float)rng.NextDouble() * 0.8f));
        }
    }
}
