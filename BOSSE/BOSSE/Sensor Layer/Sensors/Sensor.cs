/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
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

    //public class SensorEventArgs : EventArgs
    //{
    //    public HashSet<Unit> AffectedUnits = new HashSet<Unit>();

    //    public SensorEventArgs(HashSet<Unit> affectedUnits)
    //    {
    //        AffectedUnits = affectedUnits;
    //    }
    //}

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
        //public enum SensorId
        //{
        //    NotSet = 0,
        //    OwnStructureWasCompletedSensor,
        //    OwnUnitChangedTypeSensor,
        //    OwnMilitaryUnitWasCompletedSensor,
        //    OwnMilitaryUnitDiedSensor,

        //    EnemyArmyUnitDetectedFirstTimeSensor,
        //}
        //public SensorId Id = SensorId.NotSet;


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
        public abstract void Tick();

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
