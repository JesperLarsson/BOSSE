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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// A single possible base location on the map (minerals etc), does not necessarily mean something is built there
    /// </summary>
    public class ResourceCluster
    {
        public Point2D Location;

        /// <summary>
        /// Ramp leading to this base, if any. Can be NULL
        /// </summary>
        //public Ramp RampToBase;

        public HashSet<Unit> MineralFields = new HashSet<Unit>();
        public HashSet<Unit> GasGeysers = new HashSet<Unit>();

        /// <summary>
        /// Returns a unique ID for this base location. Guaranteed to be unique for this map, even between runs (input order from sc2 is otherwise random)
        /// </summary>
        public int BaseId { get => SpookilySharp.SpookyHasher.SpookyHash32(this.Location.X + "__" + this.Location.Y); }
    }
}
