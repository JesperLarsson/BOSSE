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
    /// Terrain minimap
    /// </summary>
    public class TerrainMap : BaseMap
    {
        bool renderedOnce = false;
        SolidBrush pixelBrush;

        public static List<KeyValuePair<Point2D, string>> MarkedPoints = new List<KeyValuePair<Point2D, string>>();

        public TerrainMap(Graphics _formGraphics, int _baseX, int _baseY, int renderScale) : base(_formGraphics, _baseX, _baseY, renderScale)
        {
        }

        public override void Tick()
        {
            if (renderedOnce)
                return; // Does not change

            RectangleI playArea = BosseGui.GameInformation.StartRaw.PlayableArea;

            // Terrain height
            ImageData terrainMap = BosseGui.GameInformation.StartRaw.TerrainHeight;
            for (int y = 0; y < terrainMap.Size.Y; y++)
            {
                for (int x = 0; x < terrainMap.Size.X; x++)
                {
                    byte heightValue = terrainMap.Data[x + (y * terrainMap.Size.X)];
                    if (heightValue == 0)
                        continue;

                    pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, heightValue, heightValue, heightValue));

                    float posX = x - playArea.P0.X;
                    float posY = CompensateY(y - playArea.P0.Y);

                    FormGraphics.FillRectangle(pixelBrush, (RenderScale * posX) + BaseX, (RenderScale * posY) + BaseY, RenderScale, RenderScale);
                }
            }

            for (int index = 0; index < MarkedPoints.Count; index++)
            {
                KeyValuePair<Point2D, string> iter = MarkedPoints[index];

                float posX = iter.Key.X - playArea.P0.X;
                float posY = CompensateY(iter.Key.Y - playArea.P0.Y);

                Font font = new Font("Arial", 12);
                FormGraphics.DrawString(iter.Value, font, new SolidBrush(System.Drawing.Color.Red), new PointF((RenderScale * posX) + BaseX, (RenderScale * posY) + BaseY));

                //FormGraphics.FillRectangle(new SolidBrush(System.Drawing.Color.Red), , (RenderScale * poxY) + BaseY, RenderScale, RenderScale);
            }

            renderedOnce = true;
        }
    }
}
