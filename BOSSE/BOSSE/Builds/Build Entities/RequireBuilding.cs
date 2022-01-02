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

    public class RequireBuilding : BuildStep
    {
        public UnitId BuildingType;
        public uint BuildingCount;

        public RequireBuilding(UnitId buildingType, uint buildingCount)
        {
            BuildingType = buildingType;
            BuildingCount = buildingCount;
        }

        public override bool ResolveStep()
        {
            List<Unit> matchedUnits = GeneralGameUtility.GetUnits(this.BuildingType, onlyCompleted: true, onlyVisible: true, includeWorkersTaskedToBuildRequestedUnit: true);

            if (matchedUnits.Count >= this.BuildingCount)
                return true;
            int missingBuildingCount = (int)this.BuildingCount - matchedUnits.Count;

            bool success = true;
            for (int i = 0; i < missingBuildingCount; i++)
            {
                if (CanAfford(this.BuildingType) == false)
                    return false;
                if (HaveTechRequirementsToBuild(this.BuildingType) == false)
                    return false;

                bool buildOk = BOSSE.ConstructionManagerRef.BuildAutoSelectPosition(this.BuildingType);
                if (buildOk == false)
                    return false;

                SubtractCosts(this.BuildingType);
            }

            return success;
        }
    }
}
