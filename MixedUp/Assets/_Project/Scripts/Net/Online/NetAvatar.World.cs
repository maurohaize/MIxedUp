using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MixedUp
{
    /// <summary>
    /// The world side of an avatar: its ghost can be bumped, hugged and handed boxes; and the remote calls that carry
    /// what happens in the level (boxes taken and dropped, deliveries, the truck puzzle) between the machines.
    /// The rules behind each call live in NetWorld; here is only the wiring.
    /// </summary>
    public partial class NetAvatar
    {
        static Dictionary<string, BoxData> boxCache;

        // ============================================================ the ghost

        void SetUpGhost()
        {
            if (ghost == null) return;

            // The ghost's health, boxes and death come from its owner's machine: nothing here may change them by itself.
            if (status != null)
            {
                status.IsMirror = true;
                status.Died -= OnGhostDied;
                status.Died += OnGhostDied;
            }

            // Solid, so it can be bumped into, shoved, hugged and handed boxes like any other character.
            if (ghost.GetComponent<Collider>() == null)
            {
                var capsule = ghost.AddComponent<CapsuleCollider>();
                capsule.height = 1.6f;
                capsule.radius = 0.38f;
                capsule.center = new Vector3(0f, 0.8f, 0f);
            }
            if (ghost.GetComponent<PlayerPassTarget>() == null) ghost.AddComponent<PlayerPassTarget>().allowTakeBack = true;
            if (ghost.GetComponent<NetGhost>() == null) ghost.AddComponent<NetGhost>().owner = this;
        }

        void OnGhostDied(DeathCause cause) => GameEvents.RaiseToast("toast.teammate_died", cause.Localized);

        DeathCause GhostDeathCause()
        {
            string key = deathKey.Value.ToString();
            return string.IsNullOrEmpty(key) ? DeathCause.Void : new DeathCause(key);
        }

        void PublishCarried(PlayerController local)
        {
            if (local == null || local.Status == null) return;

            var text = new StringBuilder();
            var slots = local.Status.Inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty) continue;
                if (text.Length > 0) text.Append(',');
                text.Append(slots[i].box.id);
            }
            string now = text.ToString();
            if (now != lastCarried)
            {
                lastCarried = now;
                carried.Value = Fixed64(now);
            }

            string key = local.Status.IsDead ? local.Status.LastCause.Key ?? string.Empty : string.Empty;
            if (key != lastDeathKey)
            {
                lastDeathKey = key;
                deathKey.Value = Fixed64(key);
            }
        }

        void ApplyCarried(string csv)
        {
            if (status == null || csv == appliedCarried) return;
            appliedCarried = csv;

            var inventory = status.Inventory;
            inventory.Clear();
            if (string.IsNullOrEmpty(csv)) return;
            foreach (var id in csv.Split(','))
            {
                var box = ResolveBox(id);
                if (box != null) inventory.TryAdd(box, out _);
            }
        }

        static BoxData ResolveBox(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var director = LevelDirector.Instance;
            if (director != null)
            {
                var found = director.FindBox(id);
                if (found != null) return found;
            }
            if (boxCache == null) boxCache = new Dictionary<string, BoxData>();
            if (boxCache.TryGetValue(id, out var cached) && cached != null) return cached;
            foreach (var kind in Resources.FindObjectsOfTypeAll<BoxData>())
                if (kind != null && !string.IsNullOrEmpty(kind.id)) boxCache[kind.id] = kind;
            return boxCache.TryGetValue(id, out cached) ? cached : null;
        }

        /// <summary>The avatar whose ghost is this object (null when it is not a ghost).</summary>
        public static NetAvatar OfGhost(GameObject ghostObject)
        {
            if (ghostObject == null) return null;
            foreach (var avatar in All)
                if (avatar != null && avatar.ghost == ghostObject) return avatar;
            return null;
        }

        /// <summary>The avatar of the player with this client number.</summary>
        public static NetAvatar OfClient(ulong clientId)
        {
            foreach (var avatar in All)
                if (avatar != null && avatar.IsSpawned && avatar.OwnerClientId == clientId) return avatar;
            return null;
        }

        // ============================================================ the go signal

        bool levelHooked;

        void HookLevelLoaded()
        {
            if (levelHooked || NetworkManager == null || NetworkManager.SceneManager == null) return;
            levelHooked = true;
            NetworkManager.SceneManager.OnLoadEventCompleted += OnLevelLoadedEverywhere;
        }

        void UnhookLevelLoaded()
        {
            if (!levelHooked) return;
            levelHooked = false;
            if (NetworkManager != null && NetworkManager.SceneManager != null)
                NetworkManager.SceneManager.OnLoadEventCompleted -= OnLevelLoadedEverywhere;
        }

        void OnLevelLoadedEverywhere(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            if (!IsServer) return;
            foreach (var level in LevelCatalog.All)
            {
                if (level.scene != sceneName) continue;
                GoRpc();
                return;
            }
        }

        // ============================================================ remote calls: the level

        /// <summary>Everybody: the level is loaded on every machine, start the clock and let the players move.</summary>
        [Rpc(SendTo.Everyone)]
        void GoRpc() => NetWorld.Go();

        /// <summary>Client to host: "I picked this box up".</summary>
        [Rpc(SendTo.Server)]
        public void PickupRpc(int boxId, ulong by) => NetWorld.HostPickup(boxId, by);

        /// <summary>Host to clients: that player has the box now (it disappears from the world).</summary>
        [Rpc(SendTo.NotServer)]
        public void BoxTakenRpc(int boxId, ulong by) => NetWorld.OnBoxTaken(boxId, by);

        /// <summary>Host to clients: somebody was faster; that player must give the box back.</summary>
        [Rpc(SendTo.NotServer)]
        public void PickupDeniedRpc(int boxId, ulong by) => NetWorld.OnPickupDenied(boxId, by);

        /// <summary>A player dropped a box (they died): it appears on every other machine.</summary>
        [Rpc(SendTo.NotMe)]
        public void BoxDroppedRpc(int boxId, string kind, Vector3 position) => NetWorld.OnBoxDropped(boxId, kind, position);

        /// <summary>Client to host: "I put this box into the truck".</summary>
        [Rpc(SendTo.Server)]
        public void DeliverRpc(string kind, ulong by) => NetWorld.HostDeliver(kind, by);

        /// <summary>Host to clients: the truck took this box.</summary>
        [Rpc(SendTo.NotServer)]
        public void DeliveredRpc(string kind) => NetWorld.OnDelivered(kind);

        /// <summary>Host to clients: the truck did not need that box any more; it goes back to the player.</summary>
        [Rpc(SendTo.NotServer)]
        public void DeliverDeniedRpc(string kind, ulong by) => NetWorld.OnDeliverDenied(kind, by);

        /// <summary>Client to host: how the level was built there (compared with the host's).</summary>
        [Rpc(SendTo.Server)]
        public void LayoutRpc(ulong from, int hash) => NetWorld.OnLayoutReported(from, hash);

        // ============================================================ remote calls: the truck puzzle

        [Rpc(SendTo.Server)]
        public void PuzzleSwapRpc(int a, int b) => NetWorld.HostSwap(a, b);

        [Rpc(SendTo.Server)]
        public void PuzzleTravelRpc() => NetWorld.HostTravel();

        /// <summary>Host to clients: the cargo hold as it is now ("box,box,box") and whether the truck is already driving.</summary>
        [Rpc(SendTo.NotServer)]
        public void PuzzleStateRpc(string arrangement, bool traveling) => NetWorld.OnPuzzleState(arrangement, traveling);

        /// <summary>Host to clients: the trip is over, and which pair of boxes decided it (-1 = all safe).</summary>
        [Rpc(SendTo.NotServer)]
        public void PuzzleResolvedRpc(string arrangement, int pair, bool fuseExpired, int reward, int bonus) =>
            NetWorld.OnPuzzleResolved(arrangement, pair, fuseExpired, reward, bonus);

        /// <summary>A player knocked the snowman down (its number among the level's snowmen, and from where).</summary>
        [Rpc(SendTo.NotMe)]
        public void SnowmanRpc(int index, Vector3 from) => NetWorld.OnSnowman(index, from);

        /// <summary>A player left a slab of ice in the river.</summary>
        [Rpc(SendTo.NotMe)]
        public void IceRpc(Vector3 position) => NetWorld.OnIce(position);

        // ============================================================ remote calls: between players
        // These are called on the avatar of the player they are meant for and run on that player's machine (SendTo.Owner).

        /// <summary>Somebody hands this player a box.</summary>
        [Rpc(SendTo.Owner)]
        void ReceiveBoxRpc(string kind, ulong from)
        {
            var local = PlayerRegistry.Local;
            var box = ResolveBox(kind);
            if (local != null && box != null && !local.Status.IsDead && local.Status.Inventory.TryAdd(box, out _)) return;

            // No room (or no longer alive): it goes back to the giver.
            OfClient(from)?.ReturnBoxRpc(kind);
        }

        /// <summary>The box this player handed over did not fit: it comes back.</summary>
        [Rpc(SendTo.Owner)]
        void ReturnBoxRpc(string kind) => NetWorld.GiveBack(kind);

        /// <summary>Somebody wants every box this player has (as many as `space` of theirs).</summary>
        [Rpc(SendTo.Owner)]
        void GiveAllRpc(int space, ulong requester)
        {
            var local = PlayerRegistry.Local;
            var taker = OfClient(requester);
            if (local == null || taker == null || local.Status.IsDead) return;

            var inventory = local.Status.Inventory;
            for (int moved = 0; moved < space && inventory.Count > 0; moved++)
            {
                var box = inventory.RemoveAt(inventory.FirstOccupiedIndex());
                if (box != null) taker.ReceiveBoxRpc(box.id, OwnerClientId);
            }
        }

        /// <summary>This player is shoved.</summary>
        [Rpc(SendTo.Owner)]
        void PushRpc(Vector3 impulse, float upSpeed)
        {
            var local = PlayerRegistry.Local;
            if (local != null && !local.Status.IsDead) local.AddImpulse(impulse, upSpeed);
        }

        /// <summary>Somebody hugs this player: they hold still and recover health for a while.</summary>
        [Rpc(SendTo.Owner)]
        void HugRpc(float seconds, float healPerSecond)
        {
            var local = PlayerRegistry.Local;
            if (local == null || local.Status.IsDead) return;
            local.LockMovement(seconds);
            StartCoroutine(HealOverTime(local.Status, seconds, healPerSecond));
        }

        static IEnumerator HealOverTime(PlayerStatus target, float seconds, float perSecond)
        {
            for (float t = 0f; t < seconds && target != null && !target.IsDead; t += Time.deltaTime)
            {
                target.Heal(perSecond * Time.deltaTime);
                yield return null;
            }
        }

        // ---- what the local player's own scripts call (through NetGhost and PlayerPassTarget)

        /// <summary>Hand a box to the player this ghost shows.</summary>
        public void GiveBoxTo(BoxData box)
        {
            if (box == null || Local == null) return;
            ReceiveBoxRpc(box.id, Local.OwnerClientId);
        }

        /// <summary>Ask the player this ghost shows for all their boxes that fit in `space` free places.</summary>
        public void TakeAllFrom(int space)
        {
            if (space <= 0 || Local == null) return;
            GiveAllRpc(space, Local.OwnerClientId);
        }

        public void SendPush(Vector3 impulse, float upSpeed) => PushRpc(impulse, upSpeed);

        public void SendHug(float seconds, float healPerSecond) => HugRpc(seconds, healPerSecond);
    }
}
