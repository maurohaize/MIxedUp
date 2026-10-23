using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Health and the combined result of every effect from the boxes being carried.
    /// Effects are derived from the inventory each tick, never stored, so the inventory
    /// is the only thing that needs to be synchronised in multiplayer.
    /// </summary>
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerStatus : MonoBehaviour
    {
        public float maxHealth = 100f;
        [Tooltip("Seconds without taking damage before health starts to regenerate.")]
        public float regenDelay = 10f;
        public float regenPerSecond = 2f;
        public float visionRiseSpeed = 1f;
        public float visionFallSpeed = 1.2f;
        [Tooltip("Damage per second while standing in flames.")]
        public float fireDamagePerSecond = 14f;

        PlayerInventory inventory;
        bool initialized;
        float health;
        float lastDamageTime = -999f;
        readonly Dictionary<string, float> cooldowns = new Dictionary<string, float>();

        public event Action<float, DeathCause> Damaged;
        public event Action<DeathCause> Died;
        public event Action Shocked;
        public event Action<float> Healed;

        public PlayerInventory Inventory { get { EnsureInit(); return inventory; } }
        public float Health { get { EnsureInit(); return health; } }
        public float Health01 => maxHealth <= 0f ? 0f : Health / maxHealth;
        public bool IsDead { get; private set; }
        public DeathCause LastCause { get; private set; }

        /// <summary>True when there is more than one player: health no longer regenerates by itself, a hug is needed.</summary>
        public static bool HugsRequired => RoomSession.IsMultiplayer || PlayerRegistry.All.Count > 1 || NetAvatar.All.Count > 1;

        public HazardState Hazards { get; set; }
        public MovementModifiers Modifiers { get; private set; } = MovementModifiers.Default;
        public float VisionObstruction { get; private set; }
        public float SimTime { get; private set; }

        void Awake()
        {
            // Every character gets its death animations without having to be set up in the scene.
            if (GetComponent<DeathEffects>() == null) gameObject.AddComponent<DeathEffects>();
            if (GetComponent<PlayerAudio>() == null) gameObject.AddComponent<PlayerAudio>();
            if (GetComponent<DeathDrops>() == null) gameObject.AddComponent<DeathDrops>();
        }

        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            inventory = GetComponent<PlayerInventory>();
            health = maxHealth;
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Advances the simulation. Public so it can be driven deterministically in tests.</summary>
        public void Tick(float deltaTime)
        {
            EnsureInit();
            if (IsDead || deltaTime <= 0f) return;

            SimTime += deltaTime;
            inventory.Tick(deltaTime);

            // Flames hurt in small, regular bites instead of every frame.
            if (Hazards.OnFire && TryUseCooldown("fire", 0.25f)) Damage(fireDamagePerSecond * 0.25f, DeathCause.Burn);
            if (IsDead) return;

            var modifiers = MovementModifiers.Default;
            float visionTarget = 0f;

            var slots = inventory.Slots;
            for (int i = 0; i < slots.Count && !IsDead; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.box.effects == null) continue;

                var ctx = new EffectContext(this, slot.heldTime, deltaTime, Hazards);
                foreach (var effect in slot.box.effects)
                {
                    if (effect == null) continue;
                    effect.Tick(ctx);
                    if (IsDead) break;
                    effect.ModifyMovement(ref modifiers, ctx);
                    visionTarget = Mathf.Max(visionTarget, effect.VisionObstruction(slot.heldTime));
                }
            }

            if (IsDead) return;

            Modifiers = modifiers;
            float rate = visionTarget > VisionObstruction ? visionRiseSpeed : visionFallSpeed;
            VisionObstruction = Mathf.MoveTowards(VisionObstruction, visionTarget, rate * deltaTime);

            // With teammates around, health only comes back through hugs.
            if (!HugsRequired && health < maxHealth && SimTime - lastDamageTime >= regenDelay)
                health = Mathf.Min(maxHealth, health + regenPerSecond * deltaTime);
        }

        public void Damage(float amount, DeathCause cause)
        {
            EnsureInit();
            if (IsDead || amount <= 0f) return;

            health -= amount;
            lastDamageTime = SimTime;
            Damaged?.Invoke(amount, cause);

            if (health <= 0f) Die(cause);
        }

        /// <summary>Restores health (a hug, for example). Does nothing to the dead.</summary>
        public void Heal(float amount)
        {
            EnsureInit();
            if (IsDead || amount <= 0f || health >= maxHealth) return;
            health = Mathf.Min(maxHealth, health + amount);
            Healed?.Invoke(amount);
        }

        public void Kill(DeathCause cause)
        {
            EnsureInit();
            if (IsDead) return;
            health = 0f;
            Die(cause);
        }

        public void Revive()
        {
            EnsureInit();
            IsDead = false;
            health = maxHealth;
            VisionObstruction = 0f;
            Modifiers = MovementModifiers.Default;
        }

        public bool TryUseCooldown(string key, float interval)
        {
            if (cooldowns.TryGetValue(key, out float next) && SimTime < next) return false;
            cooldowns[key] = SimTime + interval;
            return true;
        }

        public void RaiseShock() => Shocked?.Invoke();

        void Die(DeathCause cause)
        {
            health = 0f;
            IsDead = true;
            LastCause = cause;
            Modifiers = MovementModifiers.Default;
            Died?.Invoke(cause);
        }
    }
}
