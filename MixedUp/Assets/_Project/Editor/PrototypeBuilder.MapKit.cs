using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// The tool box the extra maps are dressed with: readable notice boards and arrow signs, lit windows, chimney smoke, strings
    /// of party lights, a road that comes in from the hills to the truck, and forests and villages on the slopes outside.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        static Material windowGlow, lanternGlass, toxicGlow, bulbGlow, bulbRed;

        static void EnsureKitMaterials()
        {
            EnsureExtraMaterials();
            if (windowGlow != null) return;
            windowGlow = FxMaterial("WindowGlow", "Universal Render Pipeline/Lit", mat =>
            {
                mat.SetColor("_BaseColor", new Color(1f, 0.78f, 0.42f));
                mat.SetColor("_EmissionColor", new Color(1.6f, 1.05f, 0.42f));
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
            lanternGlass = FxMaterial("LanternGlass", "Universal Render Pipeline/Lit", mat =>
            {
                mat.SetColor("_BaseColor", new Color(1f, 0.92f, 0.62f));
                mat.SetColor("_EmissionColor", new Color(3.2f, 2.6f, 1.3f));
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
            toxicGlow = FxMaterial("ToxicGlow", "Universal Render Pipeline/Lit", mat =>
            {
                mat.SetColor("_BaseColor", new Color(0.45f, 0.9f, 0.2f));
                mat.SetColor("_EmissionColor", new Color(0.55f, 1.6f, 0.2f));
                mat.SetFloat("_Smoothness", 0.85f);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
            bulbRed = FxMaterial("PartyBulbRed", "Universal Render Pipeline/Lit", mat =>
            {
                mat.SetColor("_BaseColor", new Color(1f, 0.45f, 0.4f));
                mat.SetColor("_EmissionColor", new Color(2.4f, 0.6f, 0.45f));
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
            bulbGlow = FxMaterial("PartyBulb", "Universal Render Pipeline/Lit", mat =>
            {
                mat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.5f));
                mat.SetColor("_EmissionColor", new Color(2.6f, 1.8f, 0.7f));
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
        }

        /// <summary>A size for an asset name, the same on every computer ("3.6", never "3,6").</summary>
        static string Dm(float value) => value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Generated kit meshes, saved once per build.</summary>
        static readonly Dictionary<string, Mesh> kitMeshes = new Dictionary<string, Mesh>();

        static Mesh Kit(string name, System.Func<Mesh> make, bool inked = true)
        {
            if (kitMeshes.TryGetValue(name, out var mesh) && mesh != null) return mesh;
            var made = make();
            made.name = name;
            mesh = inked ? SaveInkedMesh(made) : SaveMesh(made);
            kitMeshes[name] = mesh;
            return mesh;
        }

        /// <summary>A palette mesh standing at `position`, turned `yaw` degrees, optionally with a box collider of `solid` size (centred above the foot).</summary>
        static GameObject Piece(Transform parent, string name, Mesh mesh, Material material, Vector3 position, float yaw, float scale = 1f, Vector3? solid = null, bool shadows = true)
        {
            var go = MeshObject(name, parent, mesh, material, shadows);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            if (solid.HasValue)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, solid.Value.y * 0.5f, 0f);
                box.size = solid.Value;
            }
            return go;
        }

        /// <summary>A closed prism over a polygon (x, z), from `bottom` to `top`: the shape of a convex mesh collider.</summary>
        static Mesh PrismMesh(string name, IList<Vector2> polygon, float bottom, float top)
        {
            var b = new LowPoly.MeshBuilder();
            var centre = Vector2.zero;
            foreach (var p in polygon) centre += p;
            centre /= polygon.Count;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 p = polygon[i], q = polygon[(i + 1) % polygon.Count];
                b.Triangle(new Vector3(centre.x, top, centre.y), new Vector3(q.x, top, q.y), new Vector3(p.x, top, p.y), 0);
                b.Triangle(new Vector3(centre.x, bottom, centre.y), new Vector3(p.x, bottom, p.y), new Vector3(q.x, bottom, q.y), 0);
                b.Quad(new Vector3(p.x, bottom, p.y), new Vector3(p.x, top, p.y), new Vector3(q.x, top, q.y), new Vector3(q.x, bottom, q.y), 0);
            }
            return SaveMesh(b.ToMesh(name));
        }

        /// <summary>A primitive kept only for its collider: what is seen is a dressing mesh laid over it.</summary>
        static GameObject HideVisual(GameObject go)
        {
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(go.GetComponent<MeshFilter>());
            return go;
        }

        // ------------------------------------------------------------- signs

        static TextMeshPro WorldText(Transform parent, string key, Vector3 localPosition, Quaternion localRotation, Vector2 size, float maxFont, Color color, int line = -1, float minFont = 0.3f)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            var text = go.AddComponent<TextMeshPro>();
            if (UiFactory.Font != null) text.font = UiFactory.Font;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = minFont;
            text.fontSizeMax = maxFont;
            text.textWrappingMode = line == 0 ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            text.rectTransform.sizeDelta = size;
            text.text = LocalizedText.Pick(Localization.Get(key), line);
            var localized = go.AddComponent<LocalizedText>();
            localized.key = key;
            localized.line = line;
            return text;
        }

        static float YawToward(Vector3 from, Vector3 to)
        {
            var d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// A notice board at about eye height: the title of the text in big letters and the joke under it, on paper pinned to a
        /// board with two legs behind it and a little roof. It faces `facing`, so it is read when walking in from that side.
        /// </summary>
        static Transform Notice(Transform parent, Vector3 position, Vector3 facing, string key, Mats m, float width = 1.5f)
        {
            EnsureKitMaterials();
            var root = new GameObject("Notice").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, YawToward(position, facing), 0f));

            const float height = 0.95f, centreY = 1.32f;
            MeshObject("Board", root, Kit("Kit_Notice_" + Dm(width), () => LowPolyKit.NoticeFrame("n", width, height, centreY)), paletteMaterial);
            var post = new GameObject("Legs");
            post.transform.SetParent(root, false);
            var legs = post.AddComponent<BoxCollider>();
            legs.center = new Vector3(0f, 0.9f, -0.09f);
            legs.size = new Vector3(width, 1.8f, 0.14f);

            Prim(PrimitiveType.Cube, "Paper", root, new Vector3(0f, centreY, 0.022f), new Vector3(width - 0.2f, height - 0.18f, 0.01f), creamPaint, false);
            // TextMeshPro is read from its -Z side, so the text on the +Z face is turned round.
            var turned = Quaternion.Euler(0f, 180f, 0f);
            WorldText(root, key, new Vector3(0f, centreY + 0.2f, 0.035f), turned, new Vector2(width - 0.34f, 0.3f), 2.6f, UiFactory.Ink, 0, 0.6f);
            WorldText(root, key, new Vector3(0f, centreY - 0.13f, 0.035f), turned, new Vector2(width - 0.34f, 0.4f), 1.35f, new Color(0.38f, 0.27f, 0.14f), 1, 0.5f);
            return root;
        }

        /// <summary>A post with arrow planks pointing at the places named on them, readable from both sides.</summary>
        static Transform DirectionSign(Transform parent, Vector3 position, Mats m, params (string key, Vector3 target)[] arms)
        {
            EnsureKitMaterials();
            var root = new GameObject("DirectionSign").transform;
            root.SetParent(parent, false);
            root.position = position;

            float top = 1.75f + arms.Length * 0.46f;
            var post = MeshObject("Post", root, Kit("Kit_SignPost_" + arms.Length, () => LowPolyKit.SignPost("p", top)), paletteMaterial);
            var collider = post.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, top * 0.5f, 0f);
            collider.size = new Vector3(0.2f, top, 0.2f);

            for (int i = 0; i < arms.Length; i++)
            {
                var plank = new GameObject("Arm" + (i + 1)).transform;
                plank.SetParent(root, false);
                plank.localPosition = new Vector3(0f, top - 0.32f - i * 0.46f, 0f);
                plank.localRotation = Quaternion.Euler(0f, YawToward(position, arms[i].target), 0f);
                int colour = i % 2 == 0 ? LowPolyKit.Wood : LowPolyKit.Tan;
                var board = MeshObject("Board", plank, Kit("Kit_Arrow_" + colour, () => LowPolyKit.ArrowPlank("a", colour)), paletteMaterial);
                board.transform.localPosition = new Vector3(0f, 0f, 1.05f);
                // The arm points along +Z of the plank: the text sits on both of its flat sides, readable from either.
                WorldText(plank, arms[i].key, new Vector3(0.045f, 0f, 1.0f), Quaternion.Euler(0f, -90f, 0f), new Vector2(1.5f, 0.34f), 2.2f, UiFactory.Ink, -1, 0.6f);
                WorldText(plank, arms[i].key, new Vector3(-0.045f, 0f, 1.0f), Quaternion.Euler(0f, 90f, 0f), new Vector2(1.5f, 0.34f), 2.2f, UiFactory.Ink, -1, 0.6f);
            }
            return root;
        }

        // ------------------------------------------------------------- dressing

        static Material paletteMaterial;

        /// <summary>Glowing panes laid over the windows of a Cabin mesh placed at `building`.</summary>
        static void GlowWindows(Transform building, float w, float d, float h, bool logs)
        {
            EnsureKitMaterials();
            foreach (var (centre, normal) in LowPolyKit.CabinWindows(w, d, h))
            {
                var pane = Prim(PrimitiveType.Cube, "Pane", building, centre + normal * (logs ? 0.205f : 0.065f), new Vector3(0.78f, 0.58f, 0.02f), windowGlow, false, Quaternion.LookRotation(normal));
                pane.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>Slow grey smoke rising from a chimney.</summary>
        static void ChimneySmoke(Transform parent, Vector3 position)
        {
            var ps = NewParticles("Smoke", parent, fxSoft);
            ps.transform.position = position;
            ps.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startColor = new Color(0.85f, 0.85f, 0.88f, 0.45f);
            main.maxParticles = 40;
            var emission = ps.emission;
            emission.rateOverTime = 4f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.15f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 2.6f)));
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.1f);
        }

        /// <summary>
        /// A string of little glowing bulbs hanging between the given points (no real lights: the glow is in the material, so a
        /// whole quay can be decorated without touching the light budget).
        /// </summary>
        static void PartyLights(Transform parent, Vector3[] points, float sag = 0.45f, float spacing = 0.8f)
        {
            EnsureKitMaterials();
            var root = new GameObject("PartyLights").transform;
            root.SetParent(parent, false);
            var bulbMesh = Kit("Kit_PartyBulb", () =>
            {
                var b = new LowPoly.MeshBuilder();
                LowPoly.Icosphere(b, Vector3.zero, new Vector3(0.07f, 0.09f, 0.07f), 0, 0.02f, LowPolyKit.Gold, 1);
                return b.ToMesh("bulb");
            }, false);
            var wire = new LowPoly.MeshBuilder();
            int bulbIndex = 0;
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Vector3 a = points[i], c = points[i + 1];
                int steps = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(a, c) / spacing));
                Vector3 Point(float t) => Vector3.Lerp(a, c, t) + Vector3.down * sag * 4f * t * (1f - t);
                for (int s = 0; s < steps; s++)
                {
                    Vector3 p = Point(s / (float)steps), q = Point((s + 1f) / steps);
                    LowPolyKit.Beam(wire, p, q, 0.025f, LowPolyKit.Black);
                    if (s == 0) continue;
                    var bulb = MeshObject("Bulb", root, bulbMesh, bulbIndex++ % 3 == 1 ? bulbRed : bulbGlow, false);
                    bulb.transform.position = p + Vector3.down * 0.1f;
                }
            }
            MeshObject("Wire", root, SaveMesh(wire.ToMesh("PartyWire_" + parent.name + "_" + points[0].x.ToString("0") + "_" + points[0].z.ToString("0"))), paletteMaterial, false);
        }

        // ------------------------------------------------------------- the road in

        static float DistanceToLine(IList<Vector2> line, float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                Vector2 a = line[i], b = line[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        static List<Vector2> Smooth(Vector2[] line, float step)
        {
            var points = new List<Vector2>();
            for (int i = 0; i + 1 < line.Length; i++)
            {
                Vector2 p0 = line[Mathf.Max(0, i - 1)], p1 = line[i], p2 = line[i + 1], p3 = line[Mathf.Min(line.Length - 1, i + 2)];
                int n = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / step));
                for (int k = 0; k < n; k++)
                {
                    float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                    points.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            points.Add(line[line.Length - 1]);
            return points;
        }

        /// <summary>
        /// A dirt road on the hills from the horizon to the edge of the map (where the truck parked), with lamps along it: the
        /// truck got here somehow. The hills must have been calmed along `line` (see MapHillField).
        /// </summary>
        static void MapRoad(Transform parent, Vector2[] line, LowPoly.HillField field, ArtAssets art, string name, int litLamps, bool snowy)
        {
            var road = new GameObject("Road").transform;
            road.SetParent(parent, false);
            var points = Smooth(line, 1.5f);
            var dirt = new LowPoly.MeshBuilder();
            float Height(float x, float z) => Mathf.Max(0f, field.Surface(x, z)) + 0.06f;

            Vector3 Edge(int i, float offset)
            {
                Vector2 a = points[Mathf.Max(0, i - 1)], b = points[Mathf.Min(points.Count - 1, i + 1)];
                Vector2 dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                Vector2 p = points[i] + side * offset;
                return new Vector3(p.x, Height(p.x, p.y), p.y);
            }

            void Strip(int i, float from, float to, int colour, float lift)
            {
                Vector3 a = Edge(i, from), b = Edge(i, to), c = Edge(i + 1, to), d = Edge(i + 1, from);
                a.y += lift; b.y += lift; c.y += lift; d.y += lift;
                if (Vector3.Cross(b - a, d - a).y > 0f) dirt.Quad(a, b, c, d, colour);
                else dirt.Quad(a, d, c, b, colour);
            }

            float half = RoadWidth * 0.5f;
            for (int i = 0; i + 1 < points.Count; i++)
            {
                int tone = ((i / 3) % 3 == 0) ? 9 : 20;
                Strip(i, -half - 0.55f, -half, snowy ? 25 : 21, 0f);
                Strip(i, half, half + 0.55f, snowy ? 25 : 21, 0f);
                Strip(i, -half, half, tone, 0.012f);
                Strip(i, -1.45f, -0.85f, 8, 0.024f);
                Strip(i, 0.85f, 1.45f, 8, 0.024f);
                if (i % 4 < 2) Strip(i, -0.07f, 0.07f, 2, 0.03f);
            }
            MeshObject("RoadSurface", road, SaveMesh(dirt.ToMesh("Road_" + name)), art.palette, false);

            for (int i = 8, n = 0; i < points.Count - 4; i += 12, n++)
            {
                Vector2 a = points[i - 1], b = points[i + 1], dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                Vector2 p = points[i] + side * (half + 1.4f) * (n % 2 == 0 ? 1f : -1f);
                var lamp = MeshObject("RoadLamp", road, lampMesh, art.palette);
                lamp.transform.position = new Vector3(p.x, Height(p.x, p.y) - 0.06f, p.y);
                if (n < litLamps) AddLampLight(lamp.transform, new Vector3(0f, 2.78f, 0f), 11f, 2.4f);
            }
        }

        /// <summary>The hills round a map, calmed along its road and optionally reshaped (the harbour sinks its north into the sea).</summary>
        static LowPoly.HillField MapHillField(int seed, Vector2[] road, System.Func<float, float, float, float> reshape = null)
        {
            List<Vector2> smooth = road != null ? Smooth(road, 2f) : null;
            System.Func<float, float, float> calm = null;
            if (smooth != null) calm = (x, z) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 18f, DistanceToLine(smooth, x, z)));
            return new LowPoly.HillField(new Rect(-45f, -36f, 90f, 92f), 5f, 300f, seed, calm, reshape);
        }

        static bool OutsideMap(float x, float z, float margin = 1.5f) => x < -45f - margin || x > 45f + margin || z < -36f - margin || z > 56f + margin;

        /// <summary>
        /// Trees (or snowy pines), boulders and the odd cottage on the slopes round the map, thick near the walls and thinning out
        /// up the mountains. Nothing out there can be reached, so nothing has colliders.
        /// </summary>
        static void HillDecor(Transform parent, LowPoly.HillField field, ArtAssets art, int seed, int trees, int rocks, System.Func<float, float, bool> keepOut,
            System.Action<Transform, Vector3, System.Random> placeTree, float minHeight = -100f)
        {
            var outside = new GameObject("Outside").transform;
            outside.SetParent(parent, false);
            var rng = new System.Random(seed);

            float Density(float x, float z)
            {
                float dx = Mathf.Max(-45f - x, 0f, x - 45f), dz = Mathf.Max(-36f - z, 0f, z - 56f);
                return Mathf.Clamp01(1.15f - Mathf.Sqrt(dx * dx + dz * dz) / 120f);
            }

            bool Spot(out float x, out float z, float maxSlope)
            {
                for (int tries = 0; tries < 14; tries++)
                {
                    x = Mathf.Lerp(-160f, 160f, (float)rng.NextDouble());
                    z = Mathf.Lerp(-170f, 200f, (float)rng.NextDouble());
                    if (!OutsideMap(x, z) || rng.NextDouble() > Density(x, z)) continue;
                    if (keepOut != null && keepOut(x, z)) continue;
                    if (field.Surface(x, z) < minHeight) continue;
                    if (SlopeAt(field, x, z) > maxSlope) continue;
                    return true;
                }
                x = z = 0f;
                return false;
            }

            int placed = 0;
            for (int i = 0; i < trees * 8 && placed < trees; i++)
            {
                if (!Spot(out float x, out float z, 0.8f)) continue;
                placeTree(outside, new Vector3(x, field.Surface(x, z), z), rng);
                placed++;
            }
            placed = 0;
            for (int i = 0; i < rocks * 8 && placed < rocks; i++)
            {
                if (!Spot(out float x, out float z, 1.2f)) continue;
                float scale = rng.NextDouble() < 0.15 ? Mathf.Lerp(1.8f, 3.4f, (float)rng.NextDouble()) : Mathf.Lerp(0.7f, 1.7f, (float)rng.NextDouble());
                PlaceDecor(outside, art.rocks[rng.Next(art.rocks.Length)], new Vector3(x, field.Surface(x, z), z), (float)rng.NextDouble() * 360f, scale, null, 0.2f);
                placed++;
            }
        }

        /// <summary>A cottage on the hills outside the map, lit from within (no collider: it cannot be reached).</summary>
        static void Cottage(Transform parent, LowPoly.HillField field, Vector3 at, float yaw, float w, float d, int wall, int roof, bool snow, bool smoke, ArtAssets art)
        {
            var mesh = Kit("Kit_Cottage_" + Dm(w) + "x" + Dm(d) + "_" + wall + "_" + roof + (snow ? "_snow" : ""),
                () => LowPolyKit.Cabin("c", w, d, 2.5f, wall, roof, LowPolyKit.Snow, wall == LowPolyKit.Wood || wall == LowPolyKit.DarkWood, snow, true));
            // stand on the lowest corner, so no corner of the floor hangs in the air
            float y = float.MaxValue;
            foreach (var (cx, cz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var corner = at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(cx * w * 0.5f, 0f, cz * d * 0.5f);
                y = Mathf.Min(y, field.Surface(corner.x, corner.z));
            }
            var house = Piece(parent, "Cottage", mesh, art.palette, new Vector3(at.x, y - 0.1f, at.z), yaw);
            GlowWindows(house.transform, w, d, 2.5f, wall == LowPolyKit.Wood || wall == LowPolyKit.DarkWood);
            if (smoke) ChimneySmoke(house.transform, house.transform.TransformPoint(new Vector3(w * 0.27f, 2.5f + d * 0.42f * 1.05f + 0.75f, -d * 0.2f)));
        }
    }
}
