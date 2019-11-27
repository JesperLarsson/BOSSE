/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace Bot
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

    public static class Globals
    {
        /// <summary>
        /// Global reference to the running game instance
        /// </summary>
        public static GameConnection StarcraftRef;

        public static readonly Bot BotRef = new JesperBot();
        
        public static readonly Random random = new Random();
    }
}
