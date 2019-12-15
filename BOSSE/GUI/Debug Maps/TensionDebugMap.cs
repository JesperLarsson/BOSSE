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
    /// Debug map - Tension
    /// </summary>
    public class TensionDebugMap : BaseDebugMap
    {
        public TensionDebugMap()
        {
            this.MapName = "Strategic - Tension";
        }

        protected override Image RenderMap()
        {
            Image bmp = new Bitmap(CurrentGameState.GameInformation.StartRaw.MapSize.X * this.RenderScale, CurrentGameState.GameInformation.StartRaw.MapSize.Y * this.RenderScale);
            Graphics surface = Graphics.FromImage(bmp);
            surface.Clear(System.Drawing.Color.Black);

            if (StrategicMapSet.LatestMapSet == null)
                return null;

            var tensionMap = StrategicMapSet.LatestMapSet.TensionMap;
            int xSize = StrategicMapSet.LatestMapSet.xSize;
            int ySize = StrategicMapSet.LatestMapSet.ySize;

            // Draw it
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    int value = (int)(tensionMap[x, y] * 10.0f);
                    SolidBrush brushColor;

                    brushColor = new SolidBrush(System.Drawing.Color.FromArgb(255, Math.Min(value, 255), Math.Min(value, 255), Math.Min(value, 255)));

                    float xPos = x;
                    float yPos = CompensateY(y);

                    surface.FillRectangle(brushColor, (RenderScale * xPos), (RenderScale * yPos), RenderScale, RenderScale);
                }
            }

            return bmp;
        }
    }
}
