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
    using System.Linq;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Can be used to continue giving this unit a complex multi-part order over many frames
    /// Returns true when the order has been completed and should no longer be run, false = will be run again next tick
    /// </summary>
    public delegate bool ContinuousUnitOrder();

    /// <summary>
    /// Handles more complicated orders 
    /// </summary>
    public class OrderManager : Manager
    {
        /// <summary>
        /// List of queued continuous orders
        /// </summary>
        private readonly List<ContinuousUnitOrder> GivenContinuousOrders = new List<ContinuousUnitOrder>();

        public override void OnFrameTick()
        {
            foreach (ContinuousUnitOrder orderIter in GivenContinuousOrders)
            {
                this.RunOrder(orderIter);
            }
        }

        public void AddOrder(ContinuousUnitOrder newOrder)
        {
            Log.Info("Added new continuous order");
            GivenContinuousOrders.Add(newOrder);
            RunOrder(newOrder);
        }

        /// <summary>
        /// Gives this unit new orders if any continious orders have been given
        /// </summary>
        private void RunOrder(ContinuousUnitOrder orderToRun)
        {
            try
            {
                bool orderCompleted = orderToRun();
                if (orderCompleted)
                {
                    Log.Info("Completed a continuous order");
                    this.GivenContinuousOrders.Remove(orderToRun);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Cought exception in order manager: " + Environment.NewLine + ex);
            }
        }
    }
}
