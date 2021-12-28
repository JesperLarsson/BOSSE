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
    /// A single in-game base, can belong either to us or the enemy
    /// </summary>
    public class BaseLocation
    {
        private static int SelfCount = 1;
        private static int EnemyCount = 1;

        /// <summary>
        /// Main = 1, first expansion = 1, etc
        /// </summary>
        public int Number = 0;

        public Unit CommandCenterRef;
        public ResourceCluster CenteredAroundCluster;
        public Alliance BelongsTo;

        #region Own base only

        /// <summary>
        /// If false, this base should not have workers assigned to it
        /// </summary>
        public bool OwnBaseReadyToAcceptWorkers = true;

        /// <summary>
        /// Set if this is a "hidden" base, ie we should not rally workers here
        /// </summary>
        public bool IsHiddenBase = false;

        /// <summary>
        /// Set while we are currently transferring workers to this base
        /// </summary>
        public bool WorkerTransferInProgress = false;

        #endregion

        public BaseLocation(Unit commandCenter, ResourceCluster centeredAroundCluster, Alliance belongsTo)
        {
            this.CommandCenterRef = commandCenter;
            this.CenteredAroundCluster = centeredAroundCluster;
            this.BelongsTo = belongsTo;

            if (belongsTo == Alliance.Self)
            {
                this.Number = SelfCount;
                SelfCount += 1;
            }
            else if (belongsTo == Alliance.Enemy)
            {
                this.Number = EnemyCount;
                EnemyCount += 1;
            }
        }

        public override string ToString()
        {
            return $"Base nr{this.Number} {CenteredAroundCluster}";
        }
    }
}
