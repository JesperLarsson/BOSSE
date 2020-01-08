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

    using static GeneralGameUtility;
    using static UnitConstants;

    

    //public abstract class PlannedAction
    //{
    //    public ulong TakenOnFrame = 0;
    //    protected ulong TakesAffectOnFrame = 0;
    //    protected VirtualUnit Occupies = null;
    //    protected string Name = "N/A";

    //    /// <summary>
    //    /// Returns true if we can take this action
    //    /// </summary>
    //    public abstract bool CanTake(VirtualWorldState worldState);
    //    public abstract ulong EstimateWhenActionIsAvailable(VirtualWorldState worldState, ulong currentFrame);

    //    /// <summary>
    //    /// Queue action
    //    /// </summary>
    //    public abstract void TakeAction(VirtualWorldState worldState, ulong currentFrame);
    //    public void ActionWasTaken(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        if (this.Occupies != null)
    //        {
    //            this.Occupies.BecomesAvailableAtTick = TakesAffectOnFrame;
    //        }
    //    }

    //    /// <summary>
    //    /// Called once when the effect starts, ie unit spawned etc
    //    /// </summary>
    //    public abstract void StartedEffect(VirtualWorldState worldState, ulong currentFrame);

    //    /// <summary>
    //    /// Called each frame to search for effect start
    //    /// </summary>
    //    public virtual void SimulateActionEffects(VirtualWorldState worldState, ulong currentFrame, ulong deltaFrames)
    //    {
    //        if (currentFrame < this.TakesAffectOnFrame)
    //        {
    //            return;
    //        }

    //        if (this.Occupies != null)
    //        {
    //            // Is now available again
    //            this.Occupies.BecomesAvailableAtTick = 0;
    //            this.Occupies.IsBusy = false;
    //            this.Occupies = null;
    //            this.StartedEffect(worldState, currentFrame);
    //        }
    //    }

    //    public abstract PlannedAction Clone();

    //    public override string ToString()
    //    {
    //        return $"[Action {this.Name} {this.TakenOnFrame}-{this.TakesAffectOnFrame}]";
    //    }

    //    protected bool CanAfford(UnitTypeData unitData, VirtualWorldState worldState)
    //    {
    //        int foodConsumed = (int)(unitData.FoodRequired - unitData.FoodProvided);

    //        //bool enoughFood = FreeSupply >= foodConsumed;
    //        bool enoughMinerals = worldState.Minerals >= unitData.MineralCost;
    //        bool enoughGas = worldState.Gas >= unitData.VespeneCost;

    //        return enoughMinerals && enoughGas; // && enoughFood;
    //    }

    //    protected void SubractCosts(UnitTypeData unitData, VirtualWorldState worldState)
    //    {
    //        int foodConsumed = (int)(unitData.FoodProvided - unitData.FoodRequired);

    //        worldState.Minerals -= unitData.MineralCost;
    //        worldState.Gas -= unitData.VespeneCost;
    //        //UsedSupply = (uint)(UsedSupply - foodConsumed);
    //    }

    //    protected ulong EstimateWhenWeCanAfford(UnitTypeData unitData, VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        int mineralsNeeded = (int)(Math.Ceiling(unitData.MineralCost - worldState.Minerals));
    //        if (mineralsNeeded < 0)
    //        {
    //            Log.SanityCheckFailed("Unexepcted call to EstimateFramesUntilCanBeAfford " + worldState.Minerals);
    //            return 200 + currentFrame;
    //        }

    //        double frames = Math.Ceiling(mineralsNeeded / worldState.EstimateMineralIncomePerFrame());
    //        return (ulong)frames + currentFrame;
    //    }
    //}

    ///// <summary>
    ///// Action to build a worker unit
    ///// </summary>
    //public class BuildWorker : PlannedAction
    //{
    //    private readonly TimeSpan WorkerBuildTime = TimeSpan.FromSeconds(12);
    //    private UnitTypeData UnitData = GetUnitInfo(BotConstants.WorkerUnit);

    //    public BuildWorker()
    //    {
    //        this.Name = this.GetType().Name;
    //    }

    //    public override bool CanTake(VirtualWorldState worldState)
    //    {
    //        if (!this.CanAfford(UnitData, worldState))
    //            return false;

    //        bool havePreReq = worldState.GetUnitsOfType(UnitConstants.ResourceCenters).Where(cc => cc.IsBusy == false).ToList().Count > 0;
    //        return havePreReq;
    //    }

    //    public override ulong EstimateWhenActionIsAvailable(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        if (!this.CanAfford(UnitData, worldState))
    //            return EstimateWhenWeCanAfford(UnitData, worldState, currentFrame);

    //        var temp = worldState.GetUnitsOfType(UnitConstants.ResourceCenters).MinBy(obj => obj.BecomesAvailableAtTick);
    //        VirtualUnit soonestDependency = temp.FirstOrDefault();
    //        if (soonestDependency == null)
    //        {
    //            return (ulong)(BuiltOrderConfig.FramesPerSecond * 4) + currentFrame;
    //        }
    //        if (soonestDependency.BecomesAvailableAtTick == 0)
    //        {
    //            Log.SanityCheckFailed("Bug found, this should have a value (was 0)");
    //            return 200 + currentFrame;
    //        }

    //        return soonestDependency.BecomesAvailableAtTick;
    //    }

    //    public override void TakeAction(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        this.SubractCosts(UnitData, worldState);

    //        ulong buildTimeFrames = SecondsToFrames((float)WorkerBuildTime.TotalSeconds);
    //        this.Occupies = worldState.GetUnitsOfType(UnitConstants.ResourceCenters).Where(cc => cc.IsBusy == false).ToList()[0];
    //        this.Occupies.IsBusy = true;

    //        this.TakenOnFrame = currentFrame;
    //        this.TakesAffectOnFrame = buildTimeFrames + TakenOnFrame;
    //    }

    //    public override void StartedEffect(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        VirtualUnit newWorker = new VirtualUnit(BotConstants.WorkerUnit);
    //        worldState.Units.Add(newWorker);
    //    }

    //    public override PlannedAction Clone()
    //    {
    //        return (PlannedAction)this.MemberwiseClone();
    //    }
    //}

    ///// <summary>
    ///// Action to build a barracks
    ///// </summary>
    //public class BuildBarracks : PlannedAction
    //{
    //    private readonly TimeSpan RaxBuildTime = TimeSpan.FromSeconds(46);
    //    private UnitTypeData UnitData = GetUnitInfo(UnitId.BARRACKS);

    //    public BuildBarracks()
    //    {
    //        this.Name = this.GetType().Name;
    //    }

    //    public override bool CanTake(VirtualWorldState worldState)
    //    {
    //        if (!this.CanAfford(UnitData, worldState))
    //            return false;

    //        return worldState.GetUnitsOfType(BotConstants.WorkerUnit).Where(unit => unit.IsBusy == false).ToList().Count > 0;
    //    }

    //    public override void TakeAction(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        this.SubractCosts(UnitData, worldState);

    //        ulong buildTimeFrames = SecondsToFrames((float)RaxBuildTime.TotalSeconds);
    //        this.Occupies = worldState.GetUnitsOfType(BotConstants.WorkerUnit).Where(unit => unit.IsBusy == false).ToList()[0];
    //        this.Occupies.IsBusy = true;

    //        this.TakenOnFrame = currentFrame;
    //        this.TakesAffectOnFrame = buildTimeFrames + TakenOnFrame;
    //    }

    //    public override ulong EstimateWhenActionIsAvailable(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        if (!this.CanAfford(UnitData, worldState))
    //            return EstimateWhenWeCanAfford(UnitData, worldState, currentFrame);

    //        var temp = worldState.GetUnitsOfType(BotConstants.WorkerUnit).MinBy(obj => obj.BecomesAvailableAtTick);
    //        VirtualUnit soonestDependency = temp.FirstOrDefault();
    //        if (soonestDependency == null)
    //        {
    //            return (ulong)(BuiltOrderConfig.FramesPerSecond * 4) + currentFrame;
    //        }
    //        if (soonestDependency.BecomesAvailableAtTick == 0)
    //        {
    //            Log.SanityCheckFailed("Bug found, this should have a value (was 0)");
    //            return 200 + currentFrame;
    //        }

    //        return soonestDependency.BecomesAvailableAtTick;
    //    }

    //    public override void StartedEffect(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        VirtualUnit newUnit = new VirtualUnit(UnitId.BARRACKS);
    //        worldState.Units.Add(newUnit);
    //    }

    //    public override PlannedAction Clone()
    //    {
    //        return (PlannedAction)this.MemberwiseClone();
    //    }
    //}



    //public class BuildMarine : PlannedAction
    //{
    //    private readonly TimeSpan BuildTime = TimeSpan.FromSeconds(18);
    //    private UnitTypeData UnitData = GetUnitInfo(UnitId.MARINE);

    //    public BuildMarine()
    //    {
    //        this.Name = this.GetType().Name;
    //    }

    //    public override bool CanTake(VirtualWorldState worldState)
    //    {
    //        if (!this.CanAfford(UnitData, worldState))
    //            return false;

    //        return worldState.GetUnitsOfType(UnitId.BARRACKS).Where(unit => unit.IsBusy == false).ToList().Count > 0;
    //    }

    //    public override void TakeAction(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        this.SubractCosts(UnitData, worldState);

    //        ulong buildTimeFrames = SecondsToFrames((float)BuildTime.TotalSeconds);
    //        this.Occupies = worldState.GetUnitsOfType(UnitId.BARRACKS).Where(unit => unit.IsBusy == false).ToList()[0];
    //        this.Occupies.IsBusy = true;

    //        this.TakenOnFrame = currentFrame;
    //        this.TakesAffectOnFrame = buildTimeFrames + this.TakenOnFrame;
    //    }

    //    public override ulong EstimateWhenActionIsAvailable(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        if (!this.CanAfford(UnitData, worldState))
    //            return EstimateWhenWeCanAfford(UnitData, worldState, currentFrame);

    //        var temp = worldState.GetUnitsOfType(UnitId.BARRACKS).MinBy(obj => obj.BecomesAvailableAtTick);
    //        VirtualUnit soonestDependency = temp.FirstOrDefault();
    //        if (soonestDependency == null)
    //        {
    //            return (ulong)(BuiltOrderConfig.FramesPerSecond * 4) + currentFrame;
    //        }

    //        if (soonestDependency.BecomesAvailableAtTick == 0)
    //        {
    //            Log.SanityCheckFailed("Bug found, this should have a value (was 0)");
    //            return 200 + currentFrame;
    //        }

    //        return soonestDependency.BecomesAvailableAtTick;
    //    }

    //    public override void StartedEffect(VirtualWorldState worldState, ulong currentFrame)
    //    {
    //        VirtualUnit newUnit = new VirtualUnit(UnitId.MARINE);
    //        worldState.Units.Add(newUnit);
    //    }

    //    public override PlannedAction Clone()
    //    {
    //        return (PlannedAction)this.MemberwiseClone();
    //    }
    //}
}
