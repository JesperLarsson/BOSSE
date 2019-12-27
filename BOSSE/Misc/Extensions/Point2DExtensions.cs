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
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// Helper functions for <see cref="Point2D"/> sc2 coordinates
    /// </summary>
    public static class Point2DExtensions
    {
        /// <summary>
        /// Calculates the manhattan distance to other point, not squared
        /// </summary>
        public static float DistanceManhattan(this Point2D self, Point2D other)
        {
            return Math.Abs(self.X - other.X) + Math.Abs(self.Y - other.Y);
        }

        /// <summary>
        /// Calculates the squared distance to other point
        /// </summary>
        public static float DistanceSquared(this Point2D self, Point2D other)
        {
            return (self.X - other.X) * (self.X - other.X) + (self.Y - other.Y) * (self.Y - other.Y);
        }

        /// <summary>
        /// Calculates the absolute distance to other point, slower than using the squared method
        /// </summary>
        public static float DistanceAbsolute(this Point2D self, Point2D other)
        {
            float sq = self.DistanceSquared(other);
            float distance = (float)Math.Sqrt(sq);

            return distance;
        }

        /// <summary>
        /// Determines if we are within the given range of another point
        /// </summary>
        public static bool IsWithinRange(this Point2D self, Point2D other, float range)
        {
            // Squaring the range is faster since we don't have to calculate the root
            float sqDistance = self.DistanceSquared(other);
            float sqRange = range * range;

            return sqDistance <= sqRange;
        }

        /// <summary>
        /// Determines if we are within the given range of another point
        /// </summary>
        public static bool IsWithinRange(this Point2D self, BossePathNode node, float range)
        {
            return self.IsWithinRange(new Point2D(node.X, node.Y), range);
        }

        /// <summary>
        /// Determines if we are within the given range of another point
        /// </summary>
        public static bool IsWithinRange(this Point2D self, int x, int y, float range)
        {
            return self.IsWithinRange(new Point2D(x, y), range);
        }

        /// <summary>
        /// Determines if we are within the given range of another point
        /// </summary>
        public static bool IsWithinRange(this Point2D self, float x, float y, float range)
        {
            return self.IsWithinRange(new Point2D(x, y), range);
        }

        /// <summary>
        /// Determines if we are close to the given point
        /// </summary>
        public static bool IsClose(this Point2D self, Point2D other)
        {
            const float CloseThreshold = 1.0f;
            return self.IsWithinRange(other, CloseThreshold);
        }

        /// <summary>
        /// Determines if we are close to the given point
        /// </summary>
        public static bool IsClose(this Point2D self, BossePathNode node)
        {
            return self.IsClose(new Point2D(node.X, node.Y));
        }

        /// <summary>
        /// Finds a path to the given point
        /// </summary>
        public static LinkedList<BossePathNode> FindPath(this Point2D self, Point2D to)
        {
            return BOSSE.PathFinderRef.FindPath(self, to);
        }

        /// <summary>
        /// XY string
        /// </summary>
        public static string ToString2(this Point2D obj)
        {
            if (obj == null)
                return $"[NoLocation]";
            return $"[{obj.X}, {obj.Y}]";
        }
    }
}
