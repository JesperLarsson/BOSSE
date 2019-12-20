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

        public void SetNumberOfWorkersOnGas(int numberOfWorkersOnGas)
        {
            if (numberOfWorkersOnGas == this.RequestedWorkersOnGas)
                return;

            Log.Info($"WorkerManager - Changed number of workers on gas to {numberOfWorkersOnGas} (was {this.RequestedWorkersOnGas})");
            this.RequestedWorkersOnGas = numberOfWorkersOnGas;
        }

        /// <summary>
        /// Called periodically
        /// </summary>
        public override void OnFrameTick()
        {
            TrainWorkersIfNecessary();
            ReturnIdleWorkersToMining();

            ResolveGasNeeds();
            AssignWorkersToGasExtractors();
        }

        /// <summary>
        /// Returns a single worker close to the given point which can be used for a new job
        /// </summary>
        public Unit RequestWorkerForJobCloseToPointOrNull(Point2D point)
        {
            var result = RequestWorkersForJobCloseToPointOrNull(point, 1);
            if (result == null || result.Count == 0)
                return null;

            return result[0];
        }

        /// <summary>
        /// Returns a multiple workers close to the given point which can be used for a new job
        /// Can return a partial amount, null = no workers found
        /// </summary>
        public List<Unit> RequestWorkersForJobCloseToPointOrNull(Point2D point, int maxWorkerCount)
        {
            List<Unit> workers = GetUnits(UnitId.SCV);

            // Sort by distance to point
            workers.Sort((a, b) => a.Position.DistanceSquared(point).CompareTo(b.Position.DistanceSquared(point)));

            List<Unit> matchedWorkers = new List<Unit>();
            foreach (Unit worker in workers)
            {
                // Is worker suitable?
                if (worker.CurrentOrder == null)
                {
                    matchedWorkers.Add(worker);
                }
                else if (worker.CurrentOrder.AbilityId == (uint)AbilityId.GATHER_MINERALS)
                {
                    matchedWorkers.Add(worker);
                }

                if (matchedWorkers.Count >= maxWorkerCount)
                    break;
            }

            if (matchedWorkers.Count == 0)
                return null;

            return matchedWorkers;
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

        private void AssignWorkersToGasExtractors()
        {
            List<Unit> extractors = GetUnits(UnitId.REFINERY, onlyCompleted: true);
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

                    List<Unit> workers = GetUnits(UnitId.SCV, onlyCompleted: true);
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
                        List<Unit> workers = GetUnits(UnitId.SCV, onlyCompleted: true);
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

                    List<Unit> workersToExtractor = RequestWorkersForJobCloseToPointOrNull(extractor.Position, workersToPutOnExtractor);
                    if (workersToExtractor == null)
                        continue;

                    Queue(CommandBuilder.MineMineralsAction(workersToExtractor, extractor));
                    Log.Info($"Workermanager - Put {workersToExtractor.Count} workers on gas " + extractor.Tag);
                }
            }
        }

        private void BuildNewGasExtractors(int numberOfExtractors)
        {
            UnitTypeData extractorInfo = GetUnitInfo(UnitId.REFINERY);
            List<Unit> gasGeysers = GetUnits(UnitConstants.GasGeysers, Alliance.Neutral, false, true);

            for (int i = 0; i < gasGeysers.Count && i < numberOfExtractors && CurrentMinerals >= extractorInfo.MineralCost; i++)
            {
                Unit geyser = gasGeysers[i];

                if (!geyser.Position.IsWithinRange(Globals.MainBaseLocation, 20))
                    continue;
                if (geyser.IsReserved)
                    continue;

                Unit worker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(geyser.Position);
                if (worker == null)
                {
                    Log.Warning($"Unable to find a worker to construct gas geyser near " + geyser.Position.ToString2());
                    return;
                }

                Log.Info($"WorkerManager - Building new gas extractor at " + geyser.Position.ToString2() + ", geyser = " + geyser.Tag);
                geyser.IsReserved = true;
                Queue(CommandBuilder.ConstructActionOnTarget(UnitId.REFINERY, worker, geyser));
                CurrentMinerals -= extractorInfo.MineralCost;
                this.extractorCount++;
            }
        }

        private void TrainWorkersIfNecessary()
        {
            if (!this.AllowWorkerTraining)
                return;

            List<Unit> commandCenters = GetUnits(UnitConstants.ResourceCenters, onlyCompleted: true);
            UnitTypeData workerInfo = GetUnitInfo(UnitId.SCV);
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
                    Queue(CommandBuilder.TrainAction(cc, UnitConstants.UnitId.SCV));
                }
            }
        }

        private void ReturnIdleWorkersToMining()
        {
            Unit mineralToReturnTo = GetMineralInMainMineralLine();
            if (mineralToReturnTo == null)
            {
                Log.Warning("Unable to find a mineral to return workers to");
                return;
            }

            List<Unit> workers = GetUnits(UnitId.SCV).Where(p => p.CurrentOrder == null && p.IsReserved == false).ToList();
            Queue(CommandBuilder.MineMineralsAction(workers, mineralToReturnTo));

            if (workers.Count > 0)
            {
                Log.Info($"WorkerManager - Returned {workers.Count} workers to mining");
            }
        }
    }
}
