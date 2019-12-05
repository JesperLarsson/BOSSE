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
    /// Sensor - Triggers if one of our own units (including structures) change type (CC to orbital command, etc)
    /// </summary>
    public class OwnUnitChangedTypeSensor : Sensor
    {
        // Tag => Type
        private Dictionary<ulong, UnitId> PreviousUnitTags = new Dictionary<ulong, UnitId>();

        /// <summary>
        /// Updates sensor
        /// </summary>
        public override void OnFrameTick()
        {
            List<Unit> currentStructures = GameUtility.GetUnits(UnitConstants.Structures, onlyCompleted: true);

            HashSet<Unit> returnList = new HashSet<Unit>();
            foreach (Unit iter in currentStructures)
            {
                UnitId type = (UnitId)iter.UnitType;

                if (!PreviousUnitTags.ContainsKey(iter.Tag))
                {
                    PreviousUnitTags[iter.Tag] = type;
                }
                else if (PreviousUnitTags[iter.Tag] != type)
                {
                    PreviousUnitTags[iter.Tag] = type;
                    returnList.Add(iter);
                }
            }

            if (returnList.Count == 0)
                return;

            var details = new HashSet<Unit>(returnList);
            Trigger(details);
        }
    }
}
