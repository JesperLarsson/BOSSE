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
    using System.Threading.Tasks;
    using System.Diagnostics;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// Top level object which bootstraps the engine and runs the main loop
    /// </summary>
    public class MainLoop
    {
        // Debug settings (vs Blizzard AI)
        private static readonly string mapName = "AcropolisLE.SC2Map";
        private static readonly Race opponentRace = Race.Zerg;
        private static readonly Difficulty opponentDifficulty = Difficulty.Easy;

        /// <summary>
        /// Initializes the application
        /// </summary>
        public void Start(string[] commandLineArguments)
        {
            GameBootstrapper bootStrapper = new GameBootstrapper();
            Globals.BotRef = new BOSSE();

            // Set up game
            if (commandLineArguments.Length == 0)
            {
                // Single player development mode
                Globals.IsSinglePlayer = true;
                //Globals.Random = new Random(123456987); // use the same random number generation every time to make reproducing behaviour more likely
                DebugGui.BosseGui.StartGui();
                Globals.GameConnection = bootStrapper.RunSinglePlayer(mapName, BotConstants.SpawnAsRace, opponentRace, opponentDifficulty).Result;
            }
            else
            {
                // Ladder play
                Globals.IsSinglePlayer = false;
                Globals.GameConnection = bootStrapper.RunLadder(BotConstants.SpawnAsRace, commandLineArguments).Result;
            }

            // Game has started, read initial state
            ReadInitialState().Wait();
            Globals.BotRef.Initialize();

            // Main loop
            Globals.CurrentFrameIndex = 0;
            while (true)
            {
                try
                {
                    Log.Bulk("--- Starting frame " + Globals.CurrentFrameIndex);

                    // Remove previous frame actions
                    GameOutput.QueuedActions.Clear();

                    // Read from sc2
                    ReadPerFrameState().Wait();

                    // Update bot
                    if (Globals.CurrentFrameIndex == 0)
                    {
                        Globals.BotRef.FirstFrame();
                    }
                    Globals.BotRef.OnFrameTick();

                    // Send actions to sc2
                    SendQueuedActions().Wait();

                    Globals.CurrentFrameIndex++;
                    if (BotConstants.TickLockMode)
                    {
                        Thread.Sleep(BotConstants.TickLockSleep);
                    }
                }
                catch (BosseFatalException ex)
                {
                    Log.SanityCheckFailed("FATAL EXCEPTION: " + ex);
                    Thread.Sleep(2000); // Let log flush
                    Environment.Exit(99);
                }
                catch (BosseRecoverableException ex)
                {
                    Log.SanityCheckFailed("Cought top level bot exception: " + ex);
                }
                catch (Exception ex)
                {
                    Log.SanityCheckFailed("Cought top level exception: " + ex);
                }
            }
        }

        /// <summary>
        /// Polls sc2 once for the initial complete state, populates global state parameters
        /// </summary>
        private async Task ReadInitialState()
        {
            Request gameInfoReq = new Request();
            gameInfoReq.GameInfo = new RequestGameInfo();
            Response gameInfoResponse = await Globals.GameConnection.SendRequest(gameInfoReq);

            var dataReq = new Request();
            dataReq.Data = new RequestData();
            dataReq.Data.UnitTypeId = true;
            dataReq.Data.AbilityId = true;
            dataReq.Data.BuffId = true;
            dataReq.Data.EffectId = true;
            dataReq.Data.UpgradeId = true;
            Response dataResponse = await Globals.GameConnection.SendRequest(dataReq);

            CurrentGameState.GameInformation = gameInfoResponse.GameInfo;
            CurrentGameState.GameData = dataResponse.Data;
        }

        /// <summary>
        /// Poll observational game data every frame
        /// </summary>
        private async Task ReadPerFrameState()
        {
            Request observationRequest = new Request();
            observationRequest.Observation = new RequestObservation();
            Response response = await Globals.GameConnection.SendRequest(observationRequest);

            // Update global state
            CurrentGameState.ObservationState = response.Observation;

            // Check for errors
            foreach (ActionError errorIter in CurrentGameState.ObservationState.ActionErrors)
            {
                Log.Error("Received error from starcraft: " + errorIter.Result + "(unit " + errorIter.UnitTag + " ability " + errorIter.AbilityId + ")");
            }

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

        /// <summary>
        /// Outputs all queued actions from the bot to sc2
        /// </summary>
        private async Task SendQueuedActions()
        {
            const int stepSize = 1;

            List<Action> actions = GameOutput.QueuedActions;

            Request actionRequest = new Request();
            actionRequest.Action = new RequestAction();
            actionRequest.Action.Actions.AddRange(actions);
            if (actionRequest.Action.Actions.Count > 0)
            {
                await Globals.GameConnection.SendRequest(actionRequest);
            }

            Request stepRequest = new Request();
            stepRequest.Step = new RequestStep();
            stepRequest.Step.Count = stepSize;
            await Globals.GameConnection.SendRequest(stepRequest);
        }
    }
}
