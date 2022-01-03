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
        public BlinkStalkers()
        {
            /*
                1x pylon
                1x gateway
                1x gas
                1x cyber core
                1x nexus
                2x pylon
                2x gas
                1x stalker från gateway (chrono boost)

                vänta på cyber core
                    välj warp gate tech (chrono boost)

                1x twilight countil
                3x gateways

                vänta på twilight council
                    välj blink tech (chrono boost)

                4x gateways
                Pylon, foward position, ev x2
                Pylon
                3x Gas
             */

            const bool SendWorkerScout = false;

            Unit buildingWorker = null;
            RemainingSteps.Add(new CustomStep(() =>
            {
                // Disable auto-building of Pylons, it is a hardcoded part of our starting build
                BOSSE.HouseProviderManagerRef.Disable();

                // Send a worker to our natural right away, we will build here to start
                buildingWorker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(Globals.MainBaseLocation, true);
                buildingWorker.IsBuilder = true;

                Point2D naturalWallPos = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.GetNaturalWallPosition();
                GeneralGameUtility.Queue(CommandBuilder.MoveAction(new List<Unit> { buildingWorker }, naturalWallPos));
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
            RemainingSteps.Add(new RequireBuilding(UnitId.NEXUS, 2)); // builds expansion

            // Send a worker to scout, might not be necessary until we have better intel-parsing logic
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
            RemainingSteps.Add(new WaitForCondition(() =>
            {
#warning Refactoring TODO: Move upgrade costs somewhere insterad of hard coding here. They do not seem to be available in sc2 files

                // Save resources for warp gate upgrade
                return CurrentMinerals >= 50 && CurrentVespene >= 50;
            }));
            RemainingSteps.Add(new CustomStep(() =>
            {
                // Buy warp tech upgrade
                Unit cyberCore = GeneralGameUtility.GetUnits(UnitId.CYBERNETICS_CORE, onlyCompleted: true, onlyVisible: true).FirstOrDefault();                
                if (cyberCore != null)
                {
                    GeneralGameUtility.Queue(CommandBuilder.UseAbility(AbilityConstants.AbilityId.CYBERNETICSCORERESEARCH_RESEARCHWARPGATE, cyberCore));
                    GeneralGameUtility.ApplyChronoBoostTo(cyberCore);

                    GeneralGameUtility.SubtractCosts(50, 50, 0);
                }
            }));

            RemainingSteps.Add(new RequireBuilding(UnitId.TWILIGHT_COUNSEL, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.GATEWAY, 3));

            RemainingSteps.Add(new WaitForCompletion(UnitId.TWILIGHT_COUNSEL, 1));
            RemainingSteps.Add(new WaitForCondition(() =>
            {
                // Save resources for blink upgrade
                return CurrentMinerals >= 150 && CurrentVespene >= 150;
            }));
            RemainingSteps.Add(new CustomStep(() =>
            {
                // Buy blink upgrade
                Unit twilightCouncil = GeneralGameUtility.GetUnits(UnitId.TWILIGHT_COUNSEL, onlyCompleted: true, onlyVisible: true).FirstOrDefault();
                GeneralGameUtility.Queue(CommandBuilder.UseAbility(AbilityConstants.AbilityId.TWILIGHTCOUNCILRESEARCH_RESEARCHSTALKERTELEPORT, twilightCouncil));

                // Boost out the upgrade
                GeneralGameUtility.ApplyChronoBoostTo(twilightCouncil);
            }));

            RemainingSteps.Add(new RequireBuilding(UnitId.GATEWAY, 4));
            RemainingSteps.Add(new RequireBuilding(UnitId.PYLON, 4));

            //RemainingSteps.Add(new DebugStop());
            RemainingSteps.Add(new RequireBuilding(UnitId.ASSIMILATOR, 3));

            // Build finished
            RemainingSteps.Add(new CustomStep(() =>
            {
                // Re-enable auto-building of Pylons
                BOSSE.HouseProviderManagerRef.Enable();
            }));
        }
    }
}
