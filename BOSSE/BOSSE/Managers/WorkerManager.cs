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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GameUtility;
    using static UnitConstants;
    using static AbilityConstants;
    using System.Linq;

    /// <summary>
    /// Manages worker units
    /// </summary>
    public class WorkerManager
    {
        /// <summary>
        /// Called periodically
        /// </summary>
        public void Tick()
        {
            // Return idle workers to mining
            Unit mineralToReturnTo = GetMineralInMainMineralLine();
            if (mineralToReturnTo == null)
            {
                Log.Warning("Unable to find a mineral to return workers to");
                return;
            }

            List<Unit> workers = GetUnits(UnitId.SCV).Where(p => p.CurrentOrder == null).ToList();
            Queue(CommandBuilder.MineMineralsAction(workers, mineralToReturnTo));

            if (workers.Count > 0)
                Log.Info($"WorkerManager returned {workers.Count} workers to mining");
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

        private Unit GetMineralInMainMineralLine()
        {
            Vector3 posVector = new Vector3(Globals.MainBaseLocation.X, Globals.MainBaseLocation.Y, 0);

            List<Unit> allMinerals = GetUnits(UnitConstants.MineralFields, Alliance.Neutral, false, true);
            foreach (var iter in allMinerals)
            {
                var distance = iter.GetDistance(posVector);
                if (distance < 15)
                    return iter;
            }

            return null;
        }
    }
}
