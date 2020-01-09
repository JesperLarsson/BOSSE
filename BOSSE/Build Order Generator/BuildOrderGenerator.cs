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

    using SC2APIProtocol;
    using MoreLinq;
    using Google.Protobuf.Collections;

    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

#warning TODO: This should also take a list of optional weights for each unit, ie we need more air right now etc
#warning TODO: Also add a bias option which changes the evaluation algorithm, ex one only counts army units (agressive) while counts everything (balance focus) while another only counts workers+CC (economy focus)
    // Based on ideas from this paper: http://www.cs.mun.ca/~dchurchill/pdf/cog19_buildorder.pdf
    public static class BuiltOrderConfig
    {
        public const float WorkerMineralsPerFrameEstimate = 40.0f / 60.0f / FramesPerSecond;
        public const float FramesPerSecond = 22.4f;
        public const ulong StandardFrameDelta = 22;
    }

    //public class BuildOrderItem
    //{
    //    public UnitId UnitType = 0;
    //    public UpgradeConstants.UpgradeId UpgradeType = 0;

    //    public BuildOrderItem(UnitId unitType)
    //    {
    //        this.UnitType = unitType;
    //    }
    //    public BuildOrderItem(UpgradeConstants.UpgradeId upgradeType)
    //    {
    //        this.UpgradeType = upgradeType;
    //    }
    //}












    public abstract class Precondition
    {
        public abstract bool IsFulfilled(VirtualWorldState worldState);
    }

    public class HaveUnitCondition : Precondition
    {
        private UnitId RequiredUnit = 0;

        public HaveUnitCondition(UnitId requiredUnit)
        {
            RequiredUnit = requiredUnit;
        }

        public override bool IsFulfilled(VirtualWorldState worldState)
        {

        }
    }

    public class MineralCondition : Precondition
    {
        private int MineralsRequired = 0;

        public MineralCondition(int mineralsRequired)
        {
            MineralsRequired = mineralsRequired;
        }

        public override bool IsFulfilled(VirtualWorldState worldState)
        {

        }
    }








    public abstract class VirtualAction
    {
        /// <summary>
        /// Duration of the action, in number of logical frames
        /// </summary>
        public int Duration = 0;

        /// <summary>
        /// Required conditions must have been completed before this action can be taken
        /// </summary>
        public HashSet<Precondition> RequiredPrecondition = new HashSet<Precondition>();

        /// <summary>
        /// Borrowed preconditions means that this action must exclusively occupy a resource while the action is performed
        /// </summary>
        public HashSet<Precondition> BorrowedPrecondition = new HashSet<Precondition>();

        /// <summary>
        /// Consumed preconditions are resource that are removed as we perform the action
        /// </summary>
        public HashSet<Precondition> ConsumedPrecondition = new HashSet<Precondition>();
    }

    public class BuildWorkerAction : VirtualAction
    {

    }

    public class BuildSupplyDepotAction : VirtualAction
    {
        public override VirtualWorldState Clone()
        {

        }
    }




    public abstract class VirtualUnit
    {

    }




    public class BuildOrder
    {
        public BuildOrder Clone()
        {

        }
    }



    public class VirtualWorldState
    {
        /// <summary>
        /// Current virtual game time, measured in logical frames
        /// </summary>
        public int Time = 0;

        /// <summary>
        /// Actions that have been taken, but have not been completed yet
        /// </summary>
        public List<VirtualAction> ActionsInProgress = new List<VirtualAction>();

        public int WorkersMiningMinerals = 0;
    }





    /// <summary>
    /// Generates build orders for the current game state
    /// </summary>
    public class BuildOrderGenerator
    {
        public BuildOrder GenerateBuildOrder(ulong framesToSearch)
        {

        }

        private void RecursiveSearch(BuildOrder buildOrder, VirtualWorldState worldState, ulong currentFrame, ulong deltaFrameCount)
        {

        }
    }
}
