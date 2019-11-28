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

        public static uint CurrentSupply { get => ObservationState.Observation.PlayerCommon.FoodUsed; }
        public static uint MaxSupply { get => ObservationState.Observation.PlayerCommon.FoodCap; }
        public static uint Minerals { get => ObservationState.Observation.PlayerCommon.Minerals; }
        public static uint Vespene { get => ObservationState.Observation.PlayerCommon.Vespene; }
        public static ulong Frame { get => ObservationState.Observation.GameLoop; }
    }
}
