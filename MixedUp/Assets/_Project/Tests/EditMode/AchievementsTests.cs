using NUnit.Framework;
using UnityEngine;

namespace MixedUp.Tests
{
    public class AchievementsTests
    {
        [SetUp]
        public void SetUp() => Achievements.ResetAll();

        [TearDown]
        public void TearDown() => Achievements.ResetAll();

        [Test]
        public void CountersUnlockAtTheirGoalAndRaiseTheEventOnce()
        {
            int raised = 0;
            System.Action<AchievementDef> handler = _ => raised++;
            Achievements.Unlocked += handler;
            try
            {
                for (int i = 0; i < Achievements.SpinningLog.goal - 1; i++) Assert.IsFalse(Achievements.Add(Achievements.SpinningLog));
                Assert.IsFalse(Achievements.IsUnlocked(Achievements.SpinningLog));
                Assert.IsTrue(Achievements.Add(Achievements.SpinningLog));
                Assert.IsFalse(Achievements.Add(Achievements.SpinningLog), "already unlocked");
                Assert.AreEqual(1, raised);
                Assert.AreEqual(67, Achievements.Progress(Achievements.SpinningLog));
            }
            finally { Achievements.Unlocked -= handler; }
        }

        [Test]
        public void SetsCountDistinctItemsOnly()
        {
            Assert.IsFalse(Achievements.AddToSet(Achievements.Ducks, "a"));
            Assert.IsFalse(Achievements.AddToSet(Achievements.Ducks, "a"));
            Assert.AreEqual(1, Achievements.Progress(Achievements.Ducks));
            Achievements.AddToSet(Achievements.Ducks, "b");
            Assert.IsTrue(Achievements.AddToSet(Achievements.Ducks, "c"));
            Assert.IsTrue(Achievements.IsUnlocked(Achievements.Ducks));
        }

        [Test]
        public void EveryAchievementHasUniqueIdAndTexts()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var def in Achievements.All)
            {
                Assert.IsTrue(ids.Add(def.id), "duplicate " + def.id);
                Assert.GreaterOrEqual(def.goal, 1);
            }
        }
    }
}
