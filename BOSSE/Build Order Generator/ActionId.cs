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


    public class ActionPerformed
    {
        public ActionId ActionType;
        public uint ActionQueuedOnFrame = 0;
        public uint MineralsWhenQueued = 0;
        public uint GasWhenQueued = 0;
        
        public ActionPerformed(ActionId actionType, uint actionQueuedOnFrame = 0, uint mineralsWhenQueued = 0, uint gasWhenQueued = 0)
        {
            ActionType = actionType;
            ActionQueuedOnFrame = actionQueuedOnFrame;
            MineralsWhenQueued = mineralsWhenQueued;
            GasWhenQueued = gasWhenQueued;
        }
    }




    public class ActionInProgress : IComparable<ActionInProgress>
    {
        private readonly ActionId Action = null;
        private readonly uint Time = 0;

        public ActionInProgress(ActionId action, uint time)
        {
            this.Action = action;
            this.Time = time;
        }

        public uint GetFinishTime()
        {

        }

        public ActionId GetActionId()
        {
            return this.Action;
        }

        public uint GetTime()
        {
            return this.Time;
        }

        public int CompareTo(ActionInProgress other)
        {
            return this.Time.CompareTo(other.GetTime());
        }

        public override string ToString()
        {
            return $"[ActionInProgress {this.Action} - {this.Time}]";
        }
    }

    public class ActionsInProgress
    {
        //public void AddAction(ActionId action, uint timeFrameCount)
        //{
        //    // IMPORTANT, add to list in a sorted way
        //    throw new NotImplementedException();
        //}

        public bool IsEmpty()
        {

        }

        public void PopNextAction()
        {
            // Just remove the "next" action, ie the last in the list
            throw new NotImplementedException();
        }

        public ActionInProgress GetNextAction()
        {
            if (IsEmpty())
                return null;

            // todo, take the LAST one in the list, double check
        }

        public IEnumerable<ActionInProgress> GetAllInProgressDesc()
        {
            // IMPORTANT: Sort list in descending order
        }

        public uint WhenActionsFinished(PrerequisiteSet set)
        {

        }
    }






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
            foreach (UnitId iter in BotConstants.FactionUnitsBuildable)
            {
                ActionId newObj = Get(iter);
                Log.Bulk("Initialized build order system with unit action " + newObj.GetName());
            }

#warning Build order TODO: Support upgrades
            //foreach (UnitId iter in Enum.GetValues(typeof(UpgradeId)))
            //{
            //    ActionId newObj = Get(iter);
            //    Log.Bulk("Initialized build order system with upgrade action " + newObj.GetName());
            //}
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

        private ActionId GetWhatBuilds()
        {
#warning Build order LP TODO: Optimization, cache
            if (this.IsUnit())
            {
                UnitId buildBy = GeneralGameUtility.WhichUnitBuildsUnit(this.GetUnitId());
                ActionId temp = Get(buildBy);
                return temp;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public PrerequisiteSet GetPrerequisites()
        {
#warning Build order LP TODO: Optimization, cache
            if (this.IsUnit())
            {
                PrerequisiteSet set = new PrerequisiteSet();
                foreach (UnitId iter in GeneralGameUtility.GetPrerequisites(this.UnitType))
                {
                    set.AddUnique(Get(iter));
                }
                return set;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public ActionId WhatBuildsThis()
        {
            ActionId builtBy = GetWhatBuilds();
            return builtBy;
        }

        public Race GetRace()
        {
            if (this.IsUnit())
            {
                return UnitData.Race;
            }
            else
            {
                // TODO: Look up upgrade race data
                throw new NotImplementedException();
            }
        }

        public bool IsAddon()
        {
            if (!IsUnit())
                return false;

            if (UnitConstants.TechlabVariations.Contains(UnitType))
                return true;
            if (UnitConstants.ReactorVariations.Contains(UnitType))
                return true;

            return false;
        }

        public bool IsSupplyProvider()
        {
           
        }

        public bool IsBuilding()
        {
            if (!IsUnit())
                return false;

            bool isBuilding = GeneralGameUtility.IsBuilding(this.UnitType);
            return isBuilding;
        }

        public bool WhatBuildsIsBuilding()
        {
#warning Build order LP TODO: Optimization, cache
            ActionId builtBy = GetWhatBuilds();
            if (!builtBy.IsUnit())
            {
                Log.SanityCheckFailed("Unexpected build type");
                return false;
            }

            return GeneralGameUtility.IsBuilding(builtBy.GetUnitId());
        }

        public uint GetSupplyProvided()
        {
            if (!this.IsUnit())
                return 0;

            return (uint)this.UnitData.FoodProvided;
        }

        public uint GetSupplyRequired()
        {
            if (!this.IsUnit())
                return 0;

            return (uint)this.UnitData.FoodRequired;
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

        public uint GetNumProduced()
        {
            // todo, probably 1
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

        public bool IsWorker()
        {
            if (!IsUnit())
                return false;

            bool isWorker = this.UnitType == BotConstants.WorkerUnit;
            return isWorker;
        }

        public bool IsUpgrade()
        {
            return this.UpgradeType != 0;
        }

        public bool IsRefinery()
        {
            if (!IsUnit())
                return false;

            bool isRef = this.UnitType == BotConstants.RefineryUnit;
            return isRef;
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

        public override string ToString()
        {
            return "[Action " + GetName() + "]";
        }
    }
}
