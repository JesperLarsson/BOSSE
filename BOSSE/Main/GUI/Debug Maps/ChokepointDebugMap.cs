﻿/*
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
    /// Chokepoint score between two points
    /// </summary>
    public class ChokepointDebugMap : BaseDebugMap
    {
        private ResourceCluster FromCluster;
        private ResourceCluster ToCluster;

        static readonly SolidBrush ChokeAreaColor = new SolidBrush(System.Drawing.Color.Green);

        public ChokepointDebugMap(string name, ResourceCluster fromCluster, ResourceCluster toCluster)
        {
            this.MapName = "Chokepoints - " + name;
            this.FromCluster = fromCluster;
            this.ToCluster = toCluster;
        }

        protected override Image RenderMap()
        {
            // Wait for initialization
            if (!BOSSE.HasCompletedFirstFrameInit)
                return null;

            Image bmp = new Bitmap(CurrentGameState.State.GameInformation.StartRaw.MapSize.X * this.RenderScale, CurrentGameState.State.GameInformation.StartRaw.MapSize.Y * this.RenderScale);
            Graphics surface = Graphics.FromImage(bmp);
            surface.Clear(System.Drawing.Color.Black);
            var instance = BOSSE.MapAnalysisRef.AnalysedStaticMapRef.ChokePointCollections[this.FromCluster.ClusterId][this.ToCluster.ClusterId];
            
            TileMap<byte> map = instance.ChokeScore;
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    byte value = map.GetTile(x, y);

                    var pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, value, 0, 0));
                    int yPos = map.Height - y;

                    surface.FillRectangle(pixelBrush, (RenderScale * x), (RenderScale * yPos), RenderScale, RenderScale);
                }
            }

            foreach (var iter in instance.ChokePointGroups)
            {
                RectangleF rect = iter.GetBoundingBox();

                float x = rect.X * RenderScale;
                float y = CompensateY(rect.Y + rect.Height) * RenderScale;
                float w = rect.Width * RenderScale;
                float h = rect.Height * RenderScale;

                surface.FillRectangle(ChokeAreaColor, x, y, w, h);
            }

            return bmp;
        }
    }
}
