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
    /// Sensor - Triggers if we scouted an enemy unit for the first time
    /// </summary>
    public class EnemyArmyUnitDetectedFirstTimeSensor : Sensor
    {
        private bool HasInitialized = false;
        private HashSet<ulong> PreviousUnitTags = new HashSet<ulong>();

        /// <summary>
        /// Updates sensor
        /// </summary>
        public override void Tick()
        {
            List<Unit> currentUnits = GameUtility.GetUnits(UnitConstants.ArmyUnits, Alliance.Enemy, true, false);

            HashSet<Unit> returnList = new HashSet<Unit>();
            foreach (Unit iter in currentUnits)
            {
                if (!PreviousUnitTags.Contains(iter.Tag))
                {
                    returnList.Add(iter);
                    PreviousUnitTags.Add(iter.Tag);
                }
            }

            if (HasInitialized)
            {
                if (returnList.Count == 0)
                    return;

                var details = new HashSet<Unit>(returnList);
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
