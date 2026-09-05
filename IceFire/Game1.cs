using IceFire.Classes;
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
        private Screen _screen;
        private PlayerSprite _player;

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
            _pauseMenu = new PauseMenu(Content.Load<SpriteFont>("Fonts/MenuFont"), GraphicsDevice);
            _inputCommands = new InputCommands();
            _screen = new Screen(GraphicsDevice, _graphics, _gameRenderTarget);

            //Player spawn location
            var playerSpawn1 = _tilemap.GetObjectByName("PlayerSpawn1");
            var playerSpawn2 = _tilemap.GetObjectByName("PlayerSpawn2");
            var playerSpawn1Point = new Point((int)playerSpawn1.X, (int)playerSpawn1.Y);
            var playerSpawn2Point = new Point((int)playerSpawn2.X, (int)playerSpawn2.Y);

            // Create and load player sprite. PlayerSprite will load animations on demand.
            _player = new PlayerSprite(Content);
            // Load a default sprite (idle right) and set initial position to spawn point
            _player.Load("SpriteC0101");
            _player.Position = new Vector2(playerSpawn1Point.X, playerSpawn1Point.Y - 48); // align to bottom of tile
        }

        protected override void Update(GameTime gameTime)
        {
            var command = _inputCommands.Update();
            var menuResult = _pauseMenu.Update(command);
            _screen.SetResolution(menuResult);

            // If menu is open, pause game updates (do not advance player or world)
            if (!_pauseMenu.IsOpen)
            {
                _player?.Update(gameTime, command);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_gameRenderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _tilemap.Draw(_spriteBatch);
            _player?.Draw(_spriteBatch);
            _pauseMenu.Draw(_spriteBatch, _tilemap.Size);
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_gameRenderTarget, _screen.GetPresentationRectangle(), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
