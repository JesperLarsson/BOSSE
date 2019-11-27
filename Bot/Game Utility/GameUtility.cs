/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace Bot
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
    /// Various helper functions for interacting with StarCraft 2
    /// </summary>
    public static class GameUtility
    {
        /// <summary>
        /// Converts seconds to number of logical frames
        /// </summary>
        public static ulong SecsToFrames(int seconds)
        {
            return (ulong)(GameConstants.FRAMES_PER_SECOND * seconds);
        }

        public static string GetUnitName(uint unitType)
        {
            return CurrentGameState.gameData.Units[(int)unitType].Name;
        }

        public static bool CanAfford(uint unitType)
        {
            var unitData = gameData.Units[(int)unitType];
            return (Minerals >= unitData.MineralCost) && (Vespene >= unitData.VespeneCost);
        }


    }
}
