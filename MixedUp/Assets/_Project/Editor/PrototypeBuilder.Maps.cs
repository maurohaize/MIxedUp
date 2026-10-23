using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// What every map shares: the scene set-up (light, truck, players, notes, managers, camera, HUD) and a few helpers. A map
    /// only fills in its ground, scenery, hazards and the spots where boxes can lie; see MapSummit and MapHarbour.
    /// The prototype map (the meadow) keeps its own builder.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        const string SummitScenePath = "Assets/Scenes/Level_Summit.unity";
        const string HarbourScenePath = "Assets/Scenes/Level_Harbour.unity";

        sealed class MapContext
        {
            public Transform env, points, notes;
            public GameAssets a;
            public Mats m;
            public ArtAssets art;
            public Prefabs p;
            public Truck truck;

            /// <summary>Where the truck stands (its dock is behind it, towards -z) and the two characters start next to it.</summary>
            public Vector3 truckPosition = new Vector3(0f, 0f, -26f);
            public Vector3[] noteSpots = System.Array.Empty<Vector3>();
            public readonly List<Vector3> onlineSpots = new List<Vector3>();
            public int easyCount;

            public Vector3 PlayerSpawn => truckPosition + new Vector3(4f, 0.05f, -5f);
            public Vector3 MateSpawn => truckPosition + new Vector3(-4f, 0.05f, -5f);

            public BoxSpawnPoint Classic(string name, string boxId, Vector3 position) =>
                SpawnPoint(points, "Spawn_Classic_" + name, position, boxId, false, true);

            public BoxSpawnPoint Easy(Vector3 position) =>
                SpawnPoint(points, "Spawn_Easy" + (++easyCount).ToString("00"), position);

            public BoxSpawnPoint Hard(string name, Vector3 position) =>
                SpawnPoint(points, "Spawn_Hard_" + name, position, null, true);
        }

        static void BuildMapScene(string scenePath, GameAssets a, Mats m, ArtAssets art, Prefabs p, System.Action<MapContext> fill)
        {
            flatTerrain = true;
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                ConfigureLighting(art);

                var ctx = new MapContext { a = a, m = m, art = art, p = p };
                ctx.env = new GameObject("Environment").transform;
                ctx.points = new GameObject("SpawnPoints").transform;
                ctx.points.SetParent(ctx.env, false);
                ctx.notes = new GameObject("Notes").transform;

                BuildBoundaries(ctx.env);
                ctx.truck = BuildTruck(ctx.env, m, a, art);
                BuildSkyDecor(ctx.env, art);
                fill(ctx);
                ctx.truck.transform.position = ctx.truckPosition;

                var players = new GameObject("Players").transform;
                SpawnCharacter(players, p.player, ctx.PlayerSpawn, a.palette, CharacterCustomization.DefaultSkin, CharacterCustomization.DefaultClothes);
                SpawnCharacter(players, p.teammate, ctx.MateSpawn, a.palette, 3, 9);

                if (ctx.noteSpots.Length >= 3)
                {
                    PlaceNote(ctx.notes, p.loreNote, a.rules, a.hot, a.frozen, ctx.noteSpots[0], "Note_HotFrozen");
                    PlaceNote(ctx.notes, p.loreNote, a.rules, a.hot, a.electric, ctx.noteSpots[1], "Note_HotElectric");
                    PlaceNote(ctx.notes, p.loreNote, a.rules, a.toxic, a.electric, ctx.noteSpots[2], "Note_ToxicElectric");
                }

                var managers = new GameObject("Managers");
                var game = managers.AddComponent<GameManager>();
                game.truck = ctx.truck;
                var puzzle = managers.AddComponent<TruckPuzzleController>();
                puzzle.truck = ctx.truck;
                puzzle.rules = a.rules;
                BuildDirector(managers.transform, a, p, ctx.truck);
                managers.GetComponent<LevelDirector>().onlineSpots = ctx.onlineSpots.Count > 0
                    ? ctx.onlineSpots.ToArray()
                    : new[] { ctx.PlayerSpawn, ctx.MateSpawn, ctx.PlayerSpawn + Vector3.back * 2.5f, ctx.MateSpawn + Vector3.back * 2.5f };

                ConfigureCamera();
                var camera = Camera.main;
                if (camera != null) camera.transform.position = ctx.PlayerSpawn + new Vector3(0f, 4f, -6f);
                BuildHud(a, p, ctx.truck, puzzle);
                BuildEventSystem();

                EditorSceneManager.SaveScene(scene, scenePath);
            }
            finally
            {
                flatTerrain = false;
            }
        }

        // -------------------------------------------------------------- helpers

        /// <summary>A different sky and light for a map: the colours of the horizon, the sun and the ambient light.</summary>
        static void SetMood(ArtAssets art, string skyName, Color top, Color horizon, Color bottom, Color sunColour, float sunIntensity,
            Vector3 sunAngles, Color ambientSky, Color ambientEquator, Color ambientGround)
        {
            string path = MaterialsDir + "/" + skyName + ".mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                sky = new Material(art.sky);
                AssetDatabase.CreateAsset(sky, path);
            }
            sky.SetColor("_TopColor", top);
            sky.SetColor("_HorizonColor", horizon);
            sky.SetColor("_BottomColor", bottom);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                light.color = sunColour;
                light.intensity = sunIntensity;
                light.transform.rotation = Quaternion.Euler(sunAngles);
            }
        }

        /// <summary>A flat tiled floor with a collider: `top` is the walking height.</summary>
        static void FlatGround(Transform parent, string name, float x0, float z0, float x1, float z1, float top, float tile, int[] palette, int seed, ArtAssets art)
        {
            var centre = new Vector3((x0 + x1) * 0.5f, top - 0.5f, (z0 + z1) * 0.5f);
            ColliderBox(name + "Collider", parent, centre, new Vector3(x1 - x0, 1f, z1 - z0));
            MeshObject(name, parent, SaveMesh(LowPoly.Patchwork("Map_" + name + seed, x0, z0, x1, z1, top, tile, palette, seed)), art.palette);
        }

        static GameObject Lamp(Transform parent, ArtAssets art, Vector3 position, float range = 11f, float intensity = 2.6f)
        {
            var lamp = MeshObject("Lamp", parent, lampMesh, art.palette);
            lamp.transform.position = position;
            lamp.isStatic = false;
            AddLampLight(lamp.transform, new Vector3(0f, 2.78f, 0f), range, intensity);
            return lamp;
        }

        /// <summary>A pine with a thin trunk collider, so the forest is solid where it matters.</summary>
        static void Pine(Transform parent, ArtAssets art, int variant, Vector3 position, float scale, bool solid)
        {
            var go = MeshObject("Pine", parent, snowPines[variant % snowPines.Length], art.palette);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, (position.x * 37f + position.z * 17f) % 360f, 0f);
            go.transform.localScale = Vector3.one * scale;
            if (solid)
            {
                var capsule = go.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 1f, 0f);
                capsule.radius = 0.32f;
                capsule.height = 2f;
            }
        }

        /// <summary>Scatters scenery on a rectangle with a seeded generator, skipping anything `blocked` says is taken.</summary>
        static void Scatter(int count, int seed, float x0, float z0, float x1, float z1, System.Func<float, float, bool> blocked, System.Action<float, float, System.Random> place)
        {
            var rng = new System.Random(seed);
            int placed = 0;
            for (int attempt = 0; attempt < count * 8 && placed < count; attempt++)
            {
                float x = Mathf.Lerp(x0, x1, (float)rng.NextDouble());
                float z = Mathf.Lerp(z0, z1, (float)rng.NextDouble());
                if (blocked(x, z)) continue;
                place(x, z, rng);
                placed++;
            }
        }

        static bool NearSpawnPoint(MapContext c, float x, float z, float radius)
        {
            foreach (var spot in c.points.GetComponentsInChildren<BoxSpawnPoint>())
                if (Vector2.Distance(new Vector2(x, z), new Vector2(spot.transform.position.x, spot.transform.position.z)) < radius) return true;
            return false;
        }

        /// <summary>The three hard-to-reach structures every map can borrow (built where the meadow has them, then moved).</summary>
        static Transform BuildChallengeHolder(Transform env, string name, Vector3 offset, System.Action<Transform> build)
        {
            var holder = new GameObject(name).transform;
            holder.SetParent(env, false);
            build(holder);
            holder.position = offset;
            return holder;
        }

        /// <summary>Connects the lights and flags of a borrowed structure to the box spots it holds, so they burn only when a box lies there.</summary>
        static void LinkStructure(Transform structure, Transform points, params string[] spawnNames)
        {
            var lights = structure != null ? structure.GetComponent<SpawnAreaLights>() : null;
            if (lights == null) return;
            var found = new List<BoxSpawnPoint>();
            foreach (var name in spawnNames)
            {
                var t = points.Find(name);
                if (t != null) found.Add(t.GetComponent<BoxSpawnPoint>());
            }
            lights.points = found.ToArray();
        }

        /// <summary>A walk-up ramp between two points (side = width), as a rotated slab with a collider.</summary>
        static GameObject Ramp(Transform parent, string name, Vector3 low, Vector3 high, float width, float thickness, Material material)
        {
            var rotation = Quaternion.LookRotation((high - low).normalized, Vector3.up);
            var centre = (low + high) * 0.5f - rotation * Vector3.up * (thickness * 0.5f);
            return Prim(PrimitiveType.Cube, name, parent, centre, new Vector3(width, thickness, (high - low).magnitude), material, true, rotation, true);
        }
    }
}
