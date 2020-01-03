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
        public static float AirDistanceManhattan(this Point2D self, Point2D other)
        {
            return Math.Abs(self.X - other.X) + Math.Abs(self.Y - other.Y);
        }

        /// <summary>
        /// Calculates the squared distance to other point
        /// </summary>
        public static float AirDistanceSquared(this Point2D self, Point2D other)
        {
            return (self.X - other.X) * (self.X - other.X) + (self.Y - other.Y) * (self.Y - other.Y);
        }

        /// <summary>
        /// Calculates the absolute distance to other point, slower than using the squared method
        /// </summary>
        public static float AirDistanceAbsolute(this Point2D self, Point2D other)
        {
            float sq = self.AirDistanceSquared(other);
            float distance = (float)Math.Sqrt(sq);

            return distance;
        }

        /// <summary>
        /// Calculates the ground distance to another point. null = no pathing possible
        /// Slightly overestimates the distance since it does not compute the diagonals
        /// </summary>
        public static float? GroundDistanceAbsolute(this Point2D self, Point2D other)
        {
            LinkedList<BossePathNode> path = BOSSE.PathFinderRef.FindPath(self, other);
            if (path == null)
                return null;

            return path.Count;
        }

        /// <summary>
        /// Determines if we are within the given range of another point
        /// </summary>
        public static bool IsWithinRange(this Point2D self, Point2D other, float range, bool checkGroundDistance = false)
        {
            // Squaring the range is slightly faster since we don't have to calculate the root
            float sqDistance = self.AirDistanceSquared(other);
            float sqRange = range * range;

            bool inAirRange = sqDistance <= sqRange;
            if ((!checkGroundDistance) || (!inAirRange))
                return inAirRange;

            // Optional - Check ground distance as well
            float? groundDistance = GroundDistanceAbsolute(self, other);
            if (groundDistance == null)
                return false;
            float groundSq = groundDistance.Value * groundDistance.Value;
            bool inGroundRange = groundSq <= sqRange;

            return inGroundRange && inAirRange;
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
        /// Determines if we are at the given point
        /// </summary>
        public static bool IsAt(this Point2D self, Point2D other)
        {
            const float CloseThreshold = 0.1f;
            return self.IsWithinRange(other, CloseThreshold);
        }

        /// <summary>
        /// Compares the values to another point
        /// </summary>
        public static bool IsSameCoordinates(this Point2D self, Point2D other)
        {
            if (other == null)
                return false;

            if (self.X != other.X)
                return false;
            if (self.Y != other.Y)
                return false;

            return true;
        }

        /// <summary>
        /// Builds a map of the air distance from this point to every other point on the map (distances are squared)
        /// </summary>
        public static TileMap<float> GetAirDistanceSqMap(this Point2D self)
        {
            TileMap<float> map = new TileMap<float>();

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    Point2D targetPos = new Point2D(x, y);
                    float distance = self.AirDistanceSquared(targetPos);
                    map.SetTile(x, y, distance);
                }
            }

            return map;
        }

        /// <summary>
        /// Finds a path to the given point
        /// </summary>
        public static LinkedList<BossePathNode> FindPath(this Point2D self, Point2D to)
        {
            return BOSSE.PathFinderRef.FindPath(self, to);
        }

        /// <summary>
        /// Returns true if this tile can be walked on by ground units
        /// </summary>
        public static bool IsPathable(this Point2D self)
        {
            bool isPathable = CurrentGameState.GameInformation.StartRaw.PathingGrid.GetBit((int)self.X, (int)self.Y) != 0;
            return isPathable;
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
