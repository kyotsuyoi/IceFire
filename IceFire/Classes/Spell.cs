using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IceFire.Classes
{
    public sealed class Spell
    {
        private const int Size = 32;
        private const float DetonationDelay = 3000f;
        private const float ExplosionDuration = 3000f;
        private const float ExplosionSpeed = 120f;

        private readonly Animation _castAnimation;
        private readonly Animation _plantedAnimation;
        private readonly Animation _burstAnimation;
        private readonly List<Wave> _waves = [];
        private readonly Vector2 _origin;
        private SpellState _state = SpellState.Casting;
        private float _detonationElapsed;
        private float _explosionElapsed;
        private readonly float _maximumExplosionDistance;

        public Spell(ContentManager content, Vector2 position, int detonationPower)
        {
            _origin = position;
            _maximumExplosionDistance = Math.Clamp(detonationPower, 1, 10) * Size;
            _castAnimation = LoadAnimation(content, "Spell01");
            _plantedAnimation = LoadAnimation(content, "Spell02");
            _burstAnimation = LoadAnimation(content, "Spell03");
        }

        public bool IsFinished => _state == SpellState.Finished;
        public bool IsCasting => _state == SpellState.Casting;
        public bool IsPlanted => _state == SpellState.Planted;
        public Rectangle Bounds => new((int)_origin.X, (int)_origin.Y, Size, Size);
        public int DrawLayerY => Bounds.Bottom;

        public IReadOnlyList<Rectangle> BlockingBounds
        {
            get
            {
                if (_state == SpellState.Planted) return [Bounds];
                if (_state != SpellState.Burst) return [];

                var bounds = new List<Rectangle>();
                foreach (var wave in _waves)
                {
                    if (wave.Active) bounds.Add(wave.Bounds);
                }

                return bounds;
            }
        }

        public IReadOnlyList<Rectangle> PlayerBlockingBounds => IsPlanted ? [Bounds] : [];

        public void Update(GameTime gameTime, CollisionMap collision, IReadOnlyList<Spell> otherSpells)
        {
            var elapsed = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            switch (_state)
            {
                case SpellState.Casting:
                    if (_castAnimation.Update(elapsed, loop: false))
                    {
                        _state = SpellState.Planted;
                        _plantedAnimation.Reset();
                    }
                    break;

                case SpellState.Planted:
                    _plantedAnimation.Update(elapsed, loop: true);
                    _detonationElapsed += elapsed;
                    if (_detonationElapsed >= DetonationDelay)
                    {
                        StartExplosion();
                    }
                    break;

                case SpellState.Burst:
                    _burstAnimation.Update(elapsed, loop: true);
                    _explosionElapsed += elapsed;
                    UpdateWaves(elapsed / 1000f, collision, otherSpells);
                    if (_explosionElapsed >= ExplosionDuration || !HasActiveWaves())
                    {
                        _state = SpellState.Finished;
                    }

                    break;
            }
        }

        public void DetonateNow()
        {
            if (_state == SpellState.Planted)
            {
                StartExplosion();
            }
        }

        public bool TryConsumeWave(Rectangle playerBounds)
        {
            if (_state != SpellState.Burst) return false;

            foreach (var wave in _waves)
            {
                if (wave.Active && wave.Bounds.Intersects(playerBounds))
                {
                    wave.Active = false;
                    return true;
                }
            }

            return false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            switch (_state)
            {
                case SpellState.Casting:
                    DrawAnimation(spriteBatch, _castAnimation, _origin);
                    break;
                case SpellState.Planted:
                    DrawAnimation(spriteBatch, _plantedAnimation, _origin);
                    break;
                case SpellState.Burst:
                    foreach (var wave in _waves)
                    {
                        if (wave.Active) DrawWave(spriteBatch, _burstAnimation, wave);
                    }
                    break;
            }
        }

        private void StartExplosion()
        {
            _state = SpellState.Burst;
            _burstAnimation.Reset();
            _waves.Add(new Wave(_origin, new Vector2(-1, 0)));
            _waves.Add(new Wave(_origin, new Vector2(1, 0)));
            _waves.Add(new Wave(_origin, new Vector2(0, -1)));
            _waves.Add(new Wave(_origin, new Vector2(0, 1)));
        }

        private void UpdateWaves(float seconds, CollisionMap collision, IReadOnlyList<Spell> otherSpells)
        {
            foreach (var wave in _waves)
            {
                if (!wave.Active) continue;

                var nextPosition = wave.Position + wave.Direction * ExplosionSpeed * seconds;
                wave.DistanceTravelled += Vector2.Distance(wave.Position, nextPosition);
                if (wave.DistanceTravelled >= _maximumExplosionDistance)
                {
                    wave.Active = false;
                    continue;
                }

                var nextBounds = new Rectangle((int)nextPosition.X, (int)nextPosition.Y, Size, Size);
                if (collision.TryDestroyDestructible(nextBounds))
                {
                    wave.Active = false;
                    continue;
                }

                foreach (var otherSpell in otherSpells)
                {
                    if (otherSpell.IsPlanted && otherSpell.Bounds.Intersects(nextBounds))
                    {
                        otherSpell.DetonateNow();
                    }
                }

                if (!collision.CanOccupy(nextBounds, GetPlantedSpellAreas(otherSpells)))
                {
                    wave.Active = false;
                    continue;
                }

                wave.Position = nextPosition;
            }
        }

        private static List<Rectangle> GetPlantedSpellAreas(IReadOnlyList<Spell> spells)
        {
            var areas = new List<Rectangle>();
            foreach (var spell in spells)
            {
                if (spell.IsPlanted) areas.Add(spell.Bounds);
            }

            return areas;
        }

        private bool HasActiveWaves()
        {
            foreach (var wave in _waves)
            {
                if (wave.Active) return true;
            }

            return false;
        }

        private static Animation LoadAnimation(ContentManager content, string name)
        {
            var animation = new Animation(content.Load<Texture2D>(Path.Combine("FX", name)));
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Content", "FX", name + ".json");
            if (!File.Exists(jsonPath))
            {
                animation.Frames.Add(new Frame(new Rectangle(0, 0, Size, Size), 100));
                return animation;
            }

            using var stream = File.OpenRead(jsonPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("frames", out var frames))
            {
                animation.Frames.Add(new Frame(new Rectangle(0, 0, Size, Size), 100));
                return animation;
            }

            foreach (var frameProperty in frames.EnumerateObject())
            {
                var frame = frameProperty.Value.GetProperty("frame");
                var duration = frameProperty.Value.TryGetProperty("duration", out var durationProperty)
                    ? durationProperty.GetSingle()
                    : 100f;
                animation.Frames.Add(new Frame(
                    new Rectangle(
                        frame.GetProperty("x").GetInt32(),
                        frame.GetProperty("y").GetInt32(),
                        frame.GetProperty("w").GetInt32(),
                        frame.GetProperty("h").GetInt32()),
                    duration));
            }

            return animation;
        }

        private static void DrawAnimation(SpriteBatch spriteBatch, Animation animation, Vector2 position)
        {
            var frame = animation.CurrentFrame;
            spriteBatch.Draw(animation.Texture, position, frame.Source, Color.White);
        }

        private static void DrawWave(SpriteBatch spriteBatch, Animation animation, Wave wave)
        {
            var rotation = 0f;
            var effects = SpriteEffects.None;

            if (wave.Direction.X < 0)
            {
                effects = SpriteEffects.FlipHorizontally;
            }
            else if (wave.Direction.Y > 0)
            {
                rotation = MathF.PI / 2f;
            }
            else if (wave.Direction.Y < 0)
            {
                rotation = -MathF.PI / 2f;
            }

            var frame = animation.CurrentFrame;
            var center = wave.Position + new Vector2(Size / 2f, Size / 2f);
            spriteBatch.Draw(animation.Texture, center, frame.Source, Color.White, rotation,
                new Vector2(frame.Source.Width / 2f, frame.Source.Height / 2f), 1f, effects, 0f);
        }

        private enum SpellState
        {
            Casting,
            Planted,
            Burst,
            Finished
        }

        private sealed class Wave
        {
            public Wave(Vector2 position, Vector2 direction)
            {
                Position = position;
                Direction = direction;
            }

            public Vector2 Position { get; set; }
            public Vector2 Direction { get; }
            public bool Active { get; set; } = true;
            public float DistanceTravelled { get; set; }
            public Rectangle Bounds => new((int)Position.X, (int)Position.Y, Size, Size);
        }

        private sealed class Animation
        {
            public Animation(Texture2D texture)
            {
                Texture = texture;
            }

            public Texture2D Texture { get; }
            public List<Frame> Frames { get; } = [];
            public Frame CurrentFrame => Frames.Count == 0 ? new Frame(new Rectangle(0, 0, Size, Size), 100) : Frames[Math.Clamp(Index, 0, Frames.Count - 1)];
            private int Index { get; set; }
            private float Elapsed { get; set; }

            public bool Update(float milliseconds, bool loop)
            {
                if (Frames.Count == 0) return true;

                Elapsed += milliseconds;
                while (Elapsed >= Frames[Index].Duration)
                {
                    Elapsed -= Frames[Index].Duration;
                    if (Index + 1 < Frames.Count)
                    {
                        Index++;
                    }
                    else if (loop)
                    {
                        Index = 0;
                    }
                    else
                    {
                        Index = Frames.Count - 1;
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                Index = 0;
                Elapsed = 0;
            }
        }

        private readonly record struct Frame(Rectangle Source, float Duration);
    }
}
