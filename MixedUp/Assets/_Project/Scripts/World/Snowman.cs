using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Easter egg: run into the snowman on the ice hill and it falls apart. The head and the hat roll away, the body
    /// slumps into a heap, and after a while it builds itself up again.
    /// </summary>
    public class Snowman : MonoBehaviour
    {
        public Transform body;
        public Transform head;
        public Transform hat;
        [Tooltip("The collider that makes the snowman solid while it stands.")]
        public Collider solid;
        public ParticleSystem puff;
        [Tooltip("How close a player has to get (metres, on the ground plane) to bump into it.")]
        public float bumpDistance = 1.3f;
        [Tooltip("Players need to have been running at least this fast just before touching it.")]
        public float minSpeed = 2.2f;
        public float respawnSeconds = 40f;

        struct Rest
        {
            public Transform t;
            public Vector3 position, scale;
            public Quaternion rotation;
        }

        readonly List<Rest> rest = new List<Rest>();
        readonly Dictionary<PlayerController, float> recentSpeed = new Dictionary<PlayerController, float>();
        float respawnAt;

        public bool IsCollapsed { get; private set; }
        public static event System.Action<Snowman> Collapsed;
        public static event System.Action<Snowman> Rebuilt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Collapsed = null; Rebuilt = null; }

        void Awake()
        {
            foreach (var t in new[] { body, head, hat })
                if (t != null) rest.Add(new Rest { t = t, position = t.localPosition, scale = t.localScale, rotation = t.localRotation });
        }

        void Update()
        {
            if (IsCollapsed)
            {
                if (Time.time >= respawnAt) Rebuild();
                return;
            }

            foreach (var player in PlayerRegistry.All)
            {
                if (player == null || player.Status.IsDead) continue;

                float speed = player.HorizontalVelocity.magnitude;
                recentSpeed.TryGetValue(player, out float remembered);
                remembered = Mathf.Max(speed, Mathf.MoveTowards(remembered, 0f, 12f * Time.deltaTime));
                recentSpeed[player] = remembered;

                Vector3 offset = player.transform.position - transform.position;
                if (Mathf.Abs(offset.y) > 2f) continue;
                offset.y = 0f;
                if (offset.magnitude > bumpDistance || remembered < minSpeed) continue;

                Collapse(player.transform.position);
                return;
            }
        }

        /// <summary>Knocks the snowman down, throwing the head and hat away from `from`.</summary>
        public void Collapse(Vector3 from)
        {
            if (IsCollapsed) return;
            IsCollapsed = true;
            respawnAt = Time.time + respawnSeconds;
            if (solid != null) solid.enabled = false;

            Vector3 away = transform.position - from;
            away.y = 0f;
            away = away.sqrMagnitude < 0.01f ? transform.forward : away.normalized;

            if (head != null) Launch(head, go => go.AddComponent<SphereCollider>().radius = 0.34f, away * 3.2f + Vector3.up * 3.5f, 0.8f);
            if (hat != null)
                Launch(hat, go =>
                {
                    var box = go.AddComponent<BoxCollider>();
                    box.size = new Vector3(0.5f, 0.45f, 0.5f);
                    box.center = new Vector3(0f, 0.2f, 0f);
                }, away * 4.5f + Vector3.up * 5f, 0.2f);
            if (body != null) StartCoroutine(Slump(body));
            if (puff != null) puff.Emit(40);
            Collapsed?.Invoke(this);
        }

        static void Launch(Transform part, System.Action<GameObject> addCollider, Vector3 velocity, float mass)
        {
            var go = part.gameObject;
            addCollider(go);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.3f;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Random.insideUnitSphere * 8f;
        }

        IEnumerator Slump(Transform part)
        {
            Vector3 from = part.localScale, to = new Vector3(from.x * 1.35f, from.y * 0.35f, from.z * 1.35f);
            Vector3 fromPos = part.localPosition;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                float k = t / 0.35f;
                part.localScale = Vector3.Lerp(from, to, k);
                part.localPosition = fromPos + Vector3.down * (0.18f * k);
                yield return null;
            }
            part.localScale = to;
        }

        void Rebuild()
        {
            StopAllCoroutines();
            foreach (var r in rest)
            {
                foreach (var rb in r.t.GetComponents<Rigidbody>()) Destroy(rb);
                foreach (var c in r.t.GetComponents<Collider>()) Destroy(c);
                r.t.localPosition = r.position;
                r.t.localRotation = r.rotation;
                r.t.localScale = r.scale;
            }
            if (solid != null) solid.enabled = true;
            if (puff != null) puff.Emit(25);
            IsCollapsed = false;
            Rebuilt?.Invoke(this);
        }
    }
}
