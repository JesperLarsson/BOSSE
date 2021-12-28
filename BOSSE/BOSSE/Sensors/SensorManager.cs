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
    public class SensorManager : Manager
    {
        /// <summary>
        /// Name => Sensor instance mapping
        /// </summary>
        private readonly Dictionary<Type, Sensor> ActiveSensors = new Dictionary<Type, Sensor>();

        public override void Initialize()
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
        public override void OnFrameTick()
        {
            foreach (var iter in ActiveSensors)
            {
                iter.Value.OnFrameTick();
            }
        }
    }
}
