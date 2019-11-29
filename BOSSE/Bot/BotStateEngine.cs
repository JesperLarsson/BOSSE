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

            // Build workers
            List<Unit> resourceCenters = GetUnits(UnitConstants.ResourceCenters);
            foreach (var rc in resourceCenters)
            {
                Train(rc, UnitConstants.UnitId.SCV);
            }

            // Build depots
            if (CurrentMinerals > 100)
            {
                Construct(UnitConstants.UnitId.SUPPLY_DEPOT);
            }
        }

        private bool Train(Unit fromCenter, UnitId unitTypeToBuild, bool allowQueue = false)
        {
            if (!allowQueue && fromCenter.QueuedOrders.Count > 0)
                return false;

            var abilityID = AbilityConstants.GetAbilityIdToBuildUnit(unitTypeToBuild);
            var action = CommandBuilder.CreateRawUnitCommand(abilityID);
            action.ActionRaw.UnitCommand.UnitTags.Add(fromCenter.Tag);
            GameOutput.QueuedActions.Add(action);

            Log.Info($"Training unit {unitTypeToBuild}");

            return true;
        }
    }
}