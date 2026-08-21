using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel.Design;

namespace IceFire.Classes
{
    public enum InputCommand
    {
        None,
        Pause,
        Up,
        Down,
        Confirm
    }

    public sealed class InputCommands
    {
        private KeyboardState _previousKeyboardState;
        private GamePadState _previousGamePadState;

        public InputCommand Update()
        {
            var keyboardState = Keyboard.GetState();
            var gamePadState = GamePad.GetState(PlayerIndex.One);
            var command = GetCommand(keyboardState, gamePadState);

            _previousKeyboardState = keyboardState;
            _previousGamePadState = gamePadState;

            return command;
        }

        private InputCommand GetCommand(KeyboardState keyboardState, GamePadState gamePadState)
        {
            if (IsPressed(keyboardState, Keys.Escape) || IsPressed(gamePadState, Buttons.Start) || IsPressed(gamePadState, Buttons.Back))
            {
                return InputCommand.Pause;
            }

            if (IsPressed(keyboardState, Keys.Up) || IsPressed(gamePadState, Buttons.DPadUp) || IsThumbstickPressed(gamePadState, _previousGamePadState, 1))
            {
                return InputCommand.Up;
            }

            if (IsPressed(keyboardState, Keys.Down) || IsPressed(gamePadState, Buttons.DPadDown) || IsThumbstickPressed(gamePadState, _previousGamePadState, -1))
            {
                return InputCommand.Down;
            }

            if (IsPressed(keyboardState, Keys.Enter) || IsPressed(gamePadState, Buttons.A))
            {
                return InputCommand.Confirm;
            }
            
            return InputCommand.None;
        }

        private bool IsPressed(KeyboardState currentState, Keys key)
        {
            return currentState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
        }

        private bool IsPressed(GamePadState currentState, Buttons button)
        {
            return currentState.IsButtonDown(button) && _previousGamePadState.IsButtonUp(button);
        }

        private static bool IsThumbstickPressed(GamePadState currentState, GamePadState previousState, int direction)
        {
            const float threshold = 0.5f;
            if(direction > 0) {
                return currentState.ThumbSticks.Left.Y >= threshold && previousState.ThumbSticks.Left.Y < threshold;
            }
            return currentState.ThumbSticks.Left.Y <= -threshold && previousState.ThumbSticks.Left.Y > -threshold;            
        }
    }
}
