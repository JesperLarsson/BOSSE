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
    using System.Linq;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Manages our orbital command resources (scan / mule / etc). Does not produce workers
    /// </summary>
    public class OrbitalCommandManager
    {
        private HashSet<Unit> ManagedOrbitalCommands = new HashSet<Unit>();

        const int muleEnergyCost = 50;

        public void Initialize()
        {
            BOSSE.SensorManagerRef.GetSensor(Sensor.SensorId.OwnStructureWasCompletedSensor).AddHandler(ReceiveEventBuildingFinished);
        }

        public void Tick()
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
            Queue(CommandBuilder.UseAbilityOnGround(AbilityId.CALL_DOWN_MULE, fromOrbitalCommand, GetMineralInMainMineralLine().Position));
            fromOrbitalCommand.Energy -= muleEnergyCost;
        }

        /// <summary>
        /// Callback event whenever a new building is completed
        /// </summary>
        private void ReceiveEventBuildingFinished(Object sensorRef, EventArgs args)
        {
            OwnStructureWasCompletedSensor.Details details = (OwnStructureWasCompletedSensor.Details)args;

            IEnumerable<Unit> newOrbitalCommands = details.NewStructures.Where(item => item.UnitType == (uint)UnitId.ORBITAL_COMMAND);
            foreach (Unit ocIter in newOrbitalCommands)
            {
                this.ManagedOrbitalCommands.Add(ocIter);
            }
        }
    }
}
