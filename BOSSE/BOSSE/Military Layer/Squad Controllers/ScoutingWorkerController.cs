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
    /// Scouts using an early worker
    /// </summary>
    public class ScoutingWorkerController : SquadControllerBase
    {
        private Vector3? scoutTargetLocation = null;

        /// <summary>
        /// Updates squad controller
        /// </summary>
        public override void Tick(SquadManager.MilitaryGoal currentGlobalGoal, Vector3? TargetPoint)
        {
            // We don't care about the military goal provided
            if (this.controlledSquad.AssignedUnits.Count == 0)
                return;
            if (this.controlledSquad.AssignedUnits.Count != 1)
            {
                Log.Warning("ScoutingWorkerController have more than 1 unit assigned, not supported");
            }
            Unit worker = this.controlledSquad.AssignedUnits.First();

            // Move to enemy base to start
            if (scoutTargetLocation == null)
            {
                Vector3? enemyBaseLoc = GuessEnemyBaseLocation();
                if (enemyBaseLoc == null)
                {
                    Log.Info("Unable to find enemy location to scout");
                    return;
                }
                scoutTargetLocation = enemyBaseLoc.Value;
                Log.Bulk("ScoutingWorkerController - Scouting enemy base at = " + scoutTargetLocation);
            }

            // Pick new spot when we get close
            double distanceToTarget = worker.GetDistance(scoutTargetLocation.Value);
            if (distanceToTarget < 17)
            {
                int xDiff = Globals.Random.Next(-25, 25);
                int yDiff = Globals.Random.Next(-25, 25);

                scoutTargetLocation = new Vector3(worker.Position.X + xDiff, worker.Position.Y + yDiff, worker.Position.Z);
                Log.Bulk("ScoutingWorkerController - New scout target = " + scoutTargetLocation);
            }

            // Move towards location
            Queue(CommandBuilder.MoveAction(this.controlledSquad.AssignedUnits, scoutTargetLocation.Value));
        }
    }
}
