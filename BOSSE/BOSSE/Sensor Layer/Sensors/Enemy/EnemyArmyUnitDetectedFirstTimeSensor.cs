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
        /// Details sent to the subscribers on each trigger
        /// </summary>
        public class Details : EventArgs
        {
            public List<Unit> Units;

            public Details(List<Unit> argList)
            {
                Units = argList;
            }
        }

        public EnemyArmyUnitDetectedFirstTimeSensor()
        {
            Id = SensorId.EnemyArmyUnitDetectedFirstTimeSensor;
        }

        /// <summary>
        /// Updates sensor
        /// </summary>
        public override void Tick()
        {
            List<Unit> currentUnits = GameUtility.GetUnits(UnitConstants.ArmyUnits, Alliance.Enemy, true, false);

            List<Unit> returnList = new List<Unit>();
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

                var details = new Details(returnList);
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
