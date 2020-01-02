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
    /// Handles all running instances of <see cref="ContinuousUnitOrder"/>
    /// </summary>
    public class OrderManager : Manager
    {
        private readonly List<ContinuousUnitOrder> ActiveOrders = new List<ContinuousUnitOrder>();

        public override void OnFrameTick()
        {
            List<ContinuousUnitOrder> completedOrders = new List<ContinuousUnitOrder>();
            foreach (ContinuousUnitOrder orderIter in ActiveOrders)
            {
                bool orderCompleted = this.RunOrder(orderIter);

                if (orderCompleted)
                {
                    Log.Info("Completed a continuous order");
                    completedOrders.Add(orderIter);
                }
            }

            foreach (ContinuousUnitOrder removeIter in completedOrders)
            {
                ActiveOrders.Remove(removeIter);
            }
        }

        public void AddOrder(ContinuousUnitOrder newOrder)
        {
            Log.Info("Added new continuous order");

            bool completed = RunOrder(newOrder);
            if (!completed)
                ActiveOrders.Add(newOrder);
        }

        private bool RunOrder(ContinuousUnitOrder orderToRun)
        {
            try
            {
                bool orderCompleted = orderToRun();
                return orderCompleted;
            }
            catch (Exception ex)
            {
                Log.Error("Cought exception in order manager: " + Environment.NewLine + ex);
                return false;
            }
        }
    }
}
