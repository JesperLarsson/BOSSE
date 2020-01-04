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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

#warning TODO: This should also take a list of optional weights for each unit, ie we need more air right now etc
    // Based on ideas from resarch paper: http://www.cs.mun.ca/~dchurchill/pdf/cog19_buildorder.pdf

    public class VirtualUnit
    {
        public UnitId Type;
        public bool IsActualUnit;
        public ulong BuiltAtIndex;
        public bool IsBusy = false;

        /// <summary>
        /// Import constructor
        /// </summary>
        public VirtualUnit(Unit importedActualUnit)
        {
            Type = (UnitId)importedActualUnit.UnitType;

            IsActualUnit = true;
        }

        /// <summary>
        /// Planned unit as part of our build order
        /// </summary>
        public VirtualUnit(UnitId Type)
        {

            IsActualUnit = false;
        }
    }

    public class VirtualWorldState
    {
        public List<VirtualUnit> Units = new List<VirtualUnit>();
        public float Minerals;
        public float Gas;
        //public uint Supply;

        /// <summary>
        /// From current sc2 state
        /// </summary>
        public VirtualWorldState(ResponseObservation actualCurrentGameState)
        {
            foreach (var unitIter in actualCurrentGameState.Observation.RawData.Units)
            {
                VirtualUnit obj = new VirtualUnit(unitIter);
                this.Units.Add(obj);
            }
            this.Minerals = actualCurrentGameState.Observation.PlayerCommon.Minerals;
            this.Gas = actualCurrentGameState.Observation.PlayerCommon.Vespene;
            //this.Supply = actualCurrentGameState.Observation.PlayerCommon.FoodCap; ;
        }

        public VirtualWorldState Clone()
        {
            return (VirtualWorldState)this.MemberwiseClone();
        }

        public List<VirtualUnit> GetUnitsOfType(HashSet<UnitId> types)
        {
            List<VirtualUnit> list = new List<VirtualUnit>();

            foreach (var iter in Units)
            {
                if (types.Contains(iter.Type))
                {
                    list.Add(iter);
                }
            }

            return list;
        }
        public List<VirtualUnit> GetUnitsOfType(UnitId type)
        {
            return GetUnitsOfType(new HashSet<UnitId>() { type });
        }
    }




    public abstract class PlannedAction
    {
        public abstract bool CanTake(VirtualWorldState worldState);
        public abstract void TakeAction(VirtualWorldState worldState, ulong currentFrame);
        public abstract void SimulateActionEffects(VirtualWorldState worldState, ulong currentFrame);
    }

    public class BuildWorker : PlannedAction
    {
        public ulong TakenOnFrame = 0;
        private ulong TakesAffectOnFrame = 0;

        public ulong TimeToTakeActionInFrames()
        {
            ulong BuildTimeFrames = SecondsToFrames(12);
            return BuildTimeFrames;
        }

        public override bool CanTake(VirtualWorldState worldState)
        {
            return worldState.GetUnitsOfType(UnitConstants.ResourceCenters).Where(cc => cc.IsBusy == false).ToList().Count > 0;
        }

        public override void TakeAction(VirtualWorldState worldState, ulong currentFrame)
        {
            var builtFrom = worldState.GetUnitsOfType(UnitConstants.ResourceCenters).Where(cc => cc.IsBusy == false).ToList()[0];
            builtFrom.IsBusy = true;

            TakenOnFrame = currentFrame;
            TakesAffectOnFrame = TimeToTakeActionInFrames() + TakenOnFrame;
        }

        public override void SimulateActionEffects(VirtualWorldState worldState, ulong currentFrame)
        {
            if (currentFrame < TakesAffectOnFrame)
            {
                return;
            }

            const float WorkerMineralsPerMinute = 40.0f;
            float workerMineralsPerFrame = FramesToTime(WorkerMineralsPerMinute / 60);
            worldState.Minerals += workerMineralsPerFrame;
        }
    }




    public class BuildOrder
    {
        private readonly List<PlannedAction> ActionOrder = new List<PlannedAction>();

        public bool IsEmpty()
        {
            return this.ActionOrder.Count == 0;
        }

        public void Add(PlannedAction newAction)
        {
            ActionOrder.Add(newAction);
        }

        //public void Pop()
        //{
        //    if (ActionOrder.Count == 0)
        //    {
        //        Log.SanityCheckFailed("Unable to pop empty action list");
        //        return;
        //    }

        //    ActionOrder.RemoveAt(ActionOrder.Count - 1);
        //}

        public BuildOrder Clone()
        {
            return (BuildOrder)this.MemberwiseClone();
        }

        /// <summary>
        /// Returns a scalar value with an estimate of how good our general game position is during the given time
        /// We use our total value as a good-enough heuristic
        /// </summary>
        public ulong Evaluate(VirtualWorldState worldState)
        {
            uint totalMinerals = (uint)worldState.Minerals;
            uint totalGas = (uint)worldState.Gas;

            foreach (var iter in worldState.Units)
            {
                var unitInfo = GetUnitInfo(iter.Type);
                totalMinerals += unitInfo.MineralCost;
                totalGas += unitInfo.VespeneCost;
            }

            return totalMinerals + totalGas;
        }

        public override string ToString()
        {
            string str = "";
            foreach (var iter in ActionOrder)
            {
                str += iter.GetType().Name + ", ";
            }

            return $"[BuildOrder {ActionOrder.Count} - {str}]";
        }
    }



    /// <summary>
    /// Generates build orders for the current game state
    /// </summary>
    public class BuildOrderGenerator
    {
        private BuildOrder BestBuildOrder = null;
        private ulong OptimizeForFrameOffset;
        private List<PlannedAction> PossibleActions;

        public BuildOrder GenerateBuildOrder(ulong framesToSearch)
        {
            this.PossibleActions = GetPossibleActions();
            BestBuildOrder = new BuildOrder();
            this.OptimizeForFrameOffset = framesToSearch;
            VirtualWorldState currentGameState = new VirtualWorldState(CurrentGameState.ObservationState);

            RecursiveSearch(BestBuildOrder, currentGameState, 0);

            if (this.BestBuildOrder.IsEmpty())
            {
                Log.SanityCheckFailed("Unable to search for a build order");
                throw new BosseFatalException();
            }

            return BestBuildOrder;
        }

        private void RecursiveSearch(BuildOrder previousBuildOrder, VirtualWorldState worldState, ulong currentFrame)
        {
            if (previousBuildOrder.Evaluate(worldState) > BestBuildOrder.Evaluate(worldState))
            {
                Log.Info("Found new best build order");
                BestBuildOrder = previousBuildOrder;
            }
            if (currentFrame >= OptimizeForFrameOffset)
                return;

            foreach (PlannedAction actionIter in PossibleActions)
            {
                VirtualWorldState workingWorldState = worldState.Clone();
                BuildOrder workingBuildOrder = previousBuildOrder.Clone();

                if (!actionIter.CanTake(workingWorldState))
                    continue;

                actionIter.TakeAction(workingWorldState, currentFrame);
                workingBuildOrder.Add(actionIter);
                RecursiveSearch(workingBuildOrder, workingWorldState, currentFrame + 1);
            }
        }

        private List<PlannedAction> GetPossibleActions()
        {
            List<PlannedAction> possibleActions = new List<PlannedAction>();
            foreach (Type type in Assembly.GetAssembly(typeof(Sensor)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(PlannedAction))))
            {
                PlannedAction action = (PlannedAction)Activator.CreateInstance(type);
                possibleActions.Add(action);
            }
            return possibleActions;
        }
    }
}
