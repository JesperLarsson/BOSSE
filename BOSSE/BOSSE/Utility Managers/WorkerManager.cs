/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2022 Jesper Larsson

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
namespace BOSSE
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Linq;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Manages worker units
    /// </summary>
    public class WorkerManager : Manager
    {
        private bool AllowWorkerTraining = true;
        private bool AllowWorkerOverProduction = false;
        private int RequestedWorkersOnGas = 0;
        private uint extractorCount = 0;
        //private bool SkipNextFrame = false;

        public override void Initialize()
        {
            // Subscribe to extractors being destroyed
#warning LP_TODO: Needs OwnStructureWasDestroyedSensor
            //BOSSE.SensorManagerRef.GetSensor(typeof(OwnStructureWasDestroyedSensor)).AddHandler(new SensorEventHandler(delegate (HashSet<Unit> affectedUnits)
            //{
            //    extractorCount--;
            //    Log.Info("Extractor destroyed, updated count = " + extractorCount);
            //}), unfilteredList => new HashSet<Unit>(unfilteredList.Where(unitIter => unitIter.UnitType == UnitId.REFINERY)));
        }

        public void SetWorkerTrainingAllowed(bool isAllowed)
        {
            if (isAllowed == this.AllowWorkerTraining)
                return;

            if (isAllowed)
            {
                Log.Info("WorkerManager - Allowing workers to be trained again");
            }
            else
            {
                Log.Info("WorkerManager - No longer allowing workers to be trained");
            }
            AllowWorkerTraining = isAllowed;
        }

        public override void OnFrameTick()
        {
            //if (this.SkipNextFrame)
            //{
            //    this.SkipNextFrame = false;
            //    return;
            //}

            TrainWorkersIfNecessary();
            ResolveGasNeeds();
            ReturnWorkersFromBuildingAssimilators();
            AssignWorkersToGasExtractors();

            // If we have idle workers, assign them to their closest base for now, we will then balance on the next tick if necessary
            if (ReturnIdleWorkers() == false)
            {
                BalanceWorkersBetweenBases();
            }
        }

        //public override void OnFirstFrame()
        //{
        //    // We should avoid running our general logic on the first frame, since we give new orders here
        //    this.SkipNextFrame = true;

        //    InitialWorkerSplit();
        //}

        ///// <summary>
        ///// Performs an optimal-ish split of workers when the game starts
        ///// </summary>
        //public void InitialWorkerSplit()
        //{

        //}

        /// <summary>
        /// Returns a single worker close to the given point which can be used for a new job
        /// </summary>
        public Unit RequestWorkerForJobCloseToPointOrNull(Point2D point, bool isBuildingJob)
        {
            var result = RequestWorkersForJobCloseToPointOrNull(point, 1, isBuildingJob);
            if (result == null || result.Count == 0)
                return null;

            return result[0];
        }

        /// <summary>
        /// Returns a multiple workers close to the given point which can be used for a new job
        /// Can return a partial amount, null = no workers found
        /// </summary>
        public List<Unit> RequestWorkersForJobCloseToPointOrNull(Point2D point, int maxWorkerCount, bool isBuildingJob)
        {
            List<Unit> workers = GetUnits(RaceWorkerUnitType());

            // Sort by distance to point
            workers.Sort((a, b) => a.Position.AirDistanceSquared(point).CompareTo(b.Position.AirDistanceSquared(point)));

            List<Unit> matchedWorkers = new List<Unit>();
            foreach (Unit worker in workers)
            {
                // Is worker suitable?
                if (worker.HasNewOrders)
                    continue;
                if (worker.IsReserved)
                    continue;
                if (worker.IsBuilder && (isBuildingJob == false))
                    continue;

                if (worker.CurrentOrder == null)
                    matchedWorkers.Add(worker);
                else if (HarvestGatherAbilities.Contains((AbilityId)worker.CurrentOrder.AbilityId))
                    matchedWorkers.Add(worker);

                if (matchedWorkers.Count >= maxWorkerCount)
                    break;
            }

            if (matchedWorkers.Count == 0)
                return null;

            return matchedWorkers;
        }

        #region Gas Handling

        public void SetNumberOfWorkersOnGas(int numberOfWorkersOnGas)
        {
            if (numberOfWorkersOnGas == this.RequestedWorkersOnGas)
                return;

            Log.Info($"WorkerManager - Changed number of workers on gas to {numberOfWorkersOnGas} (was {this.RequestedWorkersOnGas})");
            this.RequestedWorkersOnGas = numberOfWorkersOnGas;
        }

        private void ResolveGasNeeds()
        {
            if (this.RequestedWorkersOnGas <= 0)
                return;

            const float maxWorkersPerExtractor = 3;
            uint extractorsNecessary = (uint)Math.Ceiling((((float)RequestedWorkersOnGas) / maxWorkersPerExtractor));

            if (extractorsNecessary > this.extractorCount)
            {
                BuildNewGasExtractors((int)(extractorsNecessary - this.extractorCount));
            }
        }

        /// <summary>
        /// After assimilator is build, the worker will stand idle by and wait for it to finish. This will move them back to mining minerals instead
        /// </summary>
        private void ReturnWorkersFromBuildingAssimilators()
        {
            if (BOSSE.UseRace != Race.Protoss)
                return;

            List<Unit> extractors = GetUnits(RaceGasExtractor());
            if (extractors.Count == 0)
                return;

            UnitId workerType = RaceWorkerUnitType();
            List<Unit> workersToReturn = new List<Unit>();
            foreach (Unit extractorIter in extractors)
            {
                if (extractorIter.BuildProgress >= 0.8)
                    continue;

                foreach (Unit workerIter in Unit.AllUnitInstances.Values)
                {
                    if (workerIter.UnitType != workerType)
                        continue;
                    if (workerIter.CurrentOrder == null)
                        continue;

                    if (workerIter.CurrentOrder.TargetUnitTag == extractorIter.Tag)
                        workersToReturn.Add(workerIter);
                }
            }

            Queue(CommandBuilder.MineMineralsAction(workersToReturn, GetMineralInMainMineralLine()));
        }

        private void AssignWorkersToGasExtractors()
        {
            List<Unit> extractors = GetUnits(RaceGasExtractor());
            if (extractors.Count == 0)
                return;

            // Count workers assigned on all gases
            int currentWorkersAssigned = 0;
            foreach (Unit iter in extractors)
            {
                currentWorkersAssigned += iter.AssignedWorkers;
            }

            // Determine action
            if (currentWorkersAssigned == this.RequestedWorkersOnGas)
            {
                return;
            }
            else if (currentWorkersAssigned >= this.RequestedWorkersOnGas)
            {
                // Return workers to mining if we have too many
                int workerCountToReturn = currentWorkersAssigned - this.RequestedWorkersOnGas;
                List<Unit> workersToMoveOffGas = new List<Unit>();

                // Make sure extractors are at their ideal count first
                foreach (Unit extractor in extractors)
                {
                    if (extractor.AssignedWorkers <= extractor.IdealWorkers)
                        continue;

                    int nonIdealWorkerCount = extractor.IdealWorkers - extractor.AssignedWorkers;

                    List<Unit> workers = GetUnits(RaceWorkerUnitType(), onlyCompleted: true);
                    int movedWorkerCount = 0;
                    foreach (Unit workerIter in workers)
                    {
                        if (workerIter.CurrentOrder == null)
                            continue;
                        if (workerIter.CurrentOrder.TargetUnitTag != extractor.Tag)
                            continue;

                        workersToMoveOffGas.Add(workerIter);
                        movedWorkerCount++;

                        if (movedWorkerCount >= workerCountToReturn)
                            break;
                        if (movedWorkerCount >= nonIdealWorkerCount)
                            break;
                    }
                }

                // Move more workers if necessary (ie we reduced the number of workers allowed)
                if (workerCountToReturn > workersToMoveOffGas.Count)
                {
                    foreach (Unit extractor in extractors)
                    {
                        List<Unit> workers = GetUnits(RaceWorkerUnitType(), onlyCompleted: true);
                        int movedWorkerCount = 0;
                        foreach (Unit workerIter in workers)
                        {
                            if (workerIter.CurrentOrder == null)
                                continue;
                            if (workerIter.CurrentOrder.TargetUnitTag != extractor.Tag)
                                continue;

                            workersToMoveOffGas.Add(workerIter);
                            movedWorkerCount++;

                            if (movedWorkerCount >= workerCountToReturn)
                                break;
                        }
                    }
                }

                if (workersToMoveOffGas.Count > 0)
                {
                    Log.Info($"WorkerManager - Moving {workersToMoveOffGas.Count} workers from gas to mining minerals");
                    Queue(CommandBuilder.MineMineralsAction(workersToMoveOffGas, GetMineralInMainMineralLine()));
                }
            }
            else
            {
                // Assign workers to work on gas
                int workersNeeded = this.RequestedWorkersOnGas - currentWorkersAssigned;

                foreach (Unit extractor in extractors)
                {
                    if (extractor.AssignedWorkers >= extractor.IdealWorkers)
                        continue;

                    int workersToPutOnExtractor = extractor.IdealWorkers - extractor.AssignedWorkers;
                    if (workersNeeded < workersToPutOnExtractor)
                    {
                        workersToPutOnExtractor = workersNeeded;
                    }
                    workersNeeded -= workersToPutOnExtractor;
                    if (workersToPutOnExtractor == 0)
                        continue;

                    List<Unit> workersToExtractor = RequestWorkersForJobCloseToPointOrNull(extractor.Position, workersToPutOnExtractor, false);
                    if (workersToExtractor == null)
                        continue;

                    Queue(CommandBuilder.MineMineralsAction(workersToExtractor, extractor));
                    Log.Info($"Workermanager - Put {workersToExtractor.Count} workers on gas " + extractor.Tag);
                }
            }
        }

        public bool BuildNewGasExtractors(int numberOfExtractors)
        {
            UnitTypeData extractorInfo = GetUnitInfo(RaceGasExtractor());
            List<Unit> gasGeysers = GetUnits(UnitConstants.GasGeysers, Alliance.Neutral, false, true);

            bool success = false;
            for (int i = 0; i < gasGeysers.Count && i < numberOfExtractors && CurrentMinerals >= extractorInfo.MineralCost; i++)
            {
                Unit geyser = gasGeysers[i];

                if (!geyser.Position.IsWithinRange(Globals.MainBaseLocation, 20))
                    continue; // should also cover our natural
                if (geyser.IsReserved)
                    continue;
                if (CanAfford(RaceGasExtractor()) == false)
                    return false;

                Unit worker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(geyser.Position, true);
                if (worker == null)
                {
                    Log.Warning($"Unable to find a worker to construct gas geyser near " + geyser.Position.ToString2());
                    return false;
                }

                Log.Info($"WorkerManager - Building new gas extractor at " + geyser.Position.ToString2() + ", geyser = " + geyser.Tag);
                geyser.IsReserved = true;
                Queue(CommandBuilder.ConstructActionOnTarget(RaceGasExtractor(), worker, geyser));
                CurrentMinerals -= extractorInfo.MineralCost;
                this.extractorCount++;
                success = true;
            }

            return success;
        }

        #endregion

        private void TrainWorkersIfNecessary()
        {
            if (!this.AllowWorkerTraining)
                return;

            List<Unit> commandCenters = GetUnits(UnitConstants.ResourceCenters, onlyCompleted: true);
            UnitTypeData workerInfo = GetUnitInfo(RaceWorkerUnitType());
            foreach (Unit cc in commandCenters)
            {
                if (cc.CurrentOrder != null)
                    continue;

                int idealWorkerCount = cc.IdealWorkers;
                if (AllowWorkerOverProduction)
                    idealWorkerCount = (int)(idealWorkerCount * 1.5f);

                if (cc.AssignedWorkers >= idealWorkerCount)
                    continue;

                if (CurrentMinerals >= workerInfo.MineralCost && FreeSupply >= workerInfo.FoodRequired)
                {
                    Queue(CommandBuilder.TrainActionAndSubtractCosts(cc, RaceWorkerUnitType()));
                }
            }
        }

        /// <summary>
        /// Splits our work force between bases. Returns true if balancing was performed
        /// </summary>
        private bool BalanceWorkersBetweenBases()
        {
            List<Unit> miningWorkersGlobal = GetUnits(RaceWorkerUnitType(), onlyCompleted: true, alliance: Alliance.Self).Where(unit => unit.CurrentOrder != null && HarvestAbilities.Contains((AbilityId)unit.CurrentOrder.AbilityId)).ToList();
            List<BaseLocation> basesToMineMainFirst = BOSSE.BaseManagerRef.GetOwnBases().Where(obj => obj.OwnBaseReadyToAcceptWorkers && (!obj.WorkerTransferInProgress) && (!obj.IsHiddenBase) && obj.CommandCenterRef.Integrity > 0.95f).ToList();
            if (basesToMineMainFirst.Count == 0)
            {
                //Log.Warning("Can't balance workers without a base that wants workers"); // comented out to avoid log spam when losing
                return false;
            }
            if (basesToMineMainFirst.Count == 1)
            {
                // Nothing to balance with just one base
                return false;
            }

            // Fill bases from most recent built first in order to preserve minerals
            List<BaseLocation> basesToMineReverse = new List<BaseLocation>(basesToMineMainFirst);
            basesToMineReverse.Reverse();
            Dictionary<BaseLocation, int> workerRequest = new Dictionary<BaseLocation, int>(); // Requested amount of workers, positive = need that amount
            const int StaticWorkerDiff = 4; // the time it takes to transfer is also important, so we have the time to build workers at expansions etc 
            foreach (BaseLocation baseIter in basesToMineReverse)
            {
                //List<Unit> workersMiningThisBase = miningWorkersGlobal.Where(worker => baseIter.CenteredAroundCluster.MineralFields.Contains(Unit.AllUnitInstances[worker.CurrentOrder.TargetUnitTag])).ToList();
                int count = baseIter.CommandCenterRef.IdealWorkers - baseIter.CommandCenterRef.AssignedWorkers - StaticWorkerDiff;
                workerRequest[baseIter] = count;
            }

            Dictionary<BaseLocation, List<Unit>> newMiningTargets = new Dictionary<BaseLocation, List<Unit>>(); // target base => units to move there
            HashSet<ulong> usedWorkers = new HashSet<ulong>();
            foreach (BaseLocation toIter in basesToMineReverse)
            {
                int workerCountRequest = workerRequest[toIter];

                // Find a specific set of workers to transfer here
                List<Unit> workersToTransfer = new List<Unit>();
                foreach (BaseLocation fromIter in basesToMineMainFirst)
                {
                    if (workerCountRequest <= 0)
                        break;
                    if (fromIter == toIter)
                        continue;

                    List<Unit> workersMiningFromBase = miningWorkersGlobal.Where(
                        // Do not use workers which have already received an order this tick
                        worker => (!worker.HasNewOrders) &&
                        (!usedWorkers.Contains(worker.Tag)) &&
                        // Either no order
                        worker.CurrentOrder == null || (
                            // Or order targets one of our mineral fields, not returning cargo (no target)
                            worker.CurrentOrder != null &&
                            HarvestAbilities.Contains((AbilityId)worker.CurrentOrder.AbilityId) &&
                                (worker.CurrentOrder.TargetUnitTag == 0 || fromIter.CenteredAroundCluster.MineralFields.Contains(Unit.AllUnitInstances[worker.CurrentOrder.TargetUnitTag])))
                    ).ToList();

                    for (int i = 0; i < workerCountRequest && i < workersMiningFromBase.Count; i++)
                    {
                        Unit worker = workersMiningFromBase[i];
                        workersToTransfer.Add(worker);
                        usedWorkers.Add(worker.Tag);
                    }
                    workerCountRequest -= workersMiningFromBase.Count;
                }

                if (workersToTransfer.Count <= 0)
                    continue;

                newMiningTargets[toIter] = workersToTransfer;
            }

            // Order workers to move
            foreach (BaseLocation targetBase in newMiningTargets.Keys)
            {
                if (targetBase.CenteredAroundCluster == null || targetBase.CenteredAroundCluster.MineralFields == null || targetBase.CenteredAroundCluster.MineralFields.Count == 0)
                {
                    Log.SanityCheckFailed("Unable to move workers to new resource cluster");
                    continue;
                }

                List<Unit> workersToMoveHere = newMiningTargets[targetBase];
                List<Unit> targetMinerals = targetBase.CenteredAroundCluster.MineralFields.ToList();

                Unit mineralPatch = targetMinerals.First();
                for (int i = 0; i < workersToMoveHere.Count; i++)
                {
                    Unit worker = workersToMoveHere[i];

#warning TODO Debug: For some reason the mine action doesn't work here
                    Queue(CommandBuilder.MoveAction(new List<Unit>() { worker }, mineralPatch.Position));
                    //Queue(CommandBuilder.UseAbility(AbilityId.STOP, worker));
                    //Queue(CommandBuilder.MineMineralsAction(new List<Unit>() { worker }, mineralPatch), true);
                }

                // Set up a continuous check that checks for when the workers arrive
                HashSet<Unit> arrivedWorkers = new HashSet<Unit>();
                ulong workerTransferCompletedStartFrame = 0;
                ContinuousUnitOrder transferFinishedOrder = new ContinuousUnitOrder(delegate ()
                {
                    // We need a small hysteresis to allow workers to actually be assigned to mining after they arrive
                    const int WorkerTransferHystFrameCount = 100;
                    foreach (Unit worker in workersToMoveHere)
                    {
                        if (arrivedWorkers.Contains(worker))
                            continue;

                        if (worker.Position.IsWithinRange(mineralPatch.Position, 4))
                        {
                            worker.HasNewOrders = true;
                            Queue(CommandBuilder.MineMineralsAction(new List<Unit>() { worker }, mineralPatch), true);
                            arrivedWorkers.Add(worker);
                        }
                        else if (worker.Integrity < 0.5)
                        {
                            continue; // skip dying workers, they might get sniped off so we don't want to wait for them
                        }

                        return false;
                    }

                    if (workerTransferCompletedStartFrame == 0)
                    {
                        workerTransferCompletedStartFrame = Globals.OnCurrentFrame;
                        return false;
                    }
                    else if (Globals.OnCurrentFrame - workerTransferCompletedStartFrame > WorkerTransferHystFrameCount)
                    {
                        Log.Info("Worker transfer completed");
                        targetBase.WorkerTransferInProgress = false;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
                Log.Bulk("Transferring " + workersToMoveHere.Count + " workers to base " + targetBase);
                BOSSE.OrderManagerRef.AddOrder(transferFinishedOrder);

                targetBase.WorkerTransferInProgress = true;
            }

            if (newMiningTargets.Count > 0)
            {
                Log.Bulk("Updated worker balance for " + newMiningTargets.Count + " bases");
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ReturnIdleWorkers()
        {
            List<Unit> idleWorkers = GetUnits(RaceWorkerUnitType(), onlyCompleted: true).Where(unit => unit.CurrentOrder == null && unit.IsReserved == false && unit.HasNewOrders == false && unit.IsBuilder == false).ToList();
            if (idleWorkers.Count == 0)
                return false;

            List<Unit> commandCenters = GetUnits(RaceCommandCenterUnitType(), onlyCompleted: true);
            if (commandCenters.Count == 0)
                return false;

            List<Unit> allMineralFields = GetUnits(UnitConstants.MineralFields, alliance: Alliance.Neutral);
            if (allMineralFields.Count == 0)
                return false;

            //// CC instance => workers to send there
            //Dictionary<Unit, List<Unit>> ccToWorkerGroup = new Dictionary<Unit, List<Unit>>();
            //foreach (Unit idleWorkerIter in idleWorkers)
            //{
            //    // Find the CC which is closest to the worker
            //    commandCenters.Sort((a, b) => a.Position.AirDistanceSquared(idleWorkerIter.Position).CompareTo(b.Position.AirDistanceSquared(idleWorkerIter.Position)));
            //    Unit closestCommandCenter = commandCenters[0];

            //    if (ccToWorkerGroup.ContainsKey(closestCommandCenter) == false)
            //        ccToWorkerGroup[closestCommandCenter] = new List<Unit>();

            //    ccToWorkerGroup[closestCommandCenter].Add(idleWorkerIter);
            //}

            //foreach (var iter in ccToWorkerGroup)
            //{
            //    Unit commandCenter = iter.Key;
            //    List<Unit> workersToSendToThisCC = iter.Value;

            //    List<Unit> mineralFieldsNearCC = allMineralFields.Where( o => o.Position.AirDistanceAbsolute(commandCenter.Position) <= 8 ).ToList();
            //    mineralFieldsNearCC = mineralFieldsNearCC.OrderBy(o => o.Position.AirDistanceAbsolute(commandCenter.Position)).ToList();
            //}



            foreach (Unit idleWorkerIter in idleWorkers)
            {
                // Sort by distance to worker
                commandCenters.Sort((a, b) => a.Position.AirDistanceSquared(idleWorkerIter.Position).CompareTo(b.Position.AirDistanceSquared(idleWorkerIter.Position)));
                Unit closestCommandCenter = commandCenters[0];

                // Sort by mineral distance to CC
                List<Unit> allMinerals = GetUnits(UnitConstants.MineralFields, alliance: Alliance.Neutral);
                if (allMinerals.Count == 0)
                    continue;

                allMinerals.Sort((a, b) => a.Position.AirDistanceSquared(closestCommandCenter.Position).CompareTo(b.Position.AirDistanceSquared(closestCommandCenter.Position)));
                Unit mineralToMine = allMinerals[0];

                Log.Info("Sending idle worker " + idleWorkerIter + " to mine mineral patch " + mineralToMine);
                Queue(CommandBuilder.MineMineralsAction(new List<Unit> { idleWorkerIter }, mineralToMine));
            }

            return true;
        }
    }
}
