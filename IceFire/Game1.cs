using IceFire.Classes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IceFire
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _debugPixel;
        private TilemapRenderer _tilemap;
        private RenderTarget2D _gameRenderTarget;
        private InputCommands _inputCommands;
        private PauseMenu _pauseMenu;
        private StartMenu _startMenu;
        private Screen _screen;
        private PlayerSprite _player;
        private readonly List<Spell> _spells = [];
        private readonly List<SpellEffect> _spellEffects = [];
        private StartGameRequest _currentGameRequest;
        private bool _gameStarted;

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
            _debugPixel = new Texture2D(GraphicsDevice, 1, 1);
            _debugPixel.SetData([Color.White]);
            _inputCommands = new InputCommands();
            _startMenu = new StartMenu(Content.Load<SpriteFont>("Fonts/MenuFont"), GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            var command = _inputCommands.Update();

            if (!_gameStarted)
            {
                var request = _startMenu.Update(command);
                if (request is StartGameRequest startGameRequest)
                    StartGame(startGameRequest);

                base.Update(gameTime);
                return;
            }

            var menuResult = _pauseMenu.Update(command);
            _screen.SetResolution(menuResult);

            switch (menuResult.Action)
            {
                case PauseMenuAction.Restart:
                    StartGame(_currentGameRequest);
                    base.Update(gameTime);
                    return;
                case PauseMenuAction.StageSelect:
                    _gameStarted = false;
                    _startMenu.OpenStageSelect(_currentGameRequest.CharacterPrefix);
                    base.Update(gameTime);
                    return;
            }

            // If menu is open, pause game updates (do not advance player or world)
            if (!_pauseMenu.IsOpen)
            {
                _player.SpeedLevel = _pauseMenu.CharacterSpeedLevel;

                if ((command is InputCommand.Confirm or InputCommand.Spell) && CanCastSpell())
                {
                    CreateSpell(_pauseMenu.DetonationPower);
                }

                var spellCollisionAreas = GetSpellCollisionAreas();
                _player?.Update(gameTime, command, _tilemap.Collision, spellCollisionAreas);
                foreach (var spell in _spells)
                {
                    if (spell.TryConsumeWave(_player.CollisionBounds))
                        _spellEffects.Add(new SpellEffect(Content, new Vector2(_player.CollisionBounds.X, _player.CollisionBounds.Y)));
                }

                for (var index = 0; index < _spells.Count; index++)
                {
                    var spell = _spells[index];
                    var otherSpells = _spells.Where(otherSpell => otherSpell != spell).ToList();
                    spell.Update(gameTime, _tilemap.Collision, otherSpells);
                    foreach (var impact in spell.ConsumeDestructibleImpacts())
                    {
                        _spellEffects.Add(new SpellEffect(
                            Content,
                            impact.Position,
                            () => _tilemap.Collision.CompleteDestruction(impact.Object)));
                    }
                }

                _spells.RemoveAll(spell => spell.IsFinished);
                foreach (var effect in _spellEffects)
                    effect.Update(gameTime);
                _spellEffects.RemoveAll(effect => effect.IsFinished);
            }

            base.Update(gameTime);
        }

        private void StartGame(StartGameRequest request)
        {
            _currentGameRequest = request;
            var suffix = request.MapName[3..];
            var mapPath = Path.Combine(AppContext.BaseDirectory, "Content", "Tiles", "Tiled", request.MapName + ".json");
            var tilePath = "Tiles/Sprites/TIL" + suffix;
            var objectPath = "Tiles/Sprites/OBJ" + suffix;

            _tilemap = new TilemapRenderer(
                mapPath,
                Content.Load<Texture2D>(tilePath),
                Content.Load<Texture2D>(objectPath),
                "TIL" + suffix,
                "OBJ" + suffix);
            _gameRenderTarget = new RenderTarget2D(GraphicsDevice, _tilemap.Size.X, _tilemap.Size.Y);
            _pauseMenu = new PauseMenu(Content.Load<SpriteFont>("Fonts/MenuFont"), GraphicsDevice);
            _spells.Clear();
            _spellEffects.Clear();
            _screen = new Screen(GraphicsDevice, _graphics, _gameRenderTarget);

            var playerSpawn = _tilemap.GetObjectByName("PlayerSpawn1");
            var playerSpawnPoint = new Point((int)playerSpawn.X, (int)playerSpawn.Y);

            _player = new PlayerSprite(Content);
            _player.Load(request.CharacterPrefix + "01");
            _player.Position = new Vector2(playerSpawnPoint.X, playerSpawnPoint.Y - 48);
            _gameStarted = true;
        }

        private bool CanCastSpell()
        {
            return _spells.Count(spell => !spell.IsFinished) < _pauseMenu.SpellLimit;
        }

        private void CreateSpell(int detonationPower)
        {
            const int tileSize = 32;
            var playerBounds = _player.CollisionBounds;
            var spellPosition = new Vector2(
                MathF.Floor(playerBounds.Center.X / tileSize) * tileSize,
                MathF.Floor(playerBounds.Center.Y / tileSize) * tileSize);
            var spellBounds = new Rectangle((int)spellPosition.X, (int)spellPosition.Y, 32, 32);
            if (!_tilemap.Collision.CanOccupy(spellBounds, GetSpellPlacementAreas())) return;

            _spells.Add(new Spell(Content, spellPosition, detonationPower));
        }

        private List<Rectangle> GetSpellCollisionAreas(Spell ignoredSpell = null)
        {
            return _spells
                .Where(spell => spell != ignoredSpell)
                .SelectMany(spell => spell.PlayerBlockingBounds)
                .ToList();
        }

        private List<Rectangle> GetSpellPlacementAreas()
        {
            return _spells
                .Where(spell => !spell.IsFinished)
                .Select(spell => spell.Bounds)
                .ToList();
        }

        protected override void Draw(GameTime gameTime)
        {
            if (!_gameStarted)
            {
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.Clear(Color.Black);
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _startMenu.Draw(_spriteBatch, new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
                _spriteBatch.End();
                base.Draw(gameTime);
                return;
            }

            GraphicsDevice.SetRenderTarget(_gameRenderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _tilemap.Draw(_spriteBatch);
            if (_pauseMenu?.ShowObjectCollisions == true)
                _tilemap.DrawCollisionDebug(_spriteBatch, _debugPixel);

            var playerDrawY = _player?.CollisionBounds.Bottom ?? int.MaxValue;
            foreach (var spell in _spells.Where(spell => spell.DrawLayerY < playerDrawY))
                spell.Draw(_spriteBatch);

            _player?.Draw(_spriteBatch);
            if (_pauseMenu?.ShowPlayerCollisions == true)
                _player?.DrawCollisionDebug(_spriteBatch, _debugPixel);

            foreach (var spell in _spells.Where(spell => spell.DrawLayerY >= playerDrawY))
                spell.Draw(_spriteBatch);

            foreach (var effect in _spellEffects)
                effect.Draw(_spriteBatch);

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
