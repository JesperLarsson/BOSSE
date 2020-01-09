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
    using static UpgradeConstants;
    using static AbilityConstants;

#warning TODO: This should also take a list of optional weights for each unit, ie we need more air right now etc
#warning TODO: Also add a bias option which changes the evaluation algorithm, ex one only counts army units (agressive) while counts everything (balance focus) while another only counts workers+CC (economy focus)
    // Based on ideas from this paper: http://www.cs.mun.ca/~dchurchill/pdf/cog19_buildorder.pdf
    public static class BuildOrderConfig
    {
        public const float WorkerMineralsPerFrameEstimate = 40.0f / 60.0f / FramesPerSecond;
        public const float FramesPerSecond = 22.4f;
        public const ulong StandardFrameDelta = 22;
        public const uint IdUnitThreshold = 1000000;
    }

    /// <summary>
    /// A single build order item, is either a unit (including structures) or an upgrade
    /// Uses singleton instances which are used for type checking, retrieve through the static Get-method
    /// </summary>
    public class ItemId : IComparable<ItemId>
    {
        private readonly UnitId UnitType = 0;
        private readonly UpgradeId UpgradeType = 0;

        private static Dictionary<UnitId, ItemId> ExistingUnitTypes = new Dictionary<UnitId, ItemId>();
        private static Dictionary<UpgradeId, ItemId> ExistingUpgradeTypes = new Dictionary<UpgradeId, ItemId>();

        private ItemId(UnitId unitType)
        {
            this.UnitType = unitType;
        }
        private ItemId(UpgradeId upgradeType)
        {
            this.UpgradeType = upgradeType;
        }

        public static ItemId Get(UnitId type)
        {
            if (!ExistingUnitTypes.ContainsKey(type))
            {
                ExistingUnitTypes[type] = new ItemId(type);
            }
            return ExistingUnitTypes[type];
        }

        public static ItemId Get(UpgradeId type)
        {
            if (!ExistingUpgradeTypes.ContainsKey(type))
            {
                ExistingUpgradeTypes[type] = new ItemId(type);
            }
            return ExistingUpgradeTypes[type];
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

        public int CompareTo(ItemId other)
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












    public abstract class Precondition
    {
        public abstract bool IsFulfilled(VirtualWorldState worldState);
    }

    public class HaveUnitCondition : Precondition
    {
        private ItemId RequiredUnitId = null;

        public HaveUnitCondition(ItemId requiredUnitId)
        {
            RequiredUnitId = requiredUnitId;
        }

        public override bool IsFulfilled(VirtualWorldState worldState)
        {

        }
    }

    public class MineralCondition : Precondition
    {
        private uint MineralsRequired = 0;

        public MineralCondition(uint mineralsRequired)
        {
            MineralsRequired = mineralsRequired;
        }

        public override bool IsFulfilled(VirtualWorldState worldState)
        {

        }
    }








    public abstract class VirtualAction
    {
        private ItemId ProducesItemId = null;
        private UnitTypeData UnitData = null;

        protected VirtualAction(UnitId unitCreateType)
        {
            this.ProducesItemId = ItemId.Get(unitCreateType);
            this.UnitData = GetUnitInfo(unitCreateType);
        }

        protected VirtualAction(UpgradeId upgradeCreateType)
        {
            this.ProducesItemId = ItemId.Get(upgradeCreateType);
            this.UnitData = null;
        }

        public ItemId GetProducedItem()
        {
            return this.ProducesItemId;
        }

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

        public uint BuildTime()
        {
            return (uint)Math.Ceiling(UnitData.BuildTime);
        }

        public uint MineralPrice()
        {
            return UnitData.MineralCost;
        }

        public static List<VirtualAction> GetAllActions()
        {
            // todo, return all possible actions that we know of globally, do not check any conditions
        }
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







    public class BuildOrder
    {
        public BuildOrder Clone()
        {

        }
    }

    public class ActionSet
    {
        /// <summary>
        /// Actions registered in this set
        /// Action ID => Instance mapping
        /// </summary>
        private Dictionary<ItemId, VirtualAction> RegisteredActions = new Dictionary<ItemId, VirtualAction>();

        public void Add(VirtualAction newAction)
        {
            // todo
            throw new NotImplementedException();
        }
    }

    public class VirtualWorldState
    {
        public VirtualWorldState(ResponseObservation starcraftGameState)
        {
            // todo
            throw new NotImplementedException();
        }

        /// <summary>
        /// Current virtual game time, measured in logical frames
        /// </summary>
        public int Time = 0;

        public ActionSet ActionsInProgress = new ActionSet();

        /// <summary>
        /// Number of workers currently assigned to mine minerals
        /// </summary>
        public int WorkersMiningMinerals = 0;
    }





    public class BuildOrderGoal
    {
        /// <summary>
        /// The requested number of units
        /// Key => ID of either the unit or the upgrade
        /// Value => The number requested of that unit, expected 1 for upgrades
        /// </summary>
        public Dictionary<ItemId, uint> GoalUnitCount = new Dictionary<ItemId, uint>();

        /// <summary>
        /// The maximum number of allowed units
        /// Key => ID of either the unit or the upgrade
        /// Value => The number requested of that unit, expected 1 for upgrades
        /// </summary>
        public Dictionary<ItemId, uint> GoalUnitsMaximum = new Dictionary<ItemId, uint>();

        /// <summary>
        /// Returns the supply necessary to produce everything in GoalUnitCount
        /// </summary>
        public int GetNumberOfSupplyMinimum()
        {
            // simple


        }

        /// <summary>
        /// Returns true if this goal is achieved by the given game state
        /// </summary>
        public bool IsAchievedBy(VirtualWorldState worldState)
        {
            // simple


        }

        /// <summary>
        /// Returns a list of all actions that are relevant to produce this build order
        /// </summary>
        public ActionSet GetRelevantActions()
        {
            List<VirtualAction> allActions = VirtualAction.GetAllActions();

            ActionSet resultSet = new ActionSet();
            foreach (VirtualAction actionIter in allActions)
            {
                ItemId actionProducesItem = actionIter.GetProducedItem();

                if (!GoalUnitCount.ContainsKey(actionProducesItem))
                    continue;
                uint targetcount = GoalUnitCount[actionProducesItem];

                if (!GoalUnitsMaximum.ContainsKey(actionProducesItem))
                    GoalUnitsMaximum[actionProducesItem] = targetcount;
                uint targetMaxCount = GoalUnitsMaximum[actionProducesItem];

                if (targetcount == 0 || targetMaxCount == 0)
                    continue;

                resultSet.Add(actionIter);
            }

            return resultSet;
        }

        public uint GetGoal(UnitId unitType)
        {
            if (!GoalUnitCount.ContainsKey(unitType))
                return 0;

            return GoalUnitCount[unitType];
        }

        public uint GetGoalMax(UnitId unitType)
        {
            if (!GoalUnitsMaximum.ContainsKey(unitType))
                return 0;

            return GoalUnitsMaximum[unitType];
        }

        public bool HasGoal()
        {
            foreach (uint iter in GoalUnitCount.Values)
            {
                if (iter > 0)
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            string str = "[BuildOrderGoal ";

            foreach (var iter in GoalUnitCount)
            {
                str += iter.Key + "=";
                str += iter.Value + ", ";
            }
            str += "]";

            return str;
        }
    }

    public class BuildOrderResult
    {
        public BuildOrder BuildOrderRef = null;
        public BuildOrderGoal InputGoal = null;
        public VirtualWorldState ResultingWorldState = null;

        public bool WasSolved = false;

        public ulong NodesExpanded = 0;

        /// <summary>
        /// Upper bound of this search, in logical frames. MaxValue = no known bound
        /// </summary>
        public int UpperBound = int.MaxValue;
    }




    /// <summary>
    /// Generates build orders for the current game state
    /// </summary>
    public class BuildOrderGenerator
    {
        private readonly BuildOrderGoal Requestedgoal = null;
        private BuildOrderResult Result = null;

        public BuildOrderGenerator(BuildOrderGoal argGoal)
        {
            this.Requestedgoal = argGoal;
        }

        /// <summary>
        /// Retrieves the previous build order search results
        /// This can be called while the search is in progress to retrieve a partial or sub-optimal build order
        /// </summary>
        public BuildOrderResult GetResults()
        {
            if (this.Result != null)
            {
                return Result;
            }
            else
            {
                Log.Warning("Called GetResults() before build order result is available");
                return null;
            }
        }

        /// <summary>
        /// Generates a build order for the search parameters given to the generator constructor
        /// </summary>
        public BuildOrderResult GenerateBuildOrder()
        {
            VirtualWorldState initialWorldState = new VirtualWorldState(CurrentGameState.ObservationState);





        }

        private void RecursiveSearch()
        {
            this.Result.NodesExpanded += 1;




        }

        public ActionSet GenerateLegalActions(VirtualWorldState worldState)
        {
            ActionSet relevantActions = Requestedgoal.GetRelevantActions();


        }
    }
}
