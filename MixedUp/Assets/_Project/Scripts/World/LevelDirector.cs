using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Sets the level up for the chosen game mode before anything else starts: decides the order, places the boxes on the
    /// map's spawn points, sets the time limit and switches on the night. The level itself is the same in every mode.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class LevelDirector : MonoBehaviour
    {
        public static LevelDirector Instance { get; private set; }

        public Truck truck;
        public CombinationRules rules;
        [Tooltip("Every kind of box the random modes may order.")]
        public BoxData[] boxKinds = System.Array.Empty<BoxData>();
        public GameObject boxPickupPrefab;
        [Tooltip("Used for every other member of a room (the scene starts with one stand-in teammate for solo play).")]
        public GameObject teammatePrefab;
        public PlayerPalette palette;
        [Tooltip("Glow that makes boxes easy to find in the dark.")]
        public GameObject beaconPrefab;
        public NightLighting night;
        [Tooltip("Where each player of an online game starts, in the order they joined. Empty = the default spots of the first map.")]
        public Vector3[] onlineSpots = System.Array.Empty<Vector3>();

        /// <summary>The mode to use instead of the saved choice (tests set it before loading the scene).</summary>
        public static GameModeInfo Override;

        readonly List<BoxPickup> pickups = new List<BoxPickup>();
        Transform boxesRoot;

        public GameModeInfo Mode { get; private set; }
        public IReadOnlyList<BoxPickup> Pickups => pickups;
        public int Seed { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            Override = null;
        }

        void Awake()
        {
            Instance = this;
            var room = RoomSession.Active;
            bool online = OnlineMatch.Started;
            Mode = Override ?? (online ? OnlineMatch.Mode : null) ?? (room != null ? GameModes.Find(room.modeId) : null) ?? GameModes.Selected;
            Seed = online && OnlineMatch.Seed != 0 ? OnlineMatch.Seed : Random.Range(1, int.MaxValue);
            var rng = new System.Random(Seed);

            if (truck != null) truck.order = OrderFactory.Create(Mode, boxKinds, rules, truck.order, rng);

            if (online) foreach (var dummy in FindObjectsByType<TeammateDummy>()) Destroy(dummy.gameObject);
            else if (RoomSession.IsMultiplayer) SpawnRoomMates();

            boxesRoot = new GameObject("Boxes").transform;
            PlaceBoxes(rng);
            foreach (var area in FindObjectsByType<SpawnAreaLights>()) area.Refresh();

            if (GameManager.Instance != null) GameManager.Instance.timeLimitSeconds = Mode.timeLimit;
            if (Mode.night && night != null) night.Apply();
        }

        void Start()
        {
            // In an online game every player starts on their own spot instead of all on the same one.
            var local = PlayerRegistry.Local;
            if (!OnlineMatch.Started || local == null || NetAvatar.Local == null) return;

            var spots = onlineSpots.Length > 0 ? onlineSpots : DefaultOnlineSpots;
            int index = Mathf.Max(0, NetAvatar.Sorted().IndexOf(NetAvatar.Local)) % spots.Length;
            local.Teleport(spots[index]);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------ room mates

        static readonly Vector3[] MateSpots =
        {
            new Vector3(-4f, 0.05f, -31f), new Vector3(0f, 0.05f, -34f), new Vector3(8f, 0.05f, -31f)
        };

        static readonly Vector3[] DefaultOnlineSpots =
        {
            new Vector3(4f, 0.05f, -31f), new Vector3(-4f, 0.05f, -31f), new Vector3(0f, 0.05f, -34f), new Vector3(8f, 0.05f, -31f)
        };

        /// <summary>One character for every other member of the room, in their own colours and with their name overhead.</summary>
        void SpawnRoomMates()
        {
            if (teammatePrefab == null) return;
            foreach (var dummy in FindObjectsByType<TeammateDummy>()) Destroy(dummy.gameObject);

            var others = RoomSession.Others();
            for (int i = 0; i < others.Count && i < MateSpots.Length; i++)
            {
                var member = others[i];
                var mate = Instantiate(teammatePrefab, MateSpots[i], Quaternion.identity);
                mate.name = "Mate_" + member.name;

                var appearance = mate.GetComponent<PlayerAppearance>();
                if (appearance != null && palette != null) appearance.SetColors(palette.Skin(member.skin), palette.Clothes(member.clothes));
                var label = mate.GetComponent<OverheadLabel>();
                if (label != null) label.literalName = member.name;
            }
        }

        // ---------------------------------------------------------------- boxes

        void PlaceBoxes(System.Random rng)
        {
            if (truck == null || truck.order == null) return;

            // The boxes to put out: one entry per box of the order.
            var wanted = new List<BoxData>();
            foreach (var line in truck.order.lines)
                for (int i = 0; i < line.count; i++) wanted.Add(line.box);

            var points = new List<BoxSpawnPoint>(FindObjectsByType<BoxSpawnPoint>());
            points.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            if (Mode.spawn == SpawnStyle.Classic) PlaceClassic(wanted, points);
            else PlaceAtRandom(wanted, points, rng, Mode.spawn == SpawnStyle.Hard);
        }

        void PlaceClassic(List<BoxData> wanted, List<BoxSpawnPoint> points)
        {
            var free = new List<BoxSpawnPoint>(points.FindAll(p => p.original));
            foreach (var box in wanted)
            {
                int index = free.FindIndex(p => p.classicBoxId == box.id);
                if (index < 0) index = free.FindIndex(p => string.IsNullOrEmpty(p.classicBoxId));
                if (index < 0) index = free.Count > 0 ? 0 : -1;
                if (index < 0) continue;

                Spawn(box, free[index]);
                free.RemoveAt(index);
            }
        }

        void PlaceAtRandom(List<BoxData> wanted, List<BoxSpawnPoint> points, System.Random rng, bool hard)
        {
            var free = points.FindAll(p => p.hard == hard);
            // The hard mode still needs enough spots; borrow ordinary ones only if there are too few.
            if (hard && free.Count < wanted.Count) free.AddRange(points.FindAll(p => !p.hard));

            for (int i = free.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (free[i], free[j]) = (free[j], free[i]);
            }

            for (int i = 0; i < wanted.Count && i < free.Count; i++) Spawn(wanted[i], free[i]);
        }

        void Spawn(BoxData data, BoxSpawnPoint point)
        {
            point.InUse = true;
            SpawnAt(data, point.Position);
        }

        /// <summary>Puts a box that somebody was carrying back into the world, spread in a small ring round `around`.</summary>
        public BoxPickup Drop(BoxData data, Vector3 around, int index, int count)
        {
            float angle = (index / (float)Mathf.Max(1, count)) * Mathf.PI * 2f;
            Vector3 spot = around + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (count > 1 ? 0.8f : 0.4f);
            if (Physics.Raycast(spot + Vector3.up * 2f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) spot.y = hit.point.y + 0.6f;
            else spot = around + Vector3.up * 0.6f;
            return SpawnAt(data, spot);
        }

        BoxPickup SpawnAt(BoxData data, Vector3 position)
        {
            if (boxPickupPrefab == null || data == null) return null;
            if (boxesRoot == null) boxesRoot = new GameObject("Boxes").transform;

            var go = Instantiate(boxPickupPrefab, position, Quaternion.identity, boxesRoot);
            go.name = "Box_" + data.id;
            var pickup = go.GetComponent<BoxPickup>();
            pickup.data = data;
            if (data.worldPrefab != null && pickup.visual != null) Instantiate(data.worldPrefab, pickup.visual);
            if (Mode.night && beaconPrefab != null)
            {
                var beacon = Instantiate(beaconPrefab, go.transform);
                beacon.transform.localPosition = Vector3.up * 0.7f;
                beacon.GetComponent<BoxBeacon>()?.Tint(data.color);
            }
            pickups.Add(pickup);
            return pickup;
        }
    }
}
