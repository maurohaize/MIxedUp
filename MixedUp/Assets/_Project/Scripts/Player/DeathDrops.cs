using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>When a character dies, the boxes they carry fall to the ground around them so the team can still use them.</summary>
    [RequireComponent(typeof(PlayerStatus))]
    public class DeathDrops : MonoBehaviour
    {
        PlayerStatus status;

        void OnEnable()
        {
            status = GetComponent<PlayerStatus>();
            status.Died += OnDied;
        }

        void OnDisable()
        {
            if (status != null) status.Died -= OnDied;
        }

        void OnDied(DeathCause cause)
        {
            var director = LevelDirector.Instance;
            if (director == null) return;

            var inventory = status.Inventory;
            var dropped = new List<BoxData>();
            for (int i = 0; i < inventory.Capacity; i++)
            {
                var box = inventory.RemoveAt(i);
                if (box != null) dropped.Add(box);
            }
            for (int i = 0; i < dropped.Count; i++) director.Drop(dropped[i], transform.position, i, dropped.Count);
        }
    }
}
