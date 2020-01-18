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

    public class ActionsInProgress
    {
        public uint WhenActionsFinished(PrerequisiteSet set)
        {

        }
    }

    public class BuildingData
    {
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

        public uint GetFinishTime(PrerequisiteSet needed)
        {
            return this.Progress.WhenActionsFinished(needed);
        }
    }







    public class VirtualWorldState
    {
        private UnitData Units = new UnitData();

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

        public uint GetCurrentFrame()
        {
            // todo easy
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

        }

        private PrerequisiteSet GetPrerequistesInProgress(ActionId action)
        {

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
