/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System;
    using System.IO;
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
    /// Global parameters
    /// </summary>
    public static class Globals
    {
        /// <summary>
        /// Global reference to the running game instance
        /// </summary>
        public static GameConnection StarcraftRef;

        /// <summary>
        /// Pointer to our bot instance
        /// </summary>
        public static Bot BotRef = new BOSSE();
        
        /// <summary>
        /// Generates random numbers globally
        /// </summary>
        public static Random Random = new Random();

        /// <summary>
        /// True = Local debug build
        /// False = Ladder play
        /// </summary>
        public static bool IsSinglePlayer = false;
    }
}
