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
    /// AI top layer
    /// BOSSE - BOt Starcraft(2), e as in e-sports :)
    /// </summary>
    public class BOSSE
    {
        // Pointers to our sub-managers - see class comment for details on what they do
        public static StrategicGoalExecutor GoalExecutorRef = new StrategicGoalExecutor();
        public static WorkerManager WorkerManagerRef = new WorkerManager();
        public static SquadManager SquadManagerRef = new SquadManager();
        public static SensorManager SensorManagerRef = new SensorManager();

        /// <summary>
        /// Initializes bot layer - Game loop has read static data at this point, but has not gathered any observations
        /// </summary>
        public void Initialize()
        {
            // Initialize Tyr (map analysis)
            Tyr.Tyr.Debug = Globals.IsSinglePlayer;
            Tyr.Tyr.PlayerId = Globals.PlayerId;
            Tyr.Tyr.GameInfo = CurrentGameState.GameInformation;

            // Initialize sub-managers
            SensorManagerRef.Initialize();
            GoalExecutorRef.Initialize();
            SquadManagerRef.Initialize();

            // Test sensor
            SensorManagerRef.GetSensor(Sensor.SensorId.OwnStructureWasCompletedSensor).AddHandler(new EventHandler(delegate (Object sensorRef, EventArgs args)
            {
                OwnStructureWasCompletedSensor.Details details = (OwnStructureWasCompletedSensor.Details)args;

                foreach (var iter in details.NewStructures)
                {
                    Log.Info("Completed new building: " + iter.Name);
                }
            }));
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
            // Sensors should update first
            SensorManagerRef.Tick();

            GoalExecutorRef.Tick();
            SquadManagerRef.Tick();
        }
    }
}