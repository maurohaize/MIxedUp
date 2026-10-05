using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Stand-in teammate for phase 1: it cannot move, but it receives boxes (and gives them back),
    /// suffers their effects and reports its death. Replaced by real remote players in phase 3.
    /// </summary>
    [RequireComponent(typeof(PlayerStatus))]
    public class TeammateDummy : MonoBehaviour
    {
        PlayerStatus status;

        void Awake() => status = GetComponent<PlayerStatus>();
        void OnEnable() { if (status == null) status = GetComponent<PlayerStatus>(); status.Died += OnDied; }
        void OnDisable() { if (status != null) status.Died -= OnDied; }

        void OnDied(DeathCause cause) => GameEvents.RaiseToast("toast.teammate_died", cause.Localized);
    }
}
