using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// What the two extra maps share to feel like places: flat-colour materials for the odd primitive, secrets to find (like the
    /// ducks, the log and the snowman of the first map), rubber ducks and gusts of wind. Each map's own dressing lives in its file.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        static Material roofRed, roofSlate, clothRed, creamPaint, furWhite, darkWater, buoyOrange, coalBlack, fishBlue, krakenPurple, glassGreen;

        static void EnsureExtraMaterials()
        {
            if (roofRed != null) return;
            roofRed = Mat("RoofRed", new Color(0.6f, 0.25f, 0.2f));
            roofSlate = Mat("RoofSlate", new Color(0.3f, 0.32f, 0.37f));
            clothRed = Mat("ClothRed", new Color(0.78f, 0.22f, 0.2f));
            creamPaint = Mat("CreamPaint", new Color(0.93f, 0.88f, 0.74f));
            furWhite = Mat("FurWhite", new Color(0.8f, 0.87f, 0.95f), 0.1f);
            darkWater = Mat("DarkWater", new Color(0.09f, 0.19f, 0.25f), 0.7f);
            buoyOrange = Mat("BuoyOrange", new Color(0.95f, 0.5f, 0.14f));
            coalBlack = Mat("Coal", new Color(0.17f, 0.16f, 0.17f));
            fishBlue = Mat("FishBlue", new Color(0.5f, 0.68f, 0.8f), 0.4f);
            krakenPurple = Mat("KrakenPurple", new Color(0.46f, 0.22f, 0.5f), 0.35f);
            glassGreen = Mat("GlassGreen", new Color(0.4f, 0.62f, 0.45f), 0.8f);
        }

        // ------------------------------------------------------------- secrets and effects

        static SecretSpot AddSecret(Transform parent, string id, Vector3 position, string toastKey, string promptKey = "prompt.secret",
            float radius = 1.6f, Transform reaction = null)
        {
            var go = new GameObject("Secret_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            var spot = go.AddComponent<SecretSpot>();
            spot.id = id;
            spot.promptKey = promptKey;
            spot.toastKey = toastKey;
            spot.reaction = reaction;
            return spot;
        }

        static RubberDuck AddDuck(Transform parent, Vector3 position, float yaw, Material material)
        {
            if (duckMesh == null) duckMesh = SaveInkedMesh(LowPolyProps.Duck());
            var duck = new GameObject("RubberDuck");
            duck.transform.SetParent(parent, false);
            duck.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var visual = MeshObject("Visual", duck.transform, duckMesh, material);
            visual.isStatic = false;
            var trigger = duck.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;
            trigger.center = Vector3.up * 0.3f;
            var script = duck.AddComponent<RubberDuck>();
            script.visual = visual.transform;
            return script;
        }

        /// <summary>A patch of ground swept by gusts of wind, with streaks of wind drawn while it blows.</summary>
        static void AddGust(Transform parent, Vector3 position, Vector3 direction, Vector3 halfExtents)
        {
            var gust = new GameObject("GustZone");
            gust.transform.SetParent(parent, false);
            gust.transform.position = position;
            var zone = gust.AddComponent<GustZone>();
            zone.direction = direction;
            zone.halfExtents = halfExtents;

            var wind = NewParticles("Wind", gust.transform, fxSoft);
            Vector3 along = direction.normalized;
            wind.transform.localPosition = -along * halfExtents.x;
            wind.transform.localRotation = Quaternion.LookRotation(along);
            var main = wind.main;
            main.loop = true;
            main.startLifetime = 0.9f;
            main.startSpeed = 12f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new Color(1f, 1f, 1f, 0.55f);
            main.maxParticles = 120;
            var emission = wind.emission;
            emission.enabled = false;
            var shape = wind.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(halfExtents.z * 1.6f, 2.4f, 0.2f);
            var windRenderer = wind.GetComponent<ParticleSystemRenderer>();
            windRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            windRenderer.lengthScale = 9f;
            zone.wind = wind;
        }

        /// <summary>A wooden arch over the way in, with the map's name on a plank (read from the side of `facing`).</summary>
        static void EntranceArch(Transform parent, Vector3 centre, float yaw, float halfWidth, string nameKey, bool snow, ArtAssets art)
        {
            var root = new GameObject("EntranceArch").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(centre, Quaternion.Euler(0f, yaw, 0f));
            var mesh = Kit("Kit_Arch_" + Dm(halfWidth) + (snow ? "_snow" : ""), () =>
            {
                var b = new LowPoly.MeshBuilder();
                foreach (float side in new[] { -1f, 1f })
                {
                    LowPolyKit.Box(b, new Vector3(side * halfWidth, 1.9f, 0f), new Vector3(0.36f, 3.8f, 0.36f), LowPolyKit.DarkWood, LowPolyKit.Tan);
                    LowPolyKit.Blob(b, new Vector3(side * halfWidth, 0.15f, 0f), new Vector3(0.6f, 0.3f, 0.6f), LowPolyKit.Stone, 4, 1, 0.2f);
                    LowPolyKit.Beam(b, new Vector3(side * halfWidth, 2.9f, 0f), new Vector3(side * (halfWidth - 1.0f), 3.75f, 0f), 0.16f, LowPolyKit.DarkWood);
                }
                LowPolyKit.Box(b, new Vector3(0f, 3.9f, 0f), new Vector3(halfWidth * 2f + 1.0f, 0.3f, 0.4f), LowPolyKit.DarkWood, snow ? LowPolyKit.Snow : LowPolyKit.Tan);
                LowPolyKit.Box(b, new Vector3(0f, 4.5f, 0f), new Vector3(halfWidth * 1.5f, 0.9f, 0.12f), LowPolyKit.Wood);
                if (snow) LowPolyKit.Box(b, new Vector3(0f, 4.98f, 0f), new Vector3(halfWidth * 1.5f + 0.1f, 0.08f, 0.2f), LowPolyKit.Snow, LowPolyKit.Snow);
                return b.ToMesh("a");
            });
            MeshObject("Arch", root, mesh, art.palette);
            foreach (float side in new[] { -1f, 1f })
            {
                var post = new GameObject("PostCollider");
                post.transform.SetParent(root, false);
                post.transform.localPosition = new Vector3(side * halfWidth, 1.9f, 0f);
                post.AddComponent<BoxCollider>().size = new Vector3(0.4f, 3.8f, 0.4f);
            }
            foreach (float face in new[] { 1f, -1f })
                WorldText(root, nameKey, new Vector3(0f, 4.5f, face * 0.07f), Quaternion.Euler(0f, face > 0f ? 180f : 0f, 0f), new Vector2(halfWidth * 1.4f, 0.75f), 6f, UiFactory.Ink, -1, 1f);
        }
    }
}
