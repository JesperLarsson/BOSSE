/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
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
    using static GameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Manages worker units
    /// </summary>
    public class WorkerManager : Manager
    {
        public override void Initialize()
        {

        }

        /// <summary>
        /// Called periodically
        /// </summary>
        public override void OnFrameTick()
        {
            ReturnIdleWorkersToMining();
        }

        /// <summary>
        /// Returns a single worker close to the given point which can be used for a new job
        /// </summary>
        public Unit RequestWorkerForJobCloseToPoint(Vector3 point)
        {
            List<Unit> workers = GetUnits(UnitId.SCV);

            // Sort by distance to point
            workers.Sort((a, b) => a.GetDistance(point).CompareTo(b.GetDistance(point)));

            foreach (Unit worker in workers)
            {
                // Is worker suitable?
                if (worker.CurrentOrder == null)
                {
                    return worker;
                }
                if (worker.CurrentOrder.AbilityId == (uint)AbilityId.GATHER_MINERALS)
                {
                    return worker;
                }
            }

            return null;
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
                Log.Info($"WorkerManager returned {workers.Count} workers to mining");
        }

        
    }
}
