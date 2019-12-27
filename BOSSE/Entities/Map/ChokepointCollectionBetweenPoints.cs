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
namespace BOSSE
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Runtime.Serialization;
    using System.Drawing;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// A single high level chokepoint
    /// </summary>
    [Serializable]
    public class ChokePointGroup
    {
        public List<Point2D> ChokeMap = new List<Point2D>();

        /// <summary>
        /// Calculates an area for this cluster
        /// </summary>
        public Rectangle GetBoundingBox()
        {
            if (ChokeMap.Count == 0)
                throw new BosseFatalException("Can't calculate bounding box at choke group");

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = 0;
            float maxY = 0;
            foreach (var iter in ChokeMap)
            {
                float x = iter.X;
                float y = iter.Y;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }

            if (minX == float.MaxValue || minY == float.MaxValue || maxX == 0 || maxY == 0)
            {
                Log.SanityCheckFailed("Unexpected results in choke bounding box calculation");
            }

            int rectX = (int)Math.Floor(minX);
            int rectY = (int)Math.Floor(minY);
            int rectWidth = (int)Math.Ceiling(maxX - minX);
            int rectHeight = (int)Math.Ceiling(maxY - minY);
            return new Rectangle(rectX, rectY, rectWidth, rectHeight);
        }

        /// <summary>
        /// Gets the central point of the tiles currently in this collection
        /// </summary>
        public Point2D GetCenterOfChoke()
        {
            if (ChokeMap.Count == 0)
                throw new BosseFatalException("Can't calculate chokepoint center without coordinates");

            float xTotal = 0;
            float yTotal = 0;

            foreach (var iter in this.ChokeMap)
            {
                xTotal += iter.X;
                yTotal += iter.Y;
            }

            float x = xTotal / this.ChokeMap.Count;
            float y = yTotal / this.ChokeMap.Count;
            Point2D resultPos = new Point2D(x, y);

            return resultPos;
        }
    }

    /// <summary>
    /// Contains a set of chokepoints between two points on the map. Part of the static map analysis
    /// </summary>
    [Serializable]
    public class ChokepointCollectionBetweenPoints
    {
        public long FromResourceClusterId;
        public long ToResourceClusterId;

        /// <summary>
        /// Chokepoint score, higher values means "more chokepointy"
        /// </summary>
        public TileMap<byte> ChokeScore;

        /// <summary>
        /// Chokepoint groups, sorted by ground distance to the From point
        /// </summary>
        public List<ChokePointGroup> ChokePointGroups = null;

        public ChokepointCollectionBetweenPoints()
        {

        }

        public ChokepointCollectionBetweenPoints(long fromResourceClusterId, long toResourceClusterId, TileMap<byte> chokeScore)
        {
            FromResourceClusterId = fromResourceClusterId;
            ToResourceClusterId = toResourceClusterId;
            ChokeScore = chokeScore;
        }
    }
}
