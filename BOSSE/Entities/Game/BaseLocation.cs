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
    using System.Drawing;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// A single in-game base, can belong either to us or the enemy
    /// </summary>
    public class BaseLocation
    {
        public Unit CommandCenter;
        public ResourceCluster CenteredAroundCluster;
        public Alliance BelongsTo;

        public BaseLocation(Unit commandCenter, ResourceCluster centeredAroundCluster, Alliance belongsTo)
        {
            this.CommandCenter = commandCenter;
            this.CenteredAroundCluster = centeredAroundCluster;
            this.BelongsTo = belongsTo;
        }

        public override string ToString()
        {
            return "Base " + CenteredAroundCluster;
        }
    }
}
