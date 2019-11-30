/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using SC2APIProtocol;

    /// <summary>
    /// Holds the current state of the match, updates each logical tick
    /// </summary>
    public static class CurrentGameState
    {
        public static ResponseGameInfo GameInformation;
        public static ResponseData GameData;
        public static ResponseObservation ObservationState;

        public static uint CurrentSupply { get => ObservationState.Observation.PlayerCommon.FoodUsed; set => ObservationState.Observation.PlayerCommon.FoodUsed = value; }
        public static uint CurrentMinerals { get => ObservationState.Observation.PlayerCommon.Minerals; set => ObservationState.Observation.PlayerCommon.Minerals = value; }
        public static uint CurrentVespene { get => ObservationState.Observation.PlayerCommon.Vespene; set => ObservationState.Observation.PlayerCommon.Vespene = value; }
        public static uint MaxSupply { get => ObservationState.Observation.PlayerCommon.FoodCap; }

        /// <summary>
        /// Active logical frame counter, starts at 0
        /// </summary>
        public static ulong OnFrame { get => ObservationState.Observation.GameLoop; }
    }
}
