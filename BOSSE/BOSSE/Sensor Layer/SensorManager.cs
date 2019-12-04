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
    using System.Reflection;
    using System.Linq;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static AbilityConstants;
    
    /// <summary>
    /// Holds all of our sensors
    /// </summary>
    public class SensorManager
    {
        /// <summary>
        /// Name => Sensor instance mapping
        /// </summary>
        private readonly Dictionary<Type, Sensor> ActiveSensors = new Dictionary<Type, Sensor>();

        public void Initialize()
        {
            // Create an object of each sensor
            foreach (Type type in Assembly.GetAssembly(typeof(Sensor)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Sensor))))
            {
                AddSensor((Sensor)Activator.CreateInstance(type));
            }      
        }

        public void AddSensor(Sensor newSensor)
        {
            Type sensorType = newSensor.GetType();

            if (ActiveSensors.ContainsKey(sensorType))
            {
                Log.SanityCheckFailed("Already have a sensor with name " + newSensor.ToString());
            }

            ActiveSensors[sensorType] = newSensor;
        }

        public Sensor GetSensor(Type id)
        {
            if (!ActiveSensors.ContainsKey(id))
            {
                return null;
            }

            return ActiveSensors[id];
        }

        /// <summary>
        /// Updates all sensors
        /// </summary>
        public void Tick()
        {
            foreach (var iter in ActiveSensors)
            {
                iter.Value.Tick();
            }
        }
    }
}
