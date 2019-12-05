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
    using System.Diagnostics;
    using System.Linq;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// General utility functions not related to sc2
    /// </summary>
    public static class GeneralUtility
    {
        public static void BreakIfAttached()
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            else
            {
                Log.Warning("Called BreakIfAttached() without debugger available");
            }
        }
    }
}
