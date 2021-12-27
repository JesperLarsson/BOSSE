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
            List<Unit> currentStructures = GeneralGameUtility.GetUnits(UnitConstants.Structures, onlyCompleted: true);

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
