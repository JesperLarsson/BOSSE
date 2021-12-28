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

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// Determines which build order to follow
    /// </summary>
    public class CurrentBuildManager : Manager
    {
        /// <summary>
        /// The current build order that should be used
        /// </summary>
        private BuildStep currentBuildOrder;
        private BuildDeterminer orderDeterminer = new BuildDeterminer();

        public BuildStep GetCurrentBuild()
        {
            if (this.currentBuildOrder == null)
            {
                this.CalculateAndSetBuildOrder();
            }

            return this.currentBuildOrder;
        }

        public override void OnFrameTick()
        {
#warning TODO: Re-evaluate which build order to use intermittently, possibly triggered by enemy events or at specific points in the current build order
            if (this.currentBuildOrder == null)
            {
                this.CalculateAndSetBuildOrder();
            }
        }

        private void CalculateAndSetBuildOrder()
        {
            this.currentBuildOrder = orderDeterminer.DetermineNextStep();
        }
    }
}
