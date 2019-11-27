/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using SC2APIProtocol;

    /// <summary>
    /// Holds the current state of the match, updates each logical frame
    /// </summary>
    public static class CurrentGameState
    {
        public static ResponseGameInfo gameInfo;
        public static ResponseData gameData;
        public static ResponseObservation obs;

        public static uint CurrentSupply { get => obs.Observation.PlayerCommon.FoodUsed; }
        public static uint MaxSupply { get => CurrentGameState.obs.Observation.PlayerCommon.FoodCap; }
        public static uint Minerals { get => CurrentGameState.obs.Observation.PlayerCommon.Minerals; }
        public static uint Vespene { get => CurrentGameState.obs.Observation.PlayerCommon.Vespene; }
        public static ulong Frame { get => obs.Observation.GameLoop; }
    }
}
