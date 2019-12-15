/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2020 Jesper Larsson

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
namespace DebugGui
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using BOSSE;
    using SC2APIProtocol;

    /// <summary>
    /// Base class for GUI maps
    /// Each map is rendered in a seperate thread to keep GUI responsive
    /// </summary>
    public abstract class BaseDebugMap
    {
        public string MapName = "N/A";

        protected TimeSpan RenderInterval = TimeSpan.FromMilliseconds(500);
        protected int RenderScale = 6;
        protected Image CurrentOutputtedMap = null;

        /// <summary>
        /// Called for GUI thread periodically
        /// </summary>
        public Image GetMap()
        {
            return CurrentOutputtedMap;
        }

        public void Start()
        {
            Thread th = new Thread(new ThreadStart(MainLoop));
            th.Name = "BosseGuiMapRenderer";
            th.Start();
        }

        private void MainLoop()
        {
            while (true)
            {
                try
                {
                    Image map = RenderMap();
                    map = CropMapToPlayArea(map);
                    this.CurrentOutputtedMap = map;
                }
                catch (Exception ex)
                {
                    Log.Warning("Gui exception: " + ex);
                }

                Thread.Sleep(RenderInterval);
            }
        }

        private Image CropMapToPlayArea(Image prevImage)
        {
            RectangleI playArea = CurrentGameState.GameInformation.StartRaw.PlayableArea;

            int x = playArea.P0.X;
            int y = (int)CompensateY(playArea.P0.Y);
            int width = prevImage.Width - x;
            int height = prevImage.Height - y;

            Rectangle cropMask = new Rectangle(x, y, width, height);
            Image newImage = CropImage(prevImage, cropMask);

            return newImage;
        }

        private static Image CropImage(Image img, Rectangle cropArea)
        {
            // From https://stackoverflow.com/a/734938
            Bitmap target = new Bitmap(cropArea.Width, cropArea.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(img, new Rectangle(0, 0, target.Width, target.Height),
                                 cropArea,
                                 GraphicsUnit.Pixel);
            }

            return target;
        }

        protected abstract Image RenderMap();

        /// <summary>
        /// Game starts Y from the bottom left, and GUI from the top left. So we compensate
        /// </summary>
        protected float CompensateY(float y)
        {
            float yBase = CurrentGameState.GameInformation.StartRaw.MapSize.Y;
            return yBase - y;
        }
    }
}
