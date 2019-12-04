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
        /// <summary>
        /// Time that we last picked a new target, 0 = still going to enemy base
        /// </summary>
        private ulong LastTargetFrame = 0;
        private bool IsTimeToGoHome = false;

        private double CurrentDegrees = 0;
        const double StepSize = 0.2f;
        const double Radius = 10;
        const int UpdateFrequencyTicks = 10;

        public ScoutingWorkerController()
        {
            BOSSE.SensorManagerRef.GetSensor(typeof(EnemyArmyUnitDetectedFirstTimeSensor)).AddHandler(ReceiveEventEnemyDetected);
        }

        /// <summary>
        /// Updates squad controller
        /// </summary>
        public override void Tick(MilitaryGoal currentGlobalGoal, Vector3? TargetPoint)
        {
            // We don't care about the military goal provided
            if (this.controlledSquad.AssignedUnits.Count == 0)
                return;
            if (this.controlledSquad.AssignedUnits.Count != 1)
            {
                Log.Warning("ScoutingWorkerController have more than 1 unit assigned, not supported");
            }
            Unit worker = this.controlledSquad.AssignedUnits.First();

            // Check finish condition
            if (IsTimeToGoHome)
            {
                // This terminates the controller
                GoHome();
                return;
            }

            // Move around enmy resource center if we can find it
            List<Unit> candidateLocations = GetUnits(UnitConstants.ResourceCenters, Alliance.Enemy, false, false);
            if (candidateLocations.Count == 0)
            {
                SearchEnemyMainBase(worker);
            }
            else
            {
                ScoutAroundEnemyBase(worker, candidateLocations[0]);
            }
        }

        private void ScoutAroundEnemyBase(Unit worker, Unit enemyResourceCenter)
        {
            if ((Globals.CurrentFrameCount - LastTargetFrame) < UpdateFrequencyTicks)
                return; // Not time to update yet

            Vector3 scoutTargetLocation = PickNextSpotToGo(worker, enemyResourceCenter);
            Queue(CommandBuilder.MoveAction(this.controlledSquad.AssignedUnits, scoutTargetLocation));
            LastTargetFrame = Globals.CurrentFrameCount;
        }

        private Vector3 PickNextSpotToGo(Unit worker, Unit enemyResourceCenter)
        {
            double x = enemyResourceCenter.Position.X + (Radius * Math.Cos(CurrentDegrees));
            double y = enemyResourceCenter.Position.Y + (Radius * Math.Sin(CurrentDegrees));
            CurrentDegrees += StepSize;

            return new Vector3((float)x, (float)y, 0);
        }

        private void GoHome()
        {
            Queue(CommandBuilder.MineMineralsAction(this.controlledSquad.AssignedUnits, GetMineralInMainMineralLine()));
            Log.Info("ScoutingWorkerController - Scout going home");

            foreach (var iter in this.controlledSquad.AssignedUnits)
            {
                iter.IsReserved = false;
            }

            BOSSE.SquadManagerRef.DeleteExistingSquad("ScoutingWorker");
        }

        private void SearchEnemyMainBase(Unit worker)
        {
            if (worker.CurrentOrder.AbilityId == (uint)AbilityId.MOVE)
                return; // Already moving

            Vector3? enemyBaseLoc = GuessEnemyBaseLocation();
            if (enemyBaseLoc == null)
            {
                Log.Info("Unable to find enemy location to scout");
                return;
            }
            Queue(CommandBuilder.MoveAction(this.controlledSquad.AssignedUnits, enemyBaseLoc.Value));
            Log.Bulk("ScoutingWorkerController - Scouting enemy base at = " + enemyBaseLoc.Value);
        }

        private void ReceiveEventEnemyDetected(HashSet<Unit> detectedUnits)
        {
            IsTimeToGoHome = true;
        }
    }
}
