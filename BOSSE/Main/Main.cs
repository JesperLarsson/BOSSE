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
        /// <summary>
        /// Initializes the application
        /// </summary>
        public void Start(string[] commandLineArguments)
        {
            GameBootstrapper bootStrapper = new GameBootstrapper();
            Globals.BotRef = new BOSSE();

            Globals.BotRef.Initialize();

            // Set up game
            if (commandLineArguments.Length == 0)
            {
                // Single player development mode
                Globals.IsSinglePlayer = true;

                if (BotConstants.UseFixedGameSeed)
                    Globals.Random = new Random(BotConstants.FixedSeedValue);

                DebugGui.BosseGui.StartGui();
                Globals.GameConnection = bootStrapper.RunSinglePlayer(BotConstants.DebugMapName, BOSSE.UseRace, BotConstants.DebugOpponentRace, BotConstants.DebugOpponentDifficulty).Result;
            }
            else
            {
                // Ladder play
                Globals.IsSinglePlayer = false;
                Globals.GameConnection = bootStrapper.RunLadder(BOSSE.UseRace, commandLineArguments).Result;
            }

            // Game has started, read initial state
            ReadInitialState().Wait();

            // Main loop
            Globals.OnCurrentFrame = 0;
            while (true)
            {
                try
                {
                    // Remove previous frame actions
                    GameOutput.QueuedActions.Clear();

                    // Read from sc2
                    lock (GameOutput.GameRequestMutex)
                    {
                        ReadPerFrameState().Wait();
                    }

                    // Real time mode and stepping mode use different initialization methods
                    // RT needs to be able to start as fast as possible, so we don't have time to perform map analysis etc before sending an action
                    if (BotConstants.UseStepMode)
                    {
                        StepLockTick();
                    }
                    else
                    {
                        RealtimeTick();
                    }

                    // Send actions to sc2
                    lock (GameOutput.GameRequestMutex)
                    {
                        SendQueuedActions().Wait();
                    }

                    Globals.OnCurrentFrame++;
                    if (BotConstants.UseStepMode)
                    {
                        Thread.Sleep(BotConstants.TickLockSleep);
                    }
                }
                catch (BosseFatalException ex)
                {
                    Log.SanityCheckFailed("FATAL EXCEPTION: " + ex);
                    ExitBosse(99);
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

        private void RealtimeTick()
        {
            // Init in a separate thread
            if (Globals.OnCurrentFrame == 0)
            {
                BOSSE.WorkerManagerRef.TrainWorkersIfNecessary();
                BOSSE.WorkerManagerRef.ReturnIdleWorkers();

                Thread initThread = new Thread(Globals.BotRef.OnFirstFrame);
                initThread.Name = "InitThread";
                initThread.Priority = ThreadPriority.AboveNormal;
                initThread.Start();
            }
            else if (BOSSE.HasCompletedFirstFrameInit)
            {
                Globals.BotRef.OnFrameTick();
            }
            else
            {
                BOSSE.WorkerManagerRef.TrainWorkersIfNecessary();
                BOSSE.WorkerManagerRef.ReturnIdleWorkers();
            }
        }

        private void StepLockTick()
        {
            // Init
            if (Globals.OnCurrentFrame == 0)
            {
                Globals.BotRef.OnFirstFrame();
            }

            // Normal update
            Globals.BotRef.OnFrameTick();
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

            CurrentGameState.State.GameInformation = gameInfoResponse.GameInfo;
            CurrentGameState.State.GameData = dataResponse.Data;
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
            CurrentGameState.State.ObservationState = response.Observation;

            // Check for errors
            foreach (ActionError errorIter in CurrentGameState.State.ObservationState.ActionErrors)
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
                        ExitBosse();
                    }
                }
            }
        }

        /// <summary>
        /// Terminates BOSSE
        /// </summary>
        public static void ExitBosse(int exitCode = 0)
        {
            Thread.Sleep(2000); // Give logs time to flush
            System.Environment.Exit(exitCode);
        }

        /// <summary>
        /// Outputs all queued actions from the bot to sc2
        /// </summary>
        private async Task SendQueuedActions()
        {
            // NOTE: This might need to be increased to work around some SC2 issues where some orders take more than a single frame to apply
            const int StepSize = 1;

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
            stepRequest.Step.Count = StepSize;
            await Globals.GameConnection.SendRequest(stepRequest);
        }
    }
}
