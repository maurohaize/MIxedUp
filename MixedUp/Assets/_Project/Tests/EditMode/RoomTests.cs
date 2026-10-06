using NUnit.Framework;

namespace MixedUp.Tests
{
    public class RoomTests
    {
        LocalRoomService rooms;

        [SetUp]
        public void SetUp()
        {
            RoomSession.End();
            rooms = new LocalRoomService { BotDelay = 1f };
        }

        [TearDown]
        public void TearDown() => RoomSession.End();

        // ----------------------------------------------------------------- codes

        [Test]
        public void CodesAreSixEasyCharacters()
        {
            var rng = new System.Random(1);
            for (int i = 0; i < 50; i++)
            {
                string code = RoomCode.Generate(rng);
                Assert.AreEqual(6, code.Length);
                Assert.AreEqual(code, RoomCode.Normalize(code.ToLowerInvariant()), "typed in lower case it still works");
                StringAssert.DoesNotContain("0", code);
                StringAssert.DoesNotContain("O", code);
                StringAssert.DoesNotContain("I", code);
            }
        }

        [Test]
        public void NormalizingDropsJunkAndRejectsWrongLengths()
        {
            Assert.AreEqual("ABC234", RoomCode.Normalize(" ab-c2 34 "));
            Assert.IsNull(RoomCode.Normalize("ABC"));
            Assert.IsNull(RoomCode.Normalize(""));
            Assert.IsNull(RoomCode.Normalize(null));
        }

        // ------------------------------------------------------------------ rooms

        [Test]
        public void CreatingARoomMakesYouTheHost()
        {
            var room = rooms.CreateRoom("Mauro", 4, 1, 4);
            Assert.AreEqual(6, room.code.Length);
            Assert.AreEqual(1, room.members.Count);
            Assert.IsTrue(room.Local.isHost);
            Assert.AreEqual("Mauro", room.Local.name);
            Assert.AreSame(room, rooms.Current);
        }

        [Test]
        public void StandInFriendsJoinOneByOneAndGetReady()
        {
            rooms.BotsToAdd = 2;
            var room = rooms.CreateRoom("Mauro", 4, 1, 4);
            rooms.Tick(1.1f);
            Assert.AreEqual(2, room.members.Count);
            rooms.Tick(1.1f);
            Assert.AreEqual(3, room.members.Count);
            Assert.IsFalse(rooms.CanStart, "they have not said they are ready");

            for (int i = 0; i < 4; i++) rooms.Tick(1.1f);
            Assert.IsTrue(room.EveryoneReady);
            Assert.IsTrue(rooms.CanStart);
        }

        [Test]
        public void TheHostStartsTheGameAndTheSessionKnowsEveryone()
        {
            rooms.BotsToAdd = 1;
            rooms.CreateRoom("Mauro", 4, 1, 4);
            for (int i = 0; i < 4; i++) rooms.Tick(1.1f);

            bool started = false;
            rooms.Started += () => started = true;
            Assert.IsTrue(rooms.StartGame());

            Assert.IsTrue(started);
            Assert.IsTrue(RoomSession.IsMultiplayer);
            Assert.AreEqual(1, RoomSession.Others().Count);
            Assert.AreEqual(RoomState.InGame, rooms.Current.state);
        }

        [Test]
        public void YouCannotStartBeforeEveryoneIsReady()
        {
            rooms.CreateRoom("Mauro", 4, 1, 4);
            rooms.Tick(1.1f);       // a friend joins but is not ready yet
            Assert.IsFalse(rooms.StartGame());
            Assert.IsFalse(RoomSession.IsMultiplayer);
        }

        [Test]
        public void SoloRoomsCanStartAtOnce()
        {
            rooms.BotsToAdd = 0;
            rooms.CreateRoom("Mauro", 4, 1, 4);
            Assert.IsTrue(rooms.StartGame());
            Assert.IsFalse(RoomSession.IsMultiplayer, "one player is a solo game");
        }

        [Test]
        public void OnlyTheHostChoosesTheMode()
        {
            var mine = rooms.CreateRoom("Mauro", 4, 1, 4);
            rooms.SetMode("giant");
            Assert.AreEqual("giant", mine.modeId);
            rooms.SetMode("nonsense");
            Assert.AreEqual("giant", mine.modeId);

            rooms.JoinRoom(LocalRoomService.DemoCode, "Mauro", 1, 4);
            var joined = rooms.Current;
            rooms.SetMode("night");
            Assert.AreNotEqual("night", joined.modeId, "guests cannot change it");
        }

        // ------------------------------------------------------------------- joining

        [Test]
        public void JoiningWithABadOrUnknownCodeFails()
        {
            Assert.AreEqual(JoinResult.BadCode, rooms.JoinRoom("12", "Mauro", 0, 0));
            Assert.AreEqual(JoinResult.NotFound, rooms.JoinRoom("ZZZZZZ", "Mauro", 0, 0));
            Assert.IsNull(rooms.Current);
        }

        [Test]
        public void TheDemoRoomHasStandInFriendsAndStartsWhenYouAreReady()
        {
            Assert.AreEqual(JoinResult.Ok, rooms.JoinRoom("ametsa", "Mauro", 2, 5));
            Assert.AreEqual(3, rooms.Current.members.Count);
            Assert.IsFalse(rooms.Current.Local.isHost);
            Assert.IsFalse(rooms.CanStart, "guests cannot start");

            bool started = false;
            rooms.Started += () => started = true;
            rooms.SetReady(true);
            for (int i = 0; i < 4; i++) rooms.Tick(1f);
            Assert.IsTrue(started, "the friendly host starts it");
            Assert.AreEqual(2, RoomSession.Others().Count);
        }

        [Test]
        public void LeavingAsGuestFreesTheSeatAndLeavingAsHostClosesTheRoom()
        {
            rooms.JoinRoom(LocalRoomService.DemoCode, "Mauro", 0, 0);
            var demo = rooms.Current;
            int before = demo.members.Count;
            rooms.Leave();
            Assert.AreEqual(before - 1, demo.members.Count);
            Assert.IsNull(rooms.Current);

            var mine = rooms.CreateRoom("Mauro", 4, 0, 0);
            bool closed = false;
            rooms.Closed += () => closed = true;
            rooms.Leave();
            Assert.IsTrue(closed);
            Assert.AreEqual(RoomState.Closed, mine.state);
            Assert.AreEqual(JoinResult.NotFound, rooms.JoinRoom(mine.code, "Other", 0, 0), "the code is dead");
        }

        [Test]
        public void NamesAreCleanedUp()
        {
            Assert.AreEqual("Ane", PlayerProfile.Sanitize("  Ane  ", true));
            Assert.AreEqual("AneMaite", PlayerProfile.Sanitize("Ane;Maite:", true));
            Assert.AreEqual(14, PlayerProfile.Sanitize("a very long player name indeed", true).Length);
            Assert.AreEqual("", PlayerProfile.Sanitize("   ", false));
        }
    }
}
