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
    using System.Numerics;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using BOSSE;
    using SC2APIProtocol;

    /// <summary>
    /// Influence minimap
    /// </summary>
    public class InfluenceMapGui : BaseMap
    {
        protected static float[,] InfluenceMapInput = null;
        protected static int xSize;
        protected static int ySize;

        public InfluenceMapGui(Graphics _formGraphics, int _baseX, int _baseY, int renderScale) : base(_formGraphics, _baseX, _baseY, renderScale)
        {
        }

        public static void NewInfluenceMapIsAvailable(float[,] _influenceMapInput, int _xSize, int _ySize)
        {
            xSize = _xSize;
            ySize = _ySize;
            InfluenceMapInput = _influenceMapInput;
        }

        public override void Tick()
        {
            if (InfluenceMapInput == null)
                return;

            // Draw it
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    int value = (int)(InfluenceMapInput[x, y] * 10.0f);
                    SolidBrush brushColor;

                    if (value >= 0)
                    {
                        brushColor = new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0, Math.Min(value, 255)));
                    }
                    else
                    {
                        brushColor = new SolidBrush(System.Drawing.Color.FromArgb(255, Math.Min(-value, 255), 0, 0));
                    }

                    float xPos = x;
                    float yPos = CompensateY(y);

                    FormGraphics.FillRectangle(brushColor, (RenderScale * xPos) + BaseX, (RenderScale * yPos) + BaseY, RenderScale, RenderScale);
                }
            }

            InfluenceMapInput = null; // do not redraw the same data again
        }
    }
}
