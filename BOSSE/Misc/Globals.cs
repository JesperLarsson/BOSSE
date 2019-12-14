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
    using static GeneralGameUtility;

    /// <summary>
    /// Global parameters
    /// </summary>
    public static class Globals
    {
        /// <summary>
        /// Global reference to the running game instance, via protobuff websocket proxy
        /// </summary>
        public static ProtobufProxy GameConnection;

        /// <summary>
        /// Pointer to our bot instance
        /// </summary>
        public static BOSSE BotRef;
        
        /// <summary>
        /// Generates random numbers globally
        /// </summary>
        public static Random Random = new Random();

        /// <summary>
        /// True = Local debug build
        /// False = Ladder play
        /// </summary>
        public static bool IsSinglePlayer = false;

        /// <summary>
        /// Id of our player in the current match
        /// </summary>
        public static uint PlayerId = uint.MaxValue;

        /// <summary>
        /// We are currently running the current logical frame, starts at 0
        /// </summary>
        public static ulong CurrentFrameIndex = 0;

        /// <summary>
        /// Location of our main starting base
        /// </summary>
        public static Point2D MainBaseLocation;
    }
}
