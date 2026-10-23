using System.Collections;
using TMPro;
using UnityEngine;

namespace MixedUp.Trailer
{
    /// <summary>
    /// Little sets built only for the trailer, from the game's own materials: the loading-bay doors with the studio logo
    /// (the opening, like the lift doors of the reference), the truck blowing up, and a heap of boxes for the final picture.
    /// </summary>
    public static class TrailerSets
    {
        static Material Lit(Color colour, float smoothness = 0.25f, float metallic = 0f)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            return material;
        }

        static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        // ------------------------------------------------------------- the doors

        public sealed class Doors
        {
            public Transform root, left, right;
            public Vector3 Centre => root.position + Vector3.up * 1.75f;

            /// <summary>Slides the two halves apart (and the logo with them).</summary>
            public IEnumerator Open(float seconds)
            {
                Vector3 l0 = left.localPosition, r0 = right.localPosition;
                for (float t = 0f; t < seconds; t += Time.deltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                    k = k * k * (3f - 2f * k);
                    left.localPosition = l0 + Vector3.left * (2.35f * k);
                    right.localPosition = r0 + Vector3.right * (2.35f * k);
                    yield return null;
                }
                left.localPosition = l0 + Vector3.left * 2.35f;
                right.localPosition = r0 + Vector3.right * 2.35f;
            }
        }

        /// <summary>
        /// A dark loading bay closed by two sliding metal doors with the studio logo painted across them, and a sign over
        /// them. `position` is the middle of the doorway on the ground; the bay extends towards -forward.
        /// </summary>
        public static Doors BuildDoors(Vector3 position, Vector3 forward, string sign)
        {
            var root = new GameObject("TrailerDoors").transform;
            root.position = position;
            root.rotation = Quaternion.LookRotation(forward, Vector3.up);

            var wall = Lit(new Color(0.16f, 0.16f, 0.17f), 0.15f);
            var floor = Lit(new Color(0.22f, 0.21f, 0.2f), 0.3f);
            var metal = Lit(new Color(0.3f, 0.31f, 0.33f), 0.45f, 0.35f);
            var trim = Lit(new Color(0.85f, 0.65f, 0.12f), 0.3f);

            // the bay: floor, ceiling, side walls and the front wall around the doorway
            Block("Floor", root, new Vector3(0f, -0.035f, -3.5f), new Vector3(8f, 0.1f, 7.4f), floor);
            Block("Ceiling", root, new Vector3(0f, 4.45f, -3.5f), new Vector3(8f, 0.1f, 7.4f), wall);
            Block("WallL", root, new Vector3(-4f, 2.2f, -3.5f), new Vector3(0.1f, 4.5f, 7.4f), wall);
            Block("WallR", root, new Vector3(4f, 2.2f, -3.5f), new Vector3(0.1f, 4.5f, 7.4f), wall);
            Block("WallBack", root, new Vector3(0f, 2.2f, -7.2f), new Vector3(8f, 4.5f, 0.1f), wall);
            Block("FrontL", root, new Vector3(-3.1f, 2.2f, 0.1f), new Vector3(1.8f, 4.5f, 0.2f), wall);
            Block("FrontR", root, new Vector3(3.1f, 2.2f, 0.1f), new Vector3(1.8f, 4.5f, 0.2f), wall);
            Block("FrontTop", root, new Vector3(0f, 3.95f, 0.1f), new Vector3(4.4f, 1.0f, 0.2f), wall);
            Block("Stripe", root, new Vector3(0f, 0.01f, -0.45f), new Vector3(4.4f, 0.02f, 0.25f), trim);

            var logo = Resources.Load<Texture2D>("Trailer/nubi_logo");
            var doors = new Doors { root = root };
            for (int side = -1; side <= 1; side += 2)
            {
                var panel = new GameObject(side < 0 ? "DoorL" : "DoorR").transform;
                panel.SetParent(root, false);
                panel.localPosition = new Vector3(side * 1.1f, 0f, -0.05f);
                Block("Panel", panel, new Vector3(0f, 1.72f, 0f), new Vector3(2.2f, 3.44f, 0.12f), metal);
                Block("Rib", panel, new Vector3(0f, 0.6f, -0.07f), new Vector3(2.1f, 0.06f, 0.02f), wall);
                Block("Rib", panel, new Vector3(0f, 3.0f, -0.07f), new Vector3(2.1f, 0.06f, 0.02f), wall);

                // half of the logo on each door, so it splits down the middle when they open
                if (logo != null)
                {
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.Destroy(quad.GetComponent<Collider>());
                    quad.name = "Logo";
                    quad.transform.SetParent(panel, false);
                    float h = 2.0f, w = h * logo.width / logo.height;
                    quad.transform.localPosition = new Vector3(-side * (1.1f - w * 0.25f), 1.85f, -0.065f);
                    quad.transform.localScale = new Vector3(w * 0.5f, h, 1f);
                    var mat = Lit(Color.white, 0.2f);
                    mat.SetTexture("_BaseMap", logo);
                    mat.SetTextureScale("_BaseMap", new Vector2(0.5f, 1f));
                    mat.SetTextureOffset("_BaseMap", new Vector2(side < 0 ? 0f : 0.5f, 0f));
                    mat.SetFloat("_AlphaClip", 1f);
                    mat.SetFloat("_Cutoff", 0.5f);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    quad.GetComponent<Renderer>().sharedMaterial = mat;
                }

                if (side < 0) doors.left = panel; else doors.right = panel;
            }

            // the sign over the doors, lit by a small lamp
            var signRoot = new GameObject("Sign").transform;
            signRoot.SetParent(root, false);
            signRoot.localPosition = new Vector3(0f, 3.85f, -0.05f);
            Block("SignBoard", signRoot, Vector3.zero, new Vector3(1.9f, 0.42f, 0.06f), Lit(new Color(0.08f, 0.08f, 0.09f), 0.6f));
            var textObject = new GameObject("SignText");
            textObject.transform.SetParent(signRoot, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            var text = textObject.AddComponent<TextMeshPro>();
            text.text = sign;
            text.fontSize = 2.4f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.82f, 0.25f);
            text.rectTransform.sizeDelta = new Vector2(1.8f, 0.4f);
            var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var f in font) if (f.name.Contains("Bangers")) { text.font = f; break; }

