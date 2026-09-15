using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IceFire.Classes
{
    public class PlayerSprite
    {
        private readonly ContentManager _content;
        private readonly Dictionary<string, Animation> _animations = new();
        private Animation _current;
        private string _currentKey = string.Empty;
        private Texture2D _texture;
        private string _characterPrefix = "SpriteC01";

        public Vector2 Position { get; set; } = Vector2.Zero;
        public Rectangle CollisionBounds => GetBounds(Position);

        // Percentage of the perpendicular collision area used for corner alignment.
        // 0.25f means up to 25% of a 32px collision area (8px).
        public float AutoAlignPercentage { get; set; } = 0.25f;
        public int SpeedLevel { get; set; } = 1;

        private const int CollisionWidth = 32;
        private const int CollisionHeight = 32;
        private const int AutoAlignTolerance = 2;
        private bool _facingRight = true;
        private const float BaseSpeed = 60f;

        // Tracks whether last axis used was vertical or horizontal and direction so we can select an idle animation
        private enum LastAxis { Horizontal, Vertical }
        private LastAxis _lastAxis = LastAxis.Horizontal;
        private int _lastVerticalDir = 1; // 1 = down, -1 = up
        private int _lastHorizontalDir = 1; // 1 = right, -1 = left

        public PlayerSprite(ContentManager content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public void Load(string spriteBaseName)
        {
            if (spriteBaseName.StartsWith("SpriteC", StringComparison.OrdinalIgnoreCase) && spriteBaseName.Length > 2)
                _characterPrefix = spriteBaseName[..^2];

            // Load texture via MonoGame Content pipeline
            var tex = _content.Load<Texture2D>(Path.Combine("Sprites", spriteBaseName));

            // Load JSON metadata from the Content folder at runtime. If JSON missing, fall back to single-frame animation
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Content", "Sprites", spriteBaseName + ".json");
            var anim = new Animation();
            anim.Texture = tex;
            if (!File.Exists(jsonPath))
            {
                anim.Frames.Add(new Frame { Source = new Rectangle(0, 0, tex.Width, tex.Height), Duration = 100 });
                Console.WriteLine($"Warning: sprite JSON not found for '{spriteBaseName}', using full-texture fallback.");
            }
            else
            {
                using var fs = File.OpenRead(jsonPath);
                using var doc = JsonDocument.Parse(fs);

                if (!doc.RootElement.TryGetProperty("frames", out var framesElement))
                {
                    // no frames property: fallback
                    anim.Frames.Add(new Frame { Source = new Rectangle(0, 0, tex.Width, tex.Height), Duration = 100 });
                }
                else
                {
                    foreach (var frameProp in framesElement.EnumerateObject())
                    {
                        if (!frameProp.Value.TryGetProperty("frame", out var f)) continue;
                        var x = f.GetProperty("x").GetInt32();
                        var y = f.GetProperty("y").GetInt32();
                        var w = f.GetProperty("w").GetInt32();
                        var h = f.GetProperty("h").GetInt32();
                        var duration = 100;
                        if (frameProp.Value.TryGetProperty("duration", out var d)) duration = d.GetInt32();

                        anim.Frames.Add(new Frame { Source = new Rectangle(x, y, w, h), Duration = duration });
                    }
                }
            }

            // Use the provided base name as key for the loaded animation
            _animations[spriteBaseName] = anim;
            // set texture to the loaded texture
            _texture = tex;
            // set as current by default
            SetAnimation(spriteBaseName);
        }

        public void SetAnimation(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!_animations.TryGetValue(key, out var anim)) return;
            if (_currentKey == key) return;
            _currentKey = key;
            _current = anim;
            // ensure texture used for drawing matches the animation that was loaded
            if (_current.Texture != null) _texture = _current.Texture;
            _current.Reset();
        }

        public void Update(GameTime gameTime, InputCommand command, CollisionMap collision)
        {
            var movement = Vector2.Zero;

            // Decide animation based on input commands
            switch (command)
            {
                case InputCommand.Left:
                    _lastAxis = LastAxis.Horizontal;
                    _lastHorizontalDir = -1;
                    _facingRight = false;
                    // walking right animation flipped to face left
                    TrySwitchTo(_characterPrefix + "04");
                    movement = new Vector2(-1, 0);
                    break;
                case InputCommand.Right:
                    _lastAxis = LastAxis.Horizontal;
                    _lastHorizontalDir = 1;
                    _facingRight = true;
                    TrySwitchTo(_characterPrefix + "04");
                    movement = new Vector2(1, 0);
                    break;
                case InputCommand.Up:
                    _lastAxis = LastAxis.Vertical;
                    _lastVerticalDir = -1;
                    TrySwitchTo(_characterPrefix + "06");
                    movement = new Vector2(0, -1);
                    break;
                case InputCommand.Down:
                    _lastAxis = LastAxis.Vertical;
                    _lastVerticalDir = 1;
                    TrySwitchTo(_characterPrefix + "05");
                    movement = new Vector2(0, 1);
                    break;
                default:
                    // No input: set idle based on last axis/direction
                    if (_lastAxis == LastAxis.Horizontal)
                    {
                        // respect last horizontal direction for facing and idle selection
                        _facingRight = _lastHorizontalDir > 0;
                        TrySwitchTo(_characterPrefix + "01");
                    }
                    else
                    {
                        if (_lastVerticalDir > 0) TrySwitchTo(_characterPrefix + "02"); else TrySwitchTo(_characterPrefix + "03");
                    }
                    break;
            }

            var speedLevel = Math.Clamp(SpeedLevel, 1, 10);
            var speedMultiplier = 1f + (speedLevel - 1) * 0.15f;
            Move(movement * BaseSpeed * speedMultiplier * (float)gameTime.ElapsedGameTime.TotalSeconds, collision);

            _current?.Update(gameTime, IsMovementAnimation() ? speedMultiplier : 1f);
        }

        private void Move(Vector2 movement, CollisionMap collision)
        {
            if (movement == Vector2.Zero) return;

            var nextPosition = Position + movement;
            if (collision == null || collision.CanOccupy(GetBounds(nextPosition)))
            {
                Position = nextPosition;
                return;
            }

            // When only a corner of the hitbox catches an object, make a very
            // small correction on the perpendicular axis. The correction is
            // intentionally limited to a few pixels per frame so the player
            // slides smoothly instead of jumping into another position.
            if (!collision.TryGetAlignmentDirection(GetBounds(nextPosition), movement.X != 0, out var direction))
                return;

            if (movement.X != 0)
            {
                for (var distance = 1; distance <= GetAutoAlignDistance(horizontalMovement: true); distance++)
                {
                    if (TryMoveTo(new Vector2(nextPosition.X, Position.Y + direction * distance), collision, AutoAlignTolerance)) return;
                }
            }
            else
            {
                for (var distance = 1; distance <= GetAutoAlignDistance(horizontalMovement: false); distance++)
                {
                    if (TryMoveTo(new Vector2(Position.X + direction * distance, nextPosition.Y), collision, AutoAlignTolerance)) return;
                }
            }
        }

        private int GetAutoAlignDistance(bool horizontalMovement)
        {
            var percentage = Math.Clamp(AutoAlignPercentage, 0f, 1f);
            var perpendicularSize = horizontalMovement ? CollisionHeight : CollisionWidth;
            return (int)MathF.Ceiling(perpendicularSize * percentage);
        }

        private bool TryMoveTo(Vector2 position, CollisionMap collision, int tolerance)
        {
            if (!collision.CanOccupy(GetBounds(position), tolerance)) return false;

            Position = position;
            return true;
        }

        private Rectangle GetBounds(Vector2 position)
        {
            var frame = _current?.GetCurrentFrame();
            var width = frame?.Source.Width ?? 32;
            var height = frame?.Source.Height ?? 32;
            var offsetX = Math.Max(0, (width - CollisionWidth) / 2);
            var offsetY = Math.Max(0, height - CollisionHeight);
            return new Rectangle(
                (int)position.X + offsetX,
                (int)position.Y + offsetY,
                Math.Min(CollisionWidth, width),
                Math.Min(CollisionHeight, height));
        }

        private void TrySwitchTo(string key)
        {
            // If animation not yet loaded, attempt to load it on demand
            if (!_animations.ContainsKey(key))
            {
                try
                {
                    Load(key);
                }
                catch (Exception ex)
                {
                    // log and avoid silently keeping previous animation
                    Console.WriteLine($"Failed to load animation '{key}': {ex.Message}");
                    // create an empty animation placeholder so the current animation won't remain stale
                    _animations[key] = new Animation();
                }
            }
            SetAnimation(key);
        }

        private bool IsMovementAnimation()
        {
            return _currentKey.EndsWith("04", StringComparison.OrdinalIgnoreCase)
                || _currentKey.EndsWith("05", StringComparison.OrdinalIgnoreCase)
                || _currentKey.EndsWith("06", StringComparison.OrdinalIgnoreCase);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_texture == null || _current == null || _current.Frames.Count == 0) return;

            var frame = _current.GetCurrentFrame();
            var origin = Vector2.Zero;
            var effects = SpriteEffects.None;

            // Flip only for the animations that are designed facing right when we need them mirrored
            if ((_currentKey.EndsWith("01", StringComparison.OrdinalIgnoreCase) || _currentKey.EndsWith("04", StringComparison.OrdinalIgnoreCase)) && !_facingRight)
            {
                effects = SpriteEffects.FlipHorizontally;
                // keep origin at zero to avoid one-frame-wide displacement when flipping
                origin = Vector2.Zero;
            }

            spriteBatch.Draw(_texture, Position, frame.Source, Color.White, 0f, origin, 1f, effects, 0f);
        }

        public void DrawCollisionDebug(SpriteBatch spriteBatch, Texture2D pixel)
        {
            spriteBatch.Draw(pixel, CollisionBounds, new Color(45, 100, 230) * 0.35f);
        }

        private class Frame
        {
            public Rectangle Source;
            public int Duration;
        }

        private class Animation
        {
            public List<Frame> Frames { get; } = new();
            private int _index = 0;
            private int _elapsed = 0;
            public Texture2D Texture { get; set; }

            public void Update(GameTime gt, float speedMultiplier)
            {
                if (Frames.Count == 0) return;
                _elapsed += (int)(gt.ElapsedGameTime.TotalMilliseconds * speedMultiplier);
                var currentDuration = Frames[_index].Duration;
                if (_elapsed >= currentDuration)
                {
                    _elapsed = 0;
                    _index++;
                    if (_index >= Frames.Count) _index = 0;
                }
            }

            public Frame GetCurrentFrame()
            {
                if (Frames.Count == 0) return null;
                return Frames[Math.Clamp(_index, 0, Frames.Count - 1)];
            }

            public void Reset()
            {
                _index = 0;
                _elapsed = 0;
            }
        }
    }
}