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

    /// <summary>
    /// Main bot entry point
    /// </summary>
    public class BOSSE
    {
        /// <summary>
        /// Executes the goals we have set
        /// </summary>
        public static StrategicGoalExecutor GoalExecutorRef = new StrategicGoalExecutor();

        /// <summary>
        /// Manages our workers
        /// </summary>
        public static WorkerManager WorkerManagerRef = new WorkerManager();

        /// <summary>
        /// Manges our army
        /// </summary>
        public static SquadManager SquadManagerRef = new SquadManager();

        /// <summary>
        /// Initializes bot layer, game has an initial state when this is called
        /// </summary>
        public void Initialize()
        {
            // Initialize Tyr (map analysis)
            Tyr.Tyr.Debug = Globals.IsSinglePlayer;
            Tyr.Tyr.PlayerId = Globals.PlayerId;
            Tyr.Tyr.GameInfo = CurrentGameState.GameInformation;

            // Initialize sub-managers
            GoalExecutorRef.Initialize();
            SquadManagerRef.Initialize();
        }

        /// <summary>
        /// Entry point from main loop which updates the bot, called approx once a second in real time - Used to update expensive calculations
        /// Called before OnFrame on the first frame
        /// </summary>
        public void EverySecond()
        {
            // Refresh strategy maps
            StrategicMapSet.CalculateNewFromCurrentMapState();

            // Move workers to optimal locations
            WorkerManagerRef.Tick();
        }

        /// <summary>
        /// Entry point from main loop which updates the bot, called every now and then
        /// Called before OnFrame on the first frame
        /// </summary>
        public void LongTermPeriodical()
        {
            // Update Tyr maps
            Tyr.Tyr.Observation = CurrentGameState.ObservationState;
            Tyr.Tyr.MapAnalyzer.Analyze();
            Tyr.Tyr.MapAnalyzer.AddToGui();
        }

        /// <summary>
        /// First frame setup
        /// </summary>
        public void FirstFrame()
        {
            // Set main location
            Globals.MainBaseLocation = GetUnits(UnitId.COMMAND_CENTER)[0].Position;
        }

        /// <summary>
        /// Entry point from main loop which updates the bot, called on each logical frame
        /// </summary>
        public void OnFrame()
        {
            GoalExecutorRef.Tick();
            SquadManagerRef.Tick();
        }
    }
}