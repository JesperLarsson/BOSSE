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
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// AI top layer
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

        // Tactical managers
        public static readonly TacticalGoalManager TacticalGoalRef = new TacticalGoalManager();
        public static readonly SquadManager SquadManagerRef = new SquadManager();

        // Utility managers
        public static readonly WorkerManager WorkerManagerRef = new WorkerManager();
        public static readonly OrbitalCommandManager OrbitalCommandManagerRef = new OrbitalCommandManager();

        // List of all active managers. NOTE: Order matters for which gets to update first
        public static readonly List<Manager> AllManagers = new List<Manager>
        {
            SensorManagerRef,

            StrategicGoalRef,
            DiscrepenceyDetectorRef,
            GoalFormulatorRef,
            GoalExecutorRef,

            TacticalGoalRef,
            SquadManagerRef,

            WorkerManagerRef,
            OrbitalCommandManagerRef
        };
        
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
            Tyr.Tyr.Initialize();

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

            foreach (Manager managerIter in AllManagers)
            {
                managerIter.OnFrameTick();
            }
        }
    }
}