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
    /// Debug map - Terrain height
    /// </summary>
    public class TerrainDebugMap : BaseDebugMap
    {
        SolidBrush pixelBrush;
        public static List<KeyValuePair<Point2D, string>> MarkedPoints = new List<KeyValuePair<Point2D, string>>();
        static readonly SolidBrush NaturalEstimateLocation = new SolidBrush(System.Drawing.Color.Red);
        static readonly SolidBrush NaturalDefenseWall = new SolidBrush(System.Drawing.Color.Green);

        public TerrainDebugMap()
        {
            this.MapName = "Input - Terrain Height";
        }

        protected override Image RenderMap()
        {
            Image bmp = new Bitmap(CurrentGameState.GameInformation.StartRaw.MapSize.X * this.RenderScale, CurrentGameState.GameInformation.StartRaw.MapSize.Y * this.RenderScale);
            Graphics surface = Graphics.FromImage(bmp);
            surface.Clear(System.Drawing.Color.Black);

            // Terrain height
            ImageData terrainMap = CurrentGameState.GameInformation.StartRaw.TerrainHeight;
            for (int y = 0; y < terrainMap.Size.Y; y++)
            {
                for (int x = 0; x < terrainMap.Size.X; x++)
                {
                    byte heightValue = terrainMap.Data[x + (y * terrainMap.Size.X)];
                    if (heightValue == 0)
                        continue;

                    pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, heightValue, heightValue, heightValue));

                    float posX = x;
                    float posY = CompensateY(y);

                    surface.FillRectangle(pixelBrush, (RenderScale * posX), (RenderScale * posY), RenderScale, RenderScale);
                }
            }

            // Natural def estimate spot 
            Point2D natDefPos = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.GetNaturalWallPosition();
            surface.FillRectangle(NaturalEstimateLocation, (RenderScale * natDefPos.X), (RenderScale * CompensateY(natDefPos.Y)), RenderScale, RenderScale);

            // Natural def parts
            var naturalWall = BOSSE.ConstructionManagerRef.NaturalWallRef;
            if (naturalWall != null)
            {
                foreach (Wall.BuildingInWall iter in naturalWall.Buildings)
                {
                    surface.FillRectangle(NaturalDefenseWall, (RenderScale * iter.BuildingCenterPosition.X), (RenderScale * CompensateY(iter.BuildingCenterPosition.Y)), RenderScale, RenderScale);
                }
            }

            // Extra texts
            for (int index = 0; index < MarkedPoints.Count; index++)
            {
                KeyValuePair<Point2D, string> iter = MarkedPoints[index];

                float posX = iter.Key.X;
                float posY = CompensateY(iter.Key.Y);

                Font font = new Font("Arial", 12);
                surface.DrawString(iter.Value, font, new SolidBrush(System.Drawing.Color.Red), new PointF((RenderScale * posX), (RenderScale * posY)));
            }

            return bmp;
        }
    }
}
