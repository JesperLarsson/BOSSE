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
    }

    public class ActionsInProgress
    {
        public IEnumerable<ActionInProgress> GetAllInProgressDesc()
        {
            // IMPORTANT: Sort list in descending order
        }

        public uint WhenActionsFinished(PrerequisiteSet set)
        {

        }
    }

    public class BuildingData
    {
        public uint GetTimeUntilCanBuild(ActionId action)
        {

        }

        public bool CanBuildEventually(ActionId action)
        {

        }
    }




    public class UnitData
    {
        /// <summary>
        /// Number of workers mining minerals
        /// </summary>
        private uint MineralWorkersCount = 0;

        /// <summary>
        /// Number of workers collecting gas
        /// </summary>
        private uint GasWorkersCount = 0;

        /// <summary>
        /// Number of workers constructing buildings
        /// </summary>
        private uint BuildingWorkersCount = 0;

        /// <summary>
        /// Maximum allowed supply
        /// </summary>
        private int MaxSupply = 0;

        /// <summary>
        /// Currently allocated supply
        /// </summary>
        private int CurrentSupply = 0;

        /// <summary>
        /// Number of each unit completed
        /// </summary>
        private Dictionary<ActionId, uint> NumUnits = new Dictionary<ActionId, uint>();

        /// <summary>
        /// Actions in progress
        /// </summary>
        private ActionsInProgress Progress = new ActionsInProgress();

        /// <summary>
        /// Available buildings
        /// </summary>
        private BuildingData Buildings = new BuildingData();

        public BuildingData GetBuildingData()
        {
            return Buildings;
        }

        public uint GetNumberMineralWorkers()
        {
            return this.MineralWorkersCount;
        }

        public uint GetNumberGasWorkers()
        {
            return this.GasWorkersCount;
        }

        public uint GetNumberBuildingWorkers()
        {
            return this.BuildingWorkersCount;
        }

        public ActionsInProgress GetInProgressActions()
        {
            return this.Progress;
        }

        public uint GetNumberInProgress(ActionId action)
        {

        }

        public uint GetFinishTime(ActionId action)
        {

        }

        public PrerequisiteSet GetPrerequistesInProgress(ActionId action)
        {

        }

        public uint GetFinishTime(PrerequisiteSet needed)
        {
            return this.Progress.WhenActionsFinished(needed);
        }
    }







    public class VirtualWorldState
    {
        private UnitData Units = new UnitData();

        private uint MineralCount = 0;
        private uint GasCount = 0;

        private uint CurrentFrame = 0;

        public VirtualWorldState(ResponseObservation starcraftGameState)
        {
            // todo, hard but straight forward
            throw new NotImplementedException();
        }

        public void DoAction(ActionId action)
        {
            // todo, important!, takes the given action right now
            throw new NotImplementedException();
        }

        public uint GetFinishTime(ActionId action)
        {
            // todo
        }

        public uint GetLastActionFinishTime()
        {
            // todo
        }

        /// <summary>
        /// Determines if the given action is legal in this context or not
        /// </summary>
        public bool IsLegal(ActionId action)
        {
            // todo
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetCurrentFrame()
        {
            return this.CurrentFrame;
        }

        /// <summary>
        /// Returns number of the currently available units/upgrades
        /// </summary>
        public uint GetNumberTotal(ActionId target)
        {
            // todo easy, also TODO check all usages if they should use another get num function
        }

        public uint GetNumberInProgress(ActionId target)
        {

        }

        public uint GetNumberCompleted(ActionId target)
        {

        }

        /// <summary>
        /// Returns when the given action will be possible, given that we perform no additional actions
        /// </summary>
        public uint WhenCanWePerform(ActionId action)
        {
            uint prereqTime = WhenPrerequisitesReady(action);
            uint mineralTime = WhenMineralsReady(action);
            uint gasTime = WhenGasReady(action);
            uint supplyTime = WhenSupplyReady(action);
            uint workerTime = WhenWorkerReady(action);

            uint max = this.GetCurrentFrame();
            max = Math.Max(max, prereqTime);
            max = Math.Max(max, mineralTime);
            max = Math.Max(max, gasTime);
            max = Math.Max(max, supplyTime);
            max = Math.Max(max, workerTime);

            return max;
        }

        private uint WhenBuildingPrereqReady(ActionId action)
        {
            ActionId builder = action.WhatBuildsThis();
            if (!builder.IsBuilding())
            {
                Log.SanityCheckFailedThrow("Thing that builds action is not a building");
            }

            bool buildingIsConstructed = this.Units.GetBuildingData().CanBuildEventually(action);
            bool buildingInProgress = this.Units.GetNumberInProgress(builder) > 0;

            if (buildingIsConstructed == false && buildingInProgress == false)
            {
                Log.SanityCheckFailedThrow("We can never build action " + action);
            }

            uint constructedBuildingFreeTime = uint.MaxValue;
            uint buildingInProgressFinishTime = uint.MaxValue;

            if (buildingIsConstructed)
            {
                constructedBuildingFreeTime = this.GetCurrentFrame() + this.Units.GetBuildingData().GetTimeUntilCanBuild(action);
            }
            if (buildingInProgress)
            {
                buildingInProgressFinishTime = this.Units.GetFinishTime(builder);
            }

            uint buildingAvailableTime = Math.Min(constructedBuildingFreeTime, buildingInProgressFinishTime);
            PrerequisiteSet prereqInProgress = this.Units.GetPrerequistesInProgress(action);

            // Builder has already been calculated
            prereqInProgress.Remove(builder);

            if (!prereqInProgress.IsEmpty())
            {
                // Calculate remaining prereqs
                uint c = this.Units.GetFinishTime(prereqInProgress);
                if (c > buildingAvailableTime)
                {
                    buildingAvailableTime = c;
                }
            }

            return buildingAvailableTime;
        }

        private PrerequisiteSet GetPrerequistesInProgress(ActionId action)
        {
            PrerequisiteSet inProgress = new PrerequisiteSet();

            foreach (ActionId iter in action.GetPrerequisites().GetAll())
            {
                if (GetNumberInProgress(iter) > 0 && GetNumberCompleted(iter) == 0)
                {
                    inProgress.Add(iter);
                }
            }

            return inProgress;
        }

        private uint WhenPrerequisitesReady(ActionId action)
        {
            uint readyTime = this.GetCurrentFrame();

            // If a building builds this action, get whenever the building will become available
            if (action.WhatBuildsIsBuilding())
            {
                readyTime = WhenBuildingPrereqReady(action);
            }
            else
            {
                // Something else builds it. Use the in-progress time if available
                PrerequisiteSet reqInProgress = GetPrerequistesInProgress(action);
                if (!reqInProgress.IsEmpty())
                {
                    readyTime = this.Units.GetFinishTime(reqInProgress);
                }
            }

            return readyTime;
        }

        private uint WhenMineralsReady(ActionId action)
        {
            if (this.MineralCount >= action.MineralPrice())
            {
                return this.GetCurrentFrame();
            }

            uint mineralWorkerCount = this.Units.GetNumberMineralWorkers();
            uint gasWorkerCount = this.Units.GetNumberGasWorkers();
            uint lastActionFinishFrame = this.GetCurrentFrame();
            uint addedTime = 0;
            uint addedMinerals = 0;
            uint difference = action.MineralPrice() - this.MineralCount;

            // loop through each action in progress, adding the minerals we would gather from each interval
            foreach (ActionInProgress actionPerformed in this.Units.GetInProgressActions().GetAllInProgressDesc())
            {
                uint elapsed = actionPerformed.GetFinishTime() - lastActionFinishFrame;
                uint tempAdd = (uint)(elapsed * BuildOrderUtility.GetPerFrameMinerals(mineralWorkerCount));

                // if this amount isn't enough, update the amount added for this interval
                if (addedMinerals + tempAdd < difference)
                {
                    addedMinerals += tempAdd;
                    addedTime += elapsed;
                }
                else
                {
                    // update at the end
                    break;
                }

                ActionId performedId = actionPerformed.GetActionId();

                // Finishing non-addon buildings as terran gives us a worker back
                if (performedId.IsBuilding() && (!performedId.IsAddon()) && (GetRace() == Race.Terran))
                {
                    mineralWorkerCount += 1;
                }

                if (performedId.IsWorker())
                {
                    mineralWorkerCount += 1;
                }
                else if (performedId.IsRefinery())
                {
                    if (mineralWorkerCount < 3)
                    {
                        Log.SanityCheckFailedThrow("Not enough mineral workers to transfer to gas");
                    }

                    mineralWorkerCount -= 3;
                    gasWorkerCount += 3;
                }

                lastActionFinishFrame = actionPerformed.GetFinishTime();
            }

            // if we still haven't added enough minerals, add more time
            if (addedMinerals < difference)
            {
                if (mineralWorkerCount <= 0)
                {
                    // Could possible happen when losing the game, so we don't throw
                    Log.SanityCheckFailed("Resource prediction error, shouldn't have 0 mineral workers");
                }

                uint finalTimeToAdd = 1000000;
                if (mineralWorkerCount > 0)
                {
                    finalTimeToAdd = (difference - addedMinerals) / BuildOrderUtility.GetPerFrameMinerals(mineralWorkerCount);
                }

                addedMinerals += finalTimeToAdd * BuildOrderUtility.GetPerFrameMinerals(mineralWorkerCount);
                addedTime += finalTimeToAdd;

                // Compensate for integer division issue
                if (addedMinerals < difference)
                {
                    addedTime += 1;
                    addedMinerals += BuildOrderUtility.GetPerFrameMinerals(mineralWorkerCount);
                }
            }

            if (addedMinerals < difference)
            {
                Log.SanityCheckFailedThrow("Mineral prediction issue " + addedMinerals + " vs " + difference);
            }

            return this.GetCurrentFrame() + addedTime;
        }

        private Race GetRace()
        {
            return BotConstants.SpawnAsRace;
        }

        private uint WhenGasReady(ActionId action)
        {
        }

        private uint WhenSupplyReady(ActionId action)
        {
        }

        private uint WhenWorkerReady(ActionId action)
        {
        }
    }
}
