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
