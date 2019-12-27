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
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using BOSSE;
    using SC2APIProtocol;

    /// <summary>
    /// Chokepoint score
    /// </summary>
    public class MainBasesChokeScoreDebugMap : BaseDebugMap
    {
        public MainBasesChokeScoreDebugMap()
        {
            this.MapName = "Chokepoints - Main bases";
        }

        protected override Image RenderMap()
        {
            // Wait for initialization
            if (!BOSSE.HasCompletedFirstFrameInit)
                return null;

            Image bmp = new Bitmap(CurrentGameState.GameInformation.StartRaw.MapSize.X * this.RenderScale, CurrentGameState.GameInformation.StartRaw.MapSize.Y * this.RenderScale);
            Graphics surface = Graphics.FromImage(bmp);
            surface.Clear(System.Drawing.Color.Black);

            TileMap<byte> map = BOSSE.MapAnalysisRef.AnalysedStaticMapRef.MainBaseChokeScore;
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    byte value = map.GetTile(x, y);
                    int sqValue = value * value;
                    if (sqValue > 255)
                    {
                        value = 255;
                    }
                    else
                    {
                        value = (byte)sqValue;
                    }

                    var pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, value, 0, 0));
                    int yPos = map.Height - y;

                    surface.FillRectangle(pixelBrush, (RenderScale * x), (RenderScale * yPos), RenderScale, RenderScale);
                }
            }

            return bmp;
        }
    }
}
