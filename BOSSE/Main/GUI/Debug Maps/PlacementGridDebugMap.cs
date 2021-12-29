/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2022 Jesper Larsson

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
    /// Debug map - Building placement grid
    /// </summary>
    public class PlacementGridDebugMap : BaseDebugMap
    {
        public PlacementGridDebugMap()
        {
            this.MapName = "Input - Building placement grid";
        }

        protected override Image RenderMap()
        {
            Image bmp = new Bitmap(CurrentGameState.State.GameInformation.StartRaw.MapSize.X * this.RenderScale, CurrentGameState.State.GameInformation.StartRaw.MapSize.Y * this.RenderScale);
            Graphics surface = Graphics.FromImage(bmp);
            surface.Clear(System.Drawing.Color.Black);

            SolidBrush pixelBrush;
            ImageData gridMap = CurrentGameState.State.GameInformation.StartRaw.PlacementGrid;
            for (int y = 0; y < gridMap.Size.Y; y++)
            {
                for (int x = 0; x < gridMap.Size.X; x++)
                {
                    bool canBePlaced = gridMap.GetBit(x, y) != 0;

                    if (canBePlaced)
                        pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 255, 255));
                    else
                        pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0, 0));

                    float posX = x;
                    float posY = CompensateY(y);

                    surface.FillRectangle(pixelBrush, (RenderScale * posX), (RenderScale * posY), RenderScale, RenderScale);
                }
            }

            return bmp;
        }
    }
}
