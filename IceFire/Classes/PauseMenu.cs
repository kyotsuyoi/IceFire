using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IceFire.Classes
{
    public readonly record struct PauseMenuResult(Point? Resolution, bool ToggleFullscreen);

    public sealed class PauseMenu
    {
        private static readonly Point[] Resolutions =
        {
            new(960, 640),
            new(1280, 720),
            new(1600, 900),
            new(1920, 1080)
        };

        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private int _selectedOption;

        public PauseMenu(SpriteFont font, GraphicsDevice graphicsDevice)
        {
            var pixel = new Texture2D(graphicsDevice, 1, 1);
            //Menu theme color
            pixel.SetData([Color.Blue]);

            _font = font;
            _pixel = pixel;
        }

        public bool IsOpen { get; private set; }

        public PauseMenuResult Update(InputCommand command)
        {
            if (command == InputCommand.Pause)
            {
                IsOpen = !IsOpen;
                return default;
            }

            if (!IsOpen) return default;

            var optionCount = Resolutions.Length + 1;

            var result = default(PauseMenuResult);
            switch (command)
            {
                case InputCommand.Up:
                    _selectedOption = (_selectedOption - 1 + optionCount) % optionCount;
                    break;

                case InputCommand.Down:
                    _selectedOption = (_selectedOption + 1) % optionCount;
                    break;

                case InputCommand.Confirm:
                    result = new PauseMenuResult(null, true);
                    if (_selectedOption < Resolutions.Length) 
                        result = new PauseMenuResult(Resolutions[_selectedOption], false);
                    break;
                default:
                    result = default;
                    break;
            }
            return result;
        }

        public void Draw(SpriteBatch spriteBatch, Point virtualSize)
        {
            if (!IsOpen) return;

            var menuBounds = new Rectangle((virtualSize.X - 400) / 2, (virtualSize.Y - 350) / 2, 400, 350);
            _spriteBatchDrawOverlay(spriteBatch, virtualSize);
            spriteBatch.Draw(_pixel, menuBounds, new Color(25, 32, 52));
            spriteBatch.DrawString(_font, "PAUSE MENU", new Vector2(menuBounds.X + 70, menuBounds.Y + 35), Color.White);
            spriteBatch.DrawString(_font, "Resolution", new Vector2(menuBounds.X + 70, menuBounds.Y + 80), Color.LightGray);

            for (var index = 0; index <= Resolutions.Length; index++)
            {
                var isSelected = index == _selectedOption;
                var text = "Fullscreen";
                if (index < Resolutions.Length) text=$"{Resolutions[index].X} x {Resolutions[index].Y}";

                var position = new Vector2(menuBounds.X + 85, menuBounds.Y + 120 + index * 40);
                if (isSelected)
                    spriteBatch.Draw(_pixel, new Rectangle(menuBounds.X + 60, (int)position.Y, 280, 34), new Color(69, 107, 168));                

                spriteBatch.DrawString(_font, text, position, isSelected ? Color.White : Color.LightGray);
            }

            //spriteBatch.DrawString(_font, "Arrow keys or D-pad: navigate", new Vector2(menuBounds.X + 55, menuBounds.Bottom + 25), Color.LightGray);
            spriteBatch.DrawString(_font, "Enter or A: confirm", new Vector2(menuBounds.X + 25, menuBounds.Bottom + 25), Color.LightGray);
            spriteBatch.DrawString(_font, "Esc or Start: back", new Vector2(menuBounds.X + 25, menuBounds.Bottom + 52), Color.LightGray);
        }

        private void _spriteBatchDrawOverlay(SpriteBatch spriteBatch, Point virtualSize)
        {
            spriteBatch.Draw(_pixel, new Rectangle(Point.Zero, virtualSize), Color.Black * 0.55f);
        }
    }
}
