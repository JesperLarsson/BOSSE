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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// Contains various static metrics about the map that doesn't change between runs (chokepoints etc)
    /// </summary>
    [Serializable]
    public class AnalysedStaticMap
    {
        public const int LatestFileFormatVersion = 1;
        public int FileFormatVersion = LatestFileFormatVersion;

        /// <summary>
        /// Higher values indicate chokepoints between ours and the enemy main base
        /// </summary>
        public TileMap<byte> MainBaseChokeScore = null;

        /// <summary>
        /// General "chokepoint" score for all tiles, 0 = not pathable, higher values means "more chokepointy"
        /// </summary>
        public TileMap<byte> GeneralChokeScore = null;
    }
}
