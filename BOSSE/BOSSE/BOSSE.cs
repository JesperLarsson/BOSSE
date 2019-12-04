﻿/*
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

    /// <summary>
    /// AI top layer
    /// </summary>
    public class BOSSE
    {
        // Input managers
        public static SensorManager SensorManagerRef = new SensorManager();

        // Strategic managers
        public static DiscrepenceyDetector DiscrepenceyDetectorRef = new DiscrepenceyDetector();
        public static GoalFormulator GoalFormulatorRef = new GoalFormulator();
        public static StrategicGoalManager StrategicGoalRef = new StrategicGoalManager();
        public static StrategicGoalExecutor GoalExecutorRef = new StrategicGoalExecutor();

        // Tactical managers
        public static TacticalGoalManager TacticalGoalRef = new TacticalGoalManager();
        public static SquadManager SquadManagerRef = new SquadManager();

        // Utility managers
        public static WorkerManager WorkerManagerRef = new WorkerManager();
        public static OrbitalCommandManager OrbitalCommandManagerRef = new OrbitalCommandManager();

        // Background thread
        public static BackgroundWorkerThread BackgroundWorkerThreadRef = new BackgroundWorkerThread();

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

            // Initialize Tyr (map analysis)
            Tyr.Tyr.Debug = Globals.IsSinglePlayer;
            Tyr.Tyr.PlayerId = Globals.PlayerId;
            Tyr.Tyr.GameInfo = CurrentGameState.GameInformation;
            Tyr.Tyr.Observation = CurrentGameState.ObservationState;
            Tyr.Tyr.MapAnalyzer.Analyze();
            Tyr.Tyr.TargetManager.OnStart();
            Tyr.Tyr.BaseManager.OnStart();
            Tyr.Tyr.MapAnalyzer.AddToGui();

            // Initialize sub-managers
            SensorManagerRef.Initialize();
            GoalExecutorRef.Initialize();
            SquadManagerRef.Initialize();
            WorkerManagerRef.Initialize();
            OrbitalCommandManagerRef.Initialize();
            DiscrepenceyDetectorRef.Initialize();
            GoalFormulatorRef.Initialize();

            // Start BG thread
            BackgroundWorkerThreadRef.StartThread();

            // Debug sensor - Log building completions
            SensorManagerRef.GetSensor(typeof(OwnStructureWasCompletedSensor)).AddHandler(new SensorEventHandler(delegate (HashSet<Unit> affectedUnits)
            {
                foreach (Unit iter in affectedUnits)
                {
                    Log.Info("Completed new building: " + iter.Name);
                }
            }));

            // Assign a random worker to scout
            BOSSE.SquadManagerRef.AddNewSquad(new Squad("ScoutingWorker", new ScoutingWorkerController()));
            Unit scoutingWorker = GetUnits(UnitId.SCV, onlyCompleted: true)[0];
            scoutingWorker.IsReserved = true;
            BOSSE.SquadManagerRef.GetSquadOrNull("ScoutingWorker").AddUnit(scoutingWorker);
        }

        /// <summary>
        /// Entry point from main loop which updates the bot, called on each logical frame
        /// </summary>
        public void OnFrameTick()
        {
            Unit.RefreshAllUnitData();

            // Sensor layer
            SensorManagerRef.Tick();

            // Strategic layer
            DiscrepenceyDetectorRef.Tick();
            GoalFormulatorRef.Tick();
            GoalExecutorRef.Tick();

            // Tactical (military) layer
            SquadManagerRef.Tick();

            // Utility managers
            WorkerManagerRef.Tick();
            OrbitalCommandManagerRef.Tick();
        }
    }
}