using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IceFire.Classes
{
    public sealed class SpellEffect
    {
        private const int Size = 32;
        private readonly Texture2D _texture;
        private readonly List<Frame> _frames = [];
        private int _frameIndex;
        private float _elapsed;
        private readonly Action _completed;

        public SpellEffect(ContentManager content, Vector2 position, Action completed = null)
        {
            Position = position;
            _completed = completed;
            _texture = content.Load<Texture2D>(Path.Combine("FX", "Spell05"));
            LoadFrames();
        }

        public Vector2 Position { get; }
        public bool IsFinished { get; private set; }

        public void Update(GameTime gameTime)
        {
            if (IsFinished || _frames.Count == 0) return;

            _elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            while (_elapsed >= _frames[_frameIndex].Duration)
            {
                _elapsed -= _frames[_frameIndex].Duration;
                if (_frameIndex + 1 >= _frames.Count)
                {
                    IsFinished = true;
                    _completed?.Invoke();
                    return;
                }

                _frameIndex++;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsFinished || _frames.Count == 0) return;

            spriteBatch.Draw(_texture, Position, _frames[_frameIndex].Source, Color.White);
        }

        private void LoadFrames()
        {
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Content", "FX", "Spell05.json");
            if (!File.Exists(jsonPath))
            {
                _frames.Add(new Frame(new Rectangle(0, 0, Size, Size), 100));
                return;
            }

            using var stream = File.OpenRead(jsonPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("frames", out var frames))
            {
                _frames.Add(new Frame(new Rectangle(0, 0, Size, Size), 100));
                return;
            }

            foreach (var frameProperty in frames.EnumerateObject())
            {
                var frame = frameProperty.Value.GetProperty("frame");
                var duration = frameProperty.Value.TryGetProperty("duration", out var durationProperty)
                    ? durationProperty.GetSingle()
                    : 100f;
                _frames.Add(new Frame(
                    new Rectangle(
                        frame.GetProperty("x").GetInt32(),
                        frame.GetProperty("y").GetInt32(),
                        frame.GetProperty("w").GetInt32(),
                        frame.GetProperty("h").GetInt32()),
                    duration));
            }
        }

        private readonly record struct Frame(Rectangle Source, float Duration);
    }
}
