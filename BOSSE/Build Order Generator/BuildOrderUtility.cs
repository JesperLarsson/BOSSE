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
    /// Various utility functions used internally by our build order generator
    /// </summary>
    public static class BuildOrderUtility
    {
        /// <summary>
        /// Estimated amount of minerals mined per worker, per logical frame
        /// Multiplied by ResourceScale to avoid floating point rounding issues
        /// </summary>
        public const uint MineralsPerWorkerPerFrameEstimate = 30;
        public const uint ResourceScale = 1000;

        public const float FramesPerSecond = 22.4f;

        public static ActionId GetWorkerActionId()
        {
            return ActionId.Get(BotConstants.WorkerUnit);
        }

        public static ActionId GetRefinaryActionId()
        {
            return ActionId.Get(BotConstants.RefineryUnit);
        }

        /// <summary>
        /// Returns the estimated income for X workers, per logical frame
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetPerFrameMinerals(uint workerCount)
        {
            return MineralsPerWorkerPerFrameEstimate * workerCount;
        }

        /// <summary>
        /// Calculates the lower bound for the given goal and world state, in number of logical frames
        /// </summary>
        public static uint GetLowerBound(VirtualWorldState worldState, BuildOrderGoal goal)
        {
            PrerequisiteSet wanted = new PrerequisiteSet();

            foreach (ActionId actionIter in ActionId.GetAllActions())
            {
                uint completedCount = worldState.GetNumberTotal(actionIter);

                if (goal.GetGoal(actionIter) > completedCount)
                {
                    wanted.AddUnique(actionIter);
                }
            }

            PrerequisiteSet added = new PrerequisiteSet();
            uint lowerBound = CalculatePrerequisitesLowerBound(worldState, wanted, 0, 0);
            return lowerBound;
        }

        private static uint CalculatePrerequisitesLowerBound(VirtualWorldState worldState, PrerequisiteSet needed, uint timeSoFar, uint depth)
        {
            uint frameMax = 0;

            foreach (ActionId iter in needed.GetAll())
            {
                uint thisActionTime = 0;

                if (worldState.GetNumberCompleted(iter) > 0)
                {
                    // Done
                    thisActionTime = timeSoFar;
                }
                else if (worldState.GetNumberInProgress(iter) > 0)
                {
                    // In progress
                    thisActionTime = timeSoFar + worldState.GetFinishTime(iter) - worldState.GetCurrentFrame();
                }
                else
                {
                    // Find recursively by prerequisites
                    thisActionTime = CalculatePrerequisitesLowerBound(worldState, iter.GetPrerequisites(), timeSoFar + iter.BuildTime(), depth + 1);
                }

                if (thisActionTime > frameMax)
                {
                    frameMax = thisActionTime;
                }
            }

            return frameMax;
        }

        private static void CalculatePrerequisitesRequiredToBuild(VirtualWorldState worldState, PrerequisiteSet needed, PrerequisiteSet added)
        {
            PrerequisiteSet allNeeded = needed.Clone();

            // Special case - If we need gas, add a gas extractor
            ActionId refinaryAction = GetRefinaryActionId();
            if ((!needed.Contains(refinaryAction)) && (!added.Contains(refinaryAction)) && worldState.GetNumberCompleted(refinaryAction) == 0)
            {
                foreach (ActionId needIter in needed.GetAll())
                {
                    if (needIter.GasPrice() > 0)
                    {
                        allNeeded.Add(refinaryAction);
                        break;
                    }
                }
            }

            // Resolve dependencies
            foreach (ActionId needIter in allNeeded.GetAll())
            {
                if (added.Contains(needIter) || worldState.GetNumberCompleted(needIter) > 0)
                {
                    continue;
                }
                if (worldState.GetNumberCompleted(needIter) > 0)
                {
                    continue;
                }

                // Needs not met, find recursive dependencies
                added.Add(needIter);
                CalculatePrerequisitesRequiredToBuild(worldState, needIter.GetPrerequisites(), added);
            }
        }
    }
}
