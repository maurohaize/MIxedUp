using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// "Dusk Harbour": a paved quay on the south shore of a bay that opens to the sea in the north, between green headlands
    /// with a village on the slopes. Planked piers lead out over the water to a barge and a little island with a ferry raft;
    /// the quay has its lighthouse, warehouse, crane, fish market, harbour master, a tavern with a terrace and party lights,
    /// a container yard and a boat on trestles. Water and electricity are never far from each other here: the map of the
    /// "night" and "challenge" modes.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        static readonly int[] SeaBedTiles = { 13, 19, 11, 19 };

        static readonly LowPoly.HillColors HarbourHills = new LowPoly.HillColors
        {
            low = new[] { 12, 12, 13, 11 }, mid = new[] { 13, 12, 11, 19 }, high = new[] { 13, 19, 11, 19 }, rock = new[] { 19, 19, 26 },
            beach = new[] { 1, 2, 1 }, beachHeight = 1.3f
        };

        /// <summary>The road down from the hills to the truck (it enters through the arch at the south edge).</summary>
        static readonly Vector2[] HarbourRoad =
        {
            V(0f, -33.5f), V(0f, -46f), V(-5f, -60f), V(3f, -76f), V(-2f, -95f), V(8f, -116f), V(2f, -140f), V(10f, -165f)
        };

        /// <summary>The cobbled lanes of the quay: truck to the main pier, and the promenade along the water.</summary>
        static readonly Vector2[][] HarbourLanes =
        {
            new[] { V(0f, -21f), V(0f, -8f) },
            new[] { V(-44f, -10.8f), V(44f, -10.8f) },
            new[] { V(0f, -17f), V(-12f, -18f), V(-24f, -22f) },
            new[] { V(0f, -17f), V(14f, -17f), V(30f, -17f), V(33f, -26f), V(37f, -27.5f) }
        };

        static float HarbourLaneDistance(float x, float z)
        {
            float best = float.MaxValue;
            foreach (var lane in HarbourLanes) best = Mathf.Min(best, DistanceToLine(lane, x, z));
            return best;
        }

        static void BuildHarbourScene(GameAssets a, Mats m, ArtAssets art, Prefabs p) =>
            BuildMapScene(HarbourScenePath, a, m, art, p, ctx => FillHarbour(ctx));

        /// <summary>A planked pier from (x0, z0) to (x1, z1) whose top is at y = 0.25, on posts that stand in the water.</summary>
        static void Pier(Transform parent, string name, float x0, float z0, float x1, float z1, Mats m, ArtAssets art)
        {
            HideVisual(Prim(PrimitiveType.Cube, name, parent, new Vector3((x0 + x1) * 0.5f, 0.125f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.25f, z1 - z0), m.wood, true));
            float sx = x1 - x0, sz = z1 - z0;
            bool alongX = sx > sz;
            float length = alongX ? sx : sz, width = alongX ? sz : sx;
            var deck = Kit("Kit_Pier_" + Dm(length) + "x" + Dm(width), () => LowPolyKit.PierDeck("p", length, width, 1.1f, Mathf.RoundToInt(length * 7 + width)));
            Piece(parent, name + "Deck", deck, art.palette, new Vector3((x0 + x1) * 0.5f, 0.25f, (z0 + z1) * 0.5f), alongX ? 90f : 0f);
        }

        static void FillHarbour(MapContext c)
        {
            EnsureKitMaterials();
            var env = c.env;
            var m = c.m;
            var art = c.art;
            var pal = art.palette;

            SetMood(art, "SkyDusk",
                new Color(0.2f, 0.22f, 0.45f), new Color(1f, 0.6f, 0.38f), new Color(0.3f, 0.24f, 0.32f),
                new Color(1f, 0.7f, 0.48f), 1.05f, new Vector3(16f, -55f, 0f),
                new Color(0.5f, 0.46f, 0.66f), new Color(0.62f, 0.5f, 0.5f), new Color(0.3f, 0.26f, 0.3f));

            // ------------------------------------------------------ the quay: paving with cobbled lanes; the bay and the open sea
            ColliderBox("QuayCollider", env, new Vector3(0f, -0.5f, -22f), new Vector3(90f, 1f, 28f));
            int Paving(float x, float z)
            {
                int hash = (Mathf.FloorToInt(x) * 73 + Mathf.FloorToInt(z) * 37) & 15;
                if (HarbourLaneDistance(x, z) < 1.7f) return hash < 2 ? 14 : hash < 4 ? 8 : 9;
                if (z > -9.2f) return 26;                                   // the stone coping along the edge
                return hash == 0 ? 2 : hash == 1 ? 19 : 20;
            }
            MeshObject("Quay", env, SaveMesh(LowPoly.Terrain("Map_HarbourQuay", -45f, -36f, 45f, -8f, 1f, new[] { 20 }, 8, (x, z) => 0f, Paving, false)), pal);
            MeshObject("QuayWall", env, Kit("Kit_QuayWall", () =>
            {
                var b = new LowPoly.MeshBuilder();
                for (float x = -45f; x < 45f; x += 1.5f)
                    for (int row = 0; row < 3; row++)
                    {
                        float off = row % 2 == 0 ? 0f : 0.75f;
                        LowPolyKit.Box(b, new Vector3(x + off + 0.72f, -0.18f - row * 0.36f, -8.05f), new Vector3(1.42f, 0.34f, 0.2f), (Mathf.FloorToInt(x * 3 + row) & 3) == 0 ? LowPolyKit.Stone : LowPolyKit.Grey);
                    }
                return b.ToMesh("w");
            }), pal);

            ColliderBox("SeaBedCollider", env, new Vector3(0f, -0.9f, 23.5f), new Vector3(90f, 1f, 63f));
            MeshObject("SeaBed", env, SaveMesh(LowPoly.Patchwork("Map_SeaBed", -45f, -8f, 45f, 56f, -0.4f, 5f, SeaBedTiles, 3)), pal, false);
            var water = fxLake != null ? fxLake : fxWater != null ? fxWater : art.paletteWater;
            MeshObject("Water", env, SaveMesh(LowPoly.Water("Map_HarbourWater", -52f, 52f, -8f, 64f, -0.3f, 17)), water, false);
            MeshObject("SeaNorth", env, SaveMesh(LowPoly.Water("Map_HarbourSeaNorth", -52f, 52f, 64f, 320f, -0.3f, 17, 8f)), water, false);
            MeshObject("SeaWest", env, SaveMesh(LowPoly.Water("Map_HarbourSeaWest", -280f, -52f, -8f, 320f, -0.3f, 17, 8f)), water, false);
            MeshObject("SeaEast", env, SaveMesh(LowPoly.Water("Map_HarbourSeaEast", 52f, 280f, -8f, 320f, -0.3f, 17, 8f)), water, false);
            HazardVolume("WaterVolume", env, new Vector3(0f, -0.3f, 23.5f), new Vector3(90f, 0.5f, 63f), Quaternion.identity, HazardType.Water);

            // Headlands either side of the bay; north of the map the land sinks under the sea, leaving a few islands.
            float Sea(float x, float z, float h)
            {
                float north = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(50f, 95f, z));
                float sides = 1f - 0.75f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 150f, Mathf.Abs(x)));
                // the bay also runs a little way past the side walls, so its shore curves instead of following them
                float bay = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-9f, 0f, z));
                float outside = Mathf.Max(Mathf.Abs(x) - 45f, 0f), wobble = Mathf.PerlinNoise(z * 0.05f + 3f, x * 0.02f + 1f) * 14f;
                float shore = bay * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f + wobble, 18f + wobble, outside))) * 5f;
                return h - shore - north * sides * (24f + 12f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(80f, 200f, z)));
            }
            var field = MapHillField(9, HarbourRoad, Sea);
            MeshObject("Hills", env, SaveMesh(LowPoly.Hills("Map_HarbourHills", field, HarbourHills)), pal);

            var life = new GameObject("WaterLife").transform;
            life.SetParent(env, false);
            var effects = life.gameObject.AddComponent<WaterEffects>();
            effects.ringMaterial = fxRing;
            effects.dropMaterial = fxSoft;
            effects.waterLevel = -0.3f;

            c.truckPosition = new Vector3(0f, 0f, -26f);
            c.noteSpots = new[] { new Vector3(-5.5f, 0f, -30.5f), new Vector3(7f, 0f, -14f), new Vector3(-8f, 0f, -12f) };

            // ------------------------------------------------------ piers, the barge and the island
            var piers = new GameObject("Piers").transform;
            piers.SetParent(env, false);
            Pier(piers, "MainPier", -3f, -8f, 3f, 26f, m, art);
            Pier(piers, "EastPier", 3f, 8f, 23f, 12f, m, art);
            Pier(piers, "WestPier", -22f, 13f, -3f, 19f, m, art);
            Pier(piers, "TeePier", -9f, 26f, 9f, 30f, m, art);
            Pier(piers, "IslandPier", 27f, 17f, 37f, 27f, m, art);
            Pier(piers, "BargeDeck", -23f, 33f, -13f, 39f, m, art);
            Pier(piers, "Gangway", -19f, 19f, -17f, 33f, m, art);
            Piece(piers, "BargeHull", Kit("Kit_BargeHull", () => LowPolyKit.BargeHull("b", new Vector2(10f, 6f))), pal, new Vector3(-18f, 0.25f, 36f), 0f);
            HideVisual(Prim(PrimitiveType.Cube, "BargeCabin", piers, new Vector3(-21f, 1.1f, 36f), new Vector3(3.4f, 1.7f, 3f), m.woodDark, true));
            var cabin = Piece(piers, "BargeCabinVisual", Kit("Kit_BargeCabin", () => LowPolyKit.Cabin("b", 3.0f, 3.4f, 1.75f, LowPolyKit.Snow, LowPolyKit.Teal, LowPolyKit.Navy, false, false, true, 0.6f)),
                pal, new Vector3(-21f, 0.25f, 36f), 90f);
            GlowWindows(cabin.transform, 3.0f, 3.4f, 1.75f, false);

            // the island at the end of the ferry: rocks round a boardwalk, a fishing shack
            var island = new GameObject("Island").transform;
            island.SetParent(piers, false);
            MeshObject("IslandRocks", island, Kit("Kit_HarbourIslandRocks", () =>
            {
                var b = new LowPoly.MeshBuilder();
                var rock = new[] { LowPolyKit.Stone, LowPolyKit.Grey, LowPolyKit.Sand };
                for (int i = 0; i < 26; i++)
                {
                    float t = i / 26f * Mathf.PI * 2f;
                    float r = 5.6f + Mathf.Sin(i * 2.7f) * 0.5f;
                    var at = new Vector3(Mathf.Cos(t) * r, -0.25f, Mathf.Sin(t) * r);
                    if (at.x < -4.5f && Mathf.Abs(at.z + 2f) < 2.4f) continue;   // keep the ferry landing open
                    LowPolyKit.Blob(b, at, new Vector3(1.1f, 0.6f + (i % 3) * 0.12f, 1.0f), rock[i % rock.Length], i, 1, 0.16f);
                }
                LowPolyKit.Blob(b, new Vector3(0f, -0.55f, 0f), new Vector3(6.4f, 0.5f, 6.4f), LowPolyKit.Sand, 99, 1, 0.08f);
                return b.ToMesh("r");
            }), pal).transform.position = new Vector3(32f, 0f, 22f);
            var shack = Piece(island, "FishingShack", Kit("Kit_FishingShack", () => LowPolyKit.Cabin("s", 2.8f, 2.4f, 2.0f, LowPolyKit.Teal, LowPolyKit.DarkRust, LowPolyKit.Snow, false, false, true)),
                pal, new Vector3(28.9f, 0.25f, 25.3f), 135f, 1f, new Vector3(2.9f, 2.6f, 2.5f));
            GlowWindows(shack.transform, 2.8f, 2.4f, 2.0f, false);
            Piece(island, "NetRack", Kit("Kit_NetRack", () => LowPolyKit.NetRack("n", 3)), pal, new Vector3(35.6f, 0.25f, 19.2f), 80f);

            // The ferry: a raft that shuttles between the end of the east pier and the island.
            var raft = new GameObject("Raft");
            raft.transform.SetParent(piers, false);
            raft.transform.position = new Vector3(25f, 0f, 10f);
            if (raftMesh == null) raftMesh = SaveInkedMesh(LowPolyProps.Raft());
            var raftDeck = MeshObject("Deck", raft.transform, raftMesh, pal);
            raftDeck.isStatic = false;
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
            Piece(brazier, "Basket", Kit("Kit_Brazier", () => LowPolyKit.Brazier("b")), pal, new Vector3(-16f, 0.25f, 16f), 0f, 1f, new Vector3(1.0f, 1.2f, 1.0f));
            AddFireFx(brazier, new Vector3(-16f, 1.35f, 16f), 1.1f);
            HazardSphere(brazier, "FireZone", new Vector3(-16f, 0.8f, 16f), 1.2f, HazardType.Fire);

            var generator = new GameObject("Generator").transform;
            generator.SetParent(env, false);
            HideVisual(Prim(PrimitiveType.Cube, "Body", generator, new Vector3(3.2f, 0.85f, 28.4f), new Vector3(1.6f, 1.2f, 1.2f), m.stone, true));
            Piece(generator, "Machine", Kit("Kit_Generator", () => LowPolyKit.Generator("g")), pal, new Vector3(3.2f, 0.25f, 28.4f), 180f);
            AddSparks(generator, new Vector3(3.2f, 1.75f, 28.4f));
            Lamp(generator, art, new Vector3(-3.3f, 0.25f, 28f));

            // ------------------------------------------------------ the crate stack (hard)
            var stack = new GameObject("CrateStack").transform;
            stack.SetParent(env, false);
            var crates = new[]
            {
                ("CrateStep", new Vector3(14.2f, 0.3f, -30f), new Vector3(1.5f, 0.6f, 1.6f)), ("Crate1", new Vector3(16f, 0.6f, -30f), new Vector3(2.2f, 1.2f, 2.4f)),
                ("Crate2", new Vector3(18.3f, 1.2f, -30f), new Vector3(2.2f, 2.4f, 2.4f)), ("Crate3", new Vector3(20.6f, 1.8f, -30f), new Vector3(2.2f, 3.6f, 2.4f))
            };
            var crateMeshBuilder = new LowPoly.MeshBuilder();
            for (int i = 0; i < crates.Length; i++)
            {
                var (name, centre, size) = crates[i];
                HideVisual(Prim(PrimitiveType.Cube, name, stack, centre, size, m.wood, true));
                LowPolyKit.CrateBlock(crateMeshBuilder, new Vector3(centre.x - 17.4f, 0f, centre.z + 30f), size, i * 5 + 1);
            }
            MeshObject("Crates", stack, Kit("Kit_HarbourCrateStack", () => crateMeshBuilder.ToMesh("c")), pal).transform.position = new Vector3(17.4f, 0f, -30f);
            Lamp(stack, art, new Vector3(22.2f, 3.6f, -29f), 10f, 2.4f);
            c.Hard("CrateStack", new Vector3(20.6f, 3.65f, -30f));
            c.Hard("IslandFar", new Vector3(35.5f, 0.25f, 25.5f));

            // ------------------------------------------------------ borrowed challenges: the lighthouse and the container yard
            var tower = BuildChallengeHolder(env, "TowerHolder", new Vector3(0f, 0f, -12f), h => BuildTower(h, m, art, MapTheme.Harbour));
            c.Hard("Tower", new Vector3(38f, 7.05f, -19f));
            var cliff = BuildChallengeHolder(env, "CliffHolder", new Vector3(-70f, 0f, -62f), h => BuildMushroomCliff(h, m, art, MapTheme.Harbour));
            c.Hard("Cliff", new Vector3(-34.5f, 6.85f, -12f));
            LinkStructure(tower.Find("ParkourTower"), c.points, "Spawn_Hard_Tower");
            LinkStructure(cliff.Find("MushroomCliff"), c.points, "Spawn_Hard_Cliff");
            AddHint(c.points, "Spawn_Hard_Tower", "hintbox.tower", 10f);
            AddHint(c.points, "Spawn_Hard_Cliff", "hintbox.containers", 10f);
            AddHint(c.points, "Spawn_Hard_CrateStack", "hintbox.crates", 9f);
            AddHint(c.points, "Spawn_Hard_IslandFar", "hintbox.raft", 12f);

            // ------------------------------------------------------ lanterns on the piers, crates and barrels
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
            AddTorchPost(props, new Vector3(-5f, 0f, -22f), 1.6f);
            AddTorchPost(props, new Vector3(5f, 0f, -22f), 1.6f);

            var crateMesh = SaveInkedMesh(LowPolyProps.Crate());
            var barrelMesh = SaveInkedMesh(LowPolyProps.Barrel());
            foreach (var (x, z, yaw) in new[] { (-2.2f, 2f, 10f), (2.1f, 14.5f, 80f), (-1.5f, 23f, 40f), (8f, 11.2f, 0f), (-8f, 18.2f, 30f), (31f, 25f, 15f), (-14f, 34.5f, 70f) })
            {
                float y = 0.25f;
                Prop(props, "Crate", crateMesh, pal, x, z, yaw, 1f, Solid.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.95f), y);
                if (x > 0f) Prop(props, "Barrel", barrelMesh, pal, x + 1.2f, z + 0.4f, 0f, 1f, Solid.Capsule, new Vector3(0f, 0.475f, 0f), new Vector3(0.4f, 0.95f, 0f), y);
            }

            // moored boats; they bob a little and can be stood on
            var boats = new GameObject("Boats").transform;
            boats.SetParent(env, false);
            var boatKinds = new[]
            {
                Kit("Kit_BoatRed", () => LowPolyKit.Boat("b", 6f, 2.2f, LowPolyKit.Brick, LowPolyKit.Snow, true, true, 1)),
                Kit("Kit_BoatTeal", () => LowPolyKit.Boat("b", 5.4f, 2.0f, LowPolyKit.Teal, LowPolyKit.Snow, false, true, 2)),
                Kit("Kit_BoatNavy", () => LowPolyKit.Boat("b", 6.4f, 2.3f, LowPolyKit.Navy, LowPolyKit.Gold, true, false, 3)),
                Kit("Kit_Rowboat", () => LowPolyKit.Boat("b", 3.6f, 1.5f, LowPolyKit.Wood, LowPolyKit.Brick, false, false, 4))
            };
            var boatSizes = new[] { new Vector2(2.2f, 6f), new Vector2(2.0f, 5.4f), new Vector2(2.3f, 6.4f), new Vector2(1.5f, 3.6f) };
            foreach (var (x, z, yaw, kind) in new[] { (-9f, 6f, 90f, 0), (9f, 18.5f, 80f, 1), (-30f, 24f, 20f, 2), (22f, 38f, 100f, 0), (8f, 44f, 70f, 2), (-6f, 9.5f, 95f, 3), (5.4f, 0f, 175f, 3), (-28f, 44f, 150f, 1) })
            {
                var boat = new GameObject("Boat");
                boat.transform.SetParent(boats, false);
                boat.transform.SetPositionAndRotation(new Vector3(x, -0.3f, z), Quaternion.Euler(0f, yaw, 0f));
                var hull = MeshObject("Hull", boat.transform, boatKinds[kind], pal);
                hull.isStatic = false;
                var solid = boat.AddComponent<BoxCollider>();
                solid.center = new Vector3(0f, 0f, 0f);
                solid.size = new Vector3(boatSizes[kind].x * 0.9f, 0.76f, boatSizes[kind].y * 0.85f);
                var bob = boat.AddComponent<Bob>();
                bob.height = 0.04f;
                bob.swayDegrees = 2.5f;
                bob.speed = 0.7f + (x * 0.013f % 0.4f);
            }
            // sailing boats out in the bay and at sea, far from everything
            foreach (var (x, z, yaw, kind) in new[] { (14f, 96f, 30f, 1), (-46f, 128f, 110f, 0), (70f, 160f, 250f, 1), (-10f, 200f, 80f, 2) })
            {
                var far = Piece(boats, "FarBoat", boatKinds[kind], pal, new Vector3(x, -0.3f, z), yaw, 1.4f);
                far.isStatic = false;
                far.AddComponent<Bob>().height = 0.08f;
            }

            AddHarbourPlaces(c, field);

            // ------------------------------------------------------ outside: the road in, trees and a village on the slopes
            MapRoad(env, HarbourRoad, field, art, "Harbour", 2, false);
            EntranceArch(env, new Vector3(0f, 0f, -34.6f), 0f, 3.6f, "map.harbour.name", false, art);
            var smoothRoad = Smooth(HarbourRoad, 2f);
            var tints = new[] { pal, pal, art.tintAutumn, art.tintGold, art.tintTeal };
            HillDecor(env, field, art, 31, 380, 90, (x, z) => DistanceToLine(smoothRoad, x, z) < 6f,
                (parent, at, rng) => PlaceDecor(parent, art.trees[rng.Next(art.trees.Length)], at, (float)rng.NextDouble() * 360f, Mathf.Lerp(0.85f, 1.45f, (float)rng.NextDouble()), tints[rng.Next(tints.Length)]),
                0.6f);
            var outside = env.Find("Outside");
            var walls = new[] { LowPolyKit.Snow, LowPolyKit.Sand, LowPolyKit.Salmon, LowPolyKit.LightTeal, LowPolyKit.Snow };
            var roofs = new[] { LowPolyKit.Brick, LowPolyKit.DarkRust, LowPolyKit.Navy, LowPolyKit.Brick };
            int house = 0;
            foreach (var (x, z, yaw) in new[] { (-55f, -22f, 80f), (-60f, -8f, 95f), (-57f, 8f, 100f), (-66f, 22f, 120f), (56f, -24f, -80f), (60f, -9f, -95f), (63f, 7f, -100f), (70f, 21f, -130f), (-18f, -46f, 10f), (20f, -47f, -15f), (-34f, -52f, 30f) })
            {
                if (field.Surface(x, z) < 0.4f || SlopeAt(field, x, z) > 0.55f) continue;
                Cottage(outside, field, new Vector3(x, 0f, z), yaw, 4.4f - (house % 2) * 0.6f, 3.6f, walls[house % walls.Length], roofs[house % roofs.Length], false, house % 3 == 0, art);
                house++;
            }

            // gulls circling over the bay
            var gull = Kit("Kit_Gull", () => LowPolyKit.Gull("g"));
            foreach (var (x, z, y, r, speed) in new[] { (6f, 20f, 13f, 9f, 22f), (-20f, 40f, 16f, 12f, -18f), (30f, -6f, 15f, 7f, 26f) })
            {
                var flock = new GameObject("Gulls").transform;
                flock.SetParent(env, false);
                flock.position = new Vector3(x, y, z);
                flock.gameObject.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, speed, 0f);
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 120f + x;
                    var bird = MeshObject("Gull", flock, gull, pal, false);
                    bird.isStatic = false;
                    bird.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(r, i * 0.7f, 0f);
                    bird.transform.localRotation = Quaternion.Euler(0f, angle + (speed > 0f ? 180f : 0f), speed > 0f ? -18f : 18f);
                    bird.transform.localScale = Vector3.one * 1.6f;
                }
            }
        }

        /// <summary>Blue sparks crackling off the generator's coil every now and then.</summary>
        static void AddSparks(Transform parent, Vector3 position)
        {
            var ps = NewParticles("Sparks", parent, fxFlame);
            ps.transform.position = position;
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new Color(0.55f, 0.85f, 1f, 1f);
            main.gravityModifier = 0.6f;
            main.maxParticles = 60;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 12, 0, 1.3f), new ParticleSystem.Burst(0.4f, 3, 6, 0, 2.1f) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
        }

        /// <summary>The buildings, props, signs and secrets that make the quay a harbour.</summary>
        static void AddHarbourPlaces(MapContext c, LowPoly.HillField field)
        {
            var env = c.env;
            var m = c.m;
            var art = c.art;
            var pal = art.palette;
            var places = new GameObject("Details").transform;
            places.SetParent(env, false);

            // ---- signs: what is where
            DirectionSign(places, new Vector3(4.6f, 0f, -13.2f), m,
                ("sign.lighthouse", new Vector3(38f, 0f, -19f)), ("sign.market", new Vector3(32f, 0f, -13f)),
                ("sign.master", new Vector3(-14f, 0f, -13f)), ("sign.tavern", new Vector3(-24f, 0f, -28f)), ("sign.barge", new Vector3(-18f, 0f, 36f)));
            Notice(places, new Vector3(-4.6f, 0f, -21.2f), new Vector3(0f, 0f, -30f), "notice.harbour.welcome", m, 1.7f);
            Notice(places, new Vector3(2.0f, 0.25f, 5.6f), new Vector3(0f, 0f, -20f), "notice.harbour.pier", m);
            Notice(places, new Vector3(21.5f, 0.25f, 11.2f), new Vector3(-20f, 0f, -20f), "notice.harbour.east", m);
            Notice(places, new Vector3(-2.4f, 0.25f, 24.8f), new Vector3(0f, 0f, -20f), "notice.harbour.generator", m);
            Notice(places, new Vector3(-14.2f, 0.25f, 13.7f), new Vector3(0f, 0f, -20f), "notice.harbour.brazier", m);
            Notice(places, new Vector3(-14.3f, 0.25f, 33.6f), new Vector3(0f, 0f, -20f), "notice.harbour.barge", m);

            // ---- the harbour master's office, with its flag
            var hutAt = new Vector3(-14f, 0f, -13f);
            var office = Piece(places, "HarbourMaster", Kit("Kit_HarbourOffice", () => LowPolyKit.Cabin("o", 4.2f, 3.4f, 2.5f, LowPolyKit.Snow, LowPolyKit.Brick, LowPolyKit.Navy, false, false, true)),
                pal, hutAt, 0f, 1f, new Vector3(4.4f, 3f, 3.6f));
            GlowWindows(office.transform, 4.2f, 3.4f, 2.5f, false);
            AddWallLantern(office.transform, new Vector3(-0.9f, 2.15f, 1.72f), Vector3.forward, 8f, 1.8f);
            Notice(places, hutAt + new Vector3(3.4f, 0f, -2.6f), new Vector3(-10f, 0f, -24f), "notice.harbour.master", m);
            Prim(PrimitiveType.Cylinder, "Flagpole", places, new Vector3(-17.2f, 3.2f, -12.6f), new Vector3(0.12f, 3.2f, 0.12f), m.woodDark, true);
            var flag = MeshObject("Flag", places, flags[0], pal);
            flag.transform.position = new Vector3(-17.2f, 5.4f, -12.6f);
            flag.transform.localScale = Vector3.one * 0.9f;

            // ---- the warehouse where the crates come from, and the crane that lifts them off the boats
            var warehouseAt = new Vector3(40f, 0f, -30f);
            var shed = Piece(places, "Warehouse", Kit("Kit_Warehouse", () => LowPolyKit.Shed("w", 7f, 5f, 3.6f, LowPolyKit.Wood, LowPolyKit.Navy, LowPolyKit.Teal)),
                pal, warehouseAt, 0f, 1f, new Vector3(7.3f, 4.6f, 5.3f));
            AddWallLantern(shed.transform, new Vector3(2.6f, 2.95f, 2.52f), Vector3.forward, 9f, 2.0f);
            Notice(places, warehouseAt + new Vector3(-5.4f, 0f, 3.6f), new Vector3(30f, 0f, -18f), "notice.harbour.warehouse", m);
            foreach (var (x, z, yaw) in new[] { (34.6f, -33.2f, 10f), (35.8f, -33.4f, 40f), (35.1f, -32.1f, 0f) })
                Prop(places, "Barrel", SaveInkedMesh(LowPolyProps.Barrel()), pal, x, z, yaw, 1f, Solid.Capsule, new Vector3(0f, 0.475f, 0f), new Vector3(0.4f, 0.95f, 0f), 0f);

            var crane = new GameObject("Crane").transform;
            crane.SetParent(places, false);
            crane.position = new Vector3(26f, 0f, -10f);
            Piece(crane, "CraneBody", Kit("Kit_Crane", () => LowPolyKit.Crane("c")), pal, crane.position, 0f, 1f, new Vector3(2.4f, 0.8f, 2.4f));
            var mast = new GameObject("MastCollider");
            mast.transform.SetParent(crane, false);
            mast.transform.position = crane.position + new Vector3(0f, 3.4f, 0f);
            mast.AddComponent<BoxCollider>().size = new Vector3(1.6f, 5.2f, 1.6f);
            var hook = new GameObject("Hook").transform;
            hook.SetParent(crane, false);
            hook.localPosition = new Vector3(0f, 8.1f, 9.6f);
            var sway = hook.gameObject.AddComponent<Bob>();
            sway.swayDegrees = 4f;
            sway.height = 0.04f;
            Prim(PrimitiveType.Cube, "Rope", hook, new Vector3(0f, -2.1f, 0f), new Vector3(0.06f, 4.2f, 0.06f), coalBlack, false);
            var hanging = MeshObject("HangingCrate", hook, SaveInkedMesh(LowPolyProps.Crate()), pal);
            hanging.isStatic = false;
            hanging.transform.localPosition = new Vector3(0f, -5.0f, 0f);
            hanging.transform.localScale = Vector3.one * 1.3f;
            AddSecret(places, "crane", crane.position + new Vector3(1.9f, 1f, -0.6f), "secret.crane", "prompt.secret", 1.4f);
            Notice(places, crane.position + new Vector3(-3.4f, 0f, -1.6f), new Vector3(20f, 0f, -24f), "notice.harbour.crane", m);

            // ---- the old lighthouse sweeps its beam round the bay
            var beam = new GameObject("LighthouseBeam");
            beam.transform.SetParent(places, false);
            beam.transform.position = new Vector3(38f, 7f + LowPolyKit.LanternGlassMid, -19f);
            var lightObject = new GameObject("Beam");
            lightObject.transform.SetParent(beam.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 75f;
            light.spotAngle = 32f;
            light.intensity = 9f;
            light.color = new Color(1f, 0.9f, 0.6f);
            light.shadows = LightShadows.None;
            beam.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 35f, 0f);
            Notice(places, new Vector3(31.6f, 0f, -21.4f), new Vector3(24f, 0f, -18f), "notice.harbour.lighthouse", m);

            // ---- the fish market: three stalls under striped awnings, facing the quay
            var stripes = new[] { LowPolyKit.Brick, LowPolyKit.Teal, LowPolyKit.Gold };
            for (int i = 0; i < 3; i++)
            {
                int stripe = stripes[i];
                Piece(places, "Stall" + (i + 1), Kit("Kit_Stall_" + stripe, () => LowPolyKit.MarketStall("s", stripe, i)), pal,
                    new Vector3(28.6f + i * 3.4f, 0f, -13.2f), 180f, 1f, new Vector3(2.4f, 1.0f, 0.9f));
            }
            AddSecret(places, "market", new Vector3(32f, 1f, -15.2f), "secret.market", "prompt.secret", 1.6f);
            Notice(places, new Vector3(36.6f, 0f, -16.2f), new Vector3(30f, 0f, -24f), "notice.harbour.market", m);
            foreach (var (x, z) in new[] { (27.2f, -11.6f), (38.4f, -11.8f) })
                Piece(places, "FishTrap", Kit("Kit_LobsterTrap", () => LowPolyKit.LobsterTrap("t")), pal, new Vector3(x, 0f, z), x * 13f, 1f, new Vector3(0.8f, 0.5f, 0.55f));

            // ---- the tavern with a terrace of tables under party lights
            var tavernAt = new Vector3(-24f, 0f, -31f);
            var tavern = Piece(places, "Tavern", Kit("Kit_Tavern", () => LowPolyKit.Cabin("t", 6.4f, 4.4f, 2.9f, LowPolyKit.Sand, LowPolyKit.Teal, LowPolyKit.Snow, false, false, true)),
                pal, tavernAt, 0f, 1f, new Vector3(6.6f, 3.4f, 4.6f));
            GlowWindows(tavern.transform, 6.4f, 4.4f, 2.9f, false);
            ChimneySmoke(places, tavern.transform.TransformPoint(new Vector3(6.4f * 0.27f, 2.9f + 4.4f * 0.42f * 1.05f + 0.8f, -4.4f * 0.2f)));
            AddWallLantern(tavern.transform, new Vector3(-2.95f, 2.3f, 2.22f), Vector3.forward, 9f, 2.0f);
            AddWallLantern(tavern.transform, new Vector3(2.95f, 2.3f, 2.22f), Vector3.forward, 9f, 2.0f);
            var tavernSign = new GameObject("TavernSign").transform;
            tavernSign.SetParent(places, false);
            tavernSign.SetPositionAndRotation(tavernAt + new Vector3(0f, 2.52f, 2.26f), Quaternion.identity);
            Prim(PrimitiveType.Cube, "Board", tavernSign, Vector3.zero, new Vector3(2.3f, 0.6f, 0.08f), creamPaint, false, null, true);
            WorldText(tavernSign, "sign.tavern", new Vector3(0f, 0f, 0.05f), Quaternion.Euler(0f, 180f, 0f), new Vector2(2.1f, 0.48f), 3f, UiFactory.Ink, -1, 0.8f);
            var table = Kit("Kit_CafeTable", () => LowPolyKit.CafeTable("t", LowPolyKit.Snow));
            var tableRed = Kit("Kit_CafeTableRed", () => LowPolyKit.CafeTable("t", LowPolyKit.Brick));
            foreach (var (x, z, red) in new[] { (-27.4f, -25.6f, false), (-24.0f, -25.0f, true), (-20.6f, -25.6f, false) })
                Piece(places, "Table", red ? tableRed : table, pal, new Vector3(x, 0f, z), x * 7f, 1f, new Vector3(1.0f, 0.8f, 1.0f));
            foreach (var (x, z) in new[] { (-29f, -23.2f), (-19f, -23.2f) })
            {
                Prim(PrimitiveType.Cube, "TerracePole", places, new Vector3(x, 1.5f, z), new Vector3(0.12f, 3f, 0.12f), m.woodDark, true, null, true);
            }
            PartyLights(places, new[] { new Vector3(-26.6f, 2.85f, -28.75f), new Vector3(-29f, 2.95f, -23.2f), new Vector3(-19f, 2.95f, -23.2f), new Vector3(-21.4f, 2.85f, -28.75f) }, 0.35f, 0.7f);
            Notice(places, tavernAt + new Vector3(4.3f, 0f, 3.6f), new Vector3(-12f, 0f, -18f), "notice.harbour.tavern", m);
            AddSecret(places, "tavern", tavernAt + new Vector3(0f, 1f, 2.9f), "secret.tavern", "prompt.knock", 1.4f);
            foreach (var (x, z) in new[] { (-28.6f, -31.6f), (-29.2f, -30.6f) })
                Prop(places, "Barrel", SaveInkedMesh(LowPolyProps.Barrel()), pal, x, z, 0f, 1f, Solid.Capsule, new Vector3(0f, 0.475f, 0f), new Vector3(0.4f, 0.95f, 0f), 0f);

            // ---- a boat on trestles being repainted
            var dryAt = new Vector3(-40f, 0f, -29.6f);
            var trestles = new LowPoly.MeshBuilder();
            foreach (float z in new[] { -1.6f, 1.6f })
            {
                LowPolyKit.Beam(trestles, new Vector3(-0.9f, 0f, z), new Vector3(0f, 1.05f, z), 0.12f, LowPolyKit.DarkWood);
                LowPolyKit.Beam(trestles, new Vector3(0.9f, 0f, z), new Vector3(0f, 1.05f, z), 0.12f, LowPolyKit.DarkWood);
                LowPolyKit.Box(trestles, new Vector3(0f, 1.0f, z), new Vector3(1.6f, 0.12f, 0.16f), LowPolyKit.Wood);
            }
            LowPolyKit.Cylinder(trestles, new Vector3(1.4f, 0f, 2.6f), 0.18f, 0.2f, 0.36f, 8, LowPolyKit.Grey, LowPolyKit.Brick);
            MeshObject("Trestles", places, Kit("Kit_Trestles", () => trestles.ToMesh("t")), pal).transform.position = dryAt;
            var dryBoat = Piece(places, "DryBoat", Kit("Kit_BoatRed", () => LowPolyKit.Boat("b", 6f, 2.2f, LowPolyKit.Brick, LowPolyKit.Snow, true, true, 1)), pal, dryAt + new Vector3(0f, 1.6f, 0f), 0f, 1f, new Vector3(2.0f, 1.2f, 5.6f));
            dryBoat.transform.rotation = Quaternion.Euler(0f, 0f, 4f);
            AddSecret(places, "boat", dryAt + new Vector3(1.7f, 1f, 0f), "secret.boat", "prompt.secret", 1.5f);

            // ---- along the quay edge: lamp posts with party lights between them, bollards, rings, benches, nets
            var promenade = new List<Vector3>();
            foreach (float x in new[] { -30f, -20f, -10f, 10f, 20f, 31f, 40f })
            {
                Lamp(places, art, new Vector3(x, 0f, -9.9f));
                promenade.Add(new Vector3(x, 2.95f, -9.9f));
            }
            for (int i = 0; i + 1 < promenade.Count; i++)
            {
                if (promenade[i].x < 0f && promenade[i + 1].x > 0f) continue;   // the main pier starts here
                PartyLights(places, new[] { promenade[i], promenade[i + 1] }, 0.6f, 0.8f);
            }
            var bollard = Kit("Kit_Bollard", () => LowPolyKit.Bollard("b"));
            for (float x = -43f; x <= 43f; x += 6f)
            {
                if (Mathf.Abs(x) < 4.5f || Mathf.Abs(x - 26f) < 2.5f || (x < -32f && x > -44f)) continue;
                Piece(places, "Bollard", bollard, pal, new Vector3(x, 0f, -8.6f), 0f, 1f, new Vector3(0.6f, 0.7f, 0.6f));
            }
            var tyre = Kit("Kit_Tyre", () =>
            {
                var b = new LowPoly.MeshBuilder();
                LowPolyKit.Cylinder(b, new Vector3(0f, 0f, -0.12f), 0.36f, 0.36f, 0.24f, 10, LowPolyKit.Black, LowPolyKit.Black, Quaternion.Euler(90f, 0f, 0f), true);
                return b.ToMesh("t");
            });
            for (float x = -38f; x <= 38f; x += 9f)
            {
                if (Mathf.Abs(x) < 4f) continue;
                MeshObject("Fender", places, tyre, pal).transform.position = new Vector3(x + 1.5f, -0.55f, -7.82f);
            }
            var ladder = Kit("Kit_QuayLadder", () =>
            {
                var b = new LowPoly.MeshBuilder();
                foreach (float x in new[] { -0.25f, 0.25f }) LowPolyKit.Box(b, new Vector3(x, -0.4f, 0f), new Vector3(0.06f, 1.4f, 0.06f), LowPolyKit.Black);
                for (int i = 0; i < 4; i++) LowPolyKit.Box(b, new Vector3(0f, -0.95f + i * 0.32f, 0f), new Vector3(0.5f, 0.04f, 0.04f), LowPolyKit.Black);
                return b.ToMesh("l");
            });
            foreach (float x in new[] { -24f, 14f, 36f })
                MeshObject("Ladder", places, ladder, pal).transform.position = new Vector3(x, 0f, -7.88f);
            var buoy = Kit("Kit_Lifebuoy", () => LowPolyKit.Lifebuoy("l"));
            foreach (float x in new[] { -8f, 16f, 34f })
                Piece(places, "Lifebuoy", buoy, pal, new Vector3(x, 0f, -9.0f), 180f, 1f, new Vector3(0.2f, 1.6f, 0.2f));
            var bench = Kit("Kit_Bench", () => LowPolyKit.Bench("b"));
            foreach (float x in new[] { -27f, -5f, 15f })
                Piece(places, "Bench", bench, pal, new Vector3(x, 0f, -11.9f), 0f, 1f, new Vector3(1.7f, 0.5f, 0.45f));
            Piece(places, "Anchor", Kit("Kit_Anchor", () => LowPolyKit.Anchor("a")), pal, new Vector3(-29.5f, 0f, -9.3f), 20f, 1f, new Vector3(1.2f, 2f, 0.3f));
            var coil = Kit("Kit_RopeCoil", () => LowPolyKit.RopeCoil("r"));
            foreach (var (x, z) in new[] { (-6.8f, -9.4f), (4.4f, -9.5f), (19.2f, -9.5f), (-31.6f, -15.6f) })
                Piece(places, "RopeCoil", coil, pal, new Vector3(x, 0f, z), x * 30f);
            var nets = Kit("Kit_NetRack", () => LowPolyKit.NetRack("n", 3));
            Piece(places, "NetRack", nets, pal, new Vector3(-26f, 0f, -15.6f), 0f, 1f, new Vector3(3.0f, 1.9f, 0.4f));
            Piece(places, "NetRack", nets, pal, new Vector3(-20f, 0f, -15.6f), 0f, 1f, new Vector3(3.0f, 1.9f, 0.4f));
            foreach (var (x, z, yaw) in new[] { (-5.0f, -14.6f, 10f), (6.4f, -9.6f, 40f), (8.4f, -21.8f, 20f) })
                Piece(places, "FishBox", Kit("Kit_LobsterTrap", () => LowPolyKit.LobsterTrap("t")), pal, new Vector3(x, 0f, z), yaw, 1f, new Vector3(0.8f, 0.5f, 0.55f));

            // more container stacks in the yard beside the tall ones
            int[] colours = { LowPolyKit.Brick, LowPolyKit.Teal, LowPolyKit.Gold, LowPolyKit.Navy, LowPolyKit.Rust };
            Piece(places, "Containers", Kit("Kit_ContainersYard", () => LowPolyKit.ContainerStack("c", new Vector3(2.5f, 2.6f, 6f), 1, colours, 17)), pal,
                new Vector3(-41.5f, 0f, -21.5f), 0f, 1f, new Vector3(2.5f, 2.6f, 6f));

            // ---- in the bay: buoys, ducks, a message in a bottle, a seagull that knows too much and something big below
            var buoyMesh = Kit("Kit_MarkerBuoy", () =>
            {
                var b = new LowPoly.MeshBuilder();
                LowPolyKit.Cylinder(b, new Vector3(0f, -0.3f, 0f), 0.45f, 0.4f, 0.55f, 8, LowPolyKit.Brick, LowPolyKit.Snow);
                LowPolyKit.Cylinder(b, new Vector3(0f, 0.25f, 0f), 0.18f, 0.06f, 0.9f, 6, LowPolyKit.Brick);
                LowPolyKit.Blob(b, new Vector3(0f, 1.2f, 0f), new Vector3(0.12f, 0.12f, 0.12f), LowPolyKit.Gold, 1, 0, 0.02f);
                return b.ToMesh("m");
            });
            foreach (var (x, z) in new[] { (-36f, 8f), (40f, 6f), (-8f, 48f), (24f, 52f), (-40f, 50f) })
            {
                var marker = Piece(places, "Buoy", buoyMesh, pal, new Vector3(x, -0.3f, z), x * 7f);
                marker.isStatic = false;
                var b = marker.AddComponent<Bob>();
                b.height = 0.08f;
                b.swayDegrees = 6f;
            }
            foreach (var (x, z, yaw) in new[] { (11f, 38f, 20f), (-27f, 10f, 140f), (19f, 31f, 300f) })
                AddDuck(places, new Vector3(x, -0.3f, z), yaw, pal);

            var bottle = new GameObject("Bottle").transform;
            bottle.SetParent(places, false);
            bottle.position = new Vector3(-6.6f, 0.25f, 29.3f);
            Prim(PrimitiveType.Cube, "Crate", bottle, new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.7f, 0.8f), m.woodDark, true, null, true);
            Prim(PrimitiveType.Cylinder, "Glass", bottle, new Vector3(0f, 0.95f, 0f), new Vector3(0.22f, 0.26f, 0.22f), glassGreen, false, Quaternion.Euler(0f, 0f, 70f));
            Prim(PrimitiveType.Cylinder, "Neck", bottle, new Vector3(0.28f, 0.95f, 0f), new Vector3(0.09f, 0.12f, 0.09f), glassGreen, false, Quaternion.Euler(0f, 0f, 70f));
            AddSecret(places, "bottle", bottle.position + new Vector3(0f, 0.9f, 0f), "secret.bottle", "prompt.secret", 1.4f, bottle);

            var seagull = new GameObject("Seagull").transform;
            seagull.SetParent(places, false);
            seagull.SetPositionAndRotation(new Vector3(2.5f, 0.25f, 17.5f), Quaternion.Euler(0f, 230f, 0f));
            Prim(PrimitiveType.Cylinder, "Perch", seagull, new Vector3(0f, 0.3f, 0f), new Vector3(0.3f, 0.3f, 0.3f), coalBlack, true);
            Prim(PrimitiveType.Sphere, "Body", seagull, new Vector3(0f, 0.9f, 0f), new Vector3(0.5f, 0.45f, 0.75f), furWhite, false);
            Prim(PrimitiveType.Sphere, "Head", seagull, new Vector3(0f, 1.2f, 0.32f), Vector3.one * 0.3f, furWhite, false);
            Prim(PrimitiveType.Cube, "Beak", seagull, new Vector3(0f, 1.18f, 0.55f), new Vector3(0.08f, 0.07f, 0.2f), buoyOrange, false);
            foreach (float side in new[] { 1f, -1f })
                Prim(PrimitiveType.Cube, "Wing", seagull, new Vector3(side * 0.3f, 0.92f, -0.05f), new Vector3(0.08f, 0.3f, 0.6f), roofSlate, false, Quaternion.Euler(0f, 0f, side * 12f));
            AddSecret(places, "gull", seagull.position + new Vector3(0f, 1f, 0f), "secret.gull", "prompt.secret", 1.3f, seagull);

            var kraken = new GameObject("Kraken").transform;
            kraken.SetParent(places, false);
            kraken.position = new Vector3(33f, -0.3f, 45f);
            for (int arm = 0; arm < 3; arm++)
            {
                var tentacle = new GameObject("Tentacle" + (arm + 1)).transform;
                tentacle.SetParent(kraken, false);
                tentacle.localPosition = new Vector3(arm * 1.8f - 1.8f, 0f, (arm % 2) * 1.2f);
                var bob = tentacle.gameObject.AddComponent<Bob>();
                bob.height = 0.18f;
                bob.swayDegrees = 9f;
                bob.speed = 0.8f + arm * 0.15f;
                // Overlapping spheres that get thinner and curl over: one smooth arm rising out of the water.
                float x = 0f, y = 0f;
                float curl = (arm == 1 ? -1f : 1f) * (38f + arm * 8f);
                for (int seg = 0; seg < 10; seg++)
                {
                    float t = seg / 9f;
                    float radius = Mathf.Lerp(0.55f, 0.14f, t);
                    Prim(PrimitiveType.Sphere, "Segment" + seg, tentacle, new Vector3(x, y, 0f), Vector3.one * radius * 2f, krakenPurple, false);
                    float heading = curl * t * t;
                    x += Mathf.Sin(heading * Mathf.Deg2Rad) * radius * 0.85f;
                    y += Mathf.Cos(heading * Mathf.Deg2Rad) * radius * 0.85f;
                }
            }
            AddSecret(places, "kraken", kraken.position + new Vector3(0f, 0.8f, -2.2f), "secret.kraken", "prompt.wave", 2.2f, kraken);

            // the rocks of the quay's east and west ends, where nobody parks
            var rng = new System.Random(21);
            foreach (var (x, z) in new[] { (-43f, -34f), (43f, -35f), (-44f, -16f), (44f, -12f), (12f, -34.5f), (-12f, -34.6f) })
                PlaceProp(places, art.rocks[rng.Next(art.rocks.Length)], new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f, 0.8f + (float)rng.NextDouble() * 0.6f);
        }
    }
}
