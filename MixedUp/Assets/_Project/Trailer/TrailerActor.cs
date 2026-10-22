using System.Collections;
using UnityEngine;

namespace MixedUp.Trailer
{
    /// <summary>
    /// One of the four friends of the trailer: a real player character (physics, animation, inventory, effects) driven by a
    /// script instead of a gamepad. Every action goes through the game's own systems, so what is filmed is real gameplay.
    /// </summary>
    public class TrailerActor : MonoBehaviour
    {
        public string displayName;
        public Color colour = Color.white;

        public PlayerController Controller { get; private set; }
        public PlayerStatus Status { get; private set; }
        public PlayerInventory Inventory { get; private set; }
        public PlayerInteractor Interactor { get; private set; }
        public PlayerPush Push { get; private set; }
        public PlayerHug Hug { get; private set; }

        public Vector3 Position => transform.position;
        public Transform Visual => Controller.visual != null ? Controller.visual : transform;

        void Awake() => Bind();

        public void Bind()
        {
            Controller = GetComponent<PlayerController>();
            Status = GetComponent<PlayerStatus>();
            Inventory = GetComponent<PlayerInventory>();
            Interactor = GetComponent<PlayerInteractor>();
            Push = GetComponent<PlayerPush>();
            Hug = GetComponent<PlayerHug>();
            Controller.puppet = true;
        }

        // ------------------------------------------------------------- movement

        public void Stop()
        {
            Controller.puppetMove = Vector3.zero;
            Controller.puppetSprint = false;
        }

        /// <summary>Walks (or runs) straight to a point, steering every frame. Ends on arrival or after `timeout` seconds.</summary>
        public IEnumerator MoveTo(Vector3 target, bool run = false, float arrive = 0.35f, float timeout = 12f, float speed01 = 1f)
        {
            float t = 0f;
            while (t < timeout)
            {
                Vector3 to = target - transform.position;
                to.y = 0f;
                if (to.magnitude <= arrive) break;
                // Ease off just before arriving so the character does not overshoot.
                float ease = Mathf.Clamp01(to.magnitude / 1.2f);
                Controller.puppetMove = to.normalized * Mathf.Max(0.35f, ease) * speed01;
                Controller.puppetSprint = run;
                t += Time.deltaTime;
                yield return null;
            }
            Stop();
        }

        public IEnumerator Path(bool run, params Vector3[] points)
        {
            foreach (var p in points) yield return MoveTo(p, run, 0.6f);
        }

        /// <summary>Walks in a direction for a while (useful to fall, slide or run off a ledge on purpose).</summary>
        public IEnumerator Walk(Vector3 direction, float seconds, bool run = false)
        {
            float t = 0f;
            direction.y = 0f;
            while (t < seconds)
            {
                Controller.puppetMove = direction.normalized;
                Controller.puppetSprint = run;
                t += Time.deltaTime;
                yield return null;
            }
            Stop();
        }

        public void Jump() => Controller.PuppetJump();

        public void Crouch(bool on) => Controller.puppetCrouch = on;

        /// <summary>Turns the body to look at a point (only while standing still).</summary>
        public IEnumerator Face(Vector3 point, float seconds = 0.3f)
        {
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.001f) yield break;
            var from = Visual.rotation;
            var goal = Quaternion.LookRotation(to.normalized);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Visual.rotation = Quaternion.Slerp(from, goal, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            Visual.rotation = goal;
        }

        public void FaceNow(Vector3 point)
        {
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.001f) Visual.rotation = Quaternion.LookRotation(to.normalized);
        }

        // -------------------------------------------------------------- actions

        /// <summary>Picks up the closest available box within reach, like pressing E next to it.</summary>
        public bool PickUp(float reach = 2.6f)
        {
            BoxPickup best = null;
            float bestDistance = reach;
            foreach (var box in FindObjectsByType<BoxPickup>(FindObjectsSortMode.None))
            {
                if (!box.IsAvailable) continue;
                float d = Vector3.Distance(box.transform.position, transform.position);
                if (d < bestDistance) { best = box; bestDistance = d; }
            }
            if (best == null) return false;
            best.Interact(Interactor);
            return true;
        }

        /// <summary>Puts a box straight into the hands (setting a shot up).</summary>
        public void Hold(BoxData box)
        {
            if (box != null) Inventory.TryAdd(box, out _);
        }

        /// <summary>Hands the selected box to a friend standing close.</summary>
        public bool GiveTo(TrailerActor friend)
        {
            var target = friend.GetComponent<PlayerPassTarget>();
            SelectCarried();
            if (target == null || Inventory.Selected.IsEmpty) return false;
            target.Interact(Interactor);
            return true;
        }

        public bool Deliver(Truck truck)
        {
            SelectCarried();
            if (truck == null || Inventory.Selected.IsEmpty || !truck.Accepts(Inventory.Selected.box)) return false;
            truck.Interact(Interactor);
            return true;
        }

        void SelectCarried()
        {
            if (Inventory.Selected.IsEmpty && Inventory.Count > 0) Inventory.Select(Inventory.FirstOccupiedIndex());
        }

        public bool Shove() => Push != null && Push.TryPush() != null;

        public bool HugNearest() => Hug != null && Hug.TryHug();
    }
}
