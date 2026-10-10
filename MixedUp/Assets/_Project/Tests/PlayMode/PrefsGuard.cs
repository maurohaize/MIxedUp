using System.Collections.Generic;
using UnityEngine;

namespace MixedUp.Tests
{
    /// <summary>
    /// Remembers the saved progress a test run can change (money, achievements, jump ranking) and puts it back afterwards, so
    /// running the tests never alters the player's own progress in the Editor's PlayerPrefs.
    /// </summary>
    public sealed class PrefsGuard
    {
        readonly Dictionary<string, object> saved = new Dictionary<string, object>();

        static IEnumerable<string> Keys()
        {
            yield return "wallet.coins";
            yield return JumpScoreboard.PrefKey;
            foreach (var def in Achievements.All)
            {
                yield return "ach." + def.id;
                yield return "ach." + def.id + ".set";
            }
        }

        public PrefsGuard()
        {
            foreach (var key in Keys())
            {
                if (!PlayerPrefs.HasKey(key)) { saved[key] = null; continue; }
                // Counters are numbers and the "found items" lists are text.
                saved[key] = key.EndsWith(".set") || key == JumpScoreboard.PrefKey
                    ? (object)PlayerPrefs.GetString(key, string.Empty)
                    : PlayerPrefs.GetInt(key, 0);
            }
        }

        public void Restore()
        {
            foreach (var pair in saved)
            {
                if (pair.Value == null) PlayerPrefs.DeleteKey(pair.Key);
                else if (pair.Value is int number) PlayerPrefs.SetInt(pair.Key, number);
                else PlayerPrefs.SetString(pair.Key, (string)pair.Value);
            }
            PlayerPrefs.Save();
        }
    }
}
