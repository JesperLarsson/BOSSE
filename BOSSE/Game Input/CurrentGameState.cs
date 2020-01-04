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
            
            uint depotFood = (uint)(GetUnitCountTotal(new HashSet<UnitId>() { UnitId.SUPPLY_DEPOT, UnitId.SUPPLY_DEPOT_LOWERED }) * houseInfo.FoodProvided);
            uint ccFood = (uint)(GetUnitCountTotal(UnitId.COMMAND_CENTER) * ccInfo.FoodProvided);

            return depotFood + ccFood;
        }
    }
}
