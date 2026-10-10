using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Lets other players hand boxes to this one. Put it on every player root.
    /// The training dummy also allows taking boxes back, since it cannot act on its own.
    /// </summary>
    [RequireComponent(typeof(PlayerInventory), typeof(PlayerStatus))]
    public class PlayerPassTarget : MonoBehaviour, IInteractable
    {
        public bool allowTakeBack;

        PlayerInventory inventoryCache;
        PlayerStatus statusCache;

        PlayerInventory inventory => inventoryCache != null ? inventoryCache : inventoryCache = GetComponent<PlayerInventory>();
        PlayerStatus status => statusCache != null ? statusCache : statusCache = GetComponent<PlayerStatus>();

        public Transform InteractionTransform => transform;

        public bool TryGetPrompt(PlayerInteractor who, out InteractionPrompt prompt)
        {
            prompt = default;
            if (status.IsDead || who.Status.IsDead) return false;

            var give = who.Inventory.Selected;
            if (!give.IsEmpty)
            {
                prompt = !inventory.HasSpace
                    ? InteractionPrompt.Blocked("prompt.teammate_full")
                    : who.PassReady
                        ? InteractionPrompt.Allowed("prompt.pass", give.box.DisplayName)
                        : InteractionPrompt.Blocked("prompt.pass", give.box.DisplayName);
                return true;
            }

            if (allowTakeBack && inventory.Count > 0)
            {
                var theirs = inventory.Slots[inventory.FirstOccupiedIndex()];
                prompt = who.Inventory.HasSpace
                    ? InteractionPrompt.Allowed("prompt.take_back", theirs.box.DisplayName)
                    : InteractionPrompt.Blocked("prompt.inventory_full");
                return true;
            }

            return false;
        }

        /// <summary>True when `who` could grab the boxes this player carries (only players that allow it, with room for them).</summary>
        public bool CanTakeFrom(PlayerInteractor who) =>
            allowTakeBack && !status.IsDead && !who.Status.IsDead && inventory.Count > 0 && who.Inventory.HasSpace;

        /// <summary>Takes every box that fits in the taker's hands. Returns how many moved.</summary>
        public int TakeAll(PlayerInteractor who)
        {
            if (!CanTakeFrom(who)) return 0;

            // The ghost of an online player: ask their machine to hand the boxes over.
            var remote = NetAvatar.OfGhost(gameObject);
            if (remote != null)
            {
                int space = who.Inventory.Capacity - who.Inventory.Count;
                remote.TakeAllFrom(Mathf.Min(space, inventory.Count));
                who.MarkPassed();
                return Mathf.Min(space, inventory.Count);
            }

            int moved = 0;
            while (who.Inventory.HasSpace && inventory.Count > 0)
            {
                if (!inventory.TryTransfer(inventory.FirstOccupiedIndex(), who.Inventory)) break;
                moved++;
            }
            if (moved > 0) who.MarkPassed();
            return moved;
        }

        public void Interact(PlayerInteractor who)
        {
            if (status.IsDead) return;

            var give = who.Inventory.Selected;
            if (!give.IsEmpty)
            {
                if (!who.PassReady) return;
                var box = give.box;

                var remote = NetAvatar.OfGhost(gameObject);
                if (remote != null)
                {
                    // An online player: the box leaves our hands now and arrives in theirs (or comes back if they have no room).
                    if (!inventory.HasSpace) return;
                    who.Inventory.RemoveAt(who.Inventory.SelectedIndex);
                    remote.GiveBoxTo(box);
                    who.MarkPassed();
                    GameEvents.RaiseToast("toast.passed", box.DisplayName);
                    return;
                }

                if (!who.Inventory.TryTransfer(who.Inventory.SelectedIndex, inventory)) return;

                who.MarkPassed();
                if (TryGetComponent(out PlayerInteractor receiver)) receiver.MarkPassed();
                GameEvents.RaiseToast("toast.passed", box.DisplayName);
                return;
            }

            if (allowTakeBack && inventory.Count > 0)
            {
                var remote = NetAvatar.OfGhost(gameObject);
                if (remote != null)
                {
                    if (who.Inventory.HasSpace) remote.TakeAllFrom(1);
                    return;
                }
                inventory.TryTransfer(inventory.FirstOccupiedIndex(), who.Inventory);
            }
        }
    }
}
