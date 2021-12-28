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
    /// Scouts using an early worker
    /// </summary>
    public class ScoutingWorkerController : SquadControllerBase
    {
        /// <summary>
        /// Time that we last picked a new target, 0 = still going to enemy base
        /// </summary>
        private ulong LastTargetFrame = 0;
        private bool IsTimeToGoHome = false;
        private bool HasScoutedNatural = false;
        private int TicksUpdated = 0;

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
        public override void Tick(MilitaryGoal currentGlobalGoal, Point2D targetPoint)
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
                // Take two loops around enemy base, then scout natural location, then loop again until we see an enemy army unit
                if (TicksUpdated >= 10 && (!HasScoutedNatural))
                {
                    ScoutNatural(worker);
                }
                else
                {
                    ScoutAroundEnemyBase(worker, candidateLocations[0]);
                }
            }
        }

        private void ScoutNatural(Unit worker)
        {
            Point2D location = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.EnemyNaturalExpansion.GetMineralCenter();

            if (worker.Position.IsWithinRange(location, 3))
            {
                Log.Bulk("Scout completed natural scouting");
                HasScoutedNatural = true;
                return;
            }

            Queue(CommandBuilder.MoveAction(this.controlledSquad.AssignedUnits, location));
        }

        private void ScoutAroundEnemyBase(Unit worker, Unit enemyResourceCenter)
        {
            if ((Globals.CurrentFrameIndex - LastTargetFrame) < UpdateFrequencyTicks)
                return; // Not time to update yet

            TicksUpdated++;
            Point2D scoutTargetLocation = PickNextSpotToGo(worker, enemyResourceCenter);
            Queue(CommandBuilder.MoveAction(this.controlledSquad.AssignedUnits, scoutTargetLocation));
            LastTargetFrame = Globals.CurrentFrameIndex;
        }

        private Point2D PickNextSpotToGo(Unit worker, Unit enemyResourceCenter)
        {
            double x = enemyResourceCenter.Position.X + (Radius * Math.Cos(CurrentDegrees));
            double y = enemyResourceCenter.Position.Y + (Radius * Math.Sin(CurrentDegrees));
            CurrentDegrees += StepSize;

            return new Point2D((float)x, (float)y);
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

            Point2D enemyBaseLoc = GuessEnemyBaseLocation();
            if (enemyBaseLoc == null)
            {
                Log.Info("Unable to find enemy location to scout");
                return;
            }
            Queue(CommandBuilder.MoveAction(this.controlledSquad.AssignedUnits, enemyBaseLoc));
            Log.Bulk("ScoutingWorkerController - Scouting enemy base at = " + enemyBaseLoc);
        }

        private void ReceiveEventEnemyDetected(HashSet<Unit> detectedUnits)
        {
            IsTimeToGoHome = true;
        }
    }
}
