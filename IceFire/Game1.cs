using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace IceFire
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private TilemapRenderer _tilemap;
        private RenderTarget2D _gameRenderTarget;
        private InputCommands _inputCommands;
        private PauseMenu _pauseMenu;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 960;
            _graphics.PreferredBackBufferHeight = 640;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _tilemap = new TilemapRenderer(
                Path.Combine(AppContext.BaseDirectory, "Content", "Tiles", "Tiled", "MAP01.json"),
                Content.Load<Texture2D>("Tiles/Sprites/TIL01"),
                Content.Load<Texture2D>("Tiles/Sprites/OBJ01")
            );
            _gameRenderTarget = new RenderTarget2D(GraphicsDevice, _tilemap.Size.X, _tilemap.Size.Y);
            var pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData([Color.White]);
            _inputCommands = new InputCommands();
            _pauseMenu = new PauseMenu(Content.Load<SpriteFont>("Fonts/MenuFont"), pixel);
        }

        protected override void Update(GameTime gameTime)
        {
            var menuResult = _pauseMenu.Update(_inputCommands.Update());
            if (menuResult.Resolution is Point resolution)
            {
                SetResolution(resolution);
            }
            else if (menuResult.ToggleFullscreen)
            {
                _graphics.IsFullScreen = !_graphics.IsFullScreen;
                _graphics.ApplyChanges();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_gameRenderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _tilemap.Draw(_spriteBatch);
            _pauseMenu.Draw(_spriteBatch, _tilemap.Size);
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_gameRenderTarget, GetPresentationRectangle(), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        //Gets the current window/monitor size. 
        //Calculates the smaller scale factor between width and height. 
        //Maintains the map's original aspect ratio (960x640). 
        //Centers the result on the screen. 
        //Example: On a 1920×1080 screen, the 3:2 map does not fill the entire height without distortion.
        //The method calculates a proportional area—such as 1620×1080 and centers it, resulting in black bars on the sides. 
        //It prevents the map from being stretched or squashed at different resolutions.
        private Rectangle GetPresentationRectangle()
        {
            var viewport = GraphicsDevice.Viewport;
            var scale = MathF.Min(viewport.Width / (float)_gameRenderTarget.Width,viewport.Height / (float)_gameRenderTarget.Height);
            var width = (int)(_gameRenderTarget.Width * scale);
            var height = (int)(_gameRenderTarget.Height * scale);

            return new Rectangle((viewport.Width - width) / 2, (viewport.Height - height) / 2, width, height);
        }

        private void SetResolution(Point resolution)
        {
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = resolution.X;
            _graphics.PreferredBackBufferHeight = resolution.Y;
            _graphics.ApplyChanges();
        }
    }
}
