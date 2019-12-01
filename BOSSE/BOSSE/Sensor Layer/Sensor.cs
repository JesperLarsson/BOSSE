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

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static AbilityConstants;
    
    /// <summary>
    /// Reads game state and notifies other subscribing modules
    /// Uses the a subscriber dessign pattern
    /// </summary>
    public abstract class Sensor
    {
        public enum SensorId
        {
            NotSet = 0,
            OwnStructureCompletedSensor
        }
        public SensorId Id = SensorId.NotSet;

        /// <summary>
        /// Callback events which are trigged when the sensor detects whatever it's looking for
        /// </summary>
        public event EventHandler SensorTriggeredEvent;

        /// <summary>
        /// Updates sensor logic
        /// </summary>
        public abstract void Tick();

        public override string ToString()
        {
            return this.Id.ToString();
        }

        /// <summary>
        /// Called when the event is triggered, notifies all subscribers
        /// </summary>
        protected void Trigger(EventArgs args)
        {
            Log.Info("Sensor: Triggered " + this);
            SensorTriggeredEvent?.Invoke(this, args);
        }
    }
}
