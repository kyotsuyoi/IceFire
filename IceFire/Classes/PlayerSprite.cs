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

        public Vector2 Position { get; set; } = Vector2.Zero;
        private bool _facingRight = true;
        private float _speed = 60f; // pixels per second

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
            // Load texture via MonoGame Content pipeline
            var tex = _content.Load<Texture2D>(Path.Combine("Sprites", spriteBaseName));

            // Load JSON metadata from the Content folder at runtime. If JSON missing, fall back to single-frame animation
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Content", "Sprites", spriteBaseName + ".json");
            var anim = new Animation();
            anim.Texture = tex;
            if (!File.Exists(jsonPath))
            {
                anim.Frames.Add(new Frame { Source = new Rectangle(0, 0, _texture.Width, _texture.Height), Duration = 100 });
                Console.WriteLine($"Warning: sprite JSON not found for '{spriteBaseName}', using full-texture fallback.");
            }
            else
            {
                using var fs = File.OpenRead(jsonPath);
                using var doc = JsonDocument.Parse(fs);

                if (!doc.RootElement.TryGetProperty("frames", out var framesElement))
                {
                    // no frames property: fallback
                    anim.Frames.Add(new Frame { Source = new Rectangle(0, 0, _texture.Width, _texture.Height), Duration = 100 });
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

        public void Update(GameTime gameTime, InputCommand command)
        {
            // Decide animation based on input commands
            switch (command)
            {
                case InputCommand.Left:
                    _lastAxis = LastAxis.Horizontal;
                    _lastHorizontalDir = -1;
                    _facingRight = false;
                    // walking right animation flipped to face left
                    TrySwitchTo("SpriteC0104");
                    Position += new Vector2(-1, 0) * _speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case InputCommand.Right:
                    _lastAxis = LastAxis.Horizontal;
                    _lastHorizontalDir = 1;
                    _facingRight = true;
                    TrySwitchTo("SpriteC0104");
                    Position += new Vector2(1, 0) * _speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case InputCommand.Up:
                    _lastAxis = LastAxis.Vertical;
                    _lastVerticalDir = -1;
                    TrySwitchTo("SpriteC0106");
                    Position += new Vector2(0, -1) * _speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case InputCommand.Down:
                    _lastAxis = LastAxis.Vertical;
                    _lastVerticalDir = 1;
                    TrySwitchTo("SpriteC0105");
                    Position += new Vector2(0, 1) * _speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                default:
                    // No input: set idle based on last axis/direction
                    if (_lastAxis == LastAxis.Horizontal)
                    {
                        // respect last horizontal direction for facing and idle selection
                        _facingRight = _lastHorizontalDir > 0;
                        TrySwitchTo("SpriteC0101");
                    }
                    else
                    {
                        if (_lastVerticalDir > 0) TrySwitchTo("SpriteC0102"); else TrySwitchTo("SpriteC0103");
                    }
                    break;
            }

            _current?.Update(gameTime);
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

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_texture == null || _current == null || _current.Frames.Count == 0) return;

            var frame = _current.GetCurrentFrame();
            var origin = Vector2.Zero;
            var effects = SpriteEffects.None;

            // Flip only for the animations that are designed facing right when we need them mirrored
            if ((_currentKey == "SpriteC0101" || _currentKey == "SpriteC0104") && !_facingRight)
            {
                effects = SpriteEffects.FlipHorizontally;
                // keep origin at zero to avoid one-frame-wide displacement when flipping
                origin = Vector2.Zero;
            }

            spriteBatch.Draw(_texture, Position, frame.Source, Color.White, 0f, origin, 1f, effects, 0f);
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

            public void Update(GameTime gt)
            {
                if (Frames.Count == 0) return;
                _elapsed += (int)gt.ElapsedGameTime.TotalMilliseconds;
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
