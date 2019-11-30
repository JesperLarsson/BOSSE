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
    public class BotStateEngine
    {
        /// <summary>
        /// Initializes bot layer, game has an initial state when this is called
        /// </summary>
        public void Initialize()
        {
            // Tyr setup
            Tyr.Tyr.Debug = Globals.IsSinglePlayer;
            Tyr.Tyr.PlayerId = Globals.PlayerId;
            Tyr.Tyr.GameInfo = CurrentGameState.GameInformation;
        }

        /// <summary>
        /// Entry point from main loop which updates the bot, called approx once a second in real time
        /// Called before OnFrame on the first loop
        /// Used to update expensive calculations
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
            // Build workers
            List<Unit> resourceCenters = GetUnits(UnitConstants.ResourceCenters);
            foreach (var rc in resourceCenters)
            {
                Queue(CommandBuilder.TrainAction(rc, UnitConstants.UnitId.SCV));
            }

            // Build depots
            if (CurrentMinerals > 100 && GetUnits(UnitConstants.UnitId.SUPPLY_DEPOT).Count < 4)
            {
                Construct(UnitConstants.UnitId.SUPPLY_DEPOT);
            }
        }

        /// <summary>
        /// Builds the given type
        /// </summary>
        public static void Construct(UnitId unitType)
        {
            const int radius = 12;
            Vector3 startingSpot;

            List<Unit> resourceCenters = GetUnits(UnitConstants.ResourceCenters);
            if (resourceCenters.Count > 0)
            {
                startingSpot = resourceCenters[0].Position;
            }
            else
            {
                Log.Error($"Unable to construct {unitType} - no resource center was found");
                return;
            }

            // Find a valid spot, the slow way
            List<Unit> mineralFields = GetUnits(UnitConstants.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
            Vector3 constructionSpot;
            while (true)
            {
                constructionSpot = new Vector3(startingSpot.X + Globals.Random.Next(-radius, radius + 1), startingSpot.Y + Globals.Random.Next(-radius, radius + 1), 0);

                //avoid building in the mineral line
                if (IsInRange(constructionSpot, mineralFields, 5)) continue;

                //check if the building fits
                if (!CanPlace(unitType, constructionSpot)) continue;

                //ok, we found a spot
                break;
            }

            Unit worker = GetAvailableWorker(constructionSpot);
            if (worker == null)
            {
                Log.Error($"Unable to find worker to construct {unitType}");
                return;
            }

            int abilityID = GetAbilityIdToBuildUnit(unitType);
            Action constructAction = CommandBuilder.RawCommand(abilityID);
            constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.Tag);
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.X = constructionSpot.X;
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = constructionSpot.Y;
            GameOutput.QueuedActions.Add(constructAction);

            Log.Info($"Constructing {unitType} at {constructionSpot.ToString2()} / {constructionSpot.Y}");
        }
    }
}