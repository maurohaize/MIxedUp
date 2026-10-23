using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>The multiplayer lobby in the main menu, and a level started from a room with several players.</summary>
    public class LobbyTests : SceneTestBase
    {
        protected override string SceneToLoad => MenuScenePath;
        protected override bool NeedsPlayer => false;

        MainMenu menu;
        LobbyPanel lobby;

        [UnitySetUp]
        public IEnumerator FindMenu()
        {
            OnlineSession.Enabled = false;   // these tests use the offline rooms with stand-in friends
            RoomServices.Use(new LocalRoomService { BotDelay = 0.2f });
            menu = Object.FindAnyObjectByType<MainMenu>();
            lobby = menu.lobby;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            OnlineSession.Enabled = true;
            RoomSession.End();
            RoomServices.Use(null);
            yield break;
        }

        [UnityTest]
        public IEnumerator TheMultiplayerPlankOpensTheLobbyAndTheSignpostMakesWay()
        {
            Assert.IsNotNull(menu.multiplayerButton, "there is a multiplayer plank");
            Assert.IsFalse(lobby.IsOpen);
            menu.multiplayerButton.onClick.Invoke();
            yield return null;
            yield return null;

            Assert.IsTrue(lobby.IsOpen);
            Assert.IsTrue(lobby.entryView.activeSelf);
            Assert.IsFalse(lobby.roomView.activeSelf);
            Assert.IsFalse(menu.signpost.activeSelf);

            lobby.backButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(lobby.IsOpen);
            Assert.IsTrue(menu.signpost.activeSelf);
        }

        [UnityTest]
        public IEnumerator CreatingARoomShowsItsCodeAndFriendsWhoJoin()
        {
            lobby.Open();
            yield return null;
            lobby.nameInput.text = "Mauro";
            lobby.createButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(lobby.roomView.activeSelf);
            var room = RoomServices.Current.Current;
            StringAssert.Contains(room.code, lobby.codeLabel.text);
            Assert.AreEqual("Mauro", lobby.memberNames[0].text.Split(' ')[0]);
            Assert.IsFalse(lobby.readyButton.gameObject.activeSelf, "the host has a start button instead");
            Assert.IsTrue(lobby.startButton.gameObject.activeSelf);

            yield return new WaitForSecondsRealtime(1.5f);
            Assert.GreaterOrEqual(room.members.Count, 2, "a friend joined");
            StringAssert.Contains("Ane", lobby.memberNames[1].text.Replace("Iker", "Ane").Replace("Maite", "Ane"));
            Assert.IsTrue(lobby.startButton.interactable, "everyone is ready");
        }

        [UnityTest]
        public IEnumerator JoiningTheDemoRoomShowsTheMembersAndPlaysTheGame()
        {
            lobby.Open();
            yield return null;
            lobby.codeInput.text = "ametsa";
            lobby.joinButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(lobby.roomView.activeSelf);
            StringAssert.Contains("AMETSA", lobby.codeLabel.text);
            Assert.IsTrue(lobby.readyButton.gameObject.activeSelf);
            Assert.IsFalse(lobby.startButton.gameObject.activeSelf);
            Assert.IsFalse(lobby.modeNext.gameObject.activeSelf, "guests cannot change the mode");

            lobby.readyButton.onClick.Invoke();
            float waited = 0f;
            while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameManager.MainMenuScene && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual(MainMenu.LevelScene, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "the level loads");
            yield return null;
            yield return null;

            var mates = Object.FindObjectsByType<TeammateDummy>();
            Assert.AreEqual(2, mates.Length, "one character for each friend in the room");
            Assert.IsTrue(mates.Any(m => m.name.Contains("Ane")));
            var label = mates.First().GetComponent<OverheadLabel>();
            Assert.IsFalse(string.IsNullOrEmpty(label.literalName), "their names float above them");
        }

        [UnityTest]
        public IEnumerator WrongCodesShowAMessage()
        {
            lobby.Open();
            yield return null;
            lobby.codeInput.text = "ZZZZZZ";
            lobby.joinButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(Localization.Get("lobby.err.notfound"), lobby.messageLabel.text);
            Assert.IsTrue(lobby.entryView.activeSelf);

            lobby.codeInput.text = "AB";
            lobby.joinButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(Localization.Get("lobby.err.bad"), lobby.messageLabel.text);
        }

        [UnityTest]
        public IEnumerator EscapeClosesTheLobbyAndLeavesTheRoom()
        {
            lobby.Open();
            yield return null;
            lobby.createButton.onClick.Invoke();
            Assert.IsNotNull(RoomServices.Current.Current);
            lobby.Close();
            Assert.IsNull(RoomServices.Current.Current);
            Assert.IsFalse(lobby.IsOpen);
        }

        [UnityTest]
        public IEnumerator TheHostPicksTheModeForEveryone()
        {
            lobby.Open();
            yield return null;
            lobby.createButton.onClick.Invoke();
            yield return null;
            lobby.modeNext.onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameModes.All[1].id, RoomServices.Current.Current.modeId);
            Assert.AreEqual(GameModes.All[1].DisplayName, lobby.modeLabel.text);
        }
    }
}
