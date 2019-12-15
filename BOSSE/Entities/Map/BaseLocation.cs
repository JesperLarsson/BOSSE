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
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// A single possible base location on the map (minerals etc), does not necessarily mean something is built there
    /// </summary>
    public class BaseLocation
    {
        public Point2D Location;

        /// <summary>
        /// Ramp leading to this base, if any. Can be NULL
        /// </summary>
        public Ramp RampToBase;

        public HashSet<Unit> MineralFields = new HashSet<Unit>();
        public HashSet<Unit> GasGeysers = new HashSet<Unit>();

        /// <summary>
        /// Returns a unique ID for this base location. Guaranteed to be unique for this map, even between runs (input order from sc2 is otherwise random)
        /// </summary>
        public int BaseId { get => SpookilySharp.SpookyHasher.SpookyHash32(this.Location.X + "__" + this.Location.Y); }
    }
}
