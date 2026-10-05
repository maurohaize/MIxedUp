using System.Collections.Generic;
using UnityEngine;

namespace MixedUp.Tests
{
    /// <summary>Builds throwaway boxes, effects and players for logic tests.</summary>
    public sealed class TestFactory
    {
        readonly List<Object> created = new List<Object>();

        public T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            return asset;
        }

        public BoxData Box(string id, params BoxEffect[] effects)
        {
            var box = Make<BoxData>();
            box.id = id;
            box.nameKey = "box." + id + ".name";
            box.effects = effects;
            return box;
        }

        /// <summary>Creates a player. Health is initialised lazily, so maxHealth set here is honoured.</summary>
        public PlayerStatus Player(string name = "Player", float maxHealth = 100f)
        {
            var go = new GameObject(name);
            created.Add(go);
            go.AddComponent<PlayerInventory>();
            var status = go.AddComponent<PlayerStatus>();
            status.maxHealth = maxHealth;
            return status;
        }

        public void Cleanup()
        {
            foreach (var obj in created)
                if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
        }
    }
}
