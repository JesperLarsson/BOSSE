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
    using System.Linq;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static AbilityConstants;

    public delegate void SensorEventHandler(HashSet<Unit> affectedUnits);

    /// <summary>
    /// Optional sensor filter which can pre-filter the list of units before the callback is triggered
    /// </summary>
    public delegate HashSet<Unit> SensorFilterComparison(HashSet<Unit> affectedUnits);

    /// <summary>
    /// Reads game state and notifies other subscribing modules
    /// Uses the a subscriber dessign pattern
    /// </summary>
    public abstract class Sensor
    {
        /// <summary>
        /// Holds a single subscriber to this sensor
        /// </summary>
        public class SubscribedCallback
        {
            public SensorEventHandler EventHandler;
            public SensorFilterComparison Filter;
            public bool IsOneShot = false;

            public SubscribedCallback(SensorEventHandler eventHandler, SensorFilterComparison filter, bool isOneShot)
            {
                EventHandler = eventHandler;
                Filter = filter;
                IsOneShot = isOneShot;
            }
        }

        // Common type filters
        public SensorFilterComparison AcceptAllLambda = unfilteredList => unfilteredList;
        public SensorFilterComparison StructuresOnlyLambda = unfilteredList => new HashSet<Unit>(unfilteredList.Where(unitIter => UnitConstants.Structures.Contains((UnitId)unitIter.UnitType)));

        /// <summary>
        /// Callback events which are trigged when the sensor detects whatever it's looking for
        /// </summary>
        private readonly List<SubscribedCallback> SensorTriggeredEventTest = new List<SubscribedCallback>();
        
        /// <summary>
        /// Updates sensor logic
        /// </summary>
        public abstract void OnFrameTick();

        public void AddHandler(SensorEventHandler handler)
        {
            AddHandler(handler, AcceptAllLambda);
        }

        public void AddHandler(SensorEventHandler handler, SensorFilterComparison filterLambda, bool isOneShot = false)
        {
            Log.Bulk("Sensor: New subscriber to " + this);

            var obj = new SubscribedCallback(handler, filterLambda, isOneShot);
            SensorTriggeredEventTest.Add(obj);
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }

        /// <summary>
        /// Called when the event is triggered, notifies all subscribers
        /// </summary>
        protected void Trigger(HashSet<Unit> args)
        {
            List<SubscribedCallback> subscribersToRemove = new List<SubscribedCallback>();

            // Call interested subscribers
            foreach (SubscribedCallback iter in SensorTriggeredEventTest)
            {
                HashSet<Unit> filteredSet = iter.Filter(args);
                if (filteredSet != null && filteredSet.Count > 0)
                {
                    iter.EventHandler(args);

                    if (iter.IsOneShot)
                    {
                        subscribersToRemove.Add(iter);
                    }
                }
            }

            foreach (SubscribedCallback removeIter in subscribersToRemove)
            {
                SensorTriggeredEventTest.Remove(removeIter);
            }
        }
    }
}
