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
    using Google.Protobuf.Collections;

    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

#warning TODO: This should also take a list of optional weights for each unit, ie we need more air right now etc
#warning TODO: Also add a bias option which changes the evaluation algorithm, ex one only counts army units (agressive) while counts everything (balance focus) while another only counts workers+CC (economy focus)
    // Based on ideas from this paper: http://www.cs.mun.ca/~dchurchill/pdf/cog19_buildorder.pdf
    public class VirtualUnit
    {
        public UnitId Type;
        public bool IsBusy = false;

        /// <summary>
        /// Import constructor
        /// </summary>
        public VirtualUnit(Unit importedActualUnit)
        {
            Type = (UnitId)importedActualUnit.UnitType;
        }

        /// <summary>
        /// Planned unit as part of our build order
        /// </summary>
        public VirtualUnit(UnitId Type)
        {
        }

        public void PerformEffectsOnWorldState(VirtualWorldState worldState)
        {
            if (this.Type != BotConstants.WorkerUnit)
            {
                return;
            }

            const float WorkerMineralsPerMinute = 40.0f;
            float workerMineralsPerFrame = FramesToTime(WorkerMineralsPerMinute / 60);
            worldState.Minerals += workerMineralsPerFrame;
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
        public ulong TakenOnFrame = 0;
        protected ulong TakesAffectOnFrame = 0;
        protected VirtualUnit Occupies = null;
        protected string Name = "N/A";

        public abstract bool CanTake(VirtualWorldState worldState);
        public abstract void TakeAction(VirtualWorldState worldState, ulong currentFrame);
        public virtual void StartedEffect(VirtualWorldState worldState, ulong currentFrame)
        {

        }
        public virtual void SimulateActionEffects(VirtualWorldState worldState, ulong currentFrame)
        {
            if (currentFrame < TakesAffectOnFrame)
            {
                return;
            }

            if (Occupies != null)
            {
                Occupies.IsBusy = false;
                Occupies = null;
                this.StartedEffect(worldState, currentFrame);
            }
        }

        public abstract PlannedAction Clone();

        public override string ToString()
        {
            return $"[Action {this.Name} {this.TakenOnFrame}-{this.TakesAffectOnFrame}]";
        }
    }

    public class BuildWorker : PlannedAction
    {
        public BuildWorker()
        {
            this.Name = this.GetType().Name;
        }

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
            this.Occupies = worldState.GetUnitsOfType(UnitConstants.ResourceCenters).Where(cc => cc.IsBusy == false).ToList()[0];
            this.Occupies.IsBusy = true;

            this.TakenOnFrame = currentFrame;
            this.TakesAffectOnFrame = TimeToTakeActionInFrames() + TakenOnFrame;
        }

        public override void StartedEffect(VirtualWorldState worldState, ulong currentFrame)
        {
            VirtualUnit newWorker = new VirtualUnit(BotConstants.WorkerUnit);
            worldState.Units.Add(newWorker);
        }

        public override PlannedAction Clone()
        {
            return (PlannedAction)this.MemberwiseClone();
        }
    }




    public class BuildOrder
    {
        private List<PlannedAction> ActionOrder = new List<PlannedAction>();

        public bool IsEmpty()
        {
            return this.ActionOrder.Count == 0;
        }

        public void Add(PlannedAction newAction)
        {
            ActionOrder.Add(newAction);
        }

        public void SimulateAll(VirtualWorldState onWorldState, ulong currentFrame)
        {
            // Take all actions
            foreach (PlannedAction actioniter in ActionOrder)
            {
                actioniter.SimulateActionEffects(onWorldState, currentFrame);
            }

            // Simulate effects of existing units
            foreach (VirtualUnit unitIter in onWorldState.Units)
            {
                unitIter.PerformEffectsOnWorldState(onWorldState);
            }
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
            BuildOrder obj = (BuildOrder)this.MemberwiseClone();
            obj.ActionOrder = new List<PlannedAction>();

            foreach (var iter in this.ActionOrder)
            {
                obj.ActionOrder.Add(iter.Clone());
            }
            return obj;
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
            Stopwatch sw = new Stopwatch();
            sw.Start();

            this.PossibleActions = GetPossibleActions();
            this.BestBuildOrder = new BuildOrder();
            this.OptimizeForFrameOffset = framesToSearch;
            VirtualWorldState currentGameState = new VirtualWorldState(CurrentGameState.ObservationState);

            RecursiveSearch(BestBuildOrder.Clone(), currentGameState, 0);

            if (this.BestBuildOrder.IsEmpty())
            {
                Log.SanityCheckFailed("Unable to search for a build order");
                throw new BosseFatalException();
            }

            sw.Stop();
            Log.Info("Determined build order in " + sw.ElapsedMilliseconds + " ms");
            return BestBuildOrder;
        }

        private void RecursiveSearch(BuildOrder buildOrderIter, VirtualWorldState worldState, ulong currentFrame)
        {
            ulong bestEval = BestBuildOrder.Evaluate(worldState);

            buildOrderIter.SimulateAll(worldState, currentFrame);
            ulong currentEval = buildOrderIter.Evaluate(worldState);            
            if (currentEval > bestEval)
            {
                Log.Info("Found new best build order");
                BestBuildOrder = buildOrderIter.Clone();
            }
            if (currentFrame >= OptimizeForFrameOffset)
                return;

            foreach (PlannedAction actionIter in PossibleActions)
            {
                VirtualWorldState workingWorldState = worldState.Clone();
                BuildOrder workingBuildOrder = buildOrderIter.Clone();

                if (actionIter.CanTake(workingWorldState))
                {
                    // Take the action and step forward
                    actionIter.TakeAction(workingWorldState, currentFrame);
                    workingBuildOrder.Add(actionIter);
                    RecursiveSearch(workingBuildOrder, workingWorldState, currentFrame + 1);
                }
                else
                {
                    // Advance one frame without taking the action
                    RecursiveSearch(workingBuildOrder, workingWorldState, currentFrame + 1);
                }
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
