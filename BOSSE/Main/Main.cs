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
    using System.Threading.Tasks;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GameUtility;

    /// <summary>
    /// Main loop used once the game is running
    /// </summary>
    public class MainLoop
    {
        // Debug settings (single player mode)
        private static readonly string mapName = "ThunderbirdLE.SC2Map";
        private static readonly Race opponentRace = Race.Protoss;
        private static readonly Difficulty opponentDifficulty = Difficulty.VeryEasy;

        /// <summary>
        /// Initializes the application
        /// </summary>
        public void Start(string[] commandLineArguments)
        {
            GameBootstrapper bootStrapper = new GameBootstrapper();

            // Set up game
            if (commandLineArguments.Length == 0)
            {
                // Single player, debug mode
                Globals.IsSinglePlayer = true;
                Globals.Random = new Random(1234567); // use the same random number generation every time to make reproducing behaviour more likely

                Globals.GameConnection = bootStrapper.RunSinglePlayer(Globals.BotRef, mapName, BotConstants.SpawnAsRace, opponentRace, opponentDifficulty).Result;
            }
            else
            {
                // Ladder play
                Globals.IsSinglePlayer = false;

                Globals.GameConnection = bootStrapper.RunLadder(Globals.BotRef, BotConstants.SpawnAsRace, commandLineArguments).Result;
            }

            // Start main loop
            InitializeGameState().Wait();
            Loop().Wait();
        }

        /// <summary>
        /// Top level main loop
        /// </summary>
        private async Task Loop()
        {
            while (true)
            {
                UpdateGameData().Wait();

                Globals.BotRef.Update();

                SendQueuedActions().Wait();
            }
        }

        /// <summary>
        /// Polls sc2 once for the initial complete state and populates our global state
        /// </summary>
        private async Task InitializeGameState()
        {
            var gameInfoReq = new Request();
            gameInfoReq.GameInfo = new RequestGameInfo();

            var gameInfoResponse = await Globals.GameConnection.SendRequest(gameInfoReq);

            var dataReq = new Request();
            dataReq.Data = new RequestData();
            dataReq.Data.UnitTypeId = true;
            dataReq.Data.AbilityId = true;
            dataReq.Data.BuffId = true;
            dataReq.Data.EffectId = true;
            dataReq.Data.UpgradeId = true;

            var dataResponse = await Globals.GameConnection.SendRequest(dataReq);

            CurrentGameState.GameInformation = gameInfoResponse.GameInfo;
            CurrentGameState.GameData = dataResponse.Data;
        }

        private async Task UpdateGameData()
        {
            Request observationRequest = new Request();
            observationRequest.Observation = new RequestObservation();
            Response response = await Globals.GameConnection.SendRequest(observationRequest);

            // Update global state
            CurrentGameState.ObservationState = response.Observation;

            // Check for game over
            if (response.Status == Status.Ended || response.Status == Status.Quit)
            {
                foreach (var result in response.Observation.PlayerResult)
                {
                    if (result.PlayerId == Globals.PlayerId)
                    {
                        Log.Info("Match is over, result = " + result.Result);
                        System.Environment.Exit(0);
                    }
                }
            }
        }

        private async Task SendQueuedActions()
        {
            const int stepSize = 1;

            var actions = GameOutput.QueuedActions;

            var actionRequest = new Request();
            actionRequest.Action = new RequestAction();
            actionRequest.Action.Actions.AddRange(actions);
            if (actionRequest.Action.Actions.Count > 0)
                await Globals.GameConnection.SendRequest(actionRequest);

            var stepRequest = new Request();
            stepRequest.Step = new RequestStep();
            stepRequest.Step.Count = stepSize;
            await Globals.GameConnection.SendRequest(stepRequest);
        }
    }
}
