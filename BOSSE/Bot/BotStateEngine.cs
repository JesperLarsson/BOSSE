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
        /// Entry point from main loop which updates the bot, called on each logical frame
        /// </summary>
        public void Update()
        {
            GameOutput.QueuedActions.Clear();

            StrategicMapSet strategyMaps = StrategicMapSet.CalculateNewFromCurrentMapState();
            DebugGui.InfluenceMapGui.NewInfluenceMapIsAvailable(strategyMaps.InfluenceMap, strategyMaps.xSize, strategyMaps.ySize);
            DebugGui.TensionMapGui.NewTensionMapIsAvailable(strategyMaps.TensionMap, strategyMaps.xSize, strategyMaps.ySize);
            DebugGui.VulnerabilityMapGui.NewVulnerabilityMapIsAvailable(strategyMaps.VulnerabilityMap, strategyMaps.xSize, strategyMaps.ySize);

            Tyr.Tyr.Debug = Globals.IsSinglePlayer;
            Tyr.Tyr.PlayerId = Globals.PlayerId;
            Tyr.Tyr.MapAnalyzer = new Tyr.MapAnalyzer();
            Tyr.Tyr.BaseManager = new Tyr.BaseManager();
            Tyr.Tyr.GameInfo = CurrentGameState.GameInformation;
            Tyr.Tyr.Observation = CurrentGameState.ObservationState;
            Tyr.Tyr.MapAnalyzer.Analyze();

            Point2D rampPoint = Tyr.Tyr.MapAnalyzer.GetMainRamp();

            // Build workers
            List<Unit> resourceCenters = GetUnits(UnitConstants.ResourceCenters);
            foreach (var rc in resourceCenters)
            {
                Train(rc, UnitConstants.UnitId.SCV);
            }

            // Build depots
            if (CurrentMinerals > 100 && GetUnits(UnitConstants.UnitId.SUPPLY_DEPOT).Count < 4)
            {
                Construct(UnitConstants.UnitId.SUPPLY_DEPOT);
            }

            var allWorkers = GetUnits(UnitConstants.Workers);
            var action = CommandBuilder.CreateMoveAction(allWorkers, new Vector3(rampPoint.X, rampPoint.Y, 0));
            GameOutput.QueuedActions.Add(action);
        }

        private bool Train(Unit fromCenter, UnitId unitTypeToBuild, bool allowQueue = false)
        {
            if (!allowQueue && fromCenter.QueuedOrders.Count > 0)
                return false;

            var abilityID = GetAbilityIdToBuildUnit(unitTypeToBuild);
            var action = CommandBuilder.CreateRawUnitCommand(abilityID);
            action.ActionRaw.UnitCommand.UnitTags.Add(fromCenter.Tag);
            GameOutput.QueuedActions.Add(action);

            Log.Info($"Training unit {unitTypeToBuild}");

            return true;
        }
    }
}