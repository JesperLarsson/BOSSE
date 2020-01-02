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
        // Common type filters
        public SensorFilterComparison AcceptAllLambda = unfilteredList => unfilteredList;
        public SensorFilterComparison StructuresOnlyLambda = unfilteredList => new HashSet<Unit>(unfilteredList.Where(unitIter => UnitConstants.Structures.Contains((UnitId)unitIter.UnitType)));

        /// <summary>
        /// Callback events which are trigged when the sensor detects whatever it's looking for
        /// </summary>
        private readonly List<KeyValuePair<SensorEventHandler, SensorFilterComparison>> SensorTriggeredEventTest = new List<KeyValuePair<SensorEventHandler, SensorFilterComparison>>();

        /// <summary>
        /// Updates sensor logic
        /// </summary>
        public abstract void OnFrameTick();

        public void AddHandler(SensorEventHandler handler)
        {
            AddHandler(handler, AcceptAllLambda);
        }

        public void AddHandler(SensorEventHandler handler, SensorFilterComparison filterLambda)
        {
            Log.Bulk("Sensor: New subscriber to " + this);

            var pair = new KeyValuePair<SensorEventHandler, SensorFilterComparison>(handler, filterLambda);
            SensorTriggeredEventTest.Add(pair);
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
            // Call interested subscribers
            foreach (var iter in SensorTriggeredEventTest)
            {
                HashSet<Unit> filteredSet = iter.Value(args);
                if (filteredSet != null && filteredSet.Count > 0)
                {
                    iter.Key(args);
                }
            }
        }
    }
}
