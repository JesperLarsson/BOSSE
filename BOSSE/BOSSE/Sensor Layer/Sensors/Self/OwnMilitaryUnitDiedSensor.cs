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
        private HashSet<ulong> PreviousUnitTags = new HashSet<ulong>();

        /// <summary>
        /// Updates sensor
        /// </summary>
        public override void Tick()
        {
            List<Unit> currentUnits = GameUtility.GetUnits(UnitConstants.ArmyUnits, onlyCompleted: true);
            HashSet<Unit> killedUnits = new HashSet<Unit>();

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

            if (killedUnits.Count == 0)
                return;

            var details = new HashSet<Unit>(killedUnits);
            Trigger(details);
        }
    }
}
