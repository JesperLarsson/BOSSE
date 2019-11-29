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

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// Extends vector class with helper functions
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// XY string
        /// </summary>
        public static string ToString2(this Vector3 obj)
        {
            return $"[{obj.X}, {obj.Y}]";
        }

        /// <summary>
        /// XYZ string
        /// </summary>
        public static string ToString3(this Vector3 obj)
        {
            return $"[{obj.X}, {obj.Y}, {obj.Y}]";
        }
    }
}
