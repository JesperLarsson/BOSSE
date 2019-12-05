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
    /// Compares the current game state against our goals, and generates high level events based on that which might result in new goals
    /// </summary>
    public class DiscrepenceyDetector : Manager
    {
        public override void Initialize()
        {

        }

        public override void OnFrameTick()
        {

        }
    }
}
