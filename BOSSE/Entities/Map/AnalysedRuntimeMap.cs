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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// Map data that is calculated during runtime
    /// </summary>
    public class AnalysedRuntimeMap
    {
        /// <summary>
        /// All resource clusters on the map. Id => instance mapping
        /// </summary>
        public Dictionary<long, ResourceCluster> ResourceClusters = new Dictionary<long, ResourceCluster>();

        public ResourceCluster MainBase;
        public ResourceCluster NaturalExpansion;
        public ResourceCluster ThirdExpansion;

        public ResourceCluster EnemyMainBase;
        public ResourceCluster EnemyNaturalExpansion;
        public ResourceCluster EnemyThirdExpansion;

        private Point2D CachedNaturalDefensePos = null;

        public AnalysedRuntimeMap(
            Dictionary<long, ResourceCluster> allClusters,
            ResourceCluster mainBase, ResourceCluster naturalExpansion, ResourceCluster thirdExpansion,
            ResourceCluster enemyMainBase, ResourceCluster enemyNaturalExpansion, ResourceCluster enemyThirdExpansion)
        {
            ResourceClusters = allClusters;
            MainBase = mainBase;
            NaturalExpansion = naturalExpansion;
            ThirdExpansion = thirdExpansion;
            EnemyMainBase = enemyMainBase;
            EnemyNaturalExpansion = enemyNaturalExpansion;
            EnemyThirdExpansion = enemyThirdExpansion;
        }

        /// <summary>
        /// Finds a cluster close to the given point if possible, otherwise NULL
        /// </summary>
        public ResourceCluster FindClusterCloseTo(Point2D searchCoordinate)
        {
            const int searchRange = 20;

            foreach (ResourceCluster iter in ResourceClusters.Values)
            {
                bool inRange = iter.GetMineralCenter().IsWithinRange(searchCoordinate, searchRange);
                if (inRange)
                    return iter;
            }

            Log.SanityCheckFailed("Searched for resource cluster that wasn't found at " + searchCoordinate.ToString2());
            return null;
        }

        /// <summary>
        /// Returns a good position to defend our natural expansion
        /// </summary>
        public Point2D GetNaturalDefensePos()
        {
            if (CachedNaturalDefensePos == null)
            {
                // Walk some steps away from our natural and use that as the defense position
                const int numberOfTilesToForward = 12;
                var path = BOSSE.PathFinderRef.FindPath(NaturalExpansion.GetCommandCenterPosition(), EnemyMainBase.GetMineralCenter());
                if (path == null || path.Count == 0)
                {
                    Log.SanityCheckFailed("Unable to determine natural defense position");
                }

                int currentTile = 0;
                BossePathNode nodeToUse = null;
                foreach (BossePathNode iter in path)
                {
                    nodeToUse = iter;
                    currentTile++;
                    if (currentTile >= numberOfTilesToForward)
                        break;
                }

                CachedNaturalDefensePos = new Point2D(nodeToUse.X, nodeToUse.Y);

                // Add to debug GUI
                KeyValuePair<Point2D, string> debugPoint = new KeyValuePair<Point2D, string>(CachedNaturalDefensePos, "NatDef");
                DebugGui.TerrainDebugMap.MarkedPoints.Add(debugPoint);
            }

            return CachedNaturalDefensePos;
        }
    }
}
