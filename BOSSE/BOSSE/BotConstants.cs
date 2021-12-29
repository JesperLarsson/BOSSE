/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2022 Jesper Larsson

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
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
    using static UnitConstants;

    /// <summary>
    /// Bot constant values, set manually during development
    /// </summary>
    public static class BotConstants
    {
        /// <summary>
        /// Version of the bot
        /// </summary>
        public const string ApplicationVersion = "0.1dev";

        /// <summary>
        /// Singleplayer only, configures sc2 time mode
        /// False = Game behaves as during normal play, aka realtime mode
        /// True = Game will wait input and runs as fast as possible
        /// </summary>
        public const bool SinglestepMode = true;

        /// <summary>
        /// Number of milliseconds to sleep after each bot tick
        /// </summary>
        public static readonly TimeSpan TickLockSleep = TimeSpan.FromMilliseconds(1);

        /// <summary>
        /// Number of logical ingame steps to perform each bot tick
        /// The API can be slightly inconsistent and cause issues for lower values, ex orders not having being applied to the next tick
        /// </summary>
        public const int stepSize = 8;

        /// <summary>
        /// Number of workers to aim for at each base
        /// </summary>
        public const int TargetWorkerPerBase = 16 + 6;

        /// <summary>
        /// Inidicates that a fixed game seed should be used (meaning is match is exactly the same)
        /// </summary>
        public const bool UseFixedGameSeed = true;

        /// <summary>
        /// Value to be used for randomizing game behaviour when UseFixedGameSeed is set
        /// </summary>
        public const int FixedSeedValue = 123456798;

        /// <summary>
        /// Maximum amount of food allowed
        /// </summary>
        public const int FoodCap = 200;
    }
}
