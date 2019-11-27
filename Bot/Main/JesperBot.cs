/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace Bot
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

    public class JesperBot : Bot
    {
        public void OnFrame()
        {
            GameOutput.QueuedActions.Clear();

            var resourceCenters = GetUnits(Units.ResourceCenters);
            foreach (var rc in resourceCenters)
            {
                if (CanConstruct(Units.SCV))
                    rc.Train(Units.SCV);
            }

            if (CurrentGameState.MaxSupply - CurrentGameState.CurrentSupply <= 5)
                if (CanConstruct(Units.SUPPLY_DEPOT))
                    if (GetPendingCount(Units.SUPPLY_DEPOT) == 0)
                        Construct(Units.SUPPLY_DEPOT);
        }
    }
}