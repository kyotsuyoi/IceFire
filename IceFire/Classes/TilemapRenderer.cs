using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotTiled;
using DotTiled.Serialization.Tmj;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IceFire.Classes
{
    public sealed class TilemapRenderer
    {
        private readonly dynamic _map;
        private readonly Dictionary<string, Texture2D> _textures;
        private readonly List<dynamic> _tilesets;

        public TilemapRenderer(string mapPath, Texture2D groundTexture, Texture2D objectTexture)
        {
            _textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase)
            {
                ["TIL01"] = groundTexture,
                ["OBJ01"] = objectTexture
            };

            _map = new TmjMapReader(
                File.ReadAllText(mapPath),
                source => throw new NotSupportedException($"External tileset not supported: {source}"),
                     _ => throw new NotSupportedException("The map does not use external templates."),
                     _ => default
            ).ReadMap();

            _tilesets = ((IEnumerable<dynamic>)_map.Tilesets).OrderBy(tileset => (uint)tileset.FirstGID.Value).ToList();
        }

        public Point Size => new((int)_map.Width * (int)_map.TileWidth, (int)_map.Height * (int)_map.TileHeight);

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (dynamic layer in _map.Layers)
            {
                if (!layer.Visible) continue;                

                if (layer is TileLayer tileLayer)
                {
                    DrawTileLayer(spriteBatch, tileLayer);
                }
                else if (layer is ObjectLayer objectLayer)
                {
                    DrawObjectLayer(spriteBatch, objectLayer);
                }
            }
        }

        private void DrawTileLayer(SpriteBatch spriteBatch, TileLayer layer)
        {
            var data = layer.Data!.Value;
            //Tile ID starts on 17
            //ID 17 corresponds to ID 0 of the TIL01 file in the Tiled application
            var globalTileIds = data.GlobalTileIDs!.Value;
            var flippingFlags = data.FlippingFlags!.Value;

            for (var index = 0; index < globalTileIds.Length; index++)
            {
                //int TileID = GetTileID(globalTileIds[index]);
                var x = index % layer.Width;
                var y = index / layer.Width;
                DrawTile(spriteBatch, globalTileIds[index], flippingFlags[index], new Vector2(x * _map.TileWidth, y * _map.TileHeight));
            }
        }

        private void DrawObjectLayer(SpriteBatch spriteBatch, ObjectLayer layer)
        {
            foreach (var tileObject in layer.Objects.OfType<TileObject>())
            {
                //Ignore the invisible collision object with ID 15 in Tiled
                //if (tileObject.GID-1 == 15) continue;
                DrawTile(spriteBatch, tileObject.GID, tileObject.FlippingFlags, new Vector2(tileObject.X, tileObject.Y - tileObject.Height));
            }
        }

        private void DrawTile(SpriteBatch spriteBatch, uint globalTileId, FlippingFlags flippingFlags, Vector2 position)
        {
            if (globalTileId == 0) return;            

            dynamic tileset = _tilesets.Last(tileset => (uint)tileset.FirstGID.Value <= globalTileId);
            var localTileId = globalTileId - (uint)tileset.FirstGID.Value;

            var texture = _textures[(string)tileset.Name];
            var source = new Rectangle(
                (int)(localTileId % (uint)tileset.Columns) * (int)tileset.TileWidth,
                (int)(localTileId / (uint)tileset.Columns) * (int)tileset.TileHeight,
                (int)tileset.TileWidth,
                (int)tileset.TileHeight);

            var (rotation, effects) = GetTransform(flippingFlags);
            var origin = new Vector2(source.Width / 2f, source.Height / 2f);
            spriteBatch.Draw(texture, position + origin, source, Color.White, rotation, origin, 1f, effects, 0f);
        }

        private static (float Rotation, SpriteEffects Effects) GetTransform(FlippingFlags flippingFlags)
        {
            var horizontallyFlipped = flippingFlags.HasFlag(FlippingFlags.FlippedHorizontally);
            var verticallyFlipped = flippingFlags.HasFlag(FlippingFlags.FlippedVertically);
            var diagonallyFlipped = flippingFlags.HasFlag(FlippingFlags.FlippedDiagonally);

            // Tiled first applies the diagonal flip and then the other flips.
            // Diagonal + Horizontal and Diagonal + Vertical represent 90° rotations.
            if (diagonallyFlipped)
            {
                //90° rotation.
                if (horizontallyFlipped) return (MathF.PI / 2f, SpriteEffects.None);
                //-90° rotation.
                if (verticallyFlipped) return (-MathF.PI / 2f, SpriteEffects.None);
                //90° rotation + horizontal flip.
                if (horizontallyFlipped && verticallyFlipped) return (MathF.PI / 2f, SpriteEffects.FlipHorizontally);
                //90° rotation + vertical flip.
                return (MathF.PI / 2f, SpriteEffects.FlipVertically);
            }

            var effects = SpriteEffects.None;
            // Effects can receive both types of flips and return none, one flip, or all
            // The logical operator |= did not assign the value by replacing the previous one, it assigns a new value to the variable like an array
            if (horizontallyFlipped) effects |= SpriteEffects.FlipHorizontally;
            if (verticallyFlipped) effects |= SpriteEffects.FlipVertically;            

            return (0f, effects);
        }
    
        private int GetTileID(uint globalTileId)
        {
            if (globalTileId == 0) return -1;
            dynamic tileset = _tilesets.Last(tileset => (uint)tileset.FirstGID.Value <= globalTileId);
            return (int)(globalTileId - (uint)tileset.FirstGID.Value);
        }

        //Get object on JSON Tiled map by name. This method is used to retrieve the player spawn point or other objects defined in the Tiled map.
        public DotTiled.Object GetObjectByName(string name)
        {
            ObjectLayer layer = ((IEnumerable<BaseLayer>)_map.Layers).OfType<ObjectLayer>().FirstOrDefault(l => l.Name == "Object");    
            if (layer == null) return null;    
            DotTiled.Object tiledObject = layer.Objects.FirstOrDefault(o => o.Name == name);
            return tiledObject;
        }
    }
}