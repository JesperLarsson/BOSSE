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
        public static StrategicGoalExecutor GoalExecutorRef;

        /// <summary>
        /// Initializes bot layer, game has an initial state when this is called
        /// </summary>
        public void Initialize()
        {
            // Tyr setup
            Tyr.Tyr.Debug = Globals.IsSinglePlayer;
            Tyr.Tyr.PlayerId = Globals.PlayerId;
            Tyr.Tyr.GameInfo = CurrentGameState.GameInformation;

            GoalExecutorRef.Initialize();
        }

        /// <summary>
        /// Entry point from main loop which updates the bot, called approx once a second in real time - Used to update expensive calculations
        /// Called before OnFrame on the first frame
        /// </summary>
        public void Every22Frames()
        {
            // Update Tyr maps
            Tyr.Tyr.Observation = CurrentGameState.ObservationState;
            Tyr.Tyr.MapAnalyzer.Analyze();
            Tyr.Tyr.MapAnalyzer.AddToGui();

            // Update strategy maps
            StrategicMapSet.CalculateNewFromCurrentMapState();
        }

        /// <summary>
        /// Entry point from main loop which updates the bot, called on each logical frame
        /// </summary>
        public void OnFrame()
        {
            GoalExecutorRef.Tick();
        }
    }
}