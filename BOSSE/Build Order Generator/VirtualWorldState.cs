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

    public class BuildingStatus
    {
        /// <summary>
        /// Type of the building
        /// </summary>
        public ActionId Type = null;

        /// <summary>
        /// Time remaining until the unit is finished constructing
        /// </summary>
        public uint TimeRemaining = 0;

        /// <summary>
        /// The type of unit that the building is currently constructing
        /// </summary>
        public ActionId IsConstructingType = null;

        public ActionId HasAddon = null;

        public BuildingStatus(ActionId action, ActionId addon)
        {
            this.Type = action;
            this.TimeRemaining = 0;
            this.IsConstructingType = null;
            this.HasAddon = addon;
        }

        public bool CanBuildEventually(ActionId action)
        {
            if (!this.Type.CanBuild(action))
            {
                return false;
            }

            // If it's an addon, make sure we don't have one already
            if (action.IsAddon())
            {
                if (this.HasAddon != null)
                    return false;
                if (this.TimeRemaining > 0 && this.IsConstructingType.IsAddon())
                    return false;
            }

            if (action.RequiresAddon() && this.HasAddon != action.RequiresAddonType() && this.IsConstructingType != action.RequiresAddonType())
            {
                return false;
            }

            return true;
        }

        public void FastForward(uint frameCount)
        {
            bool willComplete = this.TimeRemaining <= frameCount;

            if (this.TimeRemaining > 0 && willComplete)
            {
                if (this.IsConstructingType == null)
                    Log.SanityCheckFailedThrow("Building without a type set, bug - " + this);

                this.TimeRemaining = 0;

                // Building addon
                if (this.IsConstructingType.IsAddon())
                {
                    this.HasAddon = this.IsConstructingType;
                }

                this.IsConstructingType = null;
            }
            else if (this.TimeRemaining > 0)
            {
                this.TimeRemaining -= frameCount;
            }
        }

        public override string ToString()
        {
            return $"[Building {Type.GetName()} TimeRemaining={this.TimeRemaining}]";
        }
    }

    public class BuildingData
    {
        private List<BuildingStatus> Buildings = new List<BuildingStatus>();

        /// <summary>
        /// Calculates the number of frames until we can build the given action in any building
        /// </summary>
        public uint GetTimeUntilCanBuild(ActionId action)
        {
            uint min = uint.MaxValue;

            foreach (BuildingStatus iter in this.Buildings)
            {
                if (iter.CanBuildEventually(action))
                {
                    min = Math.Min(iter.TimeRemaining, min);
                }
            }

            if (min == uint.MaxValue)
                Log.SanityCheckFailedThrow("Unable to find minimum time for " + action);
            return min;
        }

        /// <summary>
        /// Fast forwards all buildings by the given amount of frames
        /// </summary>
        public void FastForwardBuildings(uint frameCount)
        {
            foreach (BuildingStatus iter in this.Buildings)
            {
                iter.FastForward(frameCount);
            }
        }

        public void AddBuilding(ActionId action, ActionId addon)
        {
            if (!action.IsBuilding())
                Log.SanityCheckFailedThrow("Trying to add non-building as a building");

            var obj = new BuildingStatus(action, addon);
            this.Buildings.Add(obj);
        }

        public bool CanBuildEventually(ActionId action)
        {
            foreach (BuildingStatus iter in this.Buildings)
            {
                if (iter.CanBuildEventually(action))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return $"[BuildingCollection BuildingCount={this.Buildings.Count}]";
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
            return this.Buildings;
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

        public void AddActionInProgress(ActionId action, uint completionFrame, bool queueAction = true)
        {
            throw new NotImplementedException();
        }

        public ActionsInProgress GetInProgressActions()
        {
            return this.Progress;
        }

        public uint getNextBuildingFinishTime()
        {
            // Returns whenever the next building is finished, ie when a worker becomes available
        }

        public uint GetNumberInProgress(ActionId action)
        {

        }

        public void SetBuildingFrame(uint frameCount)
        {
#warning LP_TODO: Refactor to be called fast forward?
            if (frameCount <= 0)
                Log.SanityCheckFailedThrow("Invalid frame count to fast forward " + frameCount);

            this.Buildings.FastForwardBuildings(frameCount);
        }

        /// <summary>
        /// Move a single worker to building from minerals
        /// </summary>
        public void SetBuildingWorker()
        {
            if (this.MineralWorkersCount <= 0)
                Log.SanityCheckFailedThrow("Tried to build without worker");

            this.MineralWorkersCount -= 1;
            this.BuildingWorkersCount += 1;
        }

        private void ReleaseBuildingWorker()
        {
            if (this.BuildingWorkersCount <= 0)
                Log.SanityCheckFailedThrow("Tried to move worker from build duty without any available");

            this.MineralWorkersCount += 1;
            this.BuildingWorkersCount -= 1;
        }

        public uint GetCurrentSupply()
        {

        }

        public uint GetMaxSupply()
        {

        }

        public ActionId FinishActionInProgress(ActionInProgress actionInProgress)
        {
            var actionId = actionInProgress.GetActionId();

            // Add to state
            AddCompletedAction(actionId);

            // Remove from progress list
            this.Progress.PopNextAction();

            // Worker is now available again
            if (actionId.GetRace() == Race.Terran)
            {
                if (actionId.IsBuilding() && (!actionId.IsAddon()))
                {
                    ReleaseBuildingWorker();
                }
            }

            return actionId;
        }

        public void AddCompletedAction(ActionId action, bool wasBuilt = true)
        {
            // Add to unit count
            if (!this.NumUnits.ContainsKey(action))
            {
                this.NumUnits[action] = 0;
            }
            if (wasBuilt)
            {
                this.NumUnits[action] += action.GetNumProduced();
            }
            else
            {
                this.NumUnits[action] += 1;
            }

            // Increase available supply
            this.MaxSupply += (int)action.GetSupplyProvided();

            // Perform some special cases depending on what was finished
            if (action.IsWorker())
            {
                this.MineralWorkersCount += 1;
            }

            if (action.IsRefinery())
            {
                if (this.MineralWorkersCount >= 3)
                {
                    this.MineralWorkersCount -= 3;
                    this.GasWorkersCount += 3;
                }
                else
                {
                    Log.SanityCheckFailed("WARNING: Unable to move workers from gas to minerals (after refinery was finished)");
                }
            }

            if (action.IsBuilding() && !action.IsSupplyProvider())
            {
                this.Buildings.AddBuilding(action, null);
            }
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

        /// <summary>
        /// Actions that have been taken
        /// </summary>
        private List<ActionPerformed> ActionsPerformed = new List<ActionPerformed>();

        /// <summary>
        /// Frame that the last action was taken on
        /// </summary>
        private uint LastActionFrame = 0;

        /// <summary>
        /// The current frame of the virtual state
        /// </summary>
        private uint CurrentFrame = 0;

        /// <summary>
        /// Number of minerals available
        /// </summary>
        private uint MineralCount = 0;

        /// <summary>
        /// Amount of gas available
        /// </summary>
        private uint GasCount = 0;

        public VirtualWorldState(ResponseObservation starcraftGameState)
        {
            // todo, hard but straight forward. Do this one last
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs the given action on our current world state
        /// </summary>
        public IEnumerable<ActionId> DoAction(ActionId action)
        {
            if (action.GetRace() != BotConstants.SpawnAsRace)
            {
                Log.SanityCheckFailedThrow("Action race is " + action.GetRace() + ", which isn't what we play");
            }

            ActionPerformed actionPerformed = new ActionPerformed(actionType: action);
            this.ActionsPerformed.Add(actionPerformed);

            if (IsLegal(action))
            {
                Log.SanityCheckFailed("Tried to perform an illegal action " + action + " during state " + this);
                return;
            }

            uint workerReadyTime = this.WhenWorkerReady(action);
            uint fastForwardTime = WhenCanWePerform(action);

            if (fastForwardTime >= 1000000)
            {
                Log.Warning("Fast forward time may have wrapped around: " + fastForwardTime);
            }

            // Fast forward time
            var actionsFinished = this.FastForward(fastForwardTime);
            this.ActionsPerformed.Last().ActionQueuedOnFrame = this.GetCurrentFrame();
            this.ActionsPerformed.Last().GasWhenQueued = this.GetGas();
            this.ActionsPerformed.Last().MineralsWhenQueued = this.GetMinerals();

            uint elapsedTime = this.GetCurrentFrame() - this.LastActionFrame;
            this.LastActionFrame = this.GetCurrentFrame();

            // Sanity checks
            if (!this.CanAffordMinerals(action))
            {
                Log.SanityCheckFailedThrow("Can not afford action (minerals): " + action);
            }
            if (!this.CanAffordGas(action))
            {
                Log.SanityCheckFailedThrow("Can not afford action (gas): " + action);
            }

            this.MineralCount -= action.MineralPrice();
            this.GasCount -= action.GasPrice();

            // Race-specific actions
            if (this.GetRace() == Race.Protoss)
            {
                this.Units.AddActionInProgress(action, this.GetCurrentFrame() + action.BuildTime());
            }
            else if (this.GetRace() == Race.Terran)
            {
                if (action.IsBuilding() && !action.IsAddon())
                {
                    if (this.GetNumMineralWorkers() <= 0)
                        Log.SanityCheckFailedThrow("Don't have any mineral workers to assign (terran)");

                    this.Units.SetBuildingWorker();
                }

                this.Units.AddActionInProgress(action, this.GetCurrentFrame() + action.BuildTime());
            }
            else
            {
                throw new BosseFatalException("Unsupported race " + this.GetRace());
            }

            return actionsFinished;
        }

        /// <summary>
        /// Fast-forwards the game state to the given frame. Returns which actions have finished
        /// </summary>
        public IEnumerable<ActionId> FastForward(uint toFrame)
        {
            uint previousFrame = this.GetCurrentFrame();
            this.Units.SetBuildingFrame(toFrame - previousFrame);

            uint lastActionFinished = this.GetCurrentFrame();
            uint totalTime = 0;
            uint moreGas = 0;
            uint moreMinerals = 0;

            List<ActionId> actionsFinished = new List<ActionId>();

            // Fast forward until the next action is complete
            while (true)
            {
                ActionsInProgress inProgress = this.Units.GetInProgressActions();
                ActionInProgress nextAction = inProgress.GetNextAction();
                if (nextAction == null)
                    break; // out of actions
                if (nextAction.GetFinishTime() <= toFrame)
                    break; // done

                // Fast forward until the action finishes
                uint timeElapsed = nextAction.GetFinishTime() - lastActionFinished;
                totalTime += timeElapsed;
                moreMinerals += timeElapsed * GetMineralsPerFrame();
                moreGas += timeElapsed * GetGasPerFrame();

                lastActionFinished = nextAction.GetFinishTime();
                ActionId actionFinished = this.Units.FinishActionInProgress(nextAction);
                actionsFinished.Add(actionFinished);
            }

            // Fast forward until the requested frame (queued actions will probably not align exactly)
            uint elapsed = toFrame - lastActionFinished;
            moreMinerals += elapsed * GetMineralsPerFrame();
            moreGas += elapsed * GetGasPerFrame();
            totalTime += elapsed;

            // Add to game state
            this.MineralCount += moreMinerals;
            this.GasCount += moreGas;
            this.CurrentFrame = toFrame;

            return actionsFinished;
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

        public bool CanAfford(ActionId action)
        {
            return this.CanAffordGas(action) && this.CanAffordMinerals(action);
        }

        public bool CanAffordMinerals(ActionId action)
        {
            return this.MineralCount >= action.MineralPrice();
        }

        public bool CanAffordGas(ActionId action)
        {

            return this.GasCount >= action.GasPrice();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetMinerals()
        {
            return this.MineralCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetGas()
        {
            return this.GasCount;
        }

        public uint GetNumMineralWorkers()
        {
            return this.Units.GetNumberMineralWorkers();
        }

        public uint GetNumGasWorkers()
        {
            return this.Units.GetNumberGasWorkers();
        }

        public uint GetNumBuildingWorkers()
        {
            return this.Units.GetNumberBuildingWorkers();
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

        private uint WhenGasReady(ActionId action)
        {
            if (this.GasCount >= action.GasPrice())
            {
                return this.GetCurrentFrame();
            }

            uint mineralWorkerCount = this.Units.GetNumberMineralWorkers();
            uint gasWorkerCount = this.Units.GetNumberGasWorkers();
            uint lastActionFinishFrame = this.GetCurrentFrame();
            uint addedTime = 0;
            uint addedGas = 0;
            uint difference = action.GasPrice() - this.GasCount;

            // loop through each action in progress, adding the minerals we would gather from each interval
            foreach (ActionInProgress actionPerformed in this.Units.GetInProgressActions().GetAllInProgressDesc())
            {
                uint elapsed = actionPerformed.GetFinishTime() - lastActionFinishFrame;
                uint tempAdd = (uint)(elapsed * BuildOrderUtility.GetPerFrameGas(gasWorkerCount));

                // if this amount isn't enough, update the amount added for this interval
                if (addedGas + tempAdd < difference)
                {
                    addedGas += tempAdd;
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

            // if we still haven't added enough gas, add more time
            if (addedGas < difference)
            {
                if (gasWorkerCount <= 0)
                {
                    // Could possible happen when losing the game, so we don't throw
                    Log.SanityCheckFailed("Resource prediction error, shouldn't have 0 gas workers");
                }

                uint finalTimeToAdd = 1000000;
                if (gasWorkerCount > 0)
                {
                    finalTimeToAdd = (difference - addedGas) / BuildOrderUtility.GetPerFrameGas(gasWorkerCount);
                }

                addedGas += finalTimeToAdd * BuildOrderUtility.GetPerFrameGas(gasWorkerCount);
                addedTime += finalTimeToAdd;

                // Compensate for integer division issue
                if (addedGas < difference)
                {
                    addedTime += 1;
                    addedGas += BuildOrderUtility.GetPerFrameGas(gasWorkerCount);
                }
            }

            if (addedGas < difference)
            {
                Log.SanityCheckFailedThrow("Gas prediction issue " + addedGas + " vs " + difference);
            }

            return this.GetCurrentFrame() + addedTime;
        }

        private uint WhenSupplyReady(ActionId action)
        {
            int supplyNeeded = (int)action.GetSupplyRequired();
            supplyNeeded += (int)this.Units.GetCurrentSupply();
            supplyNeeded -= (int)this.Units.GetMaxSupply();

            if (supplyNeeded <= 0)
            {
                return this.GetCurrentFrame();
            }

            uint whenSupplyReady = this.GetCurrentFrame();
            if (supplyNeeded > 0)
            {
                uint min = 99999;

                // Check when the next food unit completes
                foreach (ActionInProgress iter in this.Units.GetInProgressActions().GetAllInProgressDesc())
                {
                    if (iter.GetActionId().GetSupplyProvided() > supplyNeeded)
                    {
                        uint finishTime = iter.GetFinishTime();
                        min = Math.Min(finishTime, min);
                    }

                    whenSupplyReady = min;
                }

            }

            return whenSupplyReady;
        }

        private uint WhenWorkerReady(ActionId action)
        {
            if (!action.WhatBuildsThis().IsWorker())
            {
                return this.GetCurrentFrame();
            }

            // protoss doesn't tie up a worker to build, so they can build whenever a mineral worker is free
            if (GetRace() == Race.Protoss && this.GetNumMineralWorkers() > 0)
            {
                return this.GetCurrentFrame();
            }

            // Some workers may be reserved to be put onto gas
            uint refineriesInProgress = this.Units.GetNumberInProgress(ActionId.Get(BotConstants.RefineryUnit));
            if (this.GetNumMineralWorkers() > (3 * refineriesInProgress))
            {
                return this.GetCurrentFrame();
            }

            // at this point we need to wait for the next worker to become free since existing workers
            // are either all used, or they are reserved to be put into refineries
            // so we must have either a worker in progress, or a building in progress
            ActionId workerAction = ActionId.Get(BotConstants.WorkerUnit);
            if (this.Units.GetNumberInProgress(workerAction) <= 0 && GetNumBuildingWorkers() <= 0)
            {
                Log.SanityCheckFailed("No worker will ever become available to build " + action);
                return this.GetCurrentFrame() + 1000;
            }

            // Check workers in progress
            uint whenWorkerInProgressFinished = uint.MaxValue;
            if (this.Units.GetNumberInProgress(workerAction) > 0)
            {
                whenWorkerInProgressFinished = this.Units.GetFinishTime(workerAction);
            }

            uint whenBuildingWorkerFree = uint.MaxValue;
            if (GetNumBuildingWorkers() > 0)
            {
                whenBuildingWorkerFree = this.Units.getNextBuildingFinishTime();
            }

            uint min = Math.Min(whenWorkerInProgressFinished, whenBuildingWorkerFree);
            return min;
        }

        private Race GetRace()
        {
            return BotConstants.SpawnAsRace;
        }

        private uint GetMineralsPerFrame()
        {
            return BuildOrderUtility.GetPerFrameMinerals(this.Units.GetNumberMineralWorkers());
        }

        private uint GetGasPerFrame()
        {
            return BuildOrderUtility.GetPerFrameGas(this.Units.GetNumberGasWorkers());
        }

        public override string ToString()
        {
            return $"[GameState Frame={this.CurrentFrame} Min={this.MineralCount} Gas={this.GasCount} Actions={this.ActionsPerformed.Count}]";
        }
    }
}
