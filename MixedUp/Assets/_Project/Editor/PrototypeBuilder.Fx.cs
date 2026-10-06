using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// Light and effects dressing: the soft sprites (glows, rings, snow), the water and mist materials, lamp lights with their
    /// halos, flickering fires, falling snow and the flames of the camp fires.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        static Material fxSoft, fxGlow, fxRing, fxFlame, fxMist, fxWater, fxBulb;
        static Texture2D glowTexture, ringTexture;

        // ------------------------------------------------------------ assets

        static Texture2D MakeFxTexture(string file, System.Func<float, float, float> alphaAt, int size = 128)
        {
            string path = TexturesDir + "/" + file;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alphaAt(u, v)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureTexture(path, FilterMode.Bilinear, mips: false, uncompressed: true, npotNone: false);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material FxMaterial(string name, string shaderName, System.Action<Material> setup)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            bool isNew = material == null;
            if (isNew) material = new Material(shader);
            else material.shader = shader;
            setup(material);
            if (isNew) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            return material;
        }

        static void CreateFx(ArtAssets art)
        {
            glowTexture = MakeFxTexture("fx_glow.png", (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v);
                return r >= 1f ? 0f : Mathf.Pow(1f - r, 2.2f);
            });
            ringTexture = MakeFxTexture("fx_ring.png", (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v);
                float d = (r - 0.78f) / 0.09f;
                return Mathf.Exp(-d * d) * Mathf.Clamp01((1f - r) * 8f);
            }, 256);

            fxSoft = FxMaterial("FxSoft", "MixedUp/SoftParticle", m =>
            {
                m.SetTexture("_MainTex", glowTexture);
                m.SetFloat("_DstBlend", 10f);
                m.SetColor("_Tint", Color.white);
            });
            fxGlow = FxMaterial("FxGlow", "MixedUp/SoftParticle", m =>
            {
                m.SetTexture("_MainTex", glowTexture);
                m.SetFloat("_DstBlend", 1f);
                m.SetColor("_Tint", new Color(1f, 0.78f, 0.42f, 0.9f));
                m.SetFloat("_Boost", 1.5f);
            });
            fxFlame = FxMaterial("FxFlame", "MixedUp/SoftParticle", m =>
            {
                m.SetTexture("_MainTex", glowTexture);
                m.SetFloat("_DstBlend", 1f);
                m.SetColor("_Tint", Color.white);
                m.SetFloat("_Boost", 1.3f);
            });
            fxRing = FxMaterial("FxRing", "MixedUp/SoftParticle", m =>
            {
                m.SetTexture("_MainTex", ringTexture);
                m.SetFloat("_DstBlend", 10f);
                m.SetColor("_Tint", new Color(0.92f, 1f, 1f, 1f));
            });
            fxMist = FxMaterial("EdgeMist", "MixedUp/EdgeMist", m =>
            {
                m.SetColor("_Color", new Color(0.86f, 0.95f, 0.97f));
                m.SetFloat("_Height", 11f);
                m.SetFloat("_Density", 0.9f);
                m.SetFloat("_NearFade", 10f);
            });
            fxWater = FxMaterial("RiverWater", "MixedUp/Water", m =>
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesDir + "/palette.png"));
                m.SetColor("_Tint", Color.white);
                m.SetFloat("_Opacity", 0.9f);
            });
            fxBulb = FxMaterial("LampBulb", "Universal Render Pipeline/Lit", m =>
            {
                m.SetColor("_BaseColor", new Color(1f, 0.86f, 0.55f));
                m.SetColor("_EmissionColor", new Color(3.2f, 2.3f, 1.0f));
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
        }

        // ------------------------------------------------------------ lamp lights

        /// <summary>A glowing lantern: warm point light, emissive bulb and a halo sprite that always faces the camera.</summary>
        static GameObject AddLampLight(Transform parent, Vector3 localPosition, float range = 11f, float intensity = 2.6f)
        {
            var root = new GameObject("LampLight");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            var bulb = new GameObject("Bulb");
            bulb.transform.SetParent(root.transform, false);
            bulb.AddComponent<MeshFilter>().sharedMesh = LampBulbMesh();
            var bulbRenderer = bulb.AddComponent<MeshRenderer>();
            bulbRenderer.sharedMaterial = fxBulb;
            bulbRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(root.transform, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.46f);
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            var flicker = lightObject.AddComponent<FlickerLight>();
            flicker.baseIntensity = intensity;
            flicker.amount = 0.07f;
            flicker.speed = 1.5f;

            AddHalo(root.transform, Vector3.zero, 2.4f, new Color(1f, 0.8f, 0.45f, 0.55f));
            return root;
        }

        static Mesh lampBulbMesh;
        static Mesh LampBulbMesh() => lampBulbMesh != null ? lampBulbMesh : lampBulbMesh = SaveMesh(LowPolyProps.LampBulb());

        static Mesh quadMesh;

        static Mesh QuadMesh()
        {
            if (quadMesh != null) return quadMesh;
            var mesh = new Mesh { name = "Fx_Quad" };
            mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return quadMesh = SaveMesh(mesh);
        }

        /// <summary>A soft additive glow sprite that turns to face the camera.</summary>
        static GameObject AddHalo(Transform parent, Vector3 localPosition, float size, Color tint)
        {
            var go = new GameObject("Halo");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = fxGlow;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var block = new MaterialPropertyBlock();
            block.SetColor("_Tint", tint);
            renderer.SetPropertyBlock(block);
            go.AddComponent<Billboard>();
            return go;
        }

        // -------------------------------------------------------------- fire

        /// <summary>A flickering orange light, a rising flame and a few sparks.</summary>
        static void AddFireFx(Transform parent, Vector3 localPosition, float scale)
        {
            var root = new GameObject("FireFx");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            var lightObject = new GameObject("FireLight");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = Vector3.up * 0.8f * scale;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.52f, 0.2f);
            light.range = 13f * scale;
            light.intensity = 3.4f * scale;
            light.shadows = LightShadows.None;
            var flicker = lightObject.AddComponent<FlickerLight>();
            flicker.baseIntensity = light.intensity;
            flicker.amount = 0.35f;
            flicker.speed = 11f;

            // the flames
            var flame = NewParticles("Flames", root.transform, fxFlame);
            var main = flame.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f * scale, 0.9f * scale);
            main.startColor = new Color(1f, 0.75f, 0.3f, 0.9f);
            main.gravityModifier = -0.25f;
            main.maxParticles = 120;
            var emission = flame.emission;
            emission.rateOverTime = 22f * scale;
            var shape = flame.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.28f * scale;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var colour = flame.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.45f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.12f), 0.5f), new GradientColorKey(new Color(0.55f, 0.1f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.2f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;
            var size = flame.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.1f)));

            // sparks
            var sparks = NewParticles("Sparks", root.transform, fxFlame);
            var sparkMain = sparks.main;
            sparkMain.loop = true;
            sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            sparkMain.startColor = new Color(1f, 0.8f, 0.4f, 1f);
            sparkMain.gravityModifier = -0.1f;
            sparkMain.maxParticles = 40;
            var sparkEmission = sparks.emission;
            sparkEmission.rateOverTime = 5f * scale;
            var sparkShape = sparks.shape;
            sparkShape.shapeType = ParticleSystemShapeType.Cone;
            sparkShape.angle = 18f;
            sparkShape.radius = 0.2f * scale;
            sparkShape.rotation = new Vector3(-90f, 0f, 0f);
        }

        static ParticleSystem NewParticles(string name, Transform parent, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            return ps;
        }

        // -------------------------------------------------------------- snow

        /// <summary>Gentle snowfall over a box of the map (the ice hill).</summary>
        static void AddSnowfall(Transform parent, Vector3 center, Vector3 size)
        {
            var ps = NewParticles("Snowfall", parent, fxSoft);
            ps.transform.position = center;
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 9f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.17f);
            main.startColor = new Color(1f, 1f, 1f, 0.9f);
            main.maxParticles = 700;
            main.gravityModifier = 0f;
            var emission = ps.emission;
            emission.rateOverTime = 70f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            velocity.y = new ParticleSystem.MinMaxCurve(-1.5f, -0.8f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.25f;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;
        }

        // ------------------------------------------------------------ edge mist

        /// <summary>Walls of drifting mist just inside the invisible borders: the world simply fades away there.</summary>
        static void BuildEdgeMist(Transform env)
        {
            var mist = new GameObject("EdgeMist").transform;
            mist.SetParent(env, false);

            // Rectangle of the play area (the invisible walls sit half a metre outside it).
            const float x0 = -44.5f, x1 = 44.5f, z0 = -34.8f, z1 = 54.8f, height = 11f;
            void Wall(string name, Vector3 centre, float length, bool alongX)
            {
                var go = new GameObject(name);
                go.transform.SetParent(mist, false);
                go.transform.localPosition = centre + Vector3.up * height * 0.5f;
                go.transform.localRotation = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                go.transform.localScale = new Vector3(length, height, 1f);
                go.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = fxMist;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // Two layers per side, a little apart, so the mist has depth.
            for (int layer = 0; layer < 2; layer++)
            {
                float o = layer * 2.6f;
                Wall("MistSouth" + layer, new Vector3(0f, 0f, z0 + o), 92f, true);
                Wall("MistNorth" + layer, new Vector3(0f, 0f, z1 - o), 92f, true);
                Wall("MistWest" + layer, new Vector3(x0 + o, 0f, 10f), 92f, false);
                Wall("MistEast" + layer, new Vector3(x1 - o, 0f, 10f), 92f, false);
            }
        }
    }
}
