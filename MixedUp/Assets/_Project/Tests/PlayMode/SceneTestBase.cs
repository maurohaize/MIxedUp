using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Loads the prototype scene for each test and offers simulated keyboard input and teleporting.</summary>
    public abstract class SceneTestBase
    {
        protected const string ScenePath = "Assets/Scenes/Level_Prototype.unity";
        const string LanguagePref = "settings.language";
        const string ComboPref = "combos.learned";
        const string WalletPref = "wallet.coins";

        Keyboard keyboard;
        bool createdKeyboard;
        InputSettings.BackgroundBehavior savedBackground;
        InputSettings.EditorInputBehaviorInPlayMode savedEditorBehavior;
        bool hadLanguagePref;
        int savedLanguage;
        string savedCombos;
        int savedWallet;
        readonly HashSet<Key> held = new HashSet<Key>();

        protected PlayerController player;
        protected PlayerStatus status;
        protected PlayerInteractor interactor;
        protected Truck truck;
        protected BoxPickup[] pickups;
        protected UIManager ui;
        protected TruckPuzzleController puzzle;
        protected TruckPuzzleScreen screen;

        /// <summary>An arrangement of the prototype order where no neighbours react.</summary>
        protected static readonly string[] SafeOrder = { "electric", "frozen", "hot", "normal", "toxic", "normal" };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!File.Exists(ScenePath)) Assert.Ignore("Missing " + ScenePath + ". Run MixedUp > Build Phase 1 Prototype first.");

            hadLanguagePref = PlayerPrefs.HasKey(LanguagePref);
            savedLanguage = PlayerPrefs.GetInt(LanguagePref, 0);
            savedCombos = PlayerPrefs.GetString(ComboPref, string.Empty);
            savedWallet = PlayerPrefs.GetInt(WalletPref, 0);
            PlayerPrefs.DeleteKey(ComboPref);
            CombinationManual.Reload();

            savedBackground = InputSystem.settings.backgroundBehavior;
            savedEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            keyboard = Keyboard.current;
            if (keyboard == null)
            {
                keyboard = InputSystem.AddDevice<Keyboard>();
                createdKeyboard = true;
            }

#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("These tests load the scene by asset path and only run in the editor.");
#endif
            yield return null;
            yield return null;

            player = PlayerRegistry.Local;
            Assert.IsNotNull(player, "no local player registered");
            status = player.GetComponent<PlayerStatus>();
            interactor = player.GetComponent<PlayerInteractor>();
            truck = Object.FindAnyObjectByType<Truck>();
            pickups = Object.FindObjectsByType<BoxPickup>();
            ui = Object.FindAnyObjectByType<UIManager>();
            Resolve();

            yield return new WaitForSeconds(0.6f);
        }

        /// <summary>Looks up scene objects again; needed after the scene was reloaded.</summary>
        protected void Resolve()
        {
            truck = Object.FindAnyObjectByType<Truck>();
            ui = Object.FindAnyObjectByType<UIManager>();
            puzzle = Object.FindAnyObjectByType<TruckPuzzleController>();
            screen = Object.FindAnyObjectByType<TruckPuzzleScreen>(FindObjectsInactive.Include);
        }

        /// <summary>Hands every box of the order to the truck and waits for the 2D screen to open.</summary>
        protected IEnumerator DeliverEverything()
        {
            Resolve();
            foreach (var line in truck.order.lines)
                for (int i = 0; i < line.count; i++) truck.Deliver(line.box);

            float waited = 0f;
            while (!ui.puzzlePanel.activeSelf && waited < 8f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(ui.puzzlePanel.activeSelf, "the 2D truck screen never opened");
            yield return null;
        }

        protected IEnumerator WaitForState(GameState target, float timeout)
        {
            float waited = 0f;
            while (GameManager.Instance.State != target && waited < timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual(target, GameManager.Instance.State, "waited " + waited + " s");
            yield return null;
        }

        /// <summary>Rearranges the truck puzzle into the given box ids with swaps.</summary>
        protected void Arrange(params string[] ids)
        {
            var state = puzzle.State;
            for (int i = 0; i < ids.Length; i++)
            {
                int j = i;
                while (state[j].id != ids[i]) j++;
                if (j != i) state.Swap(i, j);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) ReleaseAllKeys();
            yield return null;

            Time.timeScale = 1f;
            InputSystem.settings.backgroundBehavior = savedBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorBehavior;
            if (createdKeyboard && keyboard != null) InputSystem.RemoveDevice(keyboard);
            createdKeyboard = false;

            if (hadLanguagePref) PlayerPrefs.SetInt(LanguagePref, savedLanguage);
            else PlayerPrefs.DeleteKey(LanguagePref);

            if (string.IsNullOrEmpty(savedCombos)) PlayerPrefs.DeleteKey(ComboPref);
            else PlayerPrefs.SetString(ComboPref, savedCombos);
            CombinationManual.Reload();
            PlayerPrefs.SetInt(WalletPref, savedWallet);
        }

        /// <summary>Keeps the full set of pressed keys, so several keys can be held at once.</summary>
        protected void SetKey(Key key, bool down)
        {
            if (down) held.Add(key);
            else held.Remove(key);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(held.ToArray()));
        }

        protected void ReleaseAllKeys()
        {
            held.Clear();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }

        protected IEnumerator Tap(Key key)
        {
            SetKey(key, true);
            yield return null;
            yield return null;
            SetKey(key, false);
            yield return null;
        }

        protected static IEnumerator Settle()
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        protected BoxData BoxOf(string id) => pickups.First(p => p.data.id == id).data;

        protected IEnumerator GoTo(Vector3 position)
        {
            player.Teleport(position);
            yield return Settle();
        }
    }
}
