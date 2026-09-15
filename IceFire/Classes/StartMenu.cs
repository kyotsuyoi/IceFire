using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IceFire.Classes
{
    public readonly record struct StartGameRequest(string CharacterPrefix, string MapName);

    public sealed class StartMenu
    {
        private enum MenuState
        {
            PressStart,
            Main,
            GameMode,
            Options,
            Characters,
            Maps
        }

        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly string[] _characters;
        private readonly string[] _maps;
        private MenuState _state = MenuState.PressStart;
        private int _selectedIndex;
        private InputCommand _previousCommand = InputCommand.None;

        public StartMenu(SpriteFont font, GraphicsDevice graphicsDevice)
        {
            _font = font;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData([Color.White]);
            _characters = DiscoverCharacters();
            _maps = DiscoverMaps();
        }

        public bool IsFinished { get; private set; }

        public void OpenStageSelect(string characterPrefix)
        {
            var characterIndex = Array.FindIndex(
                _characters,
                character => string.Equals(character, characterPrefix, StringComparison.OrdinalIgnoreCase));

            _characterSelection = characterIndex >= 0 ? characterIndex : 0;
            _state = MenuState.Maps;
            _selectedIndex = 0;
            _previousCommand = InputCommand.None;
            IsFinished = false;
        }

        public StartGameRequest? Update(InputCommand command)
        {
            StartGameRequest? request = null;
            var commandPressed = command != _previousCommand;

            if (commandPressed)
            {
                switch (_state)
                {
                    case MenuState.PressStart:
                        if (command is InputCommand.Start or InputCommand.Confirm)
                        {
                            _state = MenuState.Main;
                            _selectedIndex = 0;
                        }
                        break;

                    case MenuState.Main:
                        if (command == InputCommand.Up || command == InputCommand.Down)
                        {
                            _selectedIndex = MoveSelection(_selectedIndex, 2, command == InputCommand.Down);
                        }
                        else if (command is InputCommand.Start or InputCommand.Confirm)
                        {
                            if (_selectedIndex == 0)
                            {
                                _state = MenuState.GameMode;
                                _selectedIndex = 0;
                            }
                            else
                            {
                                _state = MenuState.Options;
                                _selectedIndex = 0;
                            }
                        }
                        else if (command == InputCommand.Back)
                        {
                            _state = MenuState.PressStart;
                            _selectedIndex = 0;
                        }
                        break;

                    case MenuState.GameMode:
                        if (command is InputCommand.Up or InputCommand.Down)
                        {
                            _selectedIndex = MoveSelection(_selectedIndex, 2, command == InputCommand.Down);
                        }
                        else if (command is InputCommand.Start or InputCommand.Confirm)
                        {
                            if (_selectedIndex == 0)
                            {
                                _state = MenuState.Characters;
                                _selectedIndex = 0;
                            }
                        }
                        else if (command == InputCommand.Back)
                        {
                            _state = MenuState.Main;
                            _selectedIndex = 0;
                        }
                        break;

                    case MenuState.Options:
                        if (command == InputCommand.Back)
                        {
                            _state = MenuState.Main;
                            _selectedIndex = 1;
                        }
                        break;

                    case MenuState.Characters:
                        if (_characters.Length == 0)
                        {
                            if (command == InputCommand.Back)
                            {
                                _state = MenuState.GameMode;
                                _selectedIndex = 0;
                            }
                            break;
                        }

                        if (command is InputCommand.Up or InputCommand.Down)
                        {
                            _selectedIndex = MoveSelection(_selectedIndex, _characters.Length, command == InputCommand.Down);
                        }
                        else if (command is InputCommand.Start or InputCommand.Confirm)
                        {
                            _characterSelection = _selectedIndex;
                            _state = MenuState.Maps;
                            _selectedIndex = 0;
                        }
                        else if (command == InputCommand.Back)
                        {
                            _state = MenuState.GameMode;
                            _selectedIndex = 0;
                        }
                        break;

                    case MenuState.Maps:
                        if (_maps.Length == 0)
                        {
                            if (command == InputCommand.Back)
                            {
                                _state = MenuState.Characters;
                                _selectedIndex = 0;
                            }
                            break;
                        }

                        if (command is InputCommand.Up or InputCommand.Down)
                        {
                            _selectedIndex = MoveSelection(_selectedIndex, _maps.Length, command == InputCommand.Down);
                        }
                        else if (command is InputCommand.Start or InputCommand.Confirm)
                        {
                            request = new StartGameRequest(_characters[Math.Clamp(_characterSelection, 0, _characters.Length - 1)], _maps[_selectedIndex]);
                            IsFinished = true;
                        }
                        else if (command == InputCommand.Back)
                        {
                            _state = MenuState.Characters;
                            _selectedIndex = 0;
                        }
                        break;
                }
            }

            _previousCommand = command;
            return request;
        }

        private int _characterSelection;

        public void Draw(SpriteBatch spriteBatch, Point size)
        {
            spriteBatch.Draw(_pixel, new Rectangle(Point.Zero, size), new Color(8, 12, 24));

            switch (_state)
            {
                case MenuState.PressStart:
                    DrawCentered(spriteBatch, "ICE FIRE", size, -45, Color.White);
                    DrawCentered(spriteBatch, "PRESS START", size, 45, Color.LightBlue);
                    break;
                case MenuState.Main:
                    DrawMenu(spriteBatch, size, "MAIN MENU", ["GAME", "OPTIONS"], _selectedIndex);
                    break;
                case MenuState.GameMode:
                    DrawMenu(spriteBatch, size, "GAME", ["SINGLE", "MULTIPLAYER"], _selectedIndex);
                    break;
                case MenuState.Options:
                    DrawMenu(spriteBatch, size, "OPTIONS", ["EMPTY"], 0);
                    break;
                case MenuState.Characters:
                    DrawMenu(
                        spriteBatch,
                        size,
                        "SINGLE PLAYER - CHARACTER",
                        _characters.Length == 0 ? ["NO CHARACTERS"] : _characters.Select(GetCharacterDisplayName).ToArray(),
                        _selectedIndex);
                    break;
                case MenuState.Maps:
                    DrawMenu(spriteBatch, size, "SINGLE PLAYER - MAP", _maps.Length == 0 ? ["NO MAPS"] : _maps, _selectedIndex);
                    break;
            }
        }

        private void DrawMenu(SpriteBatch spriteBatch, Point size, string title, IReadOnlyList<string> items, int selected)
        {
            DrawCentered(spriteBatch, title, size, -170, Color.White);
            var startY = (size.Y - items.Count * 45) / 2;
            for (var index = 0; index < items.Count; index++)
            {
                var text = items[index];
                var textSize = _font.MeasureString(text);
                var position = new Vector2((size.X - textSize.X) / 2f, startY + index * 45);
                if (index == selected)
                {
                    spriteBatch.Draw(_pixel, new Rectangle((int)position.X - 24, (int)position.Y - 5, (int)textSize.X + 48, (int)textSize.Y + 10), new Color(50, 85, 135));
                }
                spriteBatch.DrawString(_font, text, position, index == selected ? Color.White : Color.LightGray);
            }

            DrawCentered(spriteBatch, "A / ENTER: SELECT    B / ESC: BACK", size, 260, Color.LightGray);
        }

        private void DrawCentered(SpriteBatch spriteBatch, string text, Point size, float yOffset, Color color)
        {
            var textSize = _font.MeasureString(text);
            spriteBatch.DrawString(_font, text, new Vector2((size.X - textSize.X) / 2f, (size.Y - textSize.Y) / 2f + yOffset), color);
        }

        private static int MoveSelection(int current, int count, bool forward)
        {
            if (count <= 0) return 0;
            return (current + (forward ? 1 : count - 1)) % count;
        }

        private static string GetCharacterDisplayName(string characterPrefix)
        {
            return characterPrefix.ToUpperInvariant() switch
            {
                "SPRITEC01" => "Fire Hair",
                "SPRITEC02" => "Ice Breath",
                _ => characterPrefix
            };
        }

        private static string[] DiscoverCharacters()
        {
            var spritesPath = Path.Combine(AppContext.BaseDirectory, "Content", "Sprites");
            if (!Directory.Exists(spritesPath)) return [];

            var prefixes = Directory.GetFiles(spritesPath, "SpriteC*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => Regex.Match(name!, "^(SpriteC(?:01|02))\\d{2}$", RegexOptions.IgnoreCase))
                .Where(match => match.Success)
                .Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return prefixes;
        }

        private static string[] DiscoverMaps()
        {
            var mapsPath = Path.Combine(AppContext.BaseDirectory, "Content", "Tiles", "Tiled");
            var tilesPath = Path.Combine(AppContext.BaseDirectory, "Content", "Tiles", "Sprites");
            if (!Directory.Exists(mapsPath)) return [];

            return Directory.GetFiles(mapsPath, "MAP*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => name != null && Regex.IsMatch(name, "^MAP\\d+$", RegexOptions.IgnoreCase))
                .Where(name =>
                {
                    var suffix = name![3..];
                    return File.Exists(Path.Combine(tilesPath, "TIL" + suffix + ".xnb"))
                        && File.Exists(Path.Combine(tilesPath, "OBJ" + suffix + ".xnb"));
                })
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray()!;
        }
    }
}
