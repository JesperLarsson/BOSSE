/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
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
        public static bool IsWithinRange(this Point2D self, AStar.BossePathNode node, float range)
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
        public static bool IsClose(this Point2D self, AStar.BossePathNode node)
        {
            return self.IsClose(new Point2D(node.X, node.Y));
        }
    }
}
