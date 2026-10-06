using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// What the player has learned about combinations. Rules start unknown (unless knownByDefault)
    /// and are discovered by reading notes or by seeing boxes react. Persisted in PlayerPrefs.
    /// </summary>
    public static class CombinationManual
    {
        const string PrefKey = "combos.learned";

        static HashSet<string> learned;

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            learned = null;
            Changed = null;
        }

        static HashSet<string> Learned
        {
            get
            {
                if (learned != null) return learned;
                learned = new HashSet<string>();
                string saved = PlayerPrefs.GetString(PrefKey, string.Empty);
                foreach (var key in saved.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    learned.Add(key);
                return learned;
            }
        }

        /// <summary>A pair without a rule is plain safe, so there is nothing to learn about it.</summary>
        public static bool IsKnown(CombinationRule rule) =>
            rule == null || rule.knownByDefault || Learned.Contains(rule.Key);

        /// <summary>Returns true when this was new information.</summary>
        public static bool Discover(CombinationRule rule)
        {
            if (rule == null || IsKnown(rule)) return false;

            Learned.Add(rule.Key);
            PlayerPrefs.SetString(PrefKey, string.Join(";", Learned));
            Changed?.Invoke();
            return true;
        }

        /// <summary>Re-reads what was saved; the next query loads it from PlayerPrefs again.</summary>
        public static void Reload() => learned = null;

        public static void ForgetAll()
        {
            learned = new HashSet<string>();
            PlayerPrefs.DeleteKey(PrefKey);
            Changed?.Invoke();
        }
    }
}
