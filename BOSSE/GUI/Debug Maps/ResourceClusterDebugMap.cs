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
    /// Debug map - Resource clusters
    /// </summary>
    public class ResourceClusterDebugMap : BaseDebugMap
    {
        static readonly SolidBrush SelfMainColor = new SolidBrush(System.Drawing.Color.Blue);
        static readonly SolidBrush SelfNaturalColor = new SolidBrush(System.Drawing.Color.DarkBlue);
        static readonly SolidBrush SelfThirdColor = new SolidBrush(System.Drawing.Color.DarkBlue);

        static readonly SolidBrush EnemyMainColor = new SolidBrush(System.Drawing.Color.Red);
        static readonly SolidBrush EnemyNaturalColor = new SolidBrush(System.Drawing.Color.DarkRed);
        static readonly SolidBrush EnemyThirdColor = new SolidBrush(System.Drawing.Color.DarkRed);

        static readonly SolidBrush NeutralAreaColor = new SolidBrush(System.Drawing.Color.Green);

        SolidBrush noPathColor = new SolidBrush(System.Drawing.Color.Black);
        SolidBrush pathColor = new SolidBrush(System.Drawing.Color.DarkGray);

        public ResourceClusterDebugMap()
        {
            this.MapName = "Map analysis - Resource clusters";
        }

        protected override Image RenderMap()
        {
            Image bmp = new Bitmap(CurrentGameState.GameInformation.StartRaw.MapSize.X * this.RenderScale, CurrentGameState.GameInformation.StartRaw.MapSize.Y * this.RenderScale);
            Graphics surface = Graphics.FromImage(bmp);
            surface.Clear(System.Drawing.Color.Black);

            // Pathing overlay - input data contains 1 bit per pixel
            ImageData pathingMap = CurrentGameState.GameInformation.StartRaw.PathingGrid;
            for (int y = 0; y < pathingMap.Size.Y; y++)
            {
                for (int x = 0; x < (pathingMap.Size.X / 8); x++)
                {
                    byte value = pathingMap.Data[x + (y * pathingMap.Size.X / 8)];

                    byte pixel1 = (byte)(value & 0x01);
                    byte pixel2 = (byte)(value & 0x02);
                    byte pixel3 = (byte)(value & 0x04);
                    byte pixel4 = (byte)(value & 0x08);
                    byte pixel5 = (byte)(value & 0x10);
                    byte pixel6 = (byte)(value & 0x20);
                    byte pixel7 = (byte)(value & 0x40);
                    byte pixel8 = (byte)(value & 0x80);

                    int xPos = x * 8;
                    int yPos = y;

                    DrawPathingPixel(pixel1, xPos + 7, yPos, surface);
                    DrawPathingPixel(pixel2, xPos + 6, yPos, surface);
                    DrawPathingPixel(pixel3, xPos + 5, yPos, surface);
                    DrawPathingPixel(pixel4, xPos + 4, yPos, surface);
                    DrawPathingPixel(pixel5, xPos + 3, yPos, surface);
                    DrawPathingPixel(pixel6, xPos + 2, yPos, surface);
                    DrawPathingPixel(pixel7, xPos + 1, yPos, surface);
                    DrawPathingPixel(pixel8, xPos + 0, yPos, surface);
                }
            }

            // Resource clusters
            if (BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef == null)
                return bmp;

            AnalysedRuntimeMap runtimeMap = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef;
            foreach (ResourceCluster clusterIter in runtimeMap.ResourceClusters.Values)
            {
                RectangleF rect = clusterIter.GetBoundingBox();

                float x = rect.X * RenderScale;
                float y = CompensateY(rect.Y + rect.Height) * RenderScale;
                float w = rect.Width * RenderScale;
                float h = rect.Height * RenderScale;

                var brush = NeutralAreaColor;
                if (clusterIter == runtimeMap.MainBase)
                {
                    brush = SelfMainColor;
                }
                else if (clusterIter == runtimeMap.NaturalExpansion)
                {
                    brush = SelfNaturalColor;
                }
                else if (clusterIter == runtimeMap.EnemyMainBase)
                {
                    brush = EnemyMainColor;
                }
                else if (clusterIter == runtimeMap.EnemyNaturalExpansion)
                {
                    brush = EnemyNaturalColor;
                }
                else if (clusterIter == runtimeMap.EnemyThirdExpansion)
                {
                    brush = EnemyThirdColor;
                }
                else if (clusterIter == runtimeMap.ThirdExpansion)
                {
                    brush = SelfThirdColor;
                }

                surface.FillRectangle(brush, x, y, w, h);
            }

            return bmp;
        }

        private void DrawPathingPixel(byte pixelValue, int x, int y, Graphics surface)
        {
            SolidBrush pixelBrush;
            if (pixelValue == 0)
            {
                pixelBrush = noPathColor;
            }
            else
            {
                pixelBrush = pathColor;
            }

            float posX = x;
            float posY = y;

            posY = CompensateY(posY);
            surface.FillRectangle(pixelBrush, (RenderScale * posX), (RenderScale * posY), RenderScale, RenderScale);
        }
    }
}
