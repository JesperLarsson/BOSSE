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
    /// Building placement grid minimap
    /// </summary>
    public class PlacementGridMap : BaseMap
    {
        protected static int xSize;
        protected static int ySize;

        public PlacementGridMap(Graphics _formGraphics, int _baseX, int _baseY, int renderScale) : base(_formGraphics, _baseX, _baseY, renderScale)
        {
        }

        public override void Tick()
        {
            SolidBrush pixelBrush;
            RectangleI playArea = CurrentGameState.GameInformation.StartRaw.PlayableArea;
            ImageData gridMap = CurrentGameState.GameInformation.StartRaw.PlacementGrid;
            for (int y = 0; y < gridMap.Size.Y; y++)
            {
                for (int x = 0; x < gridMap.Size.X; x++)
                {
                    bool canBePlaced = gridMap.GetBit(x, y) != 0;

                    if (canBePlaced)
                        pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 255, 255));
                    else
                        pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0, 0));

                    float posX = x - playArea.P0.X;
                    float posY = CompensateY(y - playArea.P0.Y);

                    FormGraphics.FillRectangle(pixelBrush, (RenderScale * posX) + BaseX, (RenderScale * posY) + BaseY, RenderScale, RenderScale);
                }
            }
        }
    }
}
