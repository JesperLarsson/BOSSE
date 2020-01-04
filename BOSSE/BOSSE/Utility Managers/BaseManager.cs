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
    /// Holds information about each base on the map, both our own and the enemies
    /// </summary>
    public class BaseManager : Manager
    {
        /// <summary>
        /// All known bases, ClusterId => Instance mapping
        /// </summary>
        private Dictionary<long, BaseLocation> KnownBases = new Dictionary<long, BaseLocation>();

        public override void Initialize()
        {
            // Add our main
            Unit mainCC = GetUnits(UnitConstants.ResourceCenters)[0];
            AddBase(mainCC);

            // Auto add bases as they ar ecompleted
            BOSSE.SensorManagerRef.GetSensor(typeof(EnemyResourceCenterDetectedFirstTimeSensor)).AddHandler(FoundNewEnemyCommandCenter);
            BOSSE.SensorManagerRef.GetSensor(typeof(OwnResourceCenterCompletedSensor)).AddHandler(OwnCommandCenterCompleted);
        }

        public List<BaseLocation> GetOwnBases()
        {
            List<BaseLocation> list = new List<BaseLocation>();

            foreach (var iter in KnownBases.Values)
            {
                if (iter.BelongsTo == Alliance.Self)
                {
                    list.Add(iter);
                }
            }

            return list;
        }

        private void OwnCommandCenterCompleted(HashSet<Unit> detectedUnits)
        {
            foreach (Unit ownCC in detectedUnits)
            {
                Log.Info("Completed own CC " + ownCC);
                AddBase(ownCC);
            }
        }

        /// <summary>
        /// Callback - We scouted a new enemy CC
        /// </summary>
        private void FoundNewEnemyCommandCenter(HashSet<Unit> detectedUnits)
        {
            foreach (Unit enemyCC in detectedUnits)
            {
                Log.Info("Scouted enemy CC " + enemyCC);
                AddBase(enemyCC);
            }
        }

        private void AddBase(Unit commandCenterUnit)
        {
            Point2D closeTo = commandCenterUnit.Position;

            ResourceCluster rc = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.FindClusterCloseTo(closeTo);
            if (rc == null)
            {
                Log.Warning("Unable to add base location for CC at " + closeTo.ToString2());
                return;
            }

            BaseLocation baseObj = new BaseLocation(commandCenterUnit, rc, commandCenterUnit.Alliance);

            long id = baseObj.CenteredAroundCluster.ClusterId;
            if (KnownBases.ContainsKey(id))
            {
                // Override anyway, maybe someone rebuilt a previous base
                Log.Info("NOTE: Duplicate bases at " + baseObj);
            }

            Log.Bulk("Added new known base: " + baseObj);
            KnownBases[id] = baseObj;
        }

        public override void OnFrameTick()
        {

        }
    }
}
