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
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static global::BOSSE.UnitConstants;

    /// <summary>
    /// Builds supply houses automatically
    /// </summary>
    public class HouseProviderManager : Manager
    {
        private int buildCount = 0;

        public void ForceBuildHouse()
        {
            this.BuildHouse();
        }

        public override void OnFrameTick()
        {
            if (this.NeedHouse())
                this.BuildHouse();
        }

        private bool NeedHouse()
        {
            uint currentAndPending = GetCurrentAndPendingSupply();
            if (currentAndPending >= BotConstants.FoodCap)
                return false;

            // This has a very high priority if we need more food
            uint minSupplyMargin = GetSupplyMargin();
            uint supplyDiff = GetAvailableSupplyIncludingPending();

            if (supplyDiff < minSupplyMargin)
                return true;
            else
                return false;
        }

        private void BuildHouse()
        {
            uint minSupplyMargin = GetSupplyMargin();
            uint availableSupply = GetAvailableSupplyIncludingPending();
            uint currentAndPending = GetCurrentAndPendingSupply();

            UnitTypeData houseInfo = GetUnitInfo(RaceHouseType());
            while (availableSupply < minSupplyMargin && currentAndPending < BotConstants.FoodCap)
            {
                if (CurrentMinerals < houseInfo.MineralCost)
                    return;

                // We disallow the first house from being used as a wall so that we always have a Pylon in our home base for later use
                bool allowAsWall = buildCount >= 1;
                Log.Info($"Building house (auto selecting position)...");

                BOSSE.ConstructionManagerRef.BuildAutoSelectPosition(RaceHouseType(), allowAsWall);

                availableSupply += (uint)houseInfo.FoodProvided;
                currentAndPending += (uint)houseInfo.FoodProvided;

                CurrentMinerals -= houseInfo.MineralCost;
                buildCount++;

#warning TODO: Figure out a better way of setting how many workers should be set on gas
                if (buildCount >= 4)
                    BOSSE.WorkerManagerRef.SetNumberOfWorkersOnGas(6);
                else if (buildCount >= 2)
                    BOSSE.WorkerManagerRef.SetNumberOfWorkersOnGas(3);
            }
        }

        private uint GetSupplyMargin()
        {
#warning TODO: Perhaps we could implement a way of "pre-reserving" supply depending on our current strategy, instead of relying on the game time as a heuristic
            TimeSpan uptime = GameUptime();

            if (CurrentMinerals > 1000)
                return 32;
            else if (uptime.TotalMinutes > 8)
                return 16;
            else if (uptime.TotalMinutes > 2)
                return 8;
            else
                return 4;
        }

        private uint GetAvailableSupplyIncludingPending()
        {
            uint currentAndPendingFood = GetCurrentAndPendingSupply();
            uint supplyDiff = currentAndPendingFood - CurrentGameState.UsedSupply;

            return supplyDiff;
        }
    }
}
