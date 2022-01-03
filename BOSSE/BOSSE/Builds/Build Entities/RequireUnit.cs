/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2022 Jesper Larsson

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
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static global::BOSSE.UnitConstants;

    public class RequireUnit : BuildStep
    {
        public UnitId UnitType;
        public uint UnitCount;
        public bool AllowChronoBoost = false;

        public RequireUnit(UnitId unitType, uint unitCount)
        {
            UnitType = unitType;
            UnitCount = unitCount;
        }

        public override bool ResolveStep()
        {
            List<Unit> matchedUnits = GeneralGameUtility.GetUnits(this.UnitType, onlyCompleted: true, onlyVisible: true, includeBuildingOrdersBuildingUnit: true);

            if (matchedUnits.Count >= this.UnitCount)
                return true;

            int missingCount = (int)this.UnitCount - matchedUnits.Count;
            for (int i = 0; i < missingCount; i++)
            {
                bool ok = GeneralGameUtility.TryBuildUnit(this.UnitType, true, this.AllowChronoBoost);
                if (ok == false)
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            return $"Train {this.UnitType}";
        }
    }
}
