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
    /// Manager base class, each manager will be updated each tick
    /// </summary>
    public abstract class Manager
    {
        /// <summary>
        /// Called when the bot starts
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Update each bot logical frame
        /// </summary>
        public abstract void OnFrameTick();
    }
}
