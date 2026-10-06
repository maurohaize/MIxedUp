using UnityEngine;

namespace MixedUp
{
    /// <summary>Finds the best thing to interact with nearby and handles slot selection input.</summary>
    [RequireComponent(typeof(PlayerInventory), typeof(PlayerStatus))]
    public class PlayerInteractor : MonoBehaviour
    {
        public float radius = 2.4f;
        public float scanInterval = 0.1f;
        [Tooltip("Seconds before this player can pass again, and before a receiver can pass back.")]
        public float passCooldown = 0.75f;

        readonly Collider[] buffer = new Collider[32];
        float scanTimer;
        float nextPassTime;

        PlayerInventory inventory;
        PlayerStatus status;

        public PlayerInventory Inventory => inventory != null ? inventory : inventory = GetComponent<PlayerInventory>();
        public PlayerStatus Status => status != null ? status : status = GetComponent<PlayerStatus>();
        public IInteractable Current { get; private set; }
        /// <summary>A teammate close by whose boxes can all be taken with the Take key.</summary>
        public PlayerPassTarget TakeTarget { get; private set; }
        public InteractionPrompt CurrentPrompt { get; private set; }

        public bool PassReady => Time.time >= nextPassTime;
        public void MarkPassed() => nextPassTime = Time.time + passCooldown;

        void Update()
        {
            if (Status.IsDead || GameManager.InputBlocked)
            {
                Current = null;
                TakeTarget = null;
                return;
            }

            HandleSlotSelection();

            scanTimer -= Time.unscaledDeltaTime;
            if (scanTimer <= 0f)
            {
                Scan();
                scanTimer = scanInterval;
            }

            if (GameInput.Interact.WasPressedThisFrame())
            {
                Scan();
                TryInteract();
            }

            if (GameInput.Take.WasPressedThisFrame())
            {
                Scan();
                TryTake();
            }
        }

        /// <summary>Grabs every box (that fits) from the nearby teammate that allows it.</summary>
        public bool TryTake()
        {
            if (TakeTarget == null) return false;
            int moved = TakeTarget.TakeAll(this);
            Scan();
            return moved > 0;
        }

        void HandleSlotSelection()
        {
            if (GameInput.NextSlot.WasPressedThisFrame()) Inventory.CycleSelection(1);
            for (int i = 0; i < GameInput.MaxSlots; i++)
                if (GameInput.SelectSlot[i].WasPressedThisFrame()) Inventory.Select(i);
        }

        public bool TryInteract()
        {
            if (Current == null || !CurrentPrompt.Enabled) return false;
            Current.Interact(this);
            Scan();
            return true;
        }

        /// <summary>Re-evaluates the closest valid interactable. Enabled prompts win over blocked ones.</summary>
        public void Scan()
        {
            IInteractable best = null;
            InteractionPrompt bestPrompt = default;
            float bestScore = float.MaxValue;
            PlayerPassTarget bestTake = null;
            float bestTakeDistance = float.MaxValue;

            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var takeCandidate = buffer[i].GetComponentInParent<PlayerPassTarget>();
                if (takeCandidate != null && !takeCandidate.transform.IsChildOf(transform) && takeCandidate.CanTakeFrom(this))
                {
                    float d = Vector3.Distance(transform.position, takeCandidate.transform.position);
                    if (d < bestTakeDistance)
                    {
                        bestTake = takeCandidate;
                        bestTakeDistance = d;
                    }
                }

                var candidate = buffer[i].GetComponentInParent<IInteractable>();
                if (candidate == null) continue;

                var anchor = candidate.InteractionTransform;
                if (anchor == null || anchor.IsChildOf(transform)) continue;
                if (!candidate.TryGetPrompt(this, out var prompt)) continue;

                float score = Vector3.Distance(transform.position, anchor.position);
                if (!prompt.Enabled) score += 1000f;
                if (score >= bestScore) continue;

                best = candidate;
                bestPrompt = prompt;
                bestScore = score;
            }

            Current = best;
            CurrentPrompt = bestPrompt;
            TakeTarget = bestTake;
        }
    }
}
