using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// What an online game shares besides the players' poses: which boxes are still lying around, who delivered what to the
    /// truck, and the truck puzzle. Every machine runs the same level (same seed); the host is the referee for anything two
    /// players could do at the same moment (taking the same box, filling the last place in the order, rearranging the cargo).
    /// Everything travels as RPCs on the players' NetAvatar (see NetAvatar.World.cs); this class holds the rules.
    /// Offline, Active is false and none of it does anything.
    /// </summary>
    public static class NetWorld
    {
        /// <summary>Boxes dropped by a dead player are numbered from here (the starting boxes use the numbers below).</summary>
        public const int DropIdBase = 10000;
        const float WaitTimeout = 25f;

        static readonly HashSet<int> taken = new HashSet<int>();   // host: boxes somebody has already picked up
        static readonly HashSet<int> gone = new HashSet<int>();    // everybody: boxes known to be gone (also those not spawned yet)
        static readonly Dictionary<ulong, int> clientLayouts = new Dictionary<ulong, int>();
        static int dropCounter;
        static float waitDeadline;
        static string ownLayout;
        static int ownLayoutHash;

        static TruckPuzzleState puzzle;
        static TruckPuzzleController puzzleController;
        static string pendingArrangement;
        static bool pendingTravel;
        static bool pendingResolved;
        static int pendingPair;
        static bool pendingFuse;

        /// <summary>An online match is running and this machine has its own avatar: world events travel the network.</summary>
        public static bool Active => OnlineSession.IsOnline && OnlineMatch.Started && NetAvatar.Local != null;

        /// <summary>The moving parts of the level follow LevelClock instead of their own timers (online, after the go signal).</summary>
        public static bool SharedClock => !Waiting && Active;

        /// <summary>The level is loaded here but not on every machine yet: nobody moves until the host says go.</summary>
        public static bool Waiting { get; private set; }

        static bool IsHost => OnlineSession.IsHost;
        static ulong MyId => NetAvatar.Local != null ? NetAvatar.Local.OwnerClientId : 0UL;
        static LevelDirector Director => LevelDirector.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Reset();
            Waiting = false;
        }

        static void Reset()
        {
            taken.Clear();
            gone.Clear();
            clientLayouts.Clear();
            dropCounter = 0;
            ownLayout = null;
            ownLayoutHash = 0;
            puzzle = null;
            puzzleController = null;
            pendingArrangement = null;
            pendingTravel = false;
            pendingResolved = false;
        }

        // ------------------------------------------------------------ level start

        /// <summary>A level scene is starting on this machine.</summary>
        public static void BeginLevel()
        {
            Reset();
            Waiting = OnlineSession.IsOnline && OnlineMatch.Started;
            waitDeadline = Time.unscaledTime + WaitTimeout;
            if (!Waiting) LevelClock.Begin();
        }

        /// <summary>The host says every machine has the level: the clock starts and everybody may move.</summary>
        public static void Go()
        {
            if (!Waiting) return;
            Waiting = false;
            LevelClock.Begin();
        }

        /// <summary>Called every frame by the local avatar: do not stay frozen for ever if the go signal never comes.</summary>
        public static void Tick()
        {
            if (Waiting && Time.unscaledTime >= waitDeadline) Go();
        }

        /// <summary>Compares how this machine built the level with how the host built it (a safety net: they should match).</summary>
        public static void ReportLayout(string signature)
        {
            if (!Active) return;
            ownLayout = signature;
            ownLayoutHash = StableHash(signature);
            if (IsHost) CompareLayouts();
            else NetAvatar.Local.LayoutRpc(MyId, ownLayoutHash);
        }

        internal static void OnLayoutReported(ulong from, int hash)
        {
            clientLayouts[from] = hash;
            CompareLayouts();
        }

        static void CompareLayouts()
        {
            if (ownLayout == null) return;
            foreach (var pair in clientLayouts)
            {
                if (pair.Value == ownLayoutHash) continue;
                Debug.LogWarning("Online: the level of client " + pair.Key + " differs from the host's.");
                GameEvents.RaiseToast("toast.out_of_sync");
            }
            clientLayouts.Clear();
        }

        static int StableHash(string text)
        {
            unchecked
            {
                int hash = (int)2166136261;
                foreach (char c in text ?? string.Empty) hash = (hash ^ c) * 16777619;
                return hash;
            }
        }

        // ------------------------------------------------------------ boxes

        /// <summary>The local player picked a box up (it is already in their hands).</summary>
        public static void PickedUp(BoxPickup pickup)
        {
            if (!Active || pickup == null || pickup.NetId < 0) return;
            if (IsHost) HostPickup(pickup.NetId, MyId);
            else NetAvatar.Local.PickupRpc(pickup.NetId, MyId);
        }

        /// <summary>Host: first come, first served. The loser gives the box back.</summary>
        internal static void HostPickup(int id, ulong by)
        {
            if (!taken.Add(id))
            {
                if (by == MyId) UndoPickup(id);
                else NetAvatar.Local.PickupDeniedRpc(id, by);
                return;
            }
            gone.Add(id);
            if (by != MyId) RemoveBox(id);
            NetAvatar.Local.BoxTakenRpc(id, by);
        }

        internal static void OnBoxTaken(int id, ulong by)
        {
            if (by == MyId) gone.Add(id);
            else RemoveBox(id);
        }

        internal static void OnPickupDenied(int id, ulong by)
        {
            if (by == MyId) UndoPickup(id);
        }

        static void RemoveBox(int id)
        {
            gone.Add(id);
            var pickup = Director != null ? Director.FindPickup(id) : null;
            if (pickup != null) pickup.RemoteTake();
        }

        /// <summary>Somebody else grabbed the box a split second earlier: take it out of this player's hands again.</summary>
        static void UndoPickup(int id)
        {
            var pickup = Director != null ? Director.FindPickup(id) : null;
            var local = PlayerRegistry.Local;
            if (pickup == null || pickup.data == null || local == null) return;

            var inventory = local.Status.Inventory;
            for (int i = 0; i < inventory.Capacity; i++)
            {
                if (inventory.Slots[i].box != pickup.data) continue;
                inventory.RemoveAt(i);
                return;
            }
        }

        /// <summary>A number for a box that this machine drops (unique across the players).</summary>
        public static int NewDropId() => DropIdBase + (int)(MyId % 100UL) * 1000 + dropCounter++;

        public static void AnnounceDrop(int id, BoxData data, Vector3 position)
        {
            if (!Active || data == null) return;
            NetAvatar.Local.BoxDroppedRpc(id, data.id, position);
        }

        internal static void OnBoxDropped(int id, string boxId, Vector3 position)
        {
            if (gone.Contains(id) || Director == null) return;
            var box = Director.FindBox(boxId);
            if (box != null) Director.SpawnRemote(id, box, position);
        }

        // ------------------------------------------------------------ truck

        /// <summary>The local player puts a box into the truck. The box already left their hands.</summary>
        public static void RequestDeliver(BoxData box)
        {
            if (box == null) return;
            if (IsHost) HostDeliver(box.id, MyId);
            else NetAvatar.Local.DeliverRpc(box.id, MyId);
        }

        internal static void HostDeliver(string boxId, ulong by)
        {
            var truck = Director != null ? Director.truck : null;
            var box = Director != null ? Director.FindBox(boxId) : null;
            if (truck != null && box != null && truck.Accepts(box))
            {
                truck.Deliver(box);
                NetAvatar.Local.DeliveredRpc(boxId);
            }
            else if (by == MyId) GiveBack(boxId);
            else NetAvatar.Local.DeliverDeniedRpc(boxId, by);
        }

        internal static void OnDelivered(string boxId)
        {
            var truck = Director != null ? Director.truck : null;
            var box = Director != null ? Director.FindBox(boxId) : null;
            if (truck != null && box != null) truck.Deliver(box);
        }

        internal static void OnDeliverDenied(string boxId, ulong by)
        {
            if (by == MyId) GiveBack(boxId);
        }

        /// <summary>A box that could not be delivered returns to the player (or falls at their feet when their hands are full).</summary>
        internal static void GiveBack(string boxId)
        {
            var local = PlayerRegistry.Local;
            var box = Director != null ? Director.FindBox(boxId) : null;
            if (local == null || box == null) return;
            if (!local.Status.Inventory.TryAdd(box, out _)) Director.Drop(box, local.transform.position, 0, 1);
        }

        // ------------------------------------------------------------ easter eggs and ice

        /// <summary>The local player knocked the snowman down: it falls on the other machines too.</summary>
        public static void AnnounceSnowman(Snowman snowman, Vector3 from)
        {
            if (!Active || snowman == null) return;
            int index = Snowman.All.IndexOf(snowman);
            if (index >= 0) NetAvatar.Local.SnowmanRpc(index, from);
        }

        internal static void OnSnowman(int index, Vector3 from)
        {
            if (index >= 0 && index < Snowman.All.Count && Snowman.All[index] != null) Snowman.All[index].CollapseFromNetwork(from);
        }

        /// <summary>The local player's streak of jumps over the spinning log changed (0 = it ended): the others' boards follow.</summary>
        public static void AnnounceStreak(Sweeper sweeper, int streak)
        {
            if (!Active || sweeper == null) return;
            int index = Sweeper.All.IndexOf(sweeper);
            if (index >= 0) NetAvatar.Local.JumpRpc(index, streak, MyId);
        }

        internal static void OnStreak(int index, int streak, ulong from)
        {
            var avatar = NetAvatar.OfClient(from);
            if (avatar == null || index < 0 || index >= Sweeper.All.Count || Sweeper.All[index] == null) return;
            string name = avatar.DisplayName;
            if (!string.IsNullOrEmpty(name)) Sweeper.All[index].Board.SetStreak(name, streak);
        }

        /// <summary>The local player left a slab of ice in the river: it appears on the other machines too.</summary>
        public static void AnnounceIce(Vector3 position)
        {
            if (Active) NetAvatar.Local.IceRpc(position);
        }

        internal static void OnIce(Vector3 position)
        {
            var local = PlayerRegistry.Local;
            var emitter = local != null ? local.GetComponent<IceTrailEmitter>() : null;
            if (emitter != null) emitter.PlaceRemote(position);
        }

        // ------------------------------------------------------------ puzzle

        /// <summary>The truck puzzle opened on this machine: from now on the host's arrangement rules it.</summary>
        public static void AttachPuzzle(TruckPuzzleState state, TruckPuzzleController controller)
        {
            if (!Active || state == null) return;

            puzzle = state;
            puzzleController = controller;
            state.Networked = true;
            state.Authoritative = IsHost;
            state.SwapRequested += RequestSwap;
            if (IsHost) state.Resolved += HostResolved;

            // Things the host said before this machine had opened the puzzle.
            if (pendingArrangement != null) ApplyArrangement(pendingArrangement);
            if (pendingTravel) controller.BeginTravelNow();
            if (pendingResolved) state.ForceResolve(pendingPair, pendingFuse);
            pendingArrangement = null;
            pendingTravel = false;
            pendingResolved = false;
        }

        static void RequestSwap(int i, int j)
        {
            if (IsHost) HostSwap(i, j);
            else NetAvatar.Local.PuzzleSwapRpc(i, j);
        }

        public static void RequestTravel()
        {
            if (IsHost) HostTravel();
            else NetAvatar.Local.PuzzleTravelRpc();
        }

        internal static void HostSwap(int i, int j)
        {
            if (puzzle == null || !puzzle.ApplySwap(i, j)) return;
            NetAvatar.Local.PuzzleStateRpc(Encode(puzzle), puzzle.Phase == PuzzlePhase.Traveling);
        }

        internal static void HostTravel()
        {
            if (puzzle == null || puzzleController == null || puzzle.Phase != PuzzlePhase.Arranging) return;
            puzzleController.BeginTravelNow();
            NetAvatar.Local.PuzzleStateRpc(Encode(puzzle), true);
        }

        static void HostResolved(PuzzleResolution resolution)
        {
            if (puzzle == null || !Active) return;
            var paid = puzzleController != null ? puzzleController.LastResult : null;
            int reward = paid != null ? paid.Reward : 0;
            int bonus = paid != null ? paid.TimeBonus : 0;
            NetAvatar.Local.PuzzleResolvedRpc(Encode(puzzle), resolution.Pair, resolution.FuseExpired, reward, bonus);
        }

        // ------------------------------------------------------------ money

        static bool hasAgreedReward;
        static int agreedReward;
        static int agreedBonus;

        /// <summary>
        /// Online, the host works out what the delivery pays and everybody receives exactly that, so the whole team earns the
        /// same. Returns false offline and on the host (which uses its own figures).
        /// </summary>
        public static bool TryTakeAgreedReward(out int reward, out int bonus)
        {
            reward = agreedReward;
            bonus = agreedBonus;
            bool had = hasAgreedReward;
            hasAgreedReward = false;
            return had;
        }

        /// <summary>Client: the host changed the cargo hold (new order of boxes, or the trip has begun).</summary>
        internal static void OnPuzzleState(string arrangement, bool traveling)
        {
            if (puzzle == null)
            {
                pendingArrangement = arrangement;
                pendingTravel |= traveling;
                return;
            }
            ApplyArrangement(arrangement);
            if (traveling && puzzle.Phase == PuzzlePhase.Arranging) puzzleController.BeginTravelNow();
        }

        /// <summary>Client: the host says the trip ended.</summary>
        internal static void OnPuzzleResolved(string arrangement, int pair, bool fuseExpired, int reward, int bonus)
        {
            hasAgreedReward = true;
            agreedReward = reward;
            agreedBonus = bonus;
            if (puzzle == null)
            {
                pendingArrangement = arrangement;
                pendingResolved = true;
                pendingPair = pair;
                pendingFuse = fuseExpired;
                return;
            }
            ApplyArrangement(arrangement);
            if (puzzle.Phase == PuzzlePhase.Arranging) puzzleController.BeginTravelNow();
            puzzle.ForceResolve(pair, fuseExpired);
        }

        static void ApplyArrangement(string arrangement)
        {
            if (puzzle == null || Director == null) return;
            var boxes = new List<BoxData>();
            foreach (var id in (arrangement ?? string.Empty).Split(','))
            {
                var box = Director.FindBox(id);
                if (box == null) return;
                boxes.Add(box);
            }
            puzzle.SetArrangement(boxes);
        }

        static string Encode(TruckPuzzleState state)
        {
            var ids = new string[state.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = state[i] != null ? state[i].id : string.Empty;
            return string.Join(",", ids);
        }
    }
}
