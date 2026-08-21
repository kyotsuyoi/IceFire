using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IceFire.Classes
{
    public class Screen
    {
        private GraphicsDevice _graphicsDevice;
        private GraphicsDeviceManager _graphics;
        private RenderTarget2D _gameRenderTarget;

        public Screen(GraphicsDevice graphicsDevice, GraphicsDeviceManager graphics, RenderTarget2D gameRenderTarget)
        {
            _graphicsDevice = graphicsDevice;
            _graphics = graphics;
            _gameRenderTarget = gameRenderTarget;
        }

        //Gets the current window/monitor size. 
        //Calculates the smaller scale factor between width and height. 
        //Maintains the map's original aspect ratio (960x640). 
        //Centers the result on the screen. 
        //Example: On a 1920×1080 screen, the 3:2 map does not fill the entire height without distortion.
        //The method calculates a proportional area—such as 1620×1080 and centers it, resulting in black bars on the sides. 
        //It prevents the map from being stretched or squashed at different resolutions.
        public Microsoft.Xna.Framework.Rectangle GetPresentationRectangle()
        {
            var viewport = _graphicsDevice.Viewport;
            var scale = MathF.Min(viewport.Width / (float)_gameRenderTarget.Width, viewport.Height / (float)_gameRenderTarget.Height);
            var width = (int)(_gameRenderTarget.Width * scale);
            var height = (int)(_gameRenderTarget.Height * scale);

            return new Microsoft.Xna.Framework.Rectangle((viewport.Width - width) / 2, (viewport.Height - height) / 2, width, height);
        }

        public void SetResolution(PauseMenuResult pauseMenuResult)
        {
            if (pauseMenuResult.Resolution is Microsoft.Xna.Framework.Point resolution)
            {
                _graphics.IsFullScreen = false;
                _graphics.PreferredBackBufferWidth = resolution.X;
                _graphics.PreferredBackBufferHeight = resolution.Y;
            }
            else if (pauseMenuResult.ToggleFullscreen)
            {
                _graphics.IsFullScreen = !_graphics.IsFullScreen;
            }
            _graphics.ApplyChanges();
        }
    }
}
