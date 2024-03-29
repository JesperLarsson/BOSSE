﻿/*
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
    using System.Drawing;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;

    using SC2APIProtocol;

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
        public const bool UseStepMode = true;

        /// <summary>
        /// Number of milliseconds to sleep after each bot tick
        /// Set to a high value along with step mode flag to make the game run in slow motion
        /// </summary>
        public static readonly TimeSpan TickLockSleep = TimeSpan.FromMilliseconds(1);

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

        /// <summary>
        /// Debug map to use
        /// </summary>
        public const string DebugMapName = "AcropolisLE.SC2Map";

        /// <summary>
        /// Debug opponent race
        /// </summary>
        public const Race DebugOpponentRace = Race.Zerg;

        /// <summary>
        /// Debug opponent difficulty (official AI)
        /// </summary>
        public const Difficulty DebugOpponentDifficulty = Difficulty.Easy;
        //public const Difficulty DebugOpponentDifficulty = Difficulty.VeryHard;

        /// <summary>
        /// Toggles walling with buildings, have been bug-prone on some maps and in some configurations
        /// </summary>
        public const bool EnableWalling = true;

        /// <summary>
        /// Building setup to use when walling off our natural
        /// </summary>
        public static readonly List<Size> WallConfiguration = new List<Size>()
        {
            new Size(3, 3),
            new Size(3, 3),
            new Size(2, 2),
        };

        //public static readonly List<Size> WallConfiguration = new List<Size>()
        //{
        //    new Size(3, 3),
        //    new Size(2, 2),
        //    new Size(2, 2),
        //    new Size(2, 2),
        //};
    }
}
