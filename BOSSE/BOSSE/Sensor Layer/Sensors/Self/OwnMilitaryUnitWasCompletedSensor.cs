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
    /// Sensor - Triggers if one of our own military units has finished training
    /// </summary>
    public class OwnMilitaryUnitWasCompletedSensor : Sensor
    {
        private bool HasInitialized = false;
        private HashSet<ulong> PreviousUnitTags = new HashSet<ulong>();

        /// <summary>
        /// Updates sensor
        /// </summary>
        public override void OnFrameTick()
        {
            List<Unit> currentUnits = GameUtility.GetUnits(UnitConstants.ArmyUnits, onlyCompleted: true);

            HashSet<Unit> newStructures = new HashSet<Unit>();
            foreach (Unit iter in currentUnits)
            {
                if (!PreviousUnitTags.Contains(iter.Tag))
                {
                    newStructures.Add(iter);
                    PreviousUnitTags.Add(iter.Tag);
                }
            }

            if (newStructures.Count == 0)
                return;

            var details = new HashSet<Unit>(newStructures);
            Trigger(details);
        }
    }
}