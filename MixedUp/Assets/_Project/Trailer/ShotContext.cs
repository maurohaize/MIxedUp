using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp.Trailer
{
    /// <summary>One shot of the trailer: where it is filmed, how long it lasts, how it is set up and what happens.</summary>
    public sealed class Shot
    {
        public string id;
        public string scene;
        public GameModeInfo mode;
        public float duration = 4f;
        public float preroll = 0.6f;
        /// <summary>Also film a 9:16 take for the vertical trailer.</summary>
        public bool vertical;
        public Action<ShotContext> setup;
        public Func<ShotContext, IEnumerator> action;
    }

    /// <summary>Everything a shot script needs: the cast, the camera, boxes, the truck, subtitles and timing.</summary>
    public sealed class ShotContext
    {
        public readonly TrailerDirector director;
        public readonly Shot shot;
        public readonly bool vertical;
        public readonly int width, height;
        public readonly List<TrailerActor> cast = new List<TrailerActor>();
        public readonly List<SubtitleLine> subtitles = new List<SubtitleLine>();
        public readonly List<ShotMarker> markers = new List<ShotMarker>();
        readonly List<Coroutine> routines = new List<Coroutine>();

        /// <summary>Seconds since the first recorded frame (negative during the settle time).</summary>
        public float T;

        public TrailerCamera Camera { get; private set; }
        public Truck Truck { get; private set; }
        public float Aspect => width / (float)height;

        public ShotContext(TrailerDirector director, Shot shot, bool vertical, int width, int height)
        {
            this.director = director;
            this.shot = shot;
            this.vertical = vertical;
            this.width = width;
            this.height = height;
        }

        // ------------------------------------------------------------- set-up

        /// <summary>Clears the scene for filming: no interface, no stand-in teammate, our own camera operator.</summary>
        public void Prepare()
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            foreach (var dummy in UnityEngine.Object.FindObjectsByType<TeammateDummy>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(dummy.gameObject);

            var cam = UnityEngine.Camera.main;
            var follow = cam.GetComponent<ThirdPersonCamera>();
            if (follow != null) follow.enabled = false;
            var orbit = cam.GetComponent<MenuCameraOrbit>();
            if (orbit != null) orbit.enabled = false;
            Camera = cam.gameObject.AddComponent<TrailerCamera>();
            cam.aspect = Aspect;
            Truck = UnityEngine.Object.FindAnyObjectByType<Truck>();

            if (GameManager.Instance != null) GameManager.Instance.timeLimitSeconds = 0f;
        }

        /// <summary>
        /// The four friends. The scene's own player becomes the first one; the others are copies of it. Only the first is
        /// "local" (the one the game's camera and HUD would follow), the rest behave like remote players.
        /// </summary>
        public TrailerActor Actor(Friend friend, Vector3 position, float yaw)
        {
            var template = cast.Count == 0 ? null : cast[0].Controller;
            PlayerController controller;
            if (template == null)
            {
                controller = PlayerRegistry.Local != null ? PlayerRegistry.Local : UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            }
            else
            {
                bool wasActive = template.gameObject.activeSelf;
                template.gameObject.SetActive(false);
                var copy = UnityEngine.Object.Instantiate(template.gameObject);
                template.gameObject.SetActive(wasActive);
                controller = copy.GetComponent<PlayerController>();
                controller.isLocal = false;
                var oldActor = copy.GetComponent<TrailerActor>();
                if (oldActor != null) UnityEngine.Object.DestroyImmediate(oldActor);
                copy.GetComponent<PlayerInventory>().Clear();
                copy.SetActive(true);
            }

            controller.gameObject.name = "Friend_" + friend.name;
            controller.Teleport(position);
            if (controller.visual != null) controller.visual.rotation = Quaternion.Euler(0f, yaw, 0f);

            var appearance = controller.GetComponent<PlayerAppearance>();
            if (appearance != null)
            {
                appearance.followSavedChoice = false;
                appearance.SetColors(Friends.Skin(friend), Friends.Clothes(friend));
            }
            var label = controller.GetComponent<OverheadLabel>();
            if (label != null)
            {
                label.enabled = false;
                if (label.text != null) label.text.gameObject.SetActive(false);
            }

            var actor = controller.GetComponent<TrailerActor>() ?? controller.gameObject.AddComponent<TrailerActor>();
            actor.Bind();
            actor.displayName = friend.name;
            actor.colour = Friends.Clothes(friend);
            actor.voice = friend.voice;
            actor.pace = friend.pace;
            cast.Add(actor);
            return actor;
        }

        // ------------------------------------------------------------- timing

        public Coroutine Run(IEnumerator routine)
        {
            var handle = director.StartCoroutine(director.Guard(routine));
            routines.Add(handle);
            return handle;
        }

        public void StopAll()
        {
            foreach (var routine in routines)
                if (routine != null) director.StopCoroutine(routine);
            routines.Clear();
            foreach (var actor in cast) if (actor != null) actor.Stop();
        }

        /// <summary>Waits until the shot clock reaches `seconds` (shot time, so scripts read like a timeline).</summary>
        public IEnumerator At(float seconds)
        {
            while (T < seconds) yield return null;
        }

        public IEnumerator Wait(float seconds)
        {
            float end = T + seconds;
            while (T < end) yield return null;
        }

        // ------------------------------------------------------------- content

        public void Say(TrailerActor who, string text, float seconds = 1.6f)
        {
            subtitles.Add(new SubtitleLine
            {
                t = Mathf.Max(0f, T), dur = seconds, who = who != null ? who.displayName : "",
                colour = who != null ? "#" + ColorUtility.ToHtmlStringRGB(who.colour) : "#FFFFFF", text = text
            });
            if (who != null) Run(Speak(who, text, seconds));
        }

        // ------------------------------------------------------------- voices

        /// <summary>Sounds a friend makes without a subtitle.</summary>
        public enum Reaction { Laugh, Oof, Hup, Yay, Gasp, Scream, Hmph, Aww }

        public void React(TrailerActor who, Reaction reaction, float delay = 0f)
        {
            if (who != null) Run(Reacting(who, reaction, delay));
        }

        void Voice(TrailerActor who, float pitch, float volume, bool held = false) =>
            AudioManager.Play(held ? SfxId.VoiceLong : SfxId.Voice, who.Position + Vector3.up * 1.3f, volume, pitch);

        IEnumerator Reacting(TrailerActor who, Reaction reaction, float delay)
        {
            if (delay > 0f) yield return Wait(delay);
            float v = who.voice;
            switch (reaction)
            {
                case Reaction.Laugh:   // "ja-ja-ja-ja", a little lower each time
                    for (int i = 0; i < 5; i++) { Voice(who, v * (1.32f - i * 0.05f), 0.8f); yield return Wait(0.085f); }
                    break;
                case Reaction.Oof:
                    Voice(who, v * 0.82f, 0.95f);
                    yield return Wait(0.07f);
                    Voice(who, v * 0.66f, 0.8f);
                    break;
                case Reaction.Hup:
                    Voice(who, v * 1.12f, 0.6f);
                    break;
                case Reaction.Yay:
                    Voice(who, v * 1.28f, 0.85f, true);
                    break;
                case Reaction.Gasp:
                    Voice(who, v * 1.5f, 0.75f);
                    break;
                case Reaction.Scream:
                    Voice(who, v * 1.4f, 1f, true);
                    yield return Wait(0.55f);
                    Voice(who, v * 1.22f, 0.9f, true);
                    break;
                case Reaction.Hmph:
                    Voice(who, v * 0.9f, 0.6f);
                    yield return Wait(0.13f);
                    Voice(who, v * 0.74f, 0.6f);
                    break;
                case Reaction.Aww:
                    Voice(who, v * 1.05f, 0.6f, true);
                    break;
            }
        }

        struct Syllable
        {
            public bool held, pause;
            public float length;
        }

        const string Accented = "áéíóúü", Unaccented = "aeiouu";

        static bool IsVowel(char ch) => "aeiou".IndexOf(Plain(ch)) >= 0;

        static char Plain(char ch)
        {
            ch = char.ToLowerInvariant(ch);
            int i = Accented.IndexOf(ch);
            return i >= 0 ? Unaccented[i] : ch;
        }

        /// <summary>Splits a line into what the babble needs: one blip per vowel group, held vowels ("¡AAAH!", "voooy") and pauses.</summary>
        static List<Syllable> Syllables(string text)
        {
            var list = new List<Syllable>();
            int i = 0;
            while (i < text.Length)
            {
                char ch = text[i];
                if (IsVowel(ch))
                {
                    int run = 1, same = 1;
                    while (i + run < text.Length && IsVowel(text[i + run]))
                    {
                        if (Plain(text[i + run]) == Plain(ch)) same++;
                        run++;
                    }
                    list.Add(same >= 3 ? new Syllable { held = true, length = Mathf.Clamp(0.22f + same * 0.05f, 0.3f, 0.75f) } : new Syllable { length = 1f });
                    i += run;
                    continue;
                }
                if (ch == '.' || ch == '…' || ch == ',' || ch == ';' || ch == ':')
                {
                    // "..." is one longer pause, not three
                    bool afterPause = list.Count > 0 && list[list.Count - 1].pause;
                    float pause = afterPause ? Mathf.Min(0.32f, list[list.Count - 1].length + 0.06f) : ch == ',' ? 0.12f : 0.18f;
                    if (afterPause) list[list.Count - 1] = new Syllable { pause = true, length = pause };
                    else list.Add(new Syllable { pause = true, length = pause });
                }
                i++;
            }
            while (list.Count > 0 && list[list.Count - 1].pause) list.RemoveAt(list.Count - 1);
            return list;
        }

        /// <summary>
        /// No words, but everybody talks: one babbled syllable per vowel group in the friend's own voice, with the tune of the
        /// sentence (falls when stating, rises at the end of a question, higher and louder when exclaiming or shouting).
        /// </summary>
        IEnumerator Speak(TrailerActor who, string text, float seconds)
        {
            var syllables = Syllables(text);
            int spoken = 0;
            foreach (var s in syllables) if (!s.pause) spoken++;
            if (spoken == 0) yield break;

            int letters = 0, upper = 0;
            foreach (char ch in text) if (char.IsLetter(ch)) { letters++; if (char.IsUpper(ch)) upper++; }
            bool shout = letters >= 3 && upper >= letters * 0.6f;
            bool exclaim = text.Contains("!");
            bool question = text.Contains("?");

            float step = who.pace * (shout ? 0.82f : exclaim ? 0.9f : 1f);
            float natural = 0f;
            foreach (var s in syllables) natural += s.pause || s.held ? s.length : step;
            float fit = Mathf.Min(1f, seconds * 0.92f / Mathf.Max(0.01f, natural));

            float lift = shout ? 1.3f : exclaim ? 1.14f : 1f;
            float volume = shout ? 1f : exclaim ? 0.9f : 0.75f;
            uint seed = 2166136261;
            foreach (char ch in who.displayName + text) seed = (seed ^ ch) * 16777619;
            var rng = new System.Random((int)(seed & 0x7fffffff));

            int n = 0;
            foreach (var s in syllables)
            {
                if (s.pause) { yield return Wait(s.length * fit); continue; }
                float k = spoken > 1 ? n / (float)(spoken - 1) : 0f;
                float tune = 1.05f - 0.12f * k;                                                  // statements fall towards the end
                if (question && spoken > 1 && n >= spoken - 2) tune = n == spoken - 1 ? 1.3f : 1.12f;   // questions climb at the end
                float pitch = who.voice * lift * tune * (0.95f + (float)rng.NextDouble() * 0.1f);
                Voice(who, pitch, volume * (n == 0 ? 1f : 0.85f + (float)rng.NextDouble() * 0.15f), s.held);
                n++;
                yield return Wait((s.held ? s.length : step) * fit);
            }
        }

        public void Mark(string label) => markers.Add(new ShotMarker { t = T, label = label });

        public BoxData Box(string id)
        {
            var director = LevelDirector.Instance;
            if (director == null) return null;
            foreach (var box in director.boxKinds) if (box != null && box.id == id) return box;
            return null;
        }

        /// <summary>Puts a box on the ground at a point (a box lying on the map, ready to be picked up).</summary>
        public BoxPickup DropBox(string id, Vector3 position)
        {
            var data = Box(id);
            if (data == null || LevelDirector.Instance == null) return null;
            var pickup = LevelDirector.Instance.Drop(data, position, 0, 1);
            if (pickup != null)
            {
                pickup.transform.position = position;
            }
            return pickup;
        }

        /// <summary>Removes every box the level put out, for shots that place their own.</summary>
        public void ClearBoxes()
        {
            foreach (var box in UnityEngine.Object.FindObjectsByType<BoxPickup>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(box.gameObject);
        }

        public Vector3 Centre()
        {
            if (cast.Count == 0) return Vector3.zero;
            var sum = Vector3.zero;
            foreach (var a in cast) sum += a.Position;
            return sum / cast.Count;
        }
    }
}
