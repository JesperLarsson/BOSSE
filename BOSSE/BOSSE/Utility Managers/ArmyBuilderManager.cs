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
    using System.Linq;
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
    /// Builds army units automatically
    /// </summary>
    public class ArmyBuilderManager : Manager
    {
        private List<UnitId> UnitsToBuild = new List<UnitId>();

        /// <summary>
        /// Marks the given unit to be built from now on
        /// </summary>
        public void StartBuildingUnit(UnitId unit)
        {
            if (this.UnitsToBuild.Contains(unit))
                return;

            this.UnitsToBuild.Add(unit);
        }

        /// <summary>
        /// Marks the given unit to stop being built from now on
        /// </summary>
        public void StopBuildingUnit(UnitId unit)
        {
            this.UnitsToBuild.Remove(unit);
        }

        public override void OnFrameTick()
        {
            foreach (UnitId unitIter in this.UnitsToBuild)
            {
                bool allowChrono = ChronoboostUnits();

                GeneralGameUtility.TryBuildUnit(unitIter, true, allowChrono);
            }
        }

        private bool ChronoboostUnits()
        {
            if (BOSSE.UseRace != Race.Protoss)
                return false;

            // When we have completed the specified build order, there is no reason to not use the remaining chrono boost on boosting units
            if (BOSSE.BuildOrderManagerRef.HasCompletedBuildOrder())
                return true;

            // During buld orders, boost out units if we are running high on energy
            Unit nexusesWithHighEnergy = GetUnits(UnitId.NEXUS, onlyCompleted: true).Where(o => o.Energy >= 0.5f).FirstOrDefault();
            bool hasHighEnergy = nexusesWithHighEnergy != null;

            return hasHighEnergy;
        }
    }
}
