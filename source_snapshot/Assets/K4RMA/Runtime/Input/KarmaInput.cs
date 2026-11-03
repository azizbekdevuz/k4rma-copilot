using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace KarmaPrototype
{
    public enum KarmaAction
    {
        Left, Right, Up, Down, Jump, Attack, Counter, SwordWave, RisingSlash, Guard,
        Interact, Pause, Confirm, Restart, Title, Story, Controls, ChooseSwordWave, ChooseRisingSlash, ChooseGuard,
        PreviousPage, NextPage, ToggleInvincibility
    }

    // Shared actions stay enabled in menus and while Time.timeScale is zero.
    public static class KarmaInput
    {
        static InputActionMap map;
        static InputAction[] actions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            // Also runs when entering Play with domain reload disabled.
            if (map != null) map.Dispose();
            map = null;
            actions = null;
            Application.quitting -= Reset;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (map != null) return;
            map = new InputActionMap("K4RMA");
            actions = new InputAction[System.Enum.GetValues(typeof(KarmaAction)).Length];
            Bind(KarmaAction.Left, "a", "leftArrow");
            Bind(KarmaAction.Right, "d", "rightArrow");
            Bind(KarmaAction.Up, "w", "upArrow");
            Bind(KarmaAction.Down, "s", "downArrow");
            Bind(KarmaAction.Jump, "space");
            Bind(KarmaAction.Attack, "j");
            Bind(KarmaAction.Counter, "k");
            Bind(KarmaAction.SwordWave, "q");
            Bind(KarmaAction.RisingSlash, "leftShift");
            Bind(KarmaAction.Guard, "e");
            Bind(KarmaAction.Interact, "f");
            Bind(KarmaAction.Pause, "escape");
            Bind(KarmaAction.Confirm, "enter");
            Bind(KarmaAction.Restart, "r");
            Bind(KarmaAction.Title, "t");
            Bind(KarmaAction.Story, "s");
            Bind(KarmaAction.Controls, "h");
            Bind(KarmaAction.ChooseSwordWave, "1");
            Bind(KarmaAction.ChooseRisingSlash, "2");
            Bind(KarmaAction.ChooseGuard, "3");
            Bind(KarmaAction.PreviousPage, "leftArrow", "upArrow");
            Bind(KarmaAction.NextPage, "rightArrow", "downArrow");
            Bind(KarmaAction.ToggleInvincibility, "p");
            map.Enable();
            Application.quitting += Reset;
        }

        static void Bind(KarmaAction id, params string[] keys)
        {
            var action = map.AddAction(id.ToString(), InputActionType.Button);
            foreach (var key in keys) action.AddBinding("<Keyboard>/" + key);
            actions[(int)id] = action;
        }

        public static bool Held(KarmaAction id)
        {
            Initialize();
            return actions[(int)id].IsPressed();
        }

        public static bool Pressed(KarmaAction id)
        {
            Initialize();
            // Preserve alias edges: pressing an arrow while its WASD alias is held
            // still counts, but simultaneous aliases count as one direction.
            foreach (var control in actions[(int)id].controls)
                if (control is ButtonControl button && button.wasPressedThisFrame) return true;
            return false;
        }

        // Opposing directions cancel; diagonal inputs retain both cardinal axes.
        public static float Horizontal => (Held(KarmaAction.Right) ? 1 : 0) - (Held(KarmaAction.Left) ? 1 : 0);
        public static float Vertical => (Held(KarmaAction.Up) ? 1 : 0) - (Held(KarmaAction.Down) ? 1 : 0);

        public static bool DirectionPressed(int direction)
        {
            switch (direction)
            {
                case 1: return Pressed(KarmaAction.Left);
                case 2: return Pressed(KarmaAction.Right);
                case 3: return Pressed(KarmaAction.Up);
                case 4: return Pressed(KarmaAction.Down);
                default: return false;
            }
        }
    }
}
