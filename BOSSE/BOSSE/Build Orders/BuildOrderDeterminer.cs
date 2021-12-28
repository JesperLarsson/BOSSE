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
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static global::BOSSE.UnitConstants;

    /// <summary>
    /// Determines which build order to use, given a certain world state
    /// </summary>
    public class BuildOrderDeterminer
    {
        /// <summary>
        /// Allowed build orders, set manually to indicate which build orders that the engine can choose from
        /// </summary>
        private List<Type> availableBuildOrders = new List<Type>()
        {
            // Terran
            typeof(MarineSpam)
        };

        /// <summary>
        /// Calculates which build order is the most viable in the current world state
        /// </summary>
        public BuildOrder DetermineBestBuildOrder()
        {
            int highestVal = -1;
            BuildOrder highestBuild = null;
            foreach (Type iter in availableBuildOrders)
            {
                BuildOrder createdObj = (BuildOrder)Activator.CreateInstance(iter);
                if (createdObj == null)
                    continue;

                if (createdObj.IsRace != BOSSE.UseRace)
                    continue;

                int viabilityFactor = createdObj.EvaluateBuildOrderViability();
                if (viabilityFactor > highestVal)
                {
                    highestBuild = createdObj;
                    highestVal = viabilityFactor;
                }
            }

            if (highestBuild == null)
                Log.SanityCheckFailed($"No build order is available for configured race ({BOSSE.UseRace})");
            else
                Log.Info($"Switched to new build order - {highestBuild.BuildName}");

            return highestBuild;
        }
    }
}
