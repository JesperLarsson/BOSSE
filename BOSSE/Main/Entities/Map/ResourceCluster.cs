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
namespace BOSSE
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Drawing;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// A single possible base location on the map (minerals etc), does not necessarily mean something is built there
    /// </summary>
    [Serializable]
    public class ResourceCluster
    {
        public HashSet<Unit> MineralFields = new HashSet<Unit>();
        public HashSet<Unit> GasGeysers = new HashSet<Unit>();

        private Point2D CachedCcPositiion = null;

        /// <summary>
        /// Calculates an area for this cluster
        /// </summary>
        public Rectangle GetBoundingBox()
        {
            HashSet<Unit> temp = new HashSet<Unit>();
            temp.AddRange(MineralFields);
            temp.AddRange(GasGeysers);
            if (temp.Count == 0)
                throw new BosseFatalException("Can't calculate bounding box without units at cluster");

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = 0;
            float maxY = 0;
            foreach (Unit iter in temp)
            {
                float x = iter.Position.X;
                float y = iter.Position.Y;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }

            if (minX == float.MaxValue || minY == float.MaxValue || maxX == 0 || maxY == 0)
            {
                Log.SanityCheckFailed("Unexpected results in bounding box calculation");
            }
            
            int rectX = (int)Math.Floor(minX);
            int rectY = (int)Math.Floor(minY);
            int rectWidth = (int)Math.Ceiling(maxX - minX);
            int rectHeight = (int)Math.Ceiling(maxY - minY);
            return new Rectangle(rectX, rectY, rectWidth, rectHeight);
        }

        /// <summary>
        /// Gets the central position where a command center/nexus/hatchery can be placed for this resource cluster
        /// </summary>
        public Point2D GetCommandCenterPosition()
        {
#warning LP_TODO: Doesn't seem to work for starting positions for some reason. Treat as special case?
            if (this.CachedCcPositiion != null)
                return CachedCcPositiion;

            Point2D resourceCenter = GetResourceCenter();
            List<KeyValuePair<Point2D, float>> tilesSortedByDistance = resourceCenter.GetAirDistanceSqMap().GetSorted();

            Point2D finalLocation = null;
            float finalDistanceSq = 0;
            foreach (KeyValuePair<Point2D, float> tileIter in tilesSortedByDistance)
            {
                Point2D tilePos = tileIter.Key;
                float tileDistanceSq = tileIter.Value;

                bool canPlaceCC = CanPlaceRequest(BOSSE.BuildOrderManagerRef.GetCurrentBuild().CommandCenterUnit, tilePos);
                if (canPlaceCC)
                {
                    finalLocation = tilePos;
                    finalDistanceSq = tileDistanceSq;
                    break;
                }
            }

            if (finalLocation == null)
            {
                Log.SanityCheckFailed("Unable to find CC position for resource cluster " + this);
            }
            if (finalDistanceSq >= (30*30))
            {
                Log.SanityCheckFailed("CC position is too far away for resource cluster " + this);
            }

            this.CachedCcPositiion = finalLocation;
            return finalLocation;
        }

        /// <summary>
        /// Calculates the center point of the minerals in this cluster
        /// </summary>
        public Point2D GetMineralCenter()
        {
            if (this.MineralFields.Count == 0)
                throw new BosseFatalException("Can't calculate mineral center without units at cluster");

            float xTotal = 0;
            float yTotal = 0;

            foreach (Unit iter in this.MineralFields)
            {
                xTotal += iter.Position.X;
                yTotal += iter.Position.Y;
            }

            float x = xTotal / this.MineralFields.Count;
            float y = yTotal / this.MineralFields.Count;
            Point2D resultPos = new Point2D(x, y);

            return resultPos;
        }

        /// <summary>
        /// Calculates the center point of all resources (including geysers)
        /// </summary>
        public Point2D GetResourceCenter()
        {
            var box = GetBoundingBox();

            float x = box.X + (box.Width / 2);
            float y = box.Y + (box.Height / 2);
            return new Point2D(x, y);
        }

        public override string ToString()
        {
            return "MinCenter=" + GetMineralCenter().ToString2();
        }

        /// <summary>
        /// Returns a unique ID for this base location. Guaranteed to be unique for this map even between runs (input order from sc2 is otherwise random)
        /// </summary>
        public long ClusterId { get => SpookilySharp.SpookyHasher.SpookyHash64(this.GetMineralCenter().X + "__" + this.GetMineralCenter().Y); }
    }
}
