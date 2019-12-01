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
        private bool HasInitialized = false;
        private HashSet<ulong> PreviousUnitTags = new HashSet<ulong>();

        /// <summary>
        /// Details sent to the subscribers on each trigger
        /// </summary>
        public class Details : EventArgs
        {
            public List<Unit> NewStructures;

            public Details(List<Unit> completedStructures)
            {
                NewStructures = completedStructures;
            }
        }

        public OwnStructureWasCompletedSensor()
        {
            Id = SensorId.OwnStructureWasCompletedSensor;
        }

        /// <summary>
        /// Updates sensor
        /// </summary>
        public override void Tick()
        {
            List<Unit> currentStructures = GameUtility.GetUnits(UnitConstants.Structures, onlyCompleted: true);

            List<Unit> newStructures = new List<Unit>();
            foreach (Unit iter in currentStructures)
            {
                if (!PreviousUnitTags.Contains(iter.Tag))
                {
                    newStructures.Add(iter);
                    PreviousUnitTags.Add(iter.Tag);
                }
            }

            if (HasInitialized)
            {
                if (newStructures.Count == 0)
                    return;

                var details = new Details(newStructures);
                Trigger(details);
            }
            else
            {
                // The first iteration is a "dry run" to set the initial state
                HasInitialized = true;
            }
        }
    }
}
