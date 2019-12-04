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
    /// Sensor - Triggers if one of our own units died
    /// </summary>
    public class OwnMilitaryUnitDiedSensor : Sensor
    {
        private bool HasInitialized = false;
        private HashSet<ulong> PreviousUnitTags = new HashSet<ulong>();

        /// <summary>
        /// Details sent to the subscribers on each trigger
        /// </summary>
        public class Details : EventArgs
        {
            public List<Unit> KilledUnits;

            public Details(List<Unit> argList)
            {
                KilledUnits = argList;
            }
        }

        public OwnMilitaryUnitDiedSensor()
        {
            Id = SensorId.OwnMilitaryUnitDiedSensor;
        }

        /// <summary>
        /// Updates sensor
        /// </summary>
        public override void Tick()
        {
            List<Unit> currentUnits = GameUtility.GetUnits(UnitConstants.ArmyUnits, onlyCompleted: true);
            List<Unit> killedUnits = new List<Unit>();

            foreach (uint prevIterTag in PreviousUnitTags)
            {
                bool found = false;
                Unit refUnits = null;
                foreach (Unit currentUnit in currentUnits)
                {
                    if (currentUnit.Tag == prevIterTag)
                    {
                        found = true;
                        continue;
                    }
                }

                if (!found)
                {
                    killedUnits.Add(refUnits);
                    PreviousUnitTags.Remove(prevIterTag);
                }
            }

            if (HasInitialized)
            {
                if (killedUnits.Count == 0)
                    return;

                var details = new Details(killedUnits);
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
