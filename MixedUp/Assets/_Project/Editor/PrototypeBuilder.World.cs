using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// The living world around the delivery route: rolling meadows and dirt trails, a mud swamp, a bonfire camp, a spinning
    /// log, bounce mushrooms to a raised platform, a windmill, a farm, stepping stones across the river, and plenty of props.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        static Vector2 V(float x, float z) => new Vector2(x, z);

        // -------------------------------------------------------------- layout

        /// <summary>Dirt trails as polylines (x, z). They are painted onto the ground and kept clear of trees.</summary>
        static readonly Vector2[][] Trails =
        {
            new[] { V(2f, -33f), V(2f, -20f), V(-4f, -10f), V(-9f, -2f), V(-18f, 2.5f), V(-26f, 4.4f) },                       // start -> west bridge
            new[] { V(-26.8f, 14f), V(-22f, 21.5f), V(-12f, 27f), V(0f, 29f), V(3.2f, 29.6f) },                                // bridge -> hot ring
            new[] { V(-12f, 27f), V(-12.5f, 34f), V(-16f, 38.5f) },                                                             // fork to the mushroom
            new[] { V(9f, 29.6f), V(15f, 27f), V(20f, 23.5f), V(23f, 27f), V(30f, 33f), V(30f, 36f) },                          // ring -> windmill -> electric
            new[] { V(0f, 29.2f), V(1.5f, 36f), V(2.5f, 40.5f) },                                                               // to the farm
            new[] { V(2f, -20f), V(10f, -12f), V(16f, -4f), V(20.5f, 4.6f) },                                                   // start -> stepping stones
            new[] { V(20.5f, 13.4f), V(21f, 18f), V(20f, 23.5f) }                                                               // stones -> windmill
        };

        /// <summary>Places that stay flat (so props and boxes sit right) and keep the random scenery away: centre, radius.</summary>
        static readonly (Vector2 center, float radius)[] WorldSpots =
        {
            (V(-3f, 47f), 7.5f), (V(9f, 48.5f), 8f), (V(-9.5f, 41.5f), 3.5f), (V(14.5f, 43f), 3.5f), (V(-10f, 50.5f), 3.5f),
            (V(3.5f, 42.5f), 4f), (V(-1f, 41f), 2.5f),
            (V(22f, 19f), 4.8f), (V(22f, 26f), 5.8f),
            (V(-18f, 43f), 6.5f), (V(-18f, 38.6f), 2.8f), (V(-18f, 48.5f), 4.5f),
            (V(-17f, 22f), 7f), (V(13.5f, 32f), 7f), (V(14f, -30f), 6.5f), (V(20.5f, 9f), 4f),
            (V(6f, 30f), 5.5f), (V(30f, 38f), 6.5f), (V(-12f, 38f), 3f), (V(24f, 33f), 5.5f), (V(36f, 30f), 5.5f),
            (V(38f, -7f), 6.5f), (V(-37f, 33f), 6.5f), (V(32f, 50f), 7.5f)
        };

        static float DistanceToTrail(float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            foreach (var trail in Trails)
            {
                for (int i = 0; i + 1 < trail.Length; i++)
                {
                    Vector2 a = trail[i], b = trail[i + 1], ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                    best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
                }
            }
            return best;
        }

        static bool InWorldSpot(float x, float z, float margin)
        {
            var p = new Vector2(x, z);
            foreach (var spot in WorldSpots)
                if (Vector2.Distance(p, spot.center) < spot.radius + margin) return true;
            return false;
        }

        /// <summary>Scenery must keep out of trails and the new landmarks.</summary>
        static bool WorldBlocked(float x, float z, float margin) => InWorldSpot(x, z, margin) || DistanceToTrail(x, z) < 2.4f + margin;

        /// <summary>Rolling meadows north of the river; everything near a landmark, the start meadow and the edges stay flat.</summary>
        static float GroundHeight(float x, float z)
        {
            float north = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(15f, 26f, z));
            if (north <= 0f) return 0f;

            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 9f, Mathf.Min(44f - Mathf.Abs(x), 54f - z)));
            float h = (Mathf.PerlinNoise(x * 0.052f + 31f, z * 0.052f + 7f) - 0.5f) * 2f
                      + (Mathf.PerlinNoise(x * 0.13f + 5f, z * 0.13f + 19f) - 0.5f) * 0.5f;
            h *= 0.55f;

            float flat = 1f;
            var p = new Vector2(x, z);
            foreach (var spot in WorldSpots)
                flat = Mathf.Min(flat, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(spot.radius, spot.radius + 5f, Vector2.Distance(p, spot.center))));
            return h * north * edge * flat;
        }

        /// <summary>Palette index of a trail tile, or -1 for plain grass.</summary>
        static int TrailColor(float x, float z)
        {
            float d = DistanceToTrail(x, z);
            if (d > 1.7f) return -1;
            int hash = (Mathf.FloorToInt(x * 0.5f) * 31 + Mathf.FloorToInt(z * 0.5f) * 17) & 3;
            return d < 0.9f ? (hash == 0 ? 2 : 1) : (hash == 0 ? 1 : 20);
        }

        // -------------------------------------------------------------- helpers

        sealed class WorldMeshes
        {
            public Mesh hay, haystack, barrel, crate, lamp, log, mushroom, mushroomSmall, sweeperPost, sweeperArm, tent, campfire, bonfire;
            public Mesh windmill, blades, farmhouse, barn, well, scarecrow, cropRow;
            public Mesh[] mud, stones, lilies;
        }

        static Mesh mushroomMesh;

        static WorldMeshes CreateWorldMeshes()
        {
            var meshes = CreateWorldMeshesCore();
            mushroomMesh = meshes.mushroom;
            return meshes;
        }

        static WorldMeshes CreateWorldMeshesCore() => new WorldMeshes
        {
            hay = SaveInkedMesh(LowPolyProps.HayBale()),
            haystack = SaveInkedMesh(LowPolyProps.Haystack()),
            barrel = SaveInkedMesh(LowPolyProps.Barrel()),
            crate = SaveInkedMesh(LowPolyProps.Crate()),
            lamp = SaveInkedMesh(LowPolyProps.LampPost()),
            log = SaveInkedMesh(LowPolyProps.Log()),
            mushroom = SaveInkedMesh(LowPolyProps.Mushroom("Prop_Mushroom")),
            mushroomSmall = SaveInkedMesh(LowPolyProps.Mushroom("Prop_MushroomSmall")),
            sweeperPost = SaveInkedMesh(LowPolyProps.SweeperPost()),
            sweeperArm = SaveInkedMesh(LowPolyProps.SweeperArm()),
            tent = SaveInkedMesh(LowPolyProps.Tent()),
            campfire = SaveInkedMesh(LowPolyProps.Campfire("Prop_Campfire", 1f)),
            bonfire = SaveInkedMesh(LowPolyProps.Campfire("Prop_Bonfire", 1.45f)),
            windmill = SaveInkedMesh(LowPolyProps.Windmill()),
            blades = SaveInkedMesh(LowPolyProps.WindmillBlades()),
            farmhouse = SaveInkedMesh(LowPolyProps.Farmhouse()),
            barn = SaveInkedMesh(LowPolyProps.Barn()),
            well = SaveInkedMesh(LowPolyProps.Well()),
            scarecrow = SaveInkedMesh(LowPolyProps.Scarecrow()),
            cropRow = SaveInkedMesh(LowPolyProps.CropRow()),
            mud = new[] { SaveMesh(LowPolyProps.MudPatch(3.1f, 1)), SaveMesh(LowPolyProps.MudPatch(2.6f, 2)), SaveMesh(LowPolyProps.MudPatch(2.2f, 3)) },
            stones = new[] { SaveInkedMesh(LowPolyProps.StepStone(1)), SaveInkedMesh(LowPolyProps.StepStone(2)), SaveInkedMesh(LowPolyProps.StepStone(3)) },
            lilies = new[] { SaveMesh(LowPolyProps.Lily(1)), SaveMesh(LowPolyProps.Lily(2)) }
        };

        enum Solid { None, Box, Capsule }

        /// <summary>Puts a prop on the (rolling) ground. y is taken from GroundHeight unless yOverride is given.</summary>
        static GameObject Prop(Transform parent, string name, Mesh mesh, Material material, float x, float z, float yaw, float scale = 1f,
            Solid solid = Solid.None, Vector3 center = default, Vector3 size = default, float? yOverride = null, bool isStatic = true)
        {
            var go = MeshObject(name, parent, mesh, material);
            go.transform.position = new Vector3(x, yOverride ?? GroundHeight(x, z), z);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;

            if (solid == Solid.Box)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = center;
                box.size = size;
            }
            else if (solid == Solid.Capsule)
            {
                var capsule = go.AddComponent<CapsuleCollider>();
                capsule.center = center;
                capsule.radius = size.x;
                capsule.height = size.y;
            }
            go.isStatic = isStatic;
            return go;
        }

        static void HazardSphere(Transform parent, string name, Vector3 position, float radius, HazardType type)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = radius;
            sphere.isTrigger = true;
            go.AddComponent<HazardZone>().type = type;
        }

        /// <summary>A rail fence between two points, with posts every ~2.2 m.</summary>
        static void FenceRun(Transform parent, Mats m, Vector3 from, Vector3 to)
        {
            Vector3 along = to - from;
            float length = along.magnitude;
            int segments = Mathf.Max(1, Mathf.RoundToInt(length / 2.2f));
            var rotation = Quaternion.LookRotation(along.normalized);

            for (int i = 0; i <= segments; i++)
            {
                Vector3 post = Vector3.Lerp(from, to, i / (float)segments);
                post.y = GroundHeight(post.x, post.z);
                Prim(PrimitiveType.Cube, "FencePost", parent, post + Vector3.up * 0.6f, new Vector3(0.2f, 1.2f, 0.2f), m.woodDark, true, null, true);
            }
            for (int i = 0; i < segments; i++)
            {
                Vector3 a = Vector3.Lerp(from, to, i / (float)segments), b = Vector3.Lerp(from, to, (i + 1) / (float)segments);
                Vector3 mid = (a + b) * 0.5f;
                mid.y = GroundHeight(mid.x, mid.z);
                float segLength = Vector3.Distance(a, b);
                Prim(PrimitiveType.Cube, "FenceRailHigh", parent, mid + Vector3.up * 0.95f, new Vector3(0.1f, 0.12f, segLength), m.wood, false, rotation);
                Prim(PrimitiveType.Cube, "FenceRailLow", parent, mid + Vector3.up * 0.55f, new Vector3(0.1f, 0.12f, segLength), m.wood, false, rotation);
            }
        }

        // ---------------------------------------------------------------- world

        static void BuildWorld(Transform env, Mats m, ArtAssets art)
        {
            var world = new GameObject("World").transform;
            world.SetParent(env, false);
            var wm = CreateWorldMeshes();
            var mat = art.palette;
            var rng = new System.Random(4242);

            BuildStartCamp(world, wm, mat, m);
            BuildMudSwamp(world, wm, mat);
            BuildBonfireCamp(world, wm, mat);
            BuildSweeper(world, wm, mat, m, art);
            BuildMushroomPlatform(world, wm, mat, m);
            BuildWindmill(world, wm, mat);
            BuildFarm(world, wm, mat, m, art);
            BuildStepStones(world, wm, mat);
            BuildLampsAndSigns(world, wm, mat, art);
            BuildHurdles(world, wm, mat);
            BuildDecorMushrooms(world, wm, mat, rng);
            BuildTrailFlowers(world, art, rng);
            BuildSecrets(world, wm, mat, m, art);
        }

        /// <summary>Wild flowers along the trails, so the paths read as paths from afar.</summary>
        static void BuildTrailFlowers(Transform world, ArtAssets art, System.Random rng)
        {
            var flowers = new GameObject("TrailFlowers").transform;
            flowers.SetParent(world, false);
            int placed = 0;
            for (int attempt = 0; attempt < 2000 && placed < 260; attempt++)
            {
                var trail = Trails[rng.Next(Trails.Length)];
                int i = rng.Next(trail.Length - 1);
                Vector2 a = trail[i], b = trail[i + 1], dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                Vector2 p = a + (b - a) * (float)rng.NextDouble() + side * ((rng.Next(2) == 0 ? 1f : -1f) * (1.9f + (float)rng.NextDouble() * 1.6f));
                if (p.x < -42f || p.x > 42f || p.y < -33f || p.y > 53f) continue;
                if (p.y > 3f && p.y < 15f) continue;                       // river
                if (p.x > -41f && p.x < -8f && p.y > -28f && p.y < 0f) continue;   // ice hill
                if (InWorldSpot(p.x, p.y, 0.5f) || DistanceToTrail(p.x, p.y) < 1.6f) continue;

                var flower = MeshObject("Flower", flowers, art.flowers[rng.Next(art.flowers.Length)], art.palette, false);
                flower.transform.position = new Vector3(p.x, GroundHeight(p.x, p.y), p.y);
                flower.transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1.8f, (float)rng.NextDouble());
                placed++;
            }
        }

        static void BuildStartCamp(Transform world, WorldMeshes wm, Material mat, Mats m)
        {
            var camp = new GameObject("StartCamp").transform;
            camp.SetParent(world, false);

            Prop(camp, "Tent", wm.tent, mat, 16.8f, -30.2f, -35f, 1f, Solid.Box, new Vector3(0f, 0.8f, 0f), new Vector3(2.6f, 1.6f, 2.6f));
            var campfire = Prop(camp, "Campfire", wm.campfire, mat, 12.2f, -29.6f, 0f);
            AddFireFx(campfire.transform, new Vector3(0f, 0.25f, 0f), 1f);
            Prop(camp, "SeatA", wm.log, mat, 12.2f, -31.7f, 0f, 0.7f, Solid.Box, new Vector3(0f, 0.28f, 0f), new Vector3(2.3f, 0.6f, 0.6f));
            Prop(camp, "SeatB", wm.log, mat, 10.1f, -28.6f, 72f, 0.7f, Solid.Box, new Vector3(0f, 0.28f, 0f), new Vector3(2.3f, 0.6f, 0.6f));
            Prop(camp, "SeatC", wm.log, mat, 14.3f, -27.7f, -62f, 0.7f, Solid.Box, new Vector3(0f, 0.28f, 0f), new Vector3(2.3f, 0.6f, 0.6f));
            Prop(camp, "Barrel", wm.barrel, mat, 18.9f, -27.4f, 10f, 1f, Solid.Capsule, new Vector3(0f, 0.475f, 0f), new Vector3(0.4f, 0.95f, 0f));
            Prop(camp, "Barrel", wm.barrel, mat, 19.8f, -28.5f, 40f, 1f, Solid.Capsule, new Vector3(0f, 0.475f, 0f), new Vector3(0.4f, 0.95f, 0f));
            Prop(camp, "Crate", wm.crate, mat, 14.6f, -33.4f, 25f, 1f, Solid.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.95f));
            Prop(camp, "Crate", wm.crate, mat, 15.8f, -33.2f, -15f, 0.85f, Solid.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.95f));
            var startLamp = Prop(camp, "Lamp", wm.lamp, mat, 10.3f, -25.6f, 0f, 1f, Solid.Capsule, new Vector3(0f, 1.5f, 0f), new Vector3(0.12f, 3f, 0f));
            AddLampLight(startLamp.transform, new Vector3(0f, 2.78f, 0f));
        }

        static void BuildMudSwamp(Transform world, WorldMeshes wm, Material mat)
        {
            var swamp = new GameObject("MudSwamp").transform;
            swamp.SetParent(world, false);

            var spots = new[] { (V(-17.2f, 22.4f), 3.1f, 0), (V(-13.4f, 25.2f), 2.6f, 1), (V(-21.2f, 19.4f), 2.2f, 2) };
            foreach (var (position, radius, index) in spots)
            {
                Prop(swamp, "Mud", wm.mud[index], mat, position.x, position.y, index * 70f);
                HazardSphere(swamp, "MudZone", new Vector3(position.x, 0.05f, position.y), radius * 0.9f, HazardType.Mud);
            }

            // Reeds and a dead log at the edge make the swamp readable from afar.
            Prop(swamp, "Log", wm.log, mat, -15.3f, 18.8f, 25f, 0.9f, Solid.Box, new Vector3(0f, 0.3f, 0f), new Vector3(2.8f, 0.7f, 0.7f));
        }

        static void BuildBonfireCamp(Transform world, WorldMeshes wm, Material mat)
        {
            var camp = new GameObject("BonfireCamp").transform;
            camp.SetParent(world, false);

            const float x = 12.2f, z = 32.2f;
            var bonfire = Prop(camp, "Bonfire", wm.bonfire, mat, x, z, 0f);
            AddFireFx(bonfire.transform, new Vector3(0f, 0.35f, 0f), 1.6f);
            HazardSphere(camp, "FireZone", new Vector3(x, GroundHeight(x, z) + 0.35f, z), 1.25f, HazardType.Fire);

            Prop(camp, "SeatA", wm.log, mat, x - 3.3f, z + 0.3f, 90f, 0.8f, Solid.Box, new Vector3(0f, 0.3f, 0f), new Vector3(2.6f, 0.7f, 0.7f));
            Prop(camp, "SeatB", wm.log, mat, x + 3.2f, z - 0.6f, 100f, 0.8f, Solid.Box, new Vector3(0f, 0.3f, 0f), new Vector3(2.6f, 0.7f, 0.7f));
            Prop(camp, "Tent", wm.tent, mat, x + 4.4f, z + 3.6f, 150f, 1.15f, Solid.Box, new Vector3(0f, 0.9f, 0f), new Vector3(3f, 1.8f, 3f));
            Prop(camp, "Crate", wm.crate, mat, x - 2.4f, z + 3.6f, 15f, 1f, Solid.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.95f));
            Prop(camp, "Crate", wm.crate, mat, x - 3.5f, z + 4.1f, -20f, 1f, Solid.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.95f));
            Prop(camp, "Barrel", wm.barrel, mat, x - 1.3f, z + 4.5f, 0f, 1f, Solid.Capsule, new Vector3(0f, 0.475f, 0f), new Vector3(0.4f, 0.95f, 0f));
        }

        static void BuildSweeper(Transform world, WorldMeshes wm, Material mat, Mats m, ArtAssets art)
        {
            var root = new GameObject("Sweeper");
            root.transform.SetParent(world, false);
            const float x = 22f, z = 26f;
            root.transform.position = new Vector3(x, GroundHeight(x, z), z);

            var post = MeshObject("Post", root.transform, wm.sweeperPost, mat);
            var postCollider = post.AddComponent<CapsuleCollider>();
            postCollider.center = new Vector3(0f, 0.7f, 0f);
            postCollider.radius = 0.55f;
            postCollider.height = 1.4f;

            var arm = MeshObject("Arm", root.transform, wm.sweeperArm, mat);
            arm.isStatic = false;

            var sweeper = root.AddComponent<Sweeper>();
            sweeper.arm = arm.transform;
            sweeper.halfExtents = new Vector3(3.2f, 0.3f, 0.3f);
            sweeper.height = 0.55f;
            BuildSweeperSign(world, sweeper, m, new Vector3(18.6f, GroundHeight(18.6f, 28.8f), 28.8f), 131f);

            // A few big rocks flank the trail so the sweeper feels like a gate.
            PlaceProp(world, art.rocks[1], new Vector3(x - 5.6f, GroundHeight(x - 5.6f, z + 4f), z + 4f), 40f, 1.5f);
            PlaceProp(world, art.rocks[5], new Vector3(x + 5.5f, GroundHeight(x + 5.5f, z - 3.8f), z - 3.8f), 200f, 1.4f);
        }

        /// <summary>A stone platform that holds the toxic box, reached by a springy mushroom or by a ramp round the back.</summary>
        static void BuildMushroomPlatform(Transform world, WorldMeshes wm, Material mat, Mats m)
        {
            var group = new GameObject("MushroomPlatform").transform;
            group.SetParent(world, false);

            const float top = 2.4f;
            Prim(PrimitiveType.Cube, "Platform", group, new Vector3(-18f, top * 0.5f, 43f), new Vector3(5f, top, 5f), m.stone, true, null, true);
            Prim(PrimitiveType.Cube, "PlatformCap", group, new Vector3(-18f, top + 0.06f, 43f), new Vector3(5.3f, 0.12f, 5.3f), m.woodDark, false);

            // The walk-up ramp on the north side.
            var low = new Vector3(-18f, 0f, 50.6f);
            var high = new Vector3(-18f, top, 45.5f);
            var rotation = Quaternion.LookRotation((high - low).normalized, Vector3.up);
            const float thickness = 0.4f;
            var center = (low + high) * 0.5f - rotation * Vector3.up * (thickness * 0.5f);
            Prim(PrimitiveType.Cube, "Ramp", group, center, new Vector3(3f, thickness, (high - low).magnitude), m.wood, true, rotation, true);

            // The bounce pad.
            var pad = Prop(group, "BounceMushroom", wm.mushroom, mat, -18f, 38.6f, 0f, 1f, Solid.None, default, default, null, false);
            var bounce = pad.AddComponent<BouncePad>();
            bounce.launchSpeed = 14.5f;
            bounce.radius = 0.95f;
            bounce.capHeight = 1.1f;
            bounce.squashed = pad.transform;
        }

        static void BuildWindmill(Transform world, WorldMeshes wm, Material mat)
        {
            const float x = 22f, z = 19f;
            var tower = Prop(world, "Windmill", wm.windmill, mat, x, z, 180f, 1f, Solid.Capsule, new Vector3(0f, 3.5f, 0f), new Vector3(2.0f, 7f, 0f));
            AddWallLantern(tower.transform, new Vector3(0f, 2.4f, 1.9f), Vector3.forward, 9f, 2.2f);

            var blades = MeshObject("Blades", tower.transform, wm.blades, mat);
            blades.transform.localPosition = new Vector3(0f, 5.1f, 2.35f);
            blades.isStatic = false;
            var spin = blades.AddComponent<Spin>();
            spin.axis = Vector3.forward;
            spin.degreesPerSecond = 16f;
        }

        static void BuildFarm(Transform world, WorldMeshes wm, Material mat, Mats m, ArtAssets art)
        {
            var farm = new GameObject("Farm").transform;
            farm.SetParent(world, false);

            var house = Prop(farm, "Farmhouse", wm.farmhouse, mat, -3f, 47f, 180f, 1f, Solid.Box, new Vector3(0f, 1.6f, 0f), new Vector3(6.6f, 3.2f, 5f));
            AddWallLantern(house.transform, new Vector3(1.2f, 2.3f, 2.5f), Vector3.forward, 9f, 2.2f);
            var barn = Prop(farm, "Barn", wm.barn, mat, 9f, 48.5f, 200f, 1f, Solid.Box, new Vector3(0f, 2.1f, 0f), new Vector3(7.6f, 4.2f, 6.2f));
            AddWallLantern(barn.transform, new Vector3(1.6f, 2.8f, 3.1f), Vector3.forward, 10f, 2.2f);
            Prop(farm, "Well", wm.well, mat, -9.5f, 41.5f, 0f, 1f, Solid.Capsule, new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 0f));
            Prop(farm, "Haystack", wm.haystack, mat, 14.5f, 43f, 0f, 1f, Solid.Capsule, new Vector3(0f, 1f, 0f), new Vector3(1.4f, 2.4f, 0f));
            Prop(farm, "Haystack", wm.haystack, mat, -10f, 50.5f, 30f, 0.85f, Solid.Capsule, new Vector3(0f, 1f, 0f), new Vector3(1.4f, 2.4f, 0f));
            Prop(farm, "Scarecrow", wm.scarecrow, mat, -1f, 41f, 160f, 1f, Solid.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.3f, 2f, 0f));

            var bales = new[] { (3f, 42.4f, 20f), (4.4f, 42.8f, 80f), (3.6f, 44.0f, 5f), (11f, 42f, 60f), (-6f, 52f, 35f), (-5f, 53f, 10f) };
            foreach (var (bx, bz, yaw) in bales)
                Prop(farm, "HayBale", wm.hay, mat, bx, bz, yaw, 1f, Solid.Box, new Vector3(0f, 0.55f, 0f), new Vector3(1.1f, 1.1f, 1.1f));

            foreach (var z in new[] { 51.4f, 52.8f, 54.2f })
                Prop(farm, "CropRow", wm.cropRow, mat, 1.6f, z, 0f);

            Prop(farm, "Barrel", wm.barrel, mat, 0.9f, 43.6f, 0f, 1f, Solid.Capsule, new Vector3(0f, 0.475f, 0f), new Vector3(0.4f, 0.95f, 0f));
            Prop(farm, "Barrel", wm.barrel, mat, 1.8f, 43.9f, 40f, 1f, Solid.Capsule, new Vector3(0f, 0.475f, 0f), new Vector3(0.4f, 0.95f, 0f));
            Prop(farm, "Crate", wm.crate, mat, 5.8f, 44.2f, 15f, 1f, Solid.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.95f));
            Prop(farm, "Crate", wm.crate, mat, 6.9f, 44.0f, -10f, 1f, Solid.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.95f));

            // The yard fence, with a gap where the trail comes in.
            FenceRun(farm, m, new Vector3(-12.5f, 0f, 39.5f), new Vector3(0.5f, 0f, 39.5f));
            FenceRun(farm, m, new Vector3(4.6f, 0f, 39.5f), new Vector3(15.5f, 0f, 39.5f));
            FenceRun(farm, m, new Vector3(-12.5f, 0f, 39.5f), new Vector3(-12.5f, 0f, 53f));
            FenceRun(farm, m, new Vector3(15.5f, 0f, 39.5f), new Vector3(15.5f, 0f, 53f));
        }

        /// <summary>Three stones in the river east of the start: a shortcut that is dangerous for anyone carrying electricity.</summary>
        static void BuildStepStones(Transform world, WorldMeshes wm, Material mat)
        {
            var stones = new GameObject("StepStones").transform;
            stones.SetParent(world, false);

            var spots = new[] { (20.5f, 6.4f), (21.3f, 8.8f), (20.2f, 11.2f) };
            for (int i = 0; i < spots.Length; i++)
                Prop(stones, "StepStone", wm.stones[i % wm.stones.Length], mat, spots[i].Item1, spots[i].Item2, i * 55f, 1f,
                    Solid.Box, new Vector3(0f, 0.275f, 0f), new Vector3(1.7f, 0.55f, 1.7f), -0.45f);

            var lilies = new[] { (23.6f, 7.2f), (24.9f, 9.4f), (22.8f, 10.6f), (17.4f, 8.1f), (16.3f, 10.3f), (24.2f, 6.1f) };
            for (int i = 0; i < lilies.Length; i++)
            {
                var lily = Prop(stones, "Lily", wm.lilies[i % 2], mat, lilies[i].Item1, lilies[i].Item2, i * 40f, 1f, Solid.None, default, default, -0.1f, false);
                var bob = lily.AddComponent<Bob>();
                bob.height = 0.025f;
                bob.swayDegrees = 3f;
            }
        }

        static void BuildLampsAndSigns(Transform world, WorldMeshes wm, Material mat, ArtAssets art)
        {
            var props = new GameObject("TrailProps").transform;
            props.SetParent(world, false);

            // Lamps along the long trails, alternating sides.
            int count = 0;
            foreach (var trail in new[] { Trails[0], Trails[1], Trails[3], Trails[5] })
            {
                float carried = 6f;
                for (int i = 0; i + 1 < trail.Length; i++)
                {
                    Vector2 a = trail[i], b = trail[i + 1], dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                    float length = Vector2.Distance(a, b);
                    for (float d = carried; d < length; d += 11f)
                    {
                        Vector2 p = a + dir * d + side * (count++ % 2 == 0 ? 2.3f : -2.3f);
                        if (p.y > 2f && p.y < 16f) continue;                       // river and bridge
                        if (p.y > -8f && p.y < 1f && p.x < -3f) continue;           // the ice hill
                        if (p.x < -40f || p.x > 40f || p.y < -32f || p.y > 52f) continue;
                        if (Vector2.Distance(p, V(22f, 26f)) < 5f) continue;        // keep the sweeper's gate clear
                        var lamp = Prop(props, "Lamp", wm.lamp, mat, p.x, p.y, 0f, 1f, Solid.Capsule, new Vector3(0f, 1.5f, 0f), new Vector3(0.12f, 3f, 0f));
                        AddLampLight(lamp.transform, new Vector3(0f, 2.78f, 0f));
                    }
                    carried = 6f;
                }
            }

            // Signposts at the forks.
            foreach (var (x, z, yaw) in new[] { (-10.5f, 29.5f, 160f), (3.6f, 26.2f, -20f), (17f, 24.8f, 230f), (-24.4f, 17.5f, 200f) })
                PlaceProp(props, art.cartel, new Vector3(x, GroundHeight(x, z), z), yaw, 1f);
        }

        static void BuildHurdles(Transform world, WorldMeshes wm, Material mat)
        {
            var hurdles = new GameObject("Hurdles").transform;
            hurdles.SetParent(world, false);
            foreach (var (x, z, yaw) in new[] { (-14.2f, 2.3f, 80f), (-24.3f, 21.2f, 130f), (18f, 25.4f, 70f), (-12.8f, 31.2f, 100f) })
                Prop(hurdles, "Hurdle", wm.log, mat, x, z, yaw, 1f, Solid.Box, new Vector3(0f, 0.4f, 0f), new Vector3(3.2f, 0.8f, 0.8f));
        }

        /// <summary>Tiny harmless mushrooms in the forests (the big ones are bounce pads).</summary>
        static void BuildDecorMushrooms(Transform world, WorldMeshes wm, Material mat, System.Random rng)
        {
            var group = new GameObject("Toadstools").transform;
            group.SetParent(world, false);
            int placed = 0;
            for (int attempt = 0; attempt < 400 && placed < 26; attempt++)
            {
                float x = Mathf.Lerp(-41f, 41f, (float)rng.NextDouble());
                float z = Mathf.Lerp(16f, 52f, (float)rng.NextDouble());
                if (WorldBlocked(x, z, 1f) || !Free(x, z, 1f)) continue;
                for (int k = 0; k < 3; k++)
                {
                    float px = x + (float)(rng.NextDouble() - 0.5) * 1.4f, pz = z + (float)(rng.NextDouble() - 0.5) * 1.4f;
                    Prop(group, "Toadstool", wm.mushroomSmall, mat, px, pz, (float)rng.NextDouble() * 360f, 0.22f + (float)rng.NextDouble() * 0.16f);
                }
                placed++;
            }
        }
    }
}
