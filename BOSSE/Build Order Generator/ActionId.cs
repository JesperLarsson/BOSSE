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
namespace BOSSE.BuildOrderGenerator
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Threading;
    using System.Reflection;
    using System.Linq;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    using SC2APIProtocol;
    using MoreLinq;
    using Google.Protobuf.Collections;

    using static GeneralGameUtility;
    using static UnitConstants;
    using static UpgradeConstants;
    using static AbilityConstants;

    /// <summary>
    /// A single build order item, is either a unit (including structures) or an upgrade
    /// Uses singleton instances which are used for type checking, retrieve through the static Get-method
    /// </summary>
    public class ActionId : IComparable<ActionId>
    {
        private readonly UnitId UnitType = 0;
        private readonly UnitTypeData UnitData = null;
        private readonly UpgradeId UpgradeType = 0;

        private static Dictionary<UnitId, ActionId> ExistingUnitTypes = new Dictionary<UnitId, ActionId>();
        private static Dictionary<UpgradeId, ActionId> ExistingUpgradeTypes = new Dictionary<UpgradeId, ActionId>();

        private ActionId(UnitId unitType)
        {
            this.UnitType = unitType;
            this.UnitData = GetUnitInfo(unitType);
        }

        private ActionId(UpgradeId upgradeType)
        {
            this.UpgradeType = upgradeType;
        }

        public static void InitAll()
        {
            foreach (UnitId iter in Enum.GetValues(typeof(UnitId)))
            {
                ActionId newObj = Get(iter);
                Log.Bulk("Initialized build order system with unit action " + newObj.GetName());
            }

            foreach (UnitId iter in Enum.GetValues(typeof(UpgradeId)))
            {
                ActionId newObj = Get(iter);
                Log.Bulk("Initialized build order system with upgrade action " + newObj.GetName());
            }
        }

        public static HashSet<ActionId> GetAllActions()
        {
            HashSet<ActionId> newSet = new HashSet<ActionId>();

            foreach (ActionId iter in ExistingUnitTypes.Values)
            {
                newSet.Add(iter);
            }
            foreach (ActionId iter in ExistingUpgradeTypes.Values)
            {
                newSet.Add(iter);
            }

            return newSet;
        }

        public static ActionId Get(UnitId type)
        {
            if (!ExistingUnitTypes.ContainsKey(type))
            {
                ExistingUnitTypes[type] = new ActionId(type);
            }
            return ExistingUnitTypes[type];
        }

        public static ActionId Get(UpgradeId type)
        {
            if (!ExistingUpgradeTypes.ContainsKey(type))
            {
                ExistingUpgradeTypes[type] = new ActionId(type);
            }
            return ExistingUpgradeTypes[type];
        }

        public PrerequisiteSet GetPrerequisites()
        {
            // todo, does not need to be recursive
        }

        public bool WhatBuildsIsBuilding()
        {
            // todo, look up code
        }

        public uint GetSupplyRequired()
        {
            if (!this.IsUnit())
                return 0;

            int foodDiff = (int)(this.UnitData.FoodRequired - this.UnitData.FoodProvided);
            if (foodDiff < 0)
                return 0;
            return (uint)foodDiff;
        }

        public uint GasPrice()
        {
            if (this.IsUnit())
            {
                return UnitData.VespeneCost;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public uint MineralPrice()
        {
            if (this.IsUnit())
            {
                return UnitData.MineralCost;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public string GetName()
        {
            if (this.IsUnit())
            {
                return UnitData.Name;
            }
            else if (this.IsUpgrade())
            {
                return UpgradeType.ToString();
            }
            else
            {
                return "N/A";
            }
        }

        public uint BuildTime()
        {
            return (uint)Math.Ceiling(UnitData.BuildTime);
        }

        public UnitId GetUnitId()
        {
            if (!IsUnit())
            {
                Log.SanityCheckFailed("Called without this item being a unit");
                return 0;
            }

            return this.UnitType;
        }

        public UpgradeId GetUpgradeId()
        {
            if (!IsUpgrade())
            {
                Log.SanityCheckFailed("Called without this item being an upgrade");
                return 0;
            }

            return this.UpgradeType;
        }

        public bool IsUnit()
        {
            return this.UnitType != 0;
        }

        public bool IsUpgrade()
        {
            return this.UpgradeType != 0;
        }

        public int CompareTo(ActionId other)
        {
            if (this.IsUnit())
            {
                return this.UnitType.CompareTo(other.UnitType);
            }
            else if (this.IsUpgrade())
            {
                return this.UpgradeType.CompareTo(other.UpgradeType);
            }
            else
            {
                throw new BosseFatalException("Unexpected ItemId, is neither unit not upgrade");
            }
        }
    }
}
