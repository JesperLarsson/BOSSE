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
    using System.Linq;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Manages our orbital command resources (scan / mule / etc). Does not produce workers
    /// </summary>
    public class OrbitalCommandManager : Manager
    {
        private HashSet<Unit> ManagedOrbitalCommands = new HashSet<Unit>();

        const int muleEnergyCost = 50;

        public override void Initialize()
        {
            BOSSE.SensorManagerRef.GetSensor(
                typeof(OwnUnitChangedTypeSensor)).AddHandler(ReceiveEventNewOrbitalCommand,
                unfilteredList => new HashSet<Unit>(unfilteredList.Where(unitIter => unitIter.UnitType == UnitId.ORBITAL_COMMAND))
            );
        }

        public override void OnFrameTick()
        {
            foreach (Unit ocIter in ManagedOrbitalCommands)
            {
                this.SpendEnergyOrNot(ocIter);
            }
        }

        private void SpendEnergyOrNot(Unit orbitalCommand)
        {
            while (orbitalCommand.Energy >= muleEnergyCost)
            {
                CallDownMule(orbitalCommand);
            }
        }

        private void CallDownMule(Unit fromOrbitalCommand)
        {
            Queue(CommandBuilder.UseAbilityOnOtherUnit(AbilityId.CALL_DOWN_MULE, fromOrbitalCommand, GetMineralInMainMineralLine()));
            fromOrbitalCommand.Energy -= muleEnergyCost;
        }

        /// <summary>
        /// Callback event whenever a new building is completed
        /// </summary>
        private void ReceiveEventNewOrbitalCommand(HashSet<Unit> newOrbitalCommands)
        {
            foreach (Unit ocIter in newOrbitalCommands)
            {
                this.ManagedOrbitalCommands.Add(ocIter);
            }
        }
    }
}
