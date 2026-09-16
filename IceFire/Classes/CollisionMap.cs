using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace IceFire.Classes
{
    public sealed class CollisionObject
    {
        public CollisionObject(Rectangle bounds, bool destructible, uint globalTileId)
        {
            Bounds = bounds;
            Destructible = destructible;
            GlobalTileId = globalTileId;
        }

        public Rectangle Bounds { get; }
        public bool Destructible { get; }
        public uint GlobalTileId { get; }
        public bool IsDestroyed { get; private set; }
        public bool DestructionPending { get; private set; }

        public void BeginDestruction()
        {
            DestructionPending = true;
        }

        public void CompleteDestruction()
        {
            IsDestroyed = true;
            DestructionPending = false;
        }
    }

    public sealed class CollisionMap
    {
        private readonly IReadOnlyList<CollisionObject> _collisionObjects;
        private readonly Rectangle _mapBounds;

        public CollisionMap(IEnumerable<CollisionObject> collisionObjects, Rectangle mapBounds)
        {
            _collisionObjects = collisionObjects is IReadOnlyList<CollisionObject> readOnlyObjects
                ? readOnlyObjects
                : new List<CollisionObject>(collisionObjects ?? throw new ArgumentNullException(nameof(collisionObjects)));
            _mapBounds = mapBounds;
        }

        public IReadOnlyList<Rectangle> BlockedAreas
        {
            get
            {
                var areas = new List<Rectangle>();
                foreach (var collisionObject in _collisionObjects)
                {
                    if (!collisionObject.IsDestroyed) areas.Add(collisionObject.Bounds);
                }

                return areas;
            }
        }

        public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixel)
        {
            foreach (var area in BlockedAreas)
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

            foreach (var collisionObject in _collisionObjects)
            {
                if (!collisionObject.IsDestroyed && collisionObject.Bounds.Intersects(bounds)) return false;
            }

            return true;
        }

        public bool CanMove(Rectangle currentBounds, Rectangle nextBounds, int tolerance, IReadOnlyList<Rectangle> additionalAreas)
        {
            if (!CanOccupy(nextBounds, tolerance)) return false;

            foreach (var additionalArea in additionalAreas)
            {
                if (!additionalArea.Intersects(nextBounds)) continue;
                if (!additionalArea.Intersects(currentBounds)) return false;

                var currentOverlap = GetIntersectionArea(currentBounds, additionalArea);
                var nextOverlap = GetIntersectionArea(nextBounds, additionalArea);
                if (nextOverlap >= currentOverlap) return false;
            }

            return true;
        }

        public bool CanOccupy(Rectangle bounds, IReadOnlyList<Rectangle> additionalAreas)
        {
            return CanOccupy(bounds, 0, additionalAreas);
        }

        public bool CanOccupy(Rectangle bounds, int tolerance, IReadOnlyList<Rectangle> additionalAreas)
        {
            if (!CanOccupy(bounds, tolerance)) return false;

            foreach (var additionalArea in additionalAreas)
            {
                if (additionalArea.Intersects(bounds)) return false;
            }

            return true;
        }

        public bool TryGetAlignmentDirection(Rectangle attemptedBounds, bool horizontalMovement, out int direction)
        {
            direction = 0;
            var playerPerpendicularSize = horizontalMovement ? attemptedBounds.Height : attemptedBounds.Width;
            var smallestOverlap = int.MaxValue;
            var selectedArea = default(Rectangle);

            foreach (var collisionObject in _collisionObjects)
            {
                if (collisionObject.IsDestroyed) continue;
                var blockedArea = collisionObject.Bounds;
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

        public bool TryDestroyDestructible(Rectangle hitBounds, out CollisionObject destroyedObject)
        {
            destroyedObject = null;
            foreach (var collisionObject in _collisionObjects)
            {
                if (!collisionObject.IsDestroyed
                    && !collisionObject.DestructionPending
                    && collisionObject.Destructible
                    && collisionObject.Bounds.Intersects(hitBounds))
                {
                    collisionObject.BeginDestruction();
                    destroyedObject = collisionObject;
                    return true;
                }
            }

            return false;
        }

        public void CompleteDestruction(CollisionObject collisionObject)
        {
            collisionObject?.CompleteDestruction();
        }

        public bool IsDestroyed(Rectangle bounds, uint globalTileId)
        {
            foreach (var collisionObject in _collisionObjects)
            {
                if (collisionObject.GlobalTileId == globalTileId && collisionObject.Bounds == bounds)
                    return collisionObject.IsDestroyed;
            }

            return false;
        }

        private static int GetOverlap(int firstStart, int firstEnd, int secondStart, int secondEnd)
        {
            return Math.Max(0, Math.Min(firstEnd, secondEnd) - Math.Max(firstStart, secondStart));
        }

        private static int GetIntersectionArea(Rectangle first, Rectangle second)
        {
            return GetOverlap(first.Left, first.Right, second.Left, second.Right)
                * GetOverlap(first.Top, first.Bottom, second.Top, second.Bottom);
        }
    }
}
