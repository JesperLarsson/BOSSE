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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Bot top level class
    /// Receives input from the main loop and outputs sc2 actions in <see cref="GameOutput"/>
    /// </summary>
    public class BOSSE
    {
        // Input managers
        public static readonly SensorManager SensorManagerRef = new SensorManager();

        // Strategic managers
        public static readonly DiscrepenceyDetector DiscrepenceyDetectorRef = new DiscrepenceyDetector();
        public static readonly GoalFormulator GoalFormulatorRef = new GoalFormulator();
        public static readonly StrategicGoalManager StrategicGoalRef = new StrategicGoalManager();
        public static readonly StrategicGoalExecutor GoalExecutorRef = new StrategicGoalExecutor();

        // Military / tactical managers
        public static readonly TacticalGoalManager TacticalGoalRef = new TacticalGoalManager();
        public static readonly SquadManager SquadManagerRef = new SquadManager();

        // Utility managers
        public static readonly WorkerManager WorkerManagerRef = new WorkerManager();
        public static readonly OrbitalCommandManager OrbitalCommandManagerRef = new OrbitalCommandManager();
        public static readonly ConstructionManager ConstructionManagerRef = new ConstructionManager();
        public static readonly RampManager RampManagerRef = new RampManager();

        // List of all active managers. NOTE: Order matters for which gets to update/initialize first
        public static readonly List<Manager> AllManagers = new List<Manager>
        {
            SensorManagerRef, // should be first to generate events for other managers

            StrategicGoalRef,
            DiscrepenceyDetectorRef, // depends on StrategicGoalRef
            GoalFormulatorRef, // depends on DiscrepenceyDetectorRef
            GoalExecutorRef, // depends on GoalFormulatorRef

            TacticalGoalRef,
            SquadManagerRef, // depends on TacticalGoalRef

            OrbitalCommandManagerRef, // depends on strategy layer
            ConstructionManagerRef,
            RampManagerRef,
            WorkerManagerRef,
        };

        // Background thread
        public static BackgroundWorkerThread BackgroundWorkerThreadRef = new BackgroundWorkerThread();

        // Map handling
        public static MapAnalysisWrapper MapAnalysisRef = new MapAnalysisWrapper();
        public static PathFinder PathFinderRef = new PathFinder();

        /// <summary>
        /// Set after the first frame initialization has been completed
        /// </summary>
        public static bool HasCompletedFirstFrameInit = false;

        /// <summary>
        /// New bot instance, global sc2 state is not valid yet
        /// </summary>
        public BOSSE()
        {
            Log.Start();
        }

        /// <summary>
        /// Initializes bot layer - Game loop has read static data at this point, but has not gathered any observations
        /// </summary>
        public void Initialize()
        {
            StrategicGoalRef.SetNewGoal(StrategicGoal.EconomyFocus);
            TacticalGoalRef.SetNewGoal(MilitaryGoal.DefendGeneral, null);
        }

        /// <summary>
        /// First frame setup
        /// </summary>
        public void FirstFrame()
        {
            // Set main location
            Globals.MainBaseLocation = GetUnits(UnitId.COMMAND_CENTER)[0].Position;
            
            // General map analysis
            PathFinderRef.Initialize();
            MapAnalysisRef.Initialize();

            // Initialize sub-managers
            foreach (Manager managerIter in AllManagers)
            {
                managerIter.Initialize();
            }

            // Start background worker thread
            BackgroundWorkerThreadRef.StartThread();

            // Debug sensor - Log building completions
            SensorManagerRef.GetSensor(typeof(OwnStructureWasCompletedSensor)).AddHandler(new SensorEventHandler(delegate (HashSet<Unit> affectedUnits)
            {
                foreach (Unit iter in affectedUnits)
                {
                    Log.Info("Completed new building: " + iter + " at " + iter.Position.ToString2());

                    // Add all depots to ramp manager for now
                    if (iter.UnitType == UnitId.SUPPLY_DEPOT)
                    {
                        RampManagerRef.AddSupplyDepot(iter);
                    }
                }
            }));

            // Assign a random worker to scout
            BOSSE.SquadManagerRef.AddNewSquad(new Squad("ScoutingWorker", new ScoutingWorkerController()));
            Unit scoutingWorker = GetUnits(UnitId.SCV, onlyCompleted: true)[0];
            Log.Info("Assigning worker " + scoutingWorker.Tag + " as initial scout");
            scoutingWorker.IsReserved = true;
            BOSSE.SquadManagerRef.GetSquadOrNull("ScoutingWorker").AddUnit(scoutingWorker);

            // Insert a random joke in chat
            foreach (string jokeLine in JokeGenerator.GetJoke())
            {
                Queue(CommandBuilder.Chat(jokeLine));
            }

            HasCompletedFirstFrameInit = true;
        }

        /// <summary>
        /// Entry point from main loop which updates the bot, called on each logical frame
        /// </summary>
        public void OnFrameTick()
        {
            Unit.RefreshAllUnitData();

            foreach (Manager managerIter in AllManagers)
            {
                managerIter.OnFrameTick();
            }
        }
    }
}