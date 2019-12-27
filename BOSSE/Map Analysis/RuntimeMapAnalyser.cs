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
        public static AnalysedRuntimeMap AnalyseCurrentMapInitial()
        {
            var resourceClusters = FindResourceClusters(
                out ResourceCluster enemyMainRef, out ResourceCluster ourMainRef,
                out ResourceCluster enemyNaturalRef, out ResourceCluster ourNaturalRef,
                out ResourceCluster enemyThirdRef, out ResourceCluster ourThirdRef
                );

            // Calculate CC position for all clusters so that it's precached for later use (it also requests info from sc2, so we want to frontload the work if possible)
            foreach (var iter in resourceClusters.Values)
            {
                iter.GetCommandCenterPosition();
            }

            AnalysedRuntimeMap completedMap = new AnalysedRuntimeMap(
                allClusters: resourceClusters,
                mainBase: ourMainRef,
                naturalExpansion: ourNaturalRef,
                thirdExpansion: ourThirdRef,
                enemyMainBase: enemyMainRef,
                enemyNaturalExpansion: enemyNaturalRef,
                enemyThirdExpansion: enemyThirdRef
                );
            return completedMap;
        }

        public static void AnalyseCurrentMapPostStatic()
        {
            foreach (KeyValuePair<long, Dictionary<long, ChokepointCollectionBetweenPoints>> fromIter in BOSSE.MapAnalysisRef.AnalysedStaticMapRef.ChokePointCollections)
            {
                foreach (KeyValuePair<long, ChokepointCollectionBetweenPoints> toIter in fromIter.Value)
                {
                    ChokepointCollectionBetweenPoints chokepointCollection = toIter.Value;
                    CalculateChokepointGroups(chokepointCollection);
                }
            }
        }

        /// <summary>
        /// Groups chokepoints on all paths
        /// </summary>
        private static void CalculateChokepointGroups(ChokepointCollectionBetweenPoints chokepointCollection)
        {
            const byte chokeMinValueThreshold = 240;
            const int chokeDistanceThreshold = 15;
            TileMap<byte> map = chokepointCollection.ChokeScore;

            // 1. Create initial choke groups
            List<ChokePointGroup> chokeGroups = new List<ChokePointGroup>();
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    Point2D currentPos = new Point2D(x, y);
                    byte value = map.GetTile(x, y);
                    if (value < chokeMinValueThreshold)
                        continue;

                    // Check if tile belongs to an existing chokepoint
                    bool belongsInExisting = false;
                    foreach (ChokePointGroup chokeIter in chokeGroups)
                    {
                        Point2D centerPos = chokeIter.GetCenterOfChoke();
                        if (centerPos.IsWithinRange(currentPos, chokeDistanceThreshold, true))
                        {
                            belongsInExisting = true;
                            chokeIter.ChokeMap.Add(currentPos);
                            break;
                        }
                    }

                    // Create new choke group
                    if (!belongsInExisting)
                    {
                        ChokePointGroup groupObj = new ChokePointGroup();
                        groupObj.ChokeMap.Add(currentPos);
                        chokeGroups.Add(groupObj);
                    }
                }
            }

#warning TODO: Post-process and join groups if necessary, same as resource clustering
            chokepointCollection.ChokePointGroups = chokeGroups;
        }

        /// <summary>
        /// Clusters resources into groups (ie possible base locations). Returns ClusterID=>Instance mapping
        /// </summary>
        private static Dictionary<long, ResourceCluster> FindResourceClusters(
        out ResourceCluster enemyMainRef, out ResourceCluster ourMainRef,
        out ResourceCluster enemyNaturalRef, out ResourceCluster ourNaturalRef,
        out ResourceCluster enemyThirdRef, out ResourceCluster ourThirdRef
        )
        {
            enemyMainRef = null;
            ourMainRef = null;
            enemyNaturalRef = null;
            ourNaturalRef = null;
            enemyThirdRef = null;
            ourThirdRef = null;

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

            // Sanity check - 2 gases per cluster
            foreach (ResourceCluster clusterIter in clusters)
            {
                if (clusterIter.GasGeysers.Count != 2)
                {
                    Log.SanityCheckFailed("Unexpected gas count in resource cluster: " + clusterIter.GasGeysers.Count);
                }
            }

            // 4. Find main bases
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

            // 5. Find natural expansions
            float closestSelf = float.MaxValue;
            Unit workerRef = GetUnits(UnitConstants.Workers, onlyCompleted: true)[0]; // we use a worker since our CC location is not pathable
            foreach (ResourceCluster clusterIter in clusters)
            {
                if (clusterIter == ourMainRef)
                    continue;

                float? distance = clusterIter.GetMineralCenter().GroundDistanceAbsolute(workerRef.Position);
                if (distance == null)
                    continue;

                if (distance < closestSelf)
                {
                    closestSelf = distance.Value;
                    ourNaturalRef = clusterIter;
                }
            }
            if (ourNaturalRef == null)
            {
                Log.SanityCheckFailed("Unable to find our natural expansion");
            }

            float closestEnemy = float.MaxValue;
            foreach (ResourceCluster clusterIter in clusters)
            {
                if (clusterIter == enemyMainRef)
                    continue;

                float? distance = clusterIter.GetMineralCenter().GroundDistanceAbsolute(enemyMainRef.GetMineralCenter());
                if (distance == null)
                    continue;

                if (distance < closestEnemy)
                {
                    closestEnemy = distance.Value;
                    enemyNaturalRef = clusterIter;
                }
            }
            if (enemyNaturalRef == null)
            {
                Log.SanityCheckFailed("Unable to find enemy natural expansion");
            }

            // 6. Find third expansions
            float thirdSelfDistance = float.MaxValue;
            foreach (ResourceCluster clusterIter in clusters)
            {
                if (clusterIter == ourMainRef)
                    continue;
                if (clusterIter == ourNaturalRef)
                    continue;

                float? distance = clusterIter.GetMineralCenter().GroundDistanceAbsolute(workerRef.Position);
                if (distance == null)
                    continue;

                if (distance < thirdSelfDistance)
                {
                    thirdSelfDistance = distance.Value;
                    ourThirdRef = clusterIter;
                }
            }
            if (ourThirdRef == null)
            {
                Log.SanityCheckFailed("Unable to find our third expansion");
            }

            float thirdEnemyDistance = float.MaxValue;
            foreach (ResourceCluster clusterIter in clusters)
            {
                if (clusterIter == enemyMainRef)
                    continue;
                if (clusterIter == enemyNaturalRef)
                    continue;

                float? distance = clusterIter.GetMineralCenter().GroundDistanceAbsolute(enemyMainRef.GetMineralCenter());
                if (distance == null)
                    continue;

                if (distance < thirdEnemyDistance)
                {
                    thirdEnemyDistance = distance.Value;
                    enemyThirdRef = clusterIter;
                }
            }
            if (enemyThirdRef == null)
            {
                Log.SanityCheckFailed("Unable to find enemy third expansion");
            }

            // OK - Done
            Dictionary<long, ResourceCluster> resultDict = new Dictionary<long, ResourceCluster>();
            foreach (ResourceCluster iter in clusters)
            {
                long id = iter.ClusterId;
                if (resultDict.ContainsKey(id))
                    Log.SanityCheckFailed("Duplicate resource clusters found " + iter + " and " + resultDict[id]);

                resultDict[id] = iter;
            }
            return resultDict;
        }
    }
}
