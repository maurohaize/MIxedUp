using UnityEngine;

namespace MixedUp
{
    /// <summary>Anything that can be shoved by a player: other players and the training dummy.</summary>
    public interface IPushable
    {
        Transform PushTransform { get; }
        bool CanBePushed { get; }

        /// <summary>A shove: `horizontalImpulse` is added to the velocity, `upSpeed` launches it a little off the ground.</summary>
        void ReceivePush(Vector3 horizontalImpulse, float upSpeed);
    }
}
