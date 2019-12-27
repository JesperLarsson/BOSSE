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
    /// Performs runtime map analysis
    /// </summary>
    public static class RuntimeMapAnalyser
    {
        public static AnalysedRuntimeMap AnalyseCurrentMap()
        {
            var resourceClusters = FindBaseLocations(out ResourceCluster enemyMainRef, out ResourceCluster ourMainRef);

            AnalysedRuntimeMap completedMap = new AnalysedRuntimeMap(
                allClusters: resourceClusters,
                mainBase: ourMainRef,
                naturalExpansion: null,
                thirdExpansion: null,
                enemyMainBase: enemyMainRef,
                enemyNaturalExpansion: null,
                enemyThirdExpansion: null
                );
            return completedMap;
        }

        private static Dictionary<int, ResourceCluster> FindBaseLocations(out ResourceCluster enemyMainRef, out ResourceCluster ourMainRef)
        {
            enemyMainRef = null;
            ourMainRef = null;

            const int clusteringDistance = 14;
            List<ResourceCluster> clusters = new List<ResourceCluster>();
            List<Unit> mineralFields = GetUnits(UnitConstants.MineralFields, alliance: Alliance.Neutral);

            // 1. Group mineral fields into clusters based on distance from eachother
            foreach (Unit mineralIter in mineralFields)
            {
                // Check if it belongs to an existing field
                bool belongsInExistingField = false;
                foreach (ResourceCluster clusterIter in clusters)
                {
                    if (mineralIter.Position.IsWithinRange(clusterIter.GetMineralCenter(), clusteringDistance, true))
                    {
                        belongsInExistingField = true;
                        clusterIter.MineralFields.Add(mineralIter);
                        break;
                    }
                }
                if (belongsInExistingField)
                    continue;

                // No match - Create new resource cluster
                ResourceCluster newCluster = new ResourceCluster();
                newCluster.MineralFields.Add(mineralIter);
                clusters.Add(newCluster);
            }

            // 2. Join together clusters that are close together
            List<ResourceCluster> clustersToRemove = new List<ResourceCluster>();
            while (true)
            {
                foreach (ResourceCluster joinTargetCluster in clusters)
                {
                    if (clustersToRemove.Contains(joinTargetCluster))
                        continue;

                    foreach (ResourceCluster checkClusterIter in clusters)
                    {
                        if (clustersToRemove.Contains(checkClusterIter))
                            continue;
                        if (checkClusterIter == joinTargetCluster)
                            continue;

                        if (checkClusterIter.MineralFields.Count == 0)
                        {
                            clustersToRemove.Add(checkClusterIter);
                            continue;
                        }

                        bool inRange = checkClusterIter.GetMineralCenter().IsWithinRange(joinTargetCluster.GetMineralCenter(), clusteringDistance);
                        if (!inRange)
                            continue;

                        joinTargetCluster.MineralFields.AddRange(checkClusterIter.MineralFields);
                        clustersToRemove.Add(checkClusterIter);
                    }
                }

                if (clustersToRemove.Count == 0)
                {
                    break; // No more can be joined together
                }

                foreach (ResourceCluster toRemove in clustersToRemove)
                {
                    clusters.Remove(toRemove);
                }
                clustersToRemove.Clear();
            }
            Log.Info("RuntimeAnalysis: Found " + clusters.Count + " resource clusters");

            if (clusters.Count < 2)
            {
                Log.SanityCheckFailed("Unexpected amount of resource clusters found");
            }

            // Sanity check - Each mineral field should only appear once
            HashSet<ulong> usedMineralFields = new HashSet<ulong>();
            foreach (ResourceCluster clusterIter in clusters)
            {
                foreach (Unit mineralField in clusterIter.MineralFields)
                {
                    if (usedMineralFields.Contains(mineralField.Tag))
                    {
                        Log.SanityCheckFailed("Found repeated mineral field");
                    }

                    usedMineralFields.Add(mineralField.Tag);
                }
            }

            // Sanity check - All mineral fields should belong to a cluster
            foreach (Unit mineralIter in mineralFields)
            {
                if (!usedMineralFields.Contains(mineralIter.Tag))
                {
                    Log.SanityCheckFailed("Mineral field not assigned to cluster " + mineralIter);
                }
            }

            // 3. Add gas geysers to resource clusters
            List<Unit> gasGeysers = GetUnits(UnitConstants.GasGeysers, alliance: Alliance.Neutral);
            foreach (Unit gasIter in gasGeysers)
            {
                bool clusterMatch = false;
                foreach (ResourceCluster clusterIter in clusters)
                {
                    bool inRange = clusterIter.GetMineralCenter().IsWithinRange(gasIter.Position, clusteringDistance);
                    if (inRange)
                    {
                        clusterMatch = true;
                        clusterIter.GasGeysers.Add(gasIter);
                        break;
                    }
                }

                if (!clusterMatch)
                {
                    Log.SanityCheckFailed("Unable to add gas geyser " + gasIter + " to a resouce cluster");
                }
            }

            // Find important areas
#warning TODO: Get natural etc (closest resource center?)
            Point2D enemyLoc = GuessEnemyBaseLocation();
            foreach (ResourceCluster clusterIter in clusters)
            {
                bool containsMain = clusterIter.GetBoundingBox().Contains((int)Globals.MainBaseLocation.X, (int)Globals.MainBaseLocation.Y);
                if (containsMain)
                {
                    ourMainRef = clusterIter;
                }

                bool containsEnemyMain = clusterIter.GetBoundingBox().Contains((int)enemyLoc.X, (int)enemyLoc.Y);
                if (containsEnemyMain)
                {
                    enemyMainRef = clusterIter;
                }
            }
            if (ourMainRef == null)
            {
                Log.SanityCheckFailed("No resource cluster at our CC location");
            }
            if (enemyMainRef == null)
            {
                Log.SanityCheckFailed("Unable to find resource cluster for enemy main");
            }
            if (enemyMainRef == ourMainRef)
            {
                Log.SanityCheckFailed("Resource cluster contains both our and enemy mains");
            }

            // Done
            Dictionary<int, ResourceCluster> resultDict = new Dictionary<int, ResourceCluster>();
            foreach (ResourceCluster iter in clusters)
            {
                resultDict[iter.UniqueId] = iter;
            }
            return resultDict;
        }
    }
}