            AddLight(root, new Vector3(0f, 4.2f, -1.4f), new Color(1f, 0.86f, 0.7f), 7f, 2.2f);
            AddLight(root, new Vector3(0f, 2.2f, -5.5f), new Color(0.7f, 0.78f, 1f), 9f, 0.6f);
            return doors;
        }

        static Light AddLight(Transform parent, Vector3 local, Color colour, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            return light;
        }

        // ------------------------------------------------------------ explosion

        static Material ParticleMaterial()
        {
            // Borrow the soft additive material the game's own flames use.
            foreach (var r in Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None))
                if (r.sharedMaterial != null && r.gameObject.name.Contains("Flame")) return r.sharedMaterial;
            foreach (var r in Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None))
                if (r.sharedMaterial != null) return r.sharedMaterial;
            return new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        }

        static ParticleSystem Burst(string name, Vector3 at, Material material, int count, Vector2 speed, Vector2 size, Vector2 life,
            Color a, Color b, float gravity, float radius)
        {
            var go = new GameObject(name);
            go.transform.position = at;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            main.maxParticles = count * 2;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var grow = ps.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.6f));
            var drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 1.5f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            ps.Play();
            return ps;
        }

        /// <summary>The truck goes up in a ball of fire: flash, flames, smoke, sparks, the truck tumbling and everybody thrown back.</summary>
        public static IEnumerator BlowUp(Truck truck, ShotContext c)
        {
            var at = truck.transform.position + Vector3.up * 1.4f;
            var material = ParticleMaterial();
            Burst("Fireball", at, material, 90, new Vector2(4f, 11f), new Vector2(1.6f, 3.6f), new Vector2(0.5f, 1.2f),
                new Color(1f, 0.85f, 0.4f), new Color(1f, 0.38f, 0.1f), -0.3f, 1.4f);
            Burst("Smoke", at + Vector3.up, material, 60, new Vector2(1.5f, 5f), new Vector2(2.5f, 5f), new Vector2(1.8f, 3.4f),
                new Color(0.25f, 0.22f, 0.22f, 0.8f), new Color(0.45f, 0.42f, 0.4f, 0.6f), -0.15f, 1.8f);
            Burst("Sparks", at, material, 120, new Vector2(8f, 20f), new Vector2(0.08f, 0.22f), new Vector2(0.6f, 1.6f),
                new Color(1f, 0.95f, 0.6f), new Color(1f, 0.6f, 0.2f), 1.2f, 0.6f);
            var flash = AddLight(truck.transform, Vector3.up * 2f, new Color(1f, 0.6f, 0.25f), 40f, 30f);

            foreach (var actor in c.cast)
            {
                if (actor == null) continue;
                var away = actor.Position - truck.transform.position;
                away.y = 0f;
                actor.Controller.AddImpulse(away.normalized * 11f, 9f);
            }
            AudioManager.Play(SfxId.Collapse, at, 1f, 0.6f);
            AudioManager.Play(SfxId.Zap, at, 1f, 0.5f);

            // the truck jumps, spins and comes down a bit further on
            var t0 = truck.transform.position;
            var r0 = truck.transform.rotation;
            float vy = 9f;
            float y = 0f;
            for (float t = 0f; t < 2.2f; t += Time.deltaTime)
            {
                vy -= 22f * Time.deltaTime;
                y = Mathf.Max(0f, y + vy * Time.deltaTime);
                truck.transform.position = t0 + new Vector3(0f, y, -t * 1.2f);
                truck.transform.rotation = r0 * Quaternion.Euler(Mathf.Min(t, 0.9f) * -40f, 0f, Mathf.Min(t, 0.9f) * 25f);
                flash.intensity = Mathf.Max(0f, 30f * (1f - t / 0.6f));
                yield return null;
            }
        }

        // ------------------------------------------------------------ box heap

        /// <summary>A heap of the game's boxes (models, no gameplay) for the final picture. Returns the height of its top.</summary>
        public static float BoxHeap(ShotContext c, Vector3 at, float yaw)
        {
            var root = new GameObject("BoxHeap").transform;
            root.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            string[] ids = { "normal", "hot", "electric", "frozen", "toxic", "normal", "electric", "hot" };
            Vector3[] spots =
            {
                new Vector3(-1.1f, 0f, 0f), new Vector3(0f, 0f, 0.1f), new Vector3(1.1f, 0f, -0.05f), new Vector3(-0.55f, 1.05f, 0.05f),
                new Vector3(0.55f, 1.05f, 0f), new Vector3(0f, 2.1f, 0f), new Vector3(-2.2f, 0f, 0.4f), new Vector3(2.2f, 0f, 0.3f)
            };
            for (int i = 0; i < ids.Length; i++)
            {
                var data = c.Box(ids[i]);
                if (data == null || data.worldPrefab == null) continue;
                var box = Object.Instantiate(data.worldPrefab, root);
                box.transform.localPosition = spots[i];
                box.transform.localRotation = Quaternion.Euler(0f, (i * 37f) % 24f - 12f, 0f);
            }
            return at.y + 3.15f;
        }
    }
}
