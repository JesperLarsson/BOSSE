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

    public static class BuiltOrderConfig
    {
        public const float WorkerMineralsPerFrame = 40.0f / 60.0f / 22.4f;
        public const float FramesPerSecond = 22.4f;
    }

#warning TODO: This should also take a list of optional weights for each unit, ie we need more air right now etc
#warning TODO: Also add a bias option which changes the evaluation algorithm, ex one only counts army units (agressive) while counts everything (balance focus) while another only counts workers+CC (economy focus)
    // Based on ideas from this paper: http://www.cs.mun.ca/~dchurchill/pdf/cog19_buildorder.pdf
    public class VirtualUnit
    {
        public UnitId Type;
        public bool IsBusy = false;

        /// <summary>
        /// If busy, this unit becomes available on the given frame
        /// </summary>
        public ulong BecomesAvailableAtTick = 0;

        /// <summary>
        /// Import constructor
        /// </summary>
        public VirtualUnit(Unit importedActualUnit)
        {
            this.Type = (UnitId)importedActualUnit.UnitType;
        }

        /// <summary>
        /// Planned unit as part of our build order
        /// </summary>
        public VirtualUnit(UnitId type)
        {
            this.Type = type;
        }

        //public bool IsArmy()
        //{
        //    return UnitConstants.ArmyUnits.Contains(this.Type);
        //}

        public void PerformEffectsOnWorldState(VirtualWorldState worldState, ulong deltaFrames)
        {
            if (this.Type != BotConstants.WorkerUnit)
            {
                return;
            }

            if (this.IsBusy)
            {
                return; // busy workers don't mine
            }

            worldState.Minerals += (BuiltOrderConfig.WorkerMineralsPerFrame * deltaFrames);
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

        public float EstimateMineralIncomePerFrame()
        {
            return GetUnitsOfType(BotConstants.WorkerUnit).Count * BuiltOrderConfig.WorkerMineralsPerFrame;
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

        /// <summary>
        /// Returns true if we can take this action
        /// </summary>
        public abstract bool CanTake(VirtualWorldState worldState);
        public abstract ulong EstimateWhenActionIsAvailable(VirtualWorldState worldState, ulong currentFrame);

        /// <summary>
        /// Queue action
        /// </summary>
        public abstract void TakeAction(VirtualWorldState worldState, ulong currentFrame);
        public void ActionWasTaken(VirtualWorldState worldState, ulong currentFrame)
        {
            if (this.Occupies != null)
            {
                this.Occupies.BecomesAvailableAtTick = TakesAffectOnFrame;
            }
        }

        /// <summary>
        /// Called once when the effect starts, ie unit spawned etc
        /// </summary>
        public abstract void StartedEffect(VirtualWorldState worldState, ulong currentFrame);

        /// <summary>
        /// Called each frame to search for effect start
        /// </summary>
        public virtual void SimulateActionEffects(VirtualWorldState worldState, ulong currentFrame, ulong deltaFrames)
        {
            if (currentFrame < this.TakesAffectOnFrame)
            {
                return;
            }

            if (this.Occupies != null)
            {
                // Is now available again
                this.Occupies.BecomesAvailableAtTick = 0;
                this.Occupies.IsBusy = false;
                this.Occupies = null;
                this.StartedEffect(worldState, currentFrame);
            }
        }

        public abstract PlannedAction Clone();

        public override string ToString()
        {
            return $"[Action {this.Name} {this.TakenOnFrame}-{this.TakesAffectOnFrame}]";
        }

        protected bool CanAfford(UnitTypeData unitData, VirtualWorldState worldState)
        {
            int foodConsumed = (int)(unitData.FoodRequired - unitData.FoodProvided);

            //bool enoughFood = FreeSupply >= foodConsumed;
            bool enoughMinerals = worldState.Minerals >= unitData.MineralCost;
            bool enoughGas = worldState.Gas >= unitData.VespeneCost;

            return enoughMinerals && enoughGas; // && enoughFood;
        }

        protected void SubractCosts(UnitTypeData unitData, VirtualWorldState worldState)
        {
            int foodConsumed = (int)(unitData.FoodProvided - unitData.FoodRequired);

            worldState.Minerals -= unitData.MineralCost;
            worldState.Gas -= unitData.VespeneCost;
            //UsedSupply = (uint)(UsedSupply - foodConsumed);
        }

        protected ulong EstimateWhenWeCanAfford(UnitTypeData unitData, VirtualWorldState worldState, ulong currentFrame)
        {
            int mineralsNeeded = (int)(Math.Ceiling(unitData.MineralCost - worldState.Minerals));
            if (mineralsNeeded < 0)
            {
                Log.SanityCheckFailed("Unexepcted call to EstimateFramesUntilCanBeAfford " + worldState.Minerals);
                return 200 + currentFrame;
            }

            double frames = Math.Ceiling(mineralsNeeded / worldState.EstimateMineralIncomePerFrame());
            return (ulong)frames + currentFrame;
        }
    }

    /// <summary>
    /// Action to build a worker unit
    /// </summary>
    public class BuildWorker : PlannedAction
    {
        private readonly TimeSpan WorkerBuildTime = TimeSpan.FromSeconds(12);
        private UnitTypeData UnitData = GetUnitInfo(BotConstants.WorkerUnit);

        public BuildWorker()
        {
            this.Name = this.GetType().Name;
        }

        public override bool CanTake(VirtualWorldState worldState)
        {
            if (!this.CanAfford(UnitData, worldState))
                return false;

            bool havePreReq = worldState.GetUnitsOfType(UnitConstants.ResourceCenters).Where(cc => cc.IsBusy == false).ToList().Count > 0;
            return havePreReq;
        }

        public override ulong EstimateWhenActionIsAvailable(VirtualWorldState worldState, ulong currentFrame)
        {
            if (!this.CanAfford(UnitData, worldState))
                return EstimateWhenWeCanAfford(UnitData, worldState, currentFrame);

            var temp = worldState.GetUnitsOfType(UnitConstants.ResourceCenters).MinBy(obj => obj.BecomesAvailableAtTick);
            VirtualUnit soonestDependency = temp.FirstOrDefault();
            if (soonestDependency == null)
            {
                return (ulong)(BuiltOrderConfig.FramesPerSecond * 4) + currentFrame;
            }
            if (soonestDependency.BecomesAvailableAtTick == 0)
            {
                Log.SanityCheckFailed("Bug found, this should have a value (was 0)");
                return 200 + currentFrame;
            }

            return soonestDependency.BecomesAvailableAtTick;
        }

        public override void TakeAction(VirtualWorldState worldState, ulong currentFrame)
        {
            this.SubractCosts(UnitData, worldState);

            ulong buildTimeFrames = SecondsToFrames((float)WorkerBuildTime.TotalSeconds);
            this.Occupies = worldState.GetUnitsOfType(UnitConstants.ResourceCenters).Where(cc => cc.IsBusy == false).ToList()[0];
            this.Occupies.IsBusy = true;

            this.TakenOnFrame = currentFrame;
            this.TakesAffectOnFrame = buildTimeFrames + TakenOnFrame;
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

    /// <summary>
    /// Action to build a barracks
    /// </summary>
    public class BuildBarracks : PlannedAction
    {
        private readonly TimeSpan RaxBuildTime = TimeSpan.FromSeconds(46);
        private UnitTypeData UnitData = GetUnitInfo(UnitId.BARRACKS);

        public BuildBarracks()
        {
            this.Name = this.GetType().Name;
        }

        public override bool CanTake(VirtualWorldState worldState)
        {
            if (!this.CanAfford(UnitData, worldState))
                return false;

            return worldState.GetUnitsOfType(BotConstants.WorkerUnit).Where(unit => unit.IsBusy == false).ToList().Count > 0;
        }

        public override void TakeAction(VirtualWorldState worldState, ulong currentFrame)
        {
            this.SubractCosts(UnitData, worldState);

            ulong buildTimeFrames = SecondsToFrames((float)RaxBuildTime.TotalSeconds);
            this.Occupies = worldState.GetUnitsOfType(BotConstants.WorkerUnit).Where(unit => unit.IsBusy == false).ToList()[0];
            this.Occupies.IsBusy = true;

            this.TakenOnFrame = currentFrame;
            this.TakesAffectOnFrame = buildTimeFrames + TakenOnFrame;
        }

        public override ulong EstimateWhenActionIsAvailable(VirtualWorldState worldState, ulong currentFrame)
        {
            if (!this.CanAfford(UnitData, worldState))
                return EstimateWhenWeCanAfford(UnitData, worldState, currentFrame);

            var temp = worldState.GetUnitsOfType(BotConstants.WorkerUnit).MinBy(obj => obj.BecomesAvailableAtTick);
            VirtualUnit soonestDependency = temp.FirstOrDefault();
            if (soonestDependency == null)
            {
                return (ulong)(BuiltOrderConfig.FramesPerSecond * 4) + currentFrame;
            }
            if (soonestDependency.BecomesAvailableAtTick == 0)
            {
                Log.SanityCheckFailed("Bug found, this should have a value (was 0)");
                return 200 + currentFrame;
            }

            return soonestDependency.BecomesAvailableAtTick;
        }

        public override void StartedEffect(VirtualWorldState worldState, ulong currentFrame)
        {
            VirtualUnit newUnit = new VirtualUnit(UnitId.BARRACKS);
            worldState.Units.Add(newUnit);
        }

        public override PlannedAction Clone()
        {
            return (PlannedAction)this.MemberwiseClone();
        }
    }



    public class BuildMarine : PlannedAction
    {
        private readonly TimeSpan BuildTime = TimeSpan.FromSeconds(18);
        private UnitTypeData UnitData = GetUnitInfo(UnitId.MARINE);

        public BuildMarine()
        {
            this.Name = this.GetType().Name;
        }

        public override bool CanTake(VirtualWorldState worldState)
        {
            if (!this.CanAfford(UnitData, worldState))
                return false;

            return worldState.GetUnitsOfType(UnitId.BARRACKS).Where(unit => unit.IsBusy == false).ToList().Count > 0;
        }

        public override void TakeAction(VirtualWorldState worldState, ulong currentFrame)
        {
            this.SubractCosts(UnitData, worldState);

            ulong buildTimeFrames = SecondsToFrames((float)BuildTime.TotalSeconds);
            this.Occupies = worldState.GetUnitsOfType(UnitId.BARRACKS).Where(unit => unit.IsBusy == false).ToList()[0];
            this.Occupies.IsBusy = true;

            this.TakenOnFrame = currentFrame;
            this.TakesAffectOnFrame = buildTimeFrames + TakenOnFrame;
        }

        public override ulong EstimateWhenActionIsAvailable(VirtualWorldState worldState, ulong currentFrame)
        {
            if (!this.CanAfford(UnitData, worldState))
                return EstimateWhenWeCanAfford(UnitData, worldState, currentFrame);

            var temp = worldState.GetUnitsOfType(UnitId.BARRACKS).MinBy(obj => obj.BecomesAvailableAtTick);
            VirtualUnit soonestDependency = temp.FirstOrDefault();
            if (soonestDependency == null)
            {
                return (ulong)(BuiltOrderConfig.FramesPerSecond * 4) + currentFrame;
            }

            if (soonestDependency.BecomesAvailableAtTick == 0)
            {
                Log.SanityCheckFailed("Bug found, this should have a value (was 0)");
                return 200 + currentFrame;
            }

            return soonestDependency.BecomesAvailableAtTick;
        }

        public override void StartedEffect(VirtualWorldState worldState, ulong currentFrame)
        {
            VirtualUnit newUnit = new VirtualUnit(UnitId.MARINE);
            worldState.Units.Add(newUnit);
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

        public void SimulateAll(VirtualWorldState onWorldState, ulong currentFrame, ulong deltaFrames)
        {
            // Take all actions
            foreach (PlannedAction actioniter in ActionOrder)
            {
                actioniter.SimulateActionEffects(onWorldState, currentFrame, deltaFrames);
            }

            // Simulate effects of existing units
            foreach (VirtualUnit unitIter in onWorldState.Units)
            {
                unitIter.PerformEffectsOnWorldState(onWorldState, deltaFrames);
            }
        }

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
            uint totalMinerals = 0; // (uint)worldState.Minerals;
            uint totalGas = 0; // (uint)worldState.Gas;

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
        private ulong BestEval = 0;
        private ulong OptimizeForFrameOffset = 0;
        private List<PlannedAction> PossibleActions = null;

        public BuildOrder GenerateBuildOrder(ulong framesToSearch)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            this.PossibleActions = GetPossibleActions();
            this.BestBuildOrder = new BuildOrder();
            this.OptimizeForFrameOffset = framesToSearch;
            VirtualWorldState currentGameState = new VirtualWorldState(CurrentGameState.ObservationState);
            this.BestEval = BestBuildOrder.Evaluate(currentGameState);

            RecursiveSearch(BestBuildOrder.Clone(), currentGameState, 0, 1);

            if (this.BestBuildOrder.IsEmpty())
            {
                Log.SanityCheckFailed("Unable to search for a build order");
                throw new BosseFatalException();
            }

            sw.Stop();
            Log.Info("Determined build order in " + sw.ElapsedMilliseconds + " ms");
            return BestBuildOrder;
        }

        private void SetNewBest(BuildOrder buildOrderIter, ulong currentEval)
        {
            if (buildOrderIter.IsEmpty() && (!this.BestBuildOrder.IsEmpty()))
            {
                Log.SanityCheckFailed("Unexpected assignemnt of empty build order");
            }

            this.BestBuildOrder = buildOrderIter.Clone();
            this.BestEval = currentEval;
        }

        private void RecursiveSearch(BuildOrder buildOrderIter, VirtualWorldState worldState, ulong currentFrame, ulong currentDeltaFrames)
        {
            if (currentDeltaFrames <= 0)
            {
                Log.SanityCheckFailed("Invalid delta frame value");
                return;
            }

            buildOrderIter.SimulateAll(worldState, currentFrame, currentDeltaFrames);
            ulong currentEval = buildOrderIter.Evaluate(worldState);
            if (currentEval > this.BestEval)
            {
                //Log.Info("Found new best build order");
                SetNewBest(buildOrderIter, currentEval);
            }
            if (currentFrame >= this.OptimizeForFrameOffset)
                return;

            bool actionWasPossible = false;
            ulong anyActionNextPossibleAt = ulong.MaxValue;
            foreach (PlannedAction actionIter in this.PossibleActions)
            {
                VirtualWorldState workingWorldState = worldState.Clone();
                BuildOrder workingBuildOrder = buildOrderIter.Clone();

                if (actionIter.CanTake(workingWorldState))
                {
                    // Take the action and step forward
                    actionIter.TakeAction(workingWorldState, currentFrame);
                    actionIter.ActionWasTaken(workingWorldState, currentFrame);
                    workingBuildOrder.Add(actionIter);

                    const ulong deltaFrames = 1;
                    RecursiveSearch(workingBuildOrder, workingWorldState, currentFrame + deltaFrames, deltaFrames);
                    actionWasPossible = true;
                }
                else
                {
                    ulong actionNextPossible = actionIter.EstimateWhenActionIsAvailable(workingWorldState, currentFrame);
                    if (actionNextPossible <= currentFrame)
                    {
                        // Likely because some function returned a static estimate instead of adding to the current frame counter
                        Log.SanityCheckFailed("Unexpected frame availability, it should not be available for some time: " + actionNextPossible);
                    }

                    anyActionNextPossibleAt = Math.Min(anyActionNextPossibleAt, actionNextPossible);
                }
            }

            if (actionWasPossible)
            {
                // Step a single frame without any of the actions taken as well
                const ulong deltaFrames = 1;

                VirtualWorldState noActionWorldState = worldState.Clone();
                BuildOrder noActionBuildOrder = buildOrderIter.Clone();
                RecursiveSearch(noActionBuildOrder, noActionWorldState, currentFrame + deltaFrames, deltaFrames);
            }
            else
            {
                // No actions are possible, we have no choice but to wait. We can optimize by stepping multiple frames at once
                ulong deltaFrames = 1;
                if (anyActionNextPossibleAt == ulong.MaxValue)
                {
                    Log.SanityCheckFailed("Expected a frame offset for delta value");
                }
                else
                {
                    // Normal case
                    deltaFrames = anyActionNextPossibleAt - currentFrame;
                }

                VirtualWorldState noActionWorldState = worldState.Clone();
                BuildOrder noActionBuildOrder = buildOrderIter.Clone();
                RecursiveSearch(noActionBuildOrder, noActionWorldState, currentFrame + deltaFrames, deltaFrames);
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
