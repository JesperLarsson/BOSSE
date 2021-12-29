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
            List<Unit> matchedUnits = GeneralGameUtility.GetUnits(this.BuildingType, onlyCompleted: true, onlyVisible: true, includeWorkersTaskedToBuildUnit: true);

            if (matchedUnits.Count >= this.BuildingCount)
                return true;
            int missingBuildingCount = (int)this.BuildingCount - matchedUnits.Count;

            bool success = true;
            for (int i = 0; i < missingBuildingCount; i++)
            {
                if (CanAfford(this.BuildingType) 
                    && HaveTechRequirementsToBuild(this.BuildingType))
                {
                    BOSSE.ConstructionManagerRef.BuildAutoSelectPosition(this.BuildingType);
                    SubtractCosts(this.BuildingType);
                }
                else
                {
                    success = false;
                }
            }

            return success;
        }
    }

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
    }

    public class WaitForCompletion : BuildStep
    {
        public UnitId UnitType;
        public uint UnitCount;

        public WaitForCompletion(UnitId unitType, uint unitCount)
        {
            UnitType = unitType;
            UnitCount = unitCount;
        }

        public override bool ResolveStep()
        {
            List<Unit> matchedUnits = GeneralGameUtility.GetUnits(this.UnitType, onlyCompleted: true, onlyVisible: true);
            bool conditionOk = matchedUnits.Count >= this.UnitCount;

            return conditionOk;
        }
    }

    public class WaitForCondition : BuildStep
    {
        public Func<bool> Condition;

        public WaitForCondition(Func<bool> condition)
        {
            Condition = condition;
        }

        public override bool ResolveStep()
        {
            return this.Condition();
        }
    }

    public class CustomStep : BuildStep
    {
        public System.Action Function;

        public CustomStep(System.Action function)
        {
            Function = function;
        }

        public override bool ResolveStep()
        {
            this.Function();
            return true;
        }
    }

    /// <summary>
    /// A single build order, indicates which units and structures to build
    /// </summary>
    public abstract class BuildStep
    {
        /// <summary>
        /// Runs this step, return value indicates if it was successfully finished or not. If so, the step wil not be run again
        /// </summary>
        public abstract bool ResolveStep();
    }
}
