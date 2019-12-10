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
    using static GeneralGameUtility;
    using static UnitConstants;

    /// <summary>
    /// Holds the current state of the match, observational data is updated each tick
    /// </summary>
    public static class CurrentGameState
    {
        public static ResponseGameInfo GameInformation;
        public static ResponseData GameData;
        public static ResponseObservation ObservationState;

        // Current supply, does not take into account pending supply
        public static uint FreeSupply { get => MaxSupply - UsedSupply;}
        public static uint UsedSupply { get => ObservationState.Observation.PlayerCommon.FoodUsed; set => ObservationState.Observation.PlayerCommon.FoodUsed = value; }
        public static uint MaxSupply { get => ObservationState.Observation.PlayerCommon.FoodCap; }

        public static uint CurrentMinerals { get => ObservationState.Observation.PlayerCommon.Minerals; set => ObservationState.Observation.PlayerCommon.Minerals = value; }
        public static uint CurrentVespene { get => ObservationState.Observation.PlayerCommon.Vespene; set => ObservationState.Observation.PlayerCommon.Vespene = value; }

        public static uint GetCurrentAndPendingSupply()
        {
            UnitTypeData houseInfo = GetUnitInfo(UnitId.SUPPLY_DEPOT);
            UnitTypeData ccInfo = GetUnitInfo(UnitId.COMMAND_CENTER);

            uint depotFood = (uint)(GetUnitCountTotal(UnitId.SUPPLY_DEPOT) * houseInfo.FoodProvided);
            uint ccFood = (uint)(GetUnitCountTotal(UnitId.COMMAND_CENTER) * ccInfo.FoodProvided);

            return depotFood + ccFood;
        }
    }
}
