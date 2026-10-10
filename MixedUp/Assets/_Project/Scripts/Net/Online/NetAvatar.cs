using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MixedUp
{
    /// <summary>What a remote player is doing, sent by the player themselves a few times per second.</summary>
    public struct AvatarPose : INetworkSerializable, IEquatable<AvatarPose>
    {
        public Vector3 position;
        public float yaw;
        public float speed;
        public byte flags;

        public const byte Grounded = 1, Crouching = 2, Dead = 4, Hugging = 8;

        public bool Has(byte flag) => (flags & flag) != 0;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref yaw);
            serializer.SerializeValue(ref speed);
            serializer.SerializeValue(ref flags);
        }

        public bool Equals(AvatarPose other) =>
            position == other.position && Mathf.Approximately(yaw, other.yaw) && Mathf.Approximately(speed, other.speed) && flags == other.flags;
    }

    /// <summary>
    /// One connected player, as the network sees them. Every client owns one: it carries the player's name, colours, ready flag
    /// and pose. The owner reads their local character and publishes it; everybody else sees a ghost model that follows it.
    /// The avatar of the host also carries the match settings (map, mode, seed), so every machine builds the same level.
    /// It survives the move from the lobby to the level.
    /// </summary>
    public partial class NetAvatar : NetworkBehaviour
    {
        public static readonly List<NetAvatar> All = new List<NetAvatar>();
        public static NetAvatar Local { get; private set; }
        public static NetAvatar Host
        {
            get
            {
                foreach (var avatar in All)
                    if (avatar != null && avatar.IsSpawned && avatar.IsOwnedByServer) return avatar;
                return null;
            }
        }

        public static event Action Changed;

        [Header("Ghost (what other players see)")]
        public GameObject ghost;
        public PlayerAppearance appearance;
        public PlayerAnimator animator;
        public PlayerStatus status;
        public TMP_Text label;
        public PlayerPalette palette;

        const NetworkVariableReadPermission Everyone = NetworkVariableReadPermission.Everyone;

        readonly NetworkVariable<FixedString32Bytes> displayName =
            new NetworkVariable<FixedString32Bytes>(default, Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<int> skin = new NetworkVariable<int>(0, Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<int> clothes = new NetworkVariable<int>(0, Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<bool> ready = new NetworkVariable<bool>(false, Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<AvatarPose> pose = new NetworkVariable<AvatarPose>(default, Everyone, NetworkVariableWritePermission.Owner);
        // What the player carries ("box,box") and why they died: shown on their ghost on every other machine.
        readonly NetworkVariable<FixedString64Bytes> carried =
            new NetworkVariable<FixedString64Bytes>(default, Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<FixedString64Bytes> deathKey =
            new NetworkVariable<FixedString64Bytes>(default, Everyone, NetworkVariableWritePermission.Owner);

        // Match settings: only the host's avatar ever writes them.
        readonly NetworkVariable<FixedString32Bytes> mapId =
            new NetworkVariable<FixedString32Bytes>(default, Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<FixedString32Bytes> modeId =
            new NetworkVariable<FixedString32Bytes>(default, Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<int> seed = new NetworkVariable<int>(0, Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<bool> started = new NetworkVariable<bool>(false, Everyone, NetworkVariableWritePermission.Server);

        AvatarPose lastSent;
        string lastCarried = string.Empty;
        string lastDeathKey = string.Empty;
        string appliedCarried = string.Empty;
        bool remoteInitialised;
        Vector3 smoothedVelocity;
        Camera cam;

        static FixedString32Bytes Fixed(string text)
        {
            var value = new FixedString32Bytes();
            value.CopyFromTruncated(text ?? string.Empty);
            return value;
        }

        static FixedString64Bytes Fixed64(string text)
        {
            var value = new FixedString64Bytes();
            value.CopyFromTruncated(text ?? string.Empty);
            return value;
        }

        public string DisplayName => displayName.Value.ToString();
        public int Skin => skin.Value;
        public int Clothes => clothes.Value;
        public bool Ready => ready.Value;
        public bool IsHostAvatar => IsSpawned && IsOwnedByServer;
        public string MapId => mapId.Value.ToString();
        public string ModeId => modeId.Value.ToString();
        public int Seed => seed.Value;
        public bool Started => started.Value;
        public AvatarPose CurrentPose => pose.Value;

        /// <summary>The players in the order they joined (the host first).</summary>
        public static List<NetAvatar> Sorted()
        {
            var list = new List<NetAvatar>();
            foreach (var avatar in All)
                if (avatar != null && avatar.IsSpawned) list.Add(avatar);
            list.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));
            return list;
        }

        // ------------------------------------------------------------- network

        public override void OnNetworkSpawn()
        {
            // Dynamically spawned objects must be told to outlive the lobby scene.
            DontDestroyOnLoad(gameObject);
            if (!All.Contains(this)) All.Add(this);

            displayName.OnValueChanged += OnAnyChanged;
            skin.OnValueChanged += OnAnyChanged;
            clothes.OnValueChanged += OnAnyChanged;
            ready.OnValueChanged += OnReadyChanged;
            mapId.OnValueChanged += OnAnyChanged;
            modeId.OnValueChanged += OnAnyChanged;
            started.OnValueChanged += OnReadyChanged;

            if (IsOwner)
            {
                Local = this;
                PushLook();
                CharacterCustomization.Changed += PushLook;
            }
            if (IsServer && IsOwnedByServer)
            {
                mapId.Value = Fixed(LevelCatalog.Prototype.id);
                modeId.Value = Fixed(GameModes.Selected.id);
            }

            if (ghost != null) ghost.SetActive(!IsOwner);
            if (!IsOwner) SetUpGhost();
            if (IsServer && IsOwnedByServer) HookLevelLoaded();
            ApplyLook();
            name = "Avatar_" + DisplayName;
            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            displayName.OnValueChanged -= OnAnyChanged;
            skin.OnValueChanged -= OnAnyChanged;
            clothes.OnValueChanged -= OnAnyChanged;
            ready.OnValueChanged -= OnReadyChanged;
            mapId.OnValueChanged -= OnAnyChanged;
            modeId.OnValueChanged -= OnAnyChanged;
            started.OnValueChanged -= OnReadyChanged;

            if (IsOwner) CharacterCustomization.Changed -= PushLook;
            UnhookLevelLoaded();
            if (Local == this) Local = null;
            All.Remove(this);
            Changed?.Invoke();
        }

        void OnAnyChanged<T>(T previous, T current)
        {
            ApplyLook();
            Changed?.Invoke();
        }

        void OnReadyChanged(bool previous, bool current) => Changed?.Invoke();

        void PushLook()
        {
            if (!IsOwner) return;
            displayName.Value = Fixed(PlayerProfile.Name);
            skin.Value = CharacterCustomization.SkinIndex;
            clothes.Value = CharacterCustomization.ClothesIndex;
        }

        // ------------------------------------------------------------- actions

        public void SetReady(bool value)
        {
            if (IsOwner) ready.Value = value;
        }

        /// <summary>Host only: what to play. Seed 0 means "not decided".</summary>
        public void SetMap(string id)
        {
            if (IsServer && IsOwnedByServer) mapId.Value = Fixed(id);
        }

        public void SetMode(string id)
        {
            if (IsServer && IsOwnedByServer) modeId.Value = Fixed(id);
        }

        /// <summary>Host only: freezes the settings and picks the seed everybody will use.</summary>
        public void BeginMatch(int newSeed)
        {
            if (!IsServer || !IsOwnedByServer) return;
            seed.Value = newSeed;
            started.Value = true;
        }

        /// <summary>Host only: the match is over, back to waiting for players (and for a new seed).</summary>
        public void EndMatch()
        {
            if (!IsServer || !IsOwnedByServer) return;
            started.Value = false;
            seed.Value = 0;
        }

        // ---------------------------------------------------------- appearance

        void ApplyLook()
        {
            if (appearance != null && palette != null)
                appearance.SetColors(palette.Skin(skin.Value), palette.Clothes(clothes.Value));
            if (label != null) label.text = DisplayName;
            if (ghost != null) ghost.name = "Mate_" + DisplayName;
        }

        // -------------------------------------------------------------- update

        void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner) Publish();
            else FollowRemote(Time.deltaTime);
        }

        void Publish()
        {
            var local = PlayerRegistry.Local;
            if (local == null) return;
            NetWorld.Tick();
            PublishCarried(local);

            var next = new AvatarPose
            {
                position = local.transform.position,
                yaw = local.visual != null ? local.visual.eulerAngles.y : local.transform.eulerAngles.y,
                speed = local.HorizontalVelocity.magnitude,
                flags = (byte)((local.IsGrounded ? AvatarPose.Grounded : 0) | (local.IsCrouching ? AvatarPose.Crouching : 0)
                               | (local.Status != null && local.Status.IsDead ? AvatarPose.Dead : 0)
                               | (IsHugging(local) ? AvatarPose.Hugging : 0))
            };

            bool moved = (next.position - lastSent.position).sqrMagnitude > 0.0004f
                         || Mathf.Abs(Mathf.DeltaAngle(next.yaw, lastSent.yaw)) > 1.5f
                         || Mathf.Abs(next.speed - lastSent.speed) > 0.15f
                         || next.flags != lastSent.flags;
            if (!moved) return;

            lastSent = next;
            pose.Value = next;
        }

        void FollowRemote(float dt)
        {
            if (ghost == null) return;

            var target = pose.Value;
            var t = ghost.transform;
            if (!remoteInitialised || (t.position - target.position).sqrMagnitude > 36f)
            {
                t.position = target.position;
                t.rotation = Quaternion.Euler(0f, target.yaw, 0f);
                remoteInitialised = true;
            }
            else
            {
                t.position = Vector3.SmoothDamp(t.position, target.position, ref smoothedVelocity, 0.08f);
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(0f, target.yaw, 0f), 1f - Mathf.Exp(-14f * dt));
            }

            if (animator != null)
            {
                animator.externalSpeed = target.speed;
                animator.externalAirborne = !target.Has(AvatarPose.Grounded);
                animator.externalCrouch = target.Has(AvatarPose.Crouching) ? 1f : 0f;
                animator.externalHugging = target.Has(AvatarPose.Hugging);
            }
            if (status != null)
            {
                bool dead = target.Has(AvatarPose.Dead);
                if (dead && !status.IsDead) status.MirrorDie(GhostDeathCause());
                else if (!dead && status.IsDead) status.Revive();
                ApplyCarried(carried.Value.ToString());
            }

            if (label != null)
            {
                if (cam == null) cam = Camera.main;
                if (cam != null) label.transform.rotation = cam.transform.rotation;
            }
        }
    }
}
