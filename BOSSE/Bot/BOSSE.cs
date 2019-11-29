﻿/*
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

    /// <summary>
    /// Main bot entry point
    /// </summary>
    public class BOSSE : IBot
    {
        /// <summary>
        /// Entry point from main loop which updates the bot, called on each logical frame
        /// </summary>
        public void Update()
        {
            GameOutput.QueuedActions.Clear();

            List<Unit> resourceCenters = GetUnits(UnitConstants.ResourceCenters);
            
            //// Build workers
            //var resourceCenters = GetUnits(UnitConstants.ResourceCenters);
            //foreach (var rc in resourceCenters)
            //{
            //    if (CanConstruct(UnitConstants.SCV))
            //    {
            //        Train(rc, UnitConstants.SCV);
            //    }
            //}

            //// Build depots
            //if (CanConstruct(UnitConstants.SUPPLY_DEPOT))
            //{
            //    if (GetPendingCount(UnitConstants.SUPPLY_DEPOT) == 0)
            //    {
            //        Construct(UnitConstants.SUPPLY_DEPOT);
            //    }
            //}
        }

        //public bool Train(Unit fromCenter, uint unitTypeToBuild, bool allowQueue = false)
        //{
        //    if (!allowQueue && fromCenter.QueuedOrders.Count > 0)
        //        return false;

        //    var abilityID = Abilities.GetID(unitTypeToBuild);
        //    var action = CommandBuilder.CreateRawUnitCommand(abilityID);
        //    action.ActionRaw.UnitCommand.UnitTags.Add(fromCenter.Tag);
        //    GameOutput.QueuedActions.Add(action);

        //    var targetName = GameUtility.GetUnitName(unitTypeToBuild);
        //    Log.Info("Training unit {0}", targetName);

        //    return true;
        //}
    }
}