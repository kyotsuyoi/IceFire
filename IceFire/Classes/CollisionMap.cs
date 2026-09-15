using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace IceFire.Classes
{
    public sealed class CollisionMap
    {
        private readonly IReadOnlyList<Rectangle> _blockedAreas;
        private readonly Rectangle _mapBounds;

        public CollisionMap(IEnumerable<Rectangle> blockedAreas, Rectangle mapBounds)
        {
            _blockedAreas = blockedAreas is IReadOnlyList<Rectangle> readOnlyAreas
                ? readOnlyAreas
                : new List<Rectangle>(blockedAreas ?? throw new ArgumentNullException(nameof(blockedAreas)));
            _mapBounds = mapBounds;
        }

        public IReadOnlyList<Rectangle> BlockedAreas => _blockedAreas;

        public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
        {
            foreach (var area in _blockedAreas)
            {
                spriteBatch.Draw(pixel, area, new Color(220, 45, 45) * 0.35f);
            }
        }

        public bool CanOccupy(Rectangle bounds)
        {
            return CanOccupy(bounds, 0);
        }

        public bool CanOccupy(Rectangle bounds, int tolerance)
        {
            if (!_mapBounds.Contains(bounds)) return false;

            if (tolerance > 0)
            {
                bounds.Inflate(-tolerance, -tolerance);
            }

            foreach (var blockedArea in _blockedAreas)
            {
                if (blockedArea.Intersects(bounds)) return false;
            }

            return true;
        }

        public bool TryGetAlignmentDirection(Rectangle attemptedBounds, bool horizontalMovement, out int direction)
        {
            direction = 0;
            var playerPerpendicularSize = horizontalMovement ? attemptedBounds.Height : attemptedBounds.Width;
            var smallestOverlap = int.MaxValue;
            var selectedArea = default(Rectangle);

            foreach (var blockedArea in _blockedAreas)
            {
                if (!blockedArea.Intersects(attemptedBounds)) continue;

                var overlap = horizontalMovement
                    ? GetOverlap(attemptedBounds.Top, attemptedBounds.Bottom, blockedArea.Top, blockedArea.Bottom)
                    : GetOverlap(attemptedBounds.Left, attemptedBounds.Right, blockedArea.Left, blockedArea.Right);

                if (overlap < smallestOverlap)
                {
                    smallestOverlap = overlap;
                    selectedArea = blockedArea;
                }
            }

            if (smallestOverlap == int.MaxValue || smallestOverlap * 4 >= playerPerpendicularSize * 3)
                return false;

            var playerCenter = horizontalMovement ? attemptedBounds.Center.Y : attemptedBounds.Center.X;
            var areaCenter = horizontalMovement ? selectedArea.Center.Y : selectedArea.Center.X;
            direction = playerCenter >= areaCenter ? 1 : -1;
            return true;
        }

        private static int GetOverlap(int firstStart, int firstEnd, int secondStart, int secondEnd)
        {
            return Math.Max(0, Math.Min(firstEnd, secondEnd) - Math.Max(firstStart, secondStart));
        }
    }
}
