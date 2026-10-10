using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Makes the ghost of an online player something the local player can shove: the push is sent to the machine of the real
    /// player, who is the one that actually gets thrown about.
    /// </summary>
    public class NetGhost : MonoBehaviour, IPushable
    {
        [HideInInspector] public NetAvatar owner;

        PlayerStatus status;

        public Transform PushTransform => transform;

        public bool CanBePushed
        {
            get
            {
                if (status == null) status = GetComponent<PlayerStatus>();
                return owner != null && status != null && !status.IsDead;
            }
        }

        public void ReceivePush(Vector3 horizontalImpulse, float upSpeed)
        {
            if (owner != null) owner.SendPush(horizontalImpulse, upSpeed);
        }
    }
}
