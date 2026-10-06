using UnityEngine;
using UnityEngine.InputSystem;

namespace MixedUp
{
    /// <summary>
    /// All gameplay actions, defined in code so bindings can be remapped and persisted later.
    /// Defaults: WASD move, Shift run, Space jump, E interact, F take boxes from a teammate, Q/scroll/1-4 inventory slot, Esc pause.
    /// </summary>
    public static class GameInput
    {
        const string OverridesPrefKey = "input.bindingOverrides";
        public const int MaxSlots = 4;

        static InputActionMap map;

        public static InputAction Move { get; private set; }
        public static InputAction Look { get; private set; }
        public static InputAction Sprint { get; private set; }
        public static InputAction Jump { get; private set; }
        public static InputAction Interact { get; private set; }
        public static InputAction Take { get; private set; }
        public static InputAction Pause { get; private set; }
        public static InputAction NextSlot { get; private set; }
        public static InputAction[] SelectSlot { get; private set; }

        public static InputActionMap Map
        {
            get { EnsureCreated(); return map; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (map != null)
            {
                map.Disable();
                map.Dispose();
            }
            map = null;
        }

        public static void EnsureCreated()
        {
            if (map != null) return;

            map = new InputActionMap("Gameplay");

            Move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = map.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            Look.AddBinding("<Mouse>/delta");
            Look.AddBinding("<Gamepad>/rightStick");

            Sprint = map.AddAction("Sprint", InputActionType.Button);
            Sprint.AddBinding("<Keyboard>/leftShift");
            Sprint.AddBinding("<Gamepad>/leftStickPress");

            Jump = map.AddAction("Jump", InputActionType.Button);
            Jump.AddBinding("<Keyboard>/space");
            Jump.AddBinding("<Gamepad>/buttonSouth");

            Interact = map.AddAction("Interact", InputActionType.Button);
            Interact.AddBinding("<Keyboard>/e");
            Interact.AddBinding("<Gamepad>/buttonWest");

            Take = map.AddAction("Take", InputActionType.Button);
            Take.AddBinding("<Keyboard>/f");
            Take.AddBinding("<Gamepad>/buttonEast");

            Pause = map.AddAction("Pause", InputActionType.Button);
            Pause.AddBinding("<Keyboard>/escape");
            Pause.AddBinding("<Gamepad>/start");

            NextSlot = map.AddAction("NextSlot", InputActionType.Button);
            NextSlot.AddBinding("<Keyboard>/q");
            NextSlot.AddBinding("<Mouse>/scroll/up");
            NextSlot.AddBinding("<Mouse>/scroll/down");
            NextSlot.AddBinding("<Gamepad>/buttonNorth");

            SelectSlot = new InputAction[MaxSlots];
            for (int i = 0; i < MaxSlots; i++)
            {
                SelectSlot[i] = map.AddAction("Slot" + (i + 1), InputActionType.Button);
                SelectSlot[i].AddBinding("<Keyboard>/" + (i + 1));
            }

            LoadOverrides();
        }

        public static void Enable()
        {
            EnsureCreated();
            map.Enable();
        }

        public static void Disable()
        {
            if (map != null) map.Disable();
        }

        public static void SaveOverrides()
        {
            EnsureCreated();
            PlayerPrefs.SetString(OverridesPrefKey, map.SaveBindingOverridesAsJson());
        }

        public static void LoadOverrides()
        {
            string json = PlayerPrefs.GetString(OverridesPrefKey, string.Empty);
            if (!string.IsNullOrEmpty(json)) map.LoadBindingOverridesFromJson(json);
        }

        public static void ResetBindings()
        {
            EnsureCreated();
            map.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(OverridesPrefKey);
        }

        /// <summary>Human readable label for an action's current binding, e.g. "E".</summary>
        public static string Label(InputAction action)
        {
            EnsureCreated();
            // Index 0 is always the keyboard binding; showing every binding would print "E | X" for keyboard and gamepad.
            string label = action.GetBindingDisplayString(0);
            return string.IsNullOrEmpty(label) ? "?" : label;
        }
    }
}
