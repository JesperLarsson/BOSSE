/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2020 Jesper Larsson

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
        /// Singleplayer only, sc2 will simulate the game as fast as possible
        /// False = Game behaves as during normal play, aka realtime mode
        /// True = Game will wait for our output and runs as fast as possible
        /// </summary>
        public const bool TickLockMode = true;

        /// <summary>
        /// Number of milliseconds to sleep after each bot tick
        /// </summary>
        public static TimeSpan TickLockSleep = TimeSpan.FromMilliseconds(0);

        /// <summary>
        /// Number of logical ingame steps to perform each bot tick
        /// The API can be slightly inconsistent and cause issues for lower values, ex orders not having being applied to the next tick
        /// </summary>
        public const int stepSize = 8;

        /// <summary>
        /// Number of logical frames per second of real time
        /// </summary>
        public const double FRAMES_PER_SECOND = 22.4;

        /// <summary>
        /// Bot will spawn as the given race
        /// </summary>
        public const Race SpawnAsRace = Race.Terran;

        /// <summary>
        /// We want at least this amount of supply as a margin before we reach the max
        /// </summary>
        public const int MinSupplyMargin = 4;

        /// <summary>
        /// Number of workers to aim for at each base
        /// </summary>
        public const int TargetWorkerPerBase = 16;
    }
}
