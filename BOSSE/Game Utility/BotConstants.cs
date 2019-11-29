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
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;

    /// <summary>
    /// Bot constant values, set during development
    /// </summary>
    public static class BotConstants
    {
        /// <summary>
        /// Version of the bot
        /// </summary>
        public const string ApplicationVersion = "0.1dev";

        /// <summary>
        /// Number of logical frames per second of real time
        /// </summary>
        public const double FRAMES_PER_SECOND = 22.4;

        /// <summary>
        /// Bot will spawn as the given race
        /// </summary>
        public const Race SpawnAsRace = Race.Terran;
    }
}
