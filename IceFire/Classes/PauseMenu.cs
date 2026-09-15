using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace IceFire.Classes
{
    public enum PauseMenuAction
    {
        None,
        Continue,
        Restart,
        StageSelect,
        ResolutionChanged,
        ToggleFullscreen
    }

    public readonly record struct PauseMenuResult(PauseMenuAction Action, Point? Resolution = null);

    public sealed class PauseMenu
    {
        private enum MenuPage { Main, Options, Graphics, Debug }

        private static readonly Point[] Resolutions =
        {
            new(960, 640), new(1280, 720), new(1600, 900), new(1920, 1080)
        };

        private static readonly string[] MainOptions = ["Continue", "Restart", "Stage Select", "Options"];
        private static readonly string[] OptionMenus = ["Graphics", "DEBUG"];

        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private MenuPage _page = MenuPage.Main;
        private int _selectedOption;
        private InputCommand _previousCommand = InputCommand.None;

        public PauseMenu(SpriteFont font, GraphicsDevice graphicsDevice)
        {
            _font = font;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData([Color.Blue]);
        }

        public bool IsOpen { get; private set; }
        public bool ShowObjectCollisions { get; private set; }
        public bool ShowPlayerCollisions { get; private set; }
        public int CharacterSpeedLevel { get; private set; } = 1;

        public PauseMenuResult Update(InputCommand command)
        {
            var commandPressed = command != _previousCommand;
            var result = new PauseMenuResult(PauseMenuAction.None);

            if (commandPressed && (command is InputCommand.Start or InputCommand.Back or InputCommand.Pause))
            {
                if (_page != MenuPage.Main)
                {
                    if (_page == MenuPage.Options)
                    {
                        _page = MenuPage.Main;
                        _selectedOption = 3;
                    }
                    else
                    {
                        _page = MenuPage.Options;
                        _selectedOption = 0;
                    }
                }
                else if (IsOpen)
                {
                    IsOpen = false;
                    _selectedOption = 0;
                    result = new PauseMenuResult(PauseMenuAction.Continue);
                }
                else
                {
                    IsOpen = true;
                    _selectedOption = 0;
                }

                _previousCommand = command;
                return result;
            }

            if (IsOpen && commandPressed)
            {
                result = _page switch
                {
                    MenuPage.Main => UpdateMain(command),
                    MenuPage.Options => UpdateOptions(command),
                    MenuPage.Graphics => UpdateGraphics(command),
                    MenuPage.Debug => UpdateDebug(command),
                    _ => result
                };
            }

            _previousCommand = command;
            return result;
        }

        private PauseMenuResult UpdateMain(InputCommand command)
        {
            if (command is InputCommand.Up or InputCommand.Down)
            {
                _selectedOption = MoveSelection(_selectedOption, MainOptions.Length, command == InputCommand.Down);
            }
            else if (command == InputCommand.Confirm)
            {
                switch (_selectedOption)
                {
                    case 0: IsOpen = false; return new PauseMenuResult(PauseMenuAction.Continue);
                    case 1: IsOpen = false; return new PauseMenuResult(PauseMenuAction.Restart);
                    case 2: IsOpen = false; return new PauseMenuResult(PauseMenuAction.StageSelect);
                    case 3: _page = MenuPage.Options; _selectedOption = 0; break;
                }
            }

            return new PauseMenuResult(PauseMenuAction.None);
        }

        private PauseMenuResult UpdateOptions(InputCommand command)
        {
            if (command is InputCommand.Up or InputCommand.Down)
                _selectedOption = MoveSelection(_selectedOption, OptionMenus.Length, command == InputCommand.Down);
            else if (command == InputCommand.Confirm)
            {
                _page = _selectedOption == 0 ? MenuPage.Graphics : MenuPage.Debug;
                _selectedOption = 0;
            }

            return new PauseMenuResult(PauseMenuAction.None);
        }

        private PauseMenuResult UpdateGraphics(InputCommand command)
        {
            var optionCount = Resolutions.Length + 1;
            if (command is InputCommand.Up or InputCommand.Down)
                _selectedOption = MoveSelection(_selectedOption, optionCount, command == InputCommand.Down);
            else if (command == InputCommand.Confirm)
                return _selectedOption < Resolutions.Length
                    ? new PauseMenuResult(PauseMenuAction.ResolutionChanged, Resolutions[_selectedOption])
                    : new PauseMenuResult(PauseMenuAction.ToggleFullscreen);

            return new PauseMenuResult(PauseMenuAction.None);
        }

        private PauseMenuResult UpdateDebug(InputCommand command)
        {
            if (command is InputCommand.Up or InputCommand.Down)
                _selectedOption = MoveSelection(_selectedOption, 3, command == InputCommand.Down);
            else if (command == InputCommand.Confirm)
            {
                if (_selectedOption == 0) ShowObjectCollisions = !ShowObjectCollisions;
                else if (_selectedOption == 1) ShowPlayerCollisions = !ShowPlayerCollisions;
                else CharacterSpeedLevel = CharacterSpeedLevel == 10 ? 1 : CharacterSpeedLevel + 1;
            }

            return new PauseMenuResult(PauseMenuAction.None);
        }

        public void Draw(SpriteBatch spriteBatch, Point virtualSize)
        {
            if (!IsOpen) return;

            var menuBounds = new Rectangle((virtualSize.X - 400) / 2, (virtualSize.Y - 350) / 2, 400, 350);
            spriteBatch.Draw(_pixel, new Rectangle(Point.Zero, virtualSize), Color.Black * 0.55f);
            spriteBatch.Draw(_pixel, menuBounds, new Color(25, 32, 52));
            spriteBatch.DrawString(_font, _page == MenuPage.Main ? "PAUSE MENU" : _page.ToString().ToUpperInvariant(), new Vector2(menuBounds.X + 70, menuBounds.Y + 35), Color.White);

            var options = GetVisibleOptions();
            for (var index = 0; index < options.Length; index++)
                DrawOption(spriteBatch, menuBounds, options[index], index, _selectedOption);

            spriteBatch.DrawString(_font, "Enter or A: confirm", new Vector2(menuBounds.X + 25, menuBounds.Bottom + 25), Color.LightGray);
            spriteBatch.DrawString(_font, "Esc or B: back", new Vector2(menuBounds.X + 25, menuBounds.Bottom + 52), Color.LightGray);
        }

        private string[] GetVisibleOptions()
        {
            return _page switch
            {
                MenuPage.Main => MainOptions,
                MenuPage.Options => OptionMenus,
                MenuPage.Graphics => [.. Resolutions.Select(resolution => $"Resolution {resolution.X} x {resolution.Y}"), "Fullscreen"],
                MenuPage.Debug => [$"Objects: {(ShowObjectCollisions ? "ON" : "OFF")}", $"Players: {(ShowPlayerCollisions ? "ON" : "OFF")}", $"Char Speed = {CharacterSpeedLevel}"],
                _ => []
            };
        }

        private void DrawOption(SpriteBatch spriteBatch, Rectangle menuBounds, string text, int index, int selected)
        {
            var position = new Vector2(menuBounds.X + 55, menuBounds.Y + 90 + index * 40);
            var isSelected = index == selected;
            var textSize = _font.MeasureString(text);
            if (isSelected)
                spriteBatch.Draw(_pixel, new Rectangle(menuBounds.X + 35, (int)position.Y - 4, 330, (int)textSize.Y + 10), new Color(69, 107, 168));
            spriteBatch.DrawString(_font, text, position, isSelected ? Color.White : Color.LightGray);
        }

        private static int MoveSelection(int current, int count, bool forward) => (current + (forward ? 1 : count - 1)) % count;
    }
}