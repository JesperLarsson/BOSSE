﻿/*
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

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using System.Linq;

    /// <summary>
    /// Basic 2 base build order, focusing on producing blink stalkers
    /// Loosely based on https://www.youtube.com/watch?v=aPPHx7GAVUo
    /// </summary>
    public class BlinkStalkers : BuildOrder
    {
        /// <summary>
        /// Determines whether we will scout mid build
        /// Might not be necessary until we have better intel-parsing logic which can act on the information given
        /// </summary>
        private const bool SendWorkerScout = false;

        public BlinkStalkers()
        {
            Unit buildingWorker = null;
            RemainingSteps.Add(new CustomStep(() =>
            {
                // Disable auto-building of Pylons and army units, it is a hardcoded part of our starting build
                BOSSE.HouseProviderManagerRef.Disable();
                BOSSE.ArmyBuilderManagerRef.Disable();

                // Build stalkers only
                BOSSE.ArmyBuilderManagerRef.StartBuildingUnit(UnitId.STALKER);

                // Send a worker to our natural right away, we will build here to start
                if (BotConstants.EnableWalling)
                {
                    buildingWorker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(Globals.MainBaseLocation, true);
                    buildingWorker.IsBuilder = true;

                    Point2D naturalWallPos = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.GetNaturalWallPosition();
                    GeneralGameUtility.Queue(CommandBuilder.MoveAction(new List<Unit> { buildingWorker }, naturalWallPos));
                }
            }));

            RemainingSteps.Add(new RequireBuilding(UnitId.PYLON, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.GATEWAY, 1));

            // Assign workers to gas
            RemainingSteps.Add(new RequireBuilding(UnitId.ASSIMILATOR, 1));
            RemainingSteps.Add(new WaitForCompletion(UnitId.ASSIMILATOR, 1));
            RemainingSteps.Add(new CustomStep(() =>
            {
                BOSSE.WorkerManagerRef.SetNumberOfWorkersOnGas(3);
            }));

            RemainingSteps.Add(new RequireBuilding(UnitId.CYBERNETICS_CORE, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.NEXUS, 2)); // build expansion

            // Send a worker to scout
            if (SendWorkerScout)
            {
                RemainingSteps.Add(new CustomStep(() =>
                {
                    Unit scoutingWorker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(Globals.MainBaseLocation, false);
                    if (scoutingWorker == null)
                    {
                        Log.Warning("Unable to find a worker to use as scout");
                        return;
                    }
                    Log.Info("Assigning worker " + scoutingWorker.Tag + " as scout");

                    scoutingWorker.IsReserved = true;
                    BOSSE.SquadManagerRef.AddNewSquad(new Squad("ScoutingWorker", new ScoutingWorkerController()));
                    BOSSE.SquadManagerRef.GetSquadOrNull("ScoutingWorker").AddUnit(scoutingWorker);
                }));
            }

            //RemainingSteps.Add(new DebugStop());
            RemainingSteps.Add(new RequireBuilding(UnitId.PYLON, 2));

            // Move back builder to be a normal worker
            RemainingSteps.Add(new CustomStep(() =>
            {
                if (buildingWorker != null)
                    buildingWorker.IsBuilder = false;
                buildingWorker = null;
            }));

            RemainingSteps.Add(new RequireBuilding(UnitId.ASSIMILATOR, 2));
            RemainingSteps.Add(new RequireUnit(UnitId.STALKER, 1) { AllowChronoBoost = true });

            // Assign workers to gas nr 2
            RemainingSteps.Add(new WaitForCompletion(UnitId.ASSIMILATOR, 2));
            RemainingSteps.Add(new CustomStep(() =>
            {
                BOSSE.WorkerManagerRef.SetNumberOfWorkersOnGas(6);
            }));

            RemainingSteps.Add(new WaitForCompletion(UnitId.CYBERNETICS_CORE, 1));
            RemainingSteps.Add(new RequireUpgradeStep(AbilityConstants.AbilityId.CYBERNETICSCORERESEARCH_RESEARCHWARPGATE, true));

            RemainingSteps.Add(new RequireBuilding(UnitId.TWILIGHT_COUNSEL, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.GATEWAY, 3));
            RemainingSteps.Add(new WaitForCompletion(UnitId.TWILIGHT_COUNSEL, 1));
            RemainingSteps.Add(new RequireUpgradeStep(AbilityConstants.AbilityId.TWILIGHTCOUNCILRESEARCH_RESEARCHSTALKERTELEPORT, true));

            RemainingSteps.Add(new CustomStep(() =>
            {
                // Re-enable auto-building of army units
                BOSSE.ArmyBuilderManagerRef.Enable();
            }));

            RemainingSteps.Add(new RequireBuilding(UnitId.GATEWAY, 4));
            RemainingSteps.Add(new RequireBuilding(UnitId.PYLON, 4));
            RemainingSteps.Add(new RequireBuilding(UnitId.ASSIMILATOR, 3));

            RemainingSteps.Add(new CustomStep(() =>
            {
                // Re-enable auto-building of pylons
                BOSSE.HouseProviderManagerRef.Enable();
            }));
        }
    }
}
