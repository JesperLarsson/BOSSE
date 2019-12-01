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
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Sensor - Triggers if one of our own structures has finished building
    /// </summary>
    public class OwnStructureWasCompletedSensor : Sensor
    {
        /// <summary>
        /// Details sent to subscriber on each trigger
        /// </summary>
        public class Details : EventArgs
        {
            public Unit CompletedStructure;
        }

        public OwnStructureWasCompletedSensor()
        {
            Id = SensorId.OwnStructureCompletedSensor;
        }

        public override void Tick()
        {
            this.Trigger(new Details());
        }
    }
}
