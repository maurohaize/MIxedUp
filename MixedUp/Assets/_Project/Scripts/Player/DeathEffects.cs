using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Cartoon death animations chosen by the cause of death, built from primitives at runtime (no assets needed):
    /// electrocution flickers an X-ray skeleton with lightning, heat and fire char the body and puff smoke,
    /// falls squash it flat with dizzy stars, the void shrinks it away, and anything else gets stars and a puff.
    /// Purely visual: it only reads PlayerStatus, so remote players show it too in multiplayer.
    /// </summary>
    [RequireComponent(typeof(PlayerStatus))]
    public class DeathEffects : MonoBehaviour
    {
        enum Kind { None, Electric, Burn, Squash, Void, Generic }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        PlayerStatus status;
        Transform body;
        CarriedBoxesView carry;
        Renderer[] renderers;
        Kind kind;
        float time;
        float nextSpawn;
        Transform fxRoot, bones;
        LineRenderer[] bolts;
        readonly List<Transform> stars = new List<Transform>();
        readonly List<(Transform t, float born, Vector3 velocity, float size)> puffs = new List<(Transform, float, Vector3, float)>();

        public bool IsPlaying => kind != Kind.None;
        public string KindName => kind.ToString();
        public bool SkeletonVisible => bones != null && bones.gameObject.activeSelf;
        public int BoltCount => bolts == null ? 0 : bolts.Length;

        void Awake()
        {
            status = GetComponent<PlayerStatus>();
            carry = GetComponent<CarriedBoxesView>();
            var animator = GetComponent<PlayerAnimator>();
            body = animator != null ? animator.body : null;
        }

        void OnEnable() => status.Died += Begin;
        void OnDisable()
        {
            status.Died -= Begin;
            Stop();
        }

        static Kind KindOf(string key)
        {
            switch (key)
            {
                case "death.electric_water": return Kind.Electric;
                case "death.heat":
                case "death.burn":
                case "death.explosion":
                case "death.deadly_mix": return Kind.Burn;
                case "death.fall":
                case "death.sweeper": return Kind.Squash;
                case "death.void": return Kind.Void;
                default: return Kind.Generic;
            }
        }

        void Begin(DeathCause cause)
        {
            Stop();
            if (body == null) return;

            kind = KindOf(cause.Key);
            time = 0f;
            nextSpawn = 0f;
            fxRoot = new GameObject("DeathFx").transform;
            fxRoot.SetParent(transform, false);

            var all = new List<Renderer>();
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
            {
                if (carry != null && carry.anchor != null && r.transform.IsChildOf(carry.anchor)) continue;
                all.Add(r);
            }
            renderers = all.ToArray();

            if (kind == Kind.Electric) BuildSkeletonAndBolts();
            if (kind == Kind.Squash || kind == Kind.Generic) BuildStars();
            if (kind == Kind.Burn) SpawnPuffs(7, 0.5f, new Color(0.2f, 0.2f, 0.2f));
        }

        void Stop()
        {
            if (kind != Kind.None && renderers != null)
            {
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    r.enabled = true;
                    r.SetPropertyBlock(null);
                }
                if (body != null) body.localScale = Vector3.one;
            }
            if (fxRoot != null) Destroy(fxRoot.gameObject);
            if (bones != null) Destroy(bones.gameObject);
            fxRoot = bones = null;
            bolts = null;
            stars.Clear();
            puffs.Clear();
            kind = Kind.None;

            var appearance = GetComponent<PlayerAppearance>();
            if (appearance != null && status != null && !status.IsDead) appearance.Apply();
        }

        void Update()
        {
            if (kind != Kind.None && !status.IsDead) Stop();   // revived
        }

        void LateUpdate()
        {
            if (kind == Kind.None || body == null) return;
            time += Time.deltaTime;

            switch (kind)
            {
                case Kind.Electric: UpdateElectric(); break;
                case Kind.Burn: UpdateBurn(); break;
                case Kind.Squash: UpdateSquash(); break;
                case Kind.Void: UpdateVoid(); break;
                default: UpdateStars(1.6f); break;
            }
            UpdatePuffs();
        }

        // ------------------------------------------------------------ electric

        void UpdateElectric()
        {
            const float duration = 0.9f;
            bool active = time < duration;

            if (active)
            {
                // The body jerks while the current runs through it.
                body.localPosition += new Vector3(Random.Range(-0.04f, 0.04f), Random.Range(0f, 0.03f), Random.Range(-0.04f, 0.04f));

                // X-ray flicker: skeleton and body swap every few frames.
                bool skeleton = Mathf.FloorToInt(time * 14f) % 2 == 0;
                SetBodyVisible(!skeleton);
                if (bones != null) bones.gameObject.SetActive(skeleton);

                foreach (var bolt in bolts) Rezap(bolt);
                // The jolt also washes the body out in white-yellow.
                Tint(Color.Lerp(Color.white, new Color(1f, 0.95f, 0.4f), Mathf.PingPong(time * 20f, 1f)));
            }
            else
            {
                if (bones != null) bones.gameObject.SetActive(false);
                foreach (var bolt in bolts) if (bolt != null) bolt.enabled = false;
                SetBodyVisible(true);
                float charred = Mathf.Clamp01((time - duration) / 0.3f);
                Tint(Color.Lerp(Color.white, new Color(0.16f, 0.14f, 0.14f), charred));
                if (time < duration + 1.2f) SmokeTrickle(new Color(0.3f, 0.3f, 0.3f));
            }
        }

        void BuildSkeletonAndBolts()
        {
            bones = new GameObject("Skeleton").transform;
            bones.SetParent(body, false);
            var bone = Mat(new Color(0.96f, 0.94f, 0.86f));
            var dark = Mat(new Color(0.05f, 0.05f, 0.06f));

            Part(PrimitiveType.Sphere, bones, new Vector3(0f, 1.17f, 0f), new Vector3(0.58f, 0.6f, 0.56f), bone);          // skull
            Part(PrimitiveType.Sphere, bones, new Vector3(-0.12f, 1.18f, 0.24f), new Vector3(0.15f, 0.17f, 0.1f), dark);   // eye sockets
            Part(PrimitiveType.Sphere, bones, new Vector3(0.12f, 1.18f, 0.24f), new Vector3(0.15f, 0.17f, 0.1f), dark);
            Part(PrimitiveType.Cube, bones, new Vector3(0f, 0.92f, 0.12f), new Vector3(0.3f, 0.1f, 0.22f), bone);          // jaw
            Part(PrimitiveType.Capsule, bones, new Vector3(0f, 0.6f, 0f), new Vector3(0.07f, 0.28f, 0.07f), bone);         // spine
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Cube, bones, new Vector3(0f, 0.78f - i * 0.1f, 0.03f), new Vector3(0.4f - i * 0.04f, 0.035f, 0.22f), bone);  // ribs
            Part(PrimitiveType.Cube, bones, new Vector3(0f, 0.3f, 0f), new Vector3(0.3f, 0.1f, 0.16f), bone);               // pelvis
            foreach (var side in new[] { -1f, 1f })
            {
                Part(PrimitiveType.Capsule, bones, new Vector3(side * 0.34f, 0.6f, 0.02f), new Vector3(0.06f, 0.26f, 0.06f), bone, new Vector3(0f, 0f, side * 12f));  // arms
                Part(PrimitiveType.Capsule, bones, new Vector3(side * 0.13f, 0.12f, 0.02f), new Vector3(0.07f, 0.17f, 0.07f), bone);                                   // legs
            }
            bones.gameObject.SetActive(false);

            bolts = new LineRenderer[7];
            for (int i = 0; i < bolts.Length; i++)
            {
                var go = new GameObject("Bolt" + i);
                go.transform.SetParent(body, false);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 8;
                line.numCapVertices = 2;
                line.widthMultiplier = 0.07f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = line.endColor = i % 2 == 0 ? new Color(1f, 0.95f, 0.35f) : new Color(0.7f, 0.95f, 1f);
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bolts[i] = line;
            }
        }

        void Rezap(LineRenderer bolt)
        {
            if (bolt == null) return;
            // Re-roll the jagged path a few times a second so it crackles.
            if (Random.value > 0.45f) return;

            Vector3 a = RandomOnBody(), b = RandomOnBody();
            int n = bolt.positionCount;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                Vector3 p = Vector3.Lerp(a, b, t);
                if (i > 0 && i < n - 1) p += Random.insideUnitSphere * 0.12f;
                bolt.SetPosition(i, p);
            }
            bolt.enabled = true;
        }

        static Vector3 RandomOnBody()
        {
            Vector3 dir = Random.onUnitSphere;
            return new Vector3(dir.x * 0.55f, 0.75f + dir.y * 0.8f, dir.z * 0.5f);
        }

        // ------------------------------------------------------------ burn / heat

        void UpdateBurn()
        {
            Tint(Color.Lerp(Color.white, new Color(0.12f, 0.1f, 0.1f), Mathf.Clamp01(time / 0.7f)));
            body.localPosition += new Vector3(Mathf.Sin(time * 60f) * 0.015f, 0f, 0f);
            if (time < 1.3f)
            {
                SmokeTrickle(new Color(0.18f, 0.18f, 0.18f));
                if (time > nextSpawn)
                {
                    nextSpawn = time + 0.07f;
                    SpawnPuffs(1, 0.28f, Random.value > 0.5f ? new Color(1f, 0.5f, 0.1f) : new Color(1f, 0.8f, 0.2f), 2.2f);
                }
            }
        }

        // ------------------------------------------------------------ squash / stars

        void UpdateSquash()
        {
            float s = Mathf.Clamp01(time / 0.12f);
            body.localScale = new Vector3(Mathf.Lerp(1f, 1.4f, s), Mathf.Lerp(1f, 0.4f, s), Mathf.Lerp(1f, 1.4f, s));
            if (time < 0.1f && Time.deltaTime > 0f) SpawnPuffs(2, 0.35f, new Color(0.85f, 0.8f, 0.7f), 1.2f);
            UpdateStars(1.8f);
        }

        void UpdateVoid()
        {
            float s = Mathf.Clamp01(time / 1.2f);
            body.localScale = Vector3.one * Mathf.Lerp(1f, 0.02f, s);
            body.localRotation = Quaternion.Euler(0f, 540f * time, 0f) * body.localRotation;
        }

        void BuildStars()
        {
            var yellow = Mat(new Color(1f, 0.85f, 0.2f));
            for (int i = 0; i < 4; i++)
            {
                var star = Part(PrimitiveType.Sphere, fxRoot, Vector3.zero, Vector3.one * 0.16f, yellow);
                stars.Add(star);
            }
        }

        void UpdateStars(float lifetime)
        {
            if (stars.Count == 0) return;
            float fade = Mathf.Clamp01((lifetime - time) / 0.35f);
            for (int i = 0; i < stars.Count; i++)
            {
                float angle = time * 5.5f + i * Mathf.PI * 0.5f;
                stars[i].localPosition = new Vector3(Mathf.Cos(angle) * 0.5f, 1.72f + Mathf.Sin(time * 8f + i) * 0.05f, Mathf.Sin(angle) * 0.5f);
                stars[i].localScale = Vector3.one * 0.16f * fade;
            }
        }

        // ----------------------------------------------------------------- puffs

        void SmokeTrickle(Color color)
        {
            if (time < nextSpawn) return;
            nextSpawn = time + 0.1f;
            SpawnPuffs(1, 0.3f, color);
        }

        void SpawnPuffs(int count, float size, Color color, float rise = 1.1f)
        {
            if (fxRoot == null) return;
            for (int i = 0; i < count; i++)
            {
                var puff = Part(PrimitiveType.Sphere, fxRoot, new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.4f, 1.3f), Random.Range(-0.3f, 0.3f)),
                    Vector3.one * size, Mat(color));
                puffs.Add((puff, time, new Vector3(Random.Range(-0.3f, 0.3f), rise, Random.Range(-0.3f, 0.3f)), size));
            }
        }

        void UpdatePuffs()
        {
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var (t, born, velocity, size) = puffs[i];
                float age = time - born;
                if (t == null || age > 1f)
                {
                    if (t != null) Destroy(t.gameObject);
                    puffs.RemoveAt(i);
                    continue;
                }
                t.localPosition += velocity * Time.deltaTime;
                t.localScale = Vector3.one * size * (1f + age * 1.4f) * (1f - age * age);
            }
        }

        // --------------------------------------------------------------- helpers

        void SetBodyVisible(bool visible)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                // The face sprite and outlines belong to the body: they hide with it.
                r.enabled = visible;
            }
        }

        void Tint(Color color)
        {
            var block = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null || r.sharedMaterial == null || !r.sharedMaterial.HasProperty(BaseColorId)) continue;
                r.GetPropertyBlock(block);
                Color original = r.sharedMaterial.GetColor(BaseColorId);
                var appearance = GetComponent<PlayerAppearance>();
                if (appearance != null && IsIn(appearance.skinRenderers, r)) original = appearance.skinColor;
                else if (appearance != null && IsIn(appearance.clothesRenderers, r)) original = appearance.clothesColor;
                block.SetColor(BaseColorId, original * color);
                r.SetPropertyBlock(block);
            }
        }

        static bool IsIn(Renderer[] list, Renderer r)
        {
            if (list == null) return false;
            foreach (var item in list) if (item == r) return true;
            return false;
        }

        static Transform Part(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static Material Mat(Color color)
        {
            if (Materials.TryGetValue(color, out var existing) && existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            material.SetColor(BaseColorId, color);
            material.SetFloat("_Smoothness", 0.1f);
            Materials[color] = material;
            return material;
        }
    }
}
