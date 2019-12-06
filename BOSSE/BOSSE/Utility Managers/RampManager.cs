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
    using System.Linq;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Handles our ramps - Puts up / down supply depots etc as necessary
    /// </summary>
    public class RampManager : Manager
    {
        private HashSet<Unit> ManagedSupplyDepots = new HashSet<Unit>();

        public override void Initialize()
        {
        }

        public override void OnFrameTick()
        {
#warning TODO: Look for enemies that are close and put up depots
            foreach (Unit iter in this.ManagedSupplyDepots)
            {
                if (iter.UnitType != UnitId.SUPPLY_DEPOT_LOWERED)
                {
                    PutDownDepot(iter);
                }
            }
        }

        public void AddSupplyDepot(Unit newDepo)
        {
            ManagedSupplyDepots.Add(newDepo);
        }

        private void PutDownDepot(Unit depoToDown)
        {
            Log.Bulk("Putting down depot " + depoToDown);
            Queue(CommandBuilder.UseAbility(AbilityId.SupplyDepotLower, depoToDown));
        }

        private void PullUpDepot(Unit depoToDown)
        {
            Queue(CommandBuilder.UseAbility(AbilityId.SupplyDepotLower, depoToDown));
        }
    }
}
