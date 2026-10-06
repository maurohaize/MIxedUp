using NUnit.Framework;
using UnityEngine;

namespace MixedUp.Tests
{
    public class JumpScoreboardTests
    {
        [Test]
        public void StreaksCountAndAHitResetsThem()
        {
            var board = new JumpScoreboard(false);
            Assert.AreEqual(1, board.Clean("A"));
            Assert.AreEqual(2, board.Clean("A"));
            board.Hit("A");
            Assert.AreEqual(0, board.Streak("A"));
            Assert.AreEqual(1, board.Clean("A"));
        }

        [Test]
        public void TheBoardKeepsTheBestThreeAndOnlyOneEntryPerPlayer()
        {
            var board = new JumpScoreboard(false);
            for (int i = 0; i < 5; i++) board.Clean("A");
            for (int i = 0; i < 3; i++) board.Clean("B");
            for (int i = 0; i < 4; i++) board.Clean("C");
            for (int i = 0; i < 2; i++) board.Clean("D");   // does not make it

            Assert.AreEqual(3, board.Top.Count);
            Assert.AreEqual("A", board.Top[0].name);
            Assert.AreEqual(5, board.Top[0].score);
            Assert.AreEqual("C", board.Top[1].name);
            Assert.AreEqual("B", board.Top[2].name);
        }

        [Test]
        public void ABetterStreakReplacesTheOldRecordOfTheSamePlayer()
        {
            var board = new JumpScoreboard(false);
            board.Clean("A");
            board.Clean("A");
            board.Hit("A");
            board.Clean("A");
            Assert.AreEqual(1, board.Top.Count);
            Assert.AreEqual(2, board.Top[0].score, "the lower new streak does not overwrite the record");
        }

        [Test]
        public void ScoresAreSavedBetweenSessions()
        {
            JumpScoreboard.ClearSaved();
            var board = new JumpScoreboard();
            for (int i = 0; i < 4; i++) board.Clean("Ane: x");

            var again = new JumpScoreboard();
            Assert.AreEqual(1, again.Top.Count);
            Assert.AreEqual(4, again.Top[0].score);
            JumpScoreboard.ClearSaved();
        }
    }
}
