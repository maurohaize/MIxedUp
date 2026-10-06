using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Splashes and ripples for everyone who wades through the river: a burst of droplets and a ring when they step in or out,
    /// and rings spreading behind them while they move. One shared pair of particle systems, emitted by hand.
    /// </summary>
    public class WaterEffects : MonoBehaviour
    {
        public Material ringMaterial;
        public Material dropMaterial;
        [Tooltip("Height of the water surface.")]
        public float waterLevel = -0.1f;
        public float rippleInterval = 0.22f;

        ParticleSystem rings, drops;
        readonly Dictionary<PlayerStatus, State> states = new Dictionary<PlayerStatus, State>();

        sealed class State
        {
            public bool inWater;
            public float nextRipple;
            public Vector3 last;
        }

        /// <summary>Splashes and rings made so far: lets tests and sounds hook in.</summary>
        public int Splashes { get; private set; }
        public int Ripples { get; private set; }
        public static event System.Action<Vector3, float> Splashed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Splashed = null;

        void Awake()
        {
            rings = MakeSystem("Ripples", ringMaterial, ParticleSystemRenderMode.HorizontalBillboard, 0f);
            drops = MakeSystem("Droplets", dropMaterial, ParticleSystemRenderMode.Billboard, 1.6f);

            var size = rings.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(1f, 1f)));
        }

        ParticleSystem MakeSystem(string name, Material material, ParticleSystemRenderMode mode, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = 600;
            main.startLifetime = 0.9f;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = mode;
            renderer.sharedMaterial = material;
            ps.Play();
            return ps;
        }

        void Update()
        {
            var players = PlayerRegistry.All;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null) continue;
                var status = player.Status;
                if (!states.TryGetValue(status, out var state))
                {
                    state = new State { last = player.transform.position };
                    states[status] = state;
                }

                bool inWater = status.Hazards.InWater && !status.IsDead;
                var feet = new Vector3(player.transform.position.x, waterLevel + 0.02f, player.transform.position.z);

                if (inWater != state.inWater)
                {
                    state.inWater = inWater;
                    Splash(feet, 1f);
                }
                else if (inWater && Time.time >= state.nextRipple)
                {
                    float speed = (player.transform.position - state.last).magnitude / Mathf.Max(0.0001f, Time.deltaTime);
                    if (speed > 0.6f)
                    {
                        state.nextRipple = Time.time + rippleInterval;
                        Ring(feet, 0.9f + Mathf.Min(speed, 7f) * 0.12f, 0.55f);
                        Droplets(feet, 3, 1.2f);
                    }
                }
                state.last = player.transform.position;
            }
        }

        /// <summary>A big splash: two rings and a spray of droplets.</summary>
        public void Splash(Vector3 position, float strength)
        {
            Splashes++;
            Ring(position, 2.1f * strength, 0.9f);
            Ring(position, 1.3f * strength, 0.7f);
            Droplets(position, 22, 3.2f * strength);
            Splashed?.Invoke(position, strength);
        }

        void Ring(Vector3 position, float size, float alpha)
        {
            Ripples++;
            var p = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = Vector3.zero,
                startSize = size,
                startLifetime = 1.1f,
                startColor = new Color(1f, 1f, 1f, alpha)
            };
            rings.Emit(p, 1);
        }

        void Droplets(Vector3 position, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var direction = Random.insideUnitSphere;
                direction.y = Mathf.Abs(direction.y) * 1.4f + 0.6f;
                var p = new ParticleSystem.EmitParams
                {
                    position = position + new Vector3(Random.Range(-0.25f, 0.25f), 0f, Random.Range(-0.25f, 0.25f)),
                    velocity = direction * speed,
                    startSize = Random.Range(0.06f, 0.16f),
                    startLifetime = Random.Range(0.5f, 0.9f),
                    startColor = new Color(0.9f, 0.98f, 1f, 1f)
                };
                drops.Emit(p, 1);
            }
        }
    }
}
