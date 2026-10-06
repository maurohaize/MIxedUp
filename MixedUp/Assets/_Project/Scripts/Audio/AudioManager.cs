using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MixedUp
{
    /// <summary>
    /// Plays all the sound of the game: effects (pooled sources), music for the menu and for the levels, the ambience of the
    /// meadow (wind, river, birds). Created automatically; it listens to the game's own events, so gameplay code only needs
    /// to raise them. Volumes come from GameSettings.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        const int PoolSize = 20;

        public static AudioManager Instance { get; private set; }

        /// <summary>How many effects have been requested, and the last one (tests and debugging).</summary>
        public static int PlayedCount { get; private set; }
        public static SfxId LastPlayed { get; private set; }
        public static readonly Dictionary<SfxId, int> Counts = new Dictionary<SfxId, int>();

        readonly List<AudioSource> pool = new List<AudioSource>();
        AudioSource musicA, musicB, wind, river;
        MusicId? currentMusic;
        bool musicOnA = true;
        float musicFade = 1f;
        float nextBird;
        Truck truck;
        GameManager game;
        int variantCounter;

        public MusicId? CurrentMusic => currentMusic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            PlayedCount = 0;
            Counts.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            go.AddComponent<AudioManager>();
        }

        /// <summary>Plays an effect. `position` null = flat (not 3D), like interface sounds.</summary>
        public static void Play(SfxId id, Vector3? position = null, float volume = 1f, float pitch = 1f)
        {
            PlayedCount++;
            LastPlayed = id;
            Counts[id] = Counts.TryGetValue(id, out int n) ? n + 1 : 1;
            if (Instance != null) Instance.Emit(id, position, volume, pitch);
        }

        public static int CountOf(SfxId id) => Counts.TryGetValue(id, out int n) ? n : 0;

        // ------------------------------------------------------------------ setup

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            for (int i = 0; i < PoolSize; i++) pool.Add(MakeSource("Sfx" + i, false));
            musicA = MakeSource("MusicA", true);
            musicB = MakeSource("MusicB", true);
            wind = MakeSource("Wind", true);
            river = MakeSource("River", true);
            wind.clip = ProceduralAudio.Wind();
            river.clip = ProceduralAudio.River();

            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            Subscribe();
            nextBird = Time.time + 5f;
        }

        AudioSource MakeSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.minDistance = 3f;
            source.maxDistance = 45f;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Unsubscribe();
            }
        }

        void Subscribe()
        {
            WaterEffects.Splashed += OnSplash;
            PlayerPush.Pushed += OnPushed;
            PlayerPush.Whiffed += OnWhiff;
            PlayerHug.Started += OnHug;
            Snowman.Collapsed += OnSnowman;
            RubberDuck.Squeaked += OnDuck;
            IceTrailEmitter.Placed += OnIce;
            MovingRaft.Arrived += OnRaft;
            GustZone.PhaseChanged += OnGust;
            Sweeper.Hit += OnSweeperHit;
            BouncePad.Bounced += OnBounce;
            BoxPickup.Collected += OnPickup;
        }

        void Unsubscribe()
        {
            WaterEffects.Splashed -= OnSplash;
            PlayerPush.Pushed -= OnPushed;
            PlayerPush.Whiffed -= OnWhiff;
            PlayerHug.Started -= OnHug;
            Snowman.Collapsed -= OnSnowman;
            RubberDuck.Squeaked -= OnDuck;
            IceTrailEmitter.Placed -= OnIce;
            MovingRaft.Arrived -= OnRaft;
            GustZone.PhaseChanged -= OnGust;
            Sweeper.Hit -= OnSweeperHit;
            BouncePad.Bounced -= OnBounce;
            BoxPickup.Collected -= OnPickup;
        }

        void OnSplash(Vector3 position, float strength) => Play(SfxId.Splash, position, Mathf.Clamp(strength, 0.4f, 1f));
        void OnPushed(PlayerPush push, IPushable target) => Play(SfxId.Push, push.transform.position);
        void OnWhiff(PlayerPush push) => Play(SfxId.Whoosh, push.transform.position, 0.5f);
        void OnHug(PlayerHug hug, PlayerStatus partner) { Play(SfxId.Hug, hug.transform.position); Play(SfxId.Heal, hug.transform.position, 0.7f); }
        void OnSnowman(Snowman snowman) => Play(SfxId.Collapse, snowman.transform.position);
        void OnDuck(RubberDuck duck) => Play(SfxId.Squeak, duck.transform.position);
        void OnIce(Vector3 position) => Play(SfxId.IceCrack, position, 0.8f);
        void OnRaft(MovingRaft raft) => Play(SfxId.Bell, raft.transform.position, 0.6f);
        void OnGust(GustZone zone, GustZone.Phase phase) { if (phase != GustZone.Phase.Calm) Play(SfxId.Gust, zone.transform.position, phase == GustZone.Phase.Warning ? 0.4f : 0.9f); }
        void OnSweeperHit(Sweeper sweeper, PlayerController player) => Play(SfxId.Whoosh, sweeper.transform.position);
        void OnBounce(BouncePad pad) => Play(SfxId.Bounce, pad.transform.position);
        void OnPickup(BoxPickup box) => Play(SfxId.Pickup, box.transform.position);

        // ------------------------------------------------------------------ scenes

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool menu = scene.name == GameManager.MainMenuScene;
            SetMusic(menu ? MusicId.Menu : MusicId.Game);

            if (truck != null) truck.BoxDelivered -= OnDelivered;
            if (game != null) game.StateChanged -= OnGameState;
            truck = FindAnyObjectByType<Truck>();
            game = GameManager.Instance;
            if (truck != null) truck.BoxDelivered += OnDelivered;
            if (game != null) game.StateChanged += OnGameState;

            bool outdoors = !menu;
            if (outdoors)
            {
                if (!wind.isPlaying) wind.Play();
                if (!river.isPlaying) river.Play();
            }
            else
            {
                wind.Stop();
                river.Stop();
            }
        }

        void OnDelivered(BoxData box) => Play(SfxId.Deliver, truck != null ? truck.InteractionTransform.position : (Vector3?)null);

        void OnGameState(GameState state)
        {
            if (state == GameState.Results) Play(SfxId.Win);
            else if (state == GameState.GameOver) Play(SfxId.Lose);
        }

        void SetMusic(MusicId id)
        {
            if (currentMusic == id) return;
            currentMusic = id;
            var next = musicOnA ? musicB : musicA;
            next.clip = ProceduralAudio.Music(id);
            next.volume = 0f;
            next.Play();
            musicOnA = !musicOnA;
            musicFade = 0f;
        }

        // ------------------------------------------------------------------ playing

        void Emit(SfxId id, Vector3? position, float volume, float pitch)
        {
            var source = FreeSource();
            if (source == null) return;

            var clip = ProceduralAudio.Get(id, variantCounter++ + Random.Range(0, 3));
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume * GameSettings.SfxVolume);
            source.pitch = pitch * Random.Range(0.96f, 1.04f);
            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.spatialBlend = 0.8f;
            }
            else
            {
                source.spatialBlend = 0f;
            }
            source.Play();
        }

        AudioSource FreeSource()
        {
            AudioSource oldest = null;
            foreach (var source in pool)
            {
                if (!source.isPlaying) return source;
                if (oldest == null || source.time > oldest.time) oldest = source;
            }
            return oldest;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Crossfade the music: the new track rises while the old one falls.
            musicFade = Mathf.MoveTowards(musicFade, 1f, dt / 1.5f);
            var incoming = musicOnA ? musicA : musicB;
            var outgoing = musicOnA ? musicB : musicA;
            float level = GameSettings.MusicVolume * 0.6f;
            incoming.volume = level * musicFade;
            outgoing.volume = level * (1f - musicFade);
            if (musicFade >= 1f && outgoing.isPlaying) outgoing.Stop();

            // Ambience follows the listener: the river is loud only near the river.
            var listener = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            float riverDistance = Mathf.Max(0f, Mathf.Abs(listener.z - 9f) - 4f);
            wind.volume = GameSettings.SfxVolume * 0.18f;
            river.volume = GameSettings.SfxVolume * 0.45f * Mathf.Clamp01(1f - riverDistance / 24f);

            // Now and then a bird somewhere nearby.
            if (wind.isPlaying && Time.time >= nextBird)
            {
                nextBird = Time.time + Random.Range(5f, 12f);
                var spot = listener + new Vector3(Random.Range(-14f, 14f), Random.Range(4f, 9f), Random.Range(-14f, 14f));
                var source = FreeSource();
                if (source != null)
                {
                    source.clip = ProceduralAudio.Bird(Random.Range(0, 4));
                    source.volume = GameSettings.SfxVolume * 0.4f;
                    source.pitch = Random.Range(0.9f, 1.15f);
                    source.transform.position = spot;
                    source.spatialBlend = 0.9f;
                    source.Play();
                }
            }
        }
    }
}
