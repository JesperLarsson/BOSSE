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
    using System.Drawing;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static GeneralGameUtility;
    using static AbilityConstants;

    /// <summary>
    /// Utility functions for creating walls
    /// </summary>
    public static class WallinUtility
    {
        /// <summary>
        /// Building sizes for natural wall
        /// </summary>
        private static readonly List<Size> NaturalWallinConfigNoGap = new List<Size>()
        {
            new Size(3, 3),
            new Size(3, 3),
            new Size(2, 2),
            new Size(2, 2),
        };

        /// <summary>
        /// Natural wall will try to fit inside of this box
        /// </summary>
        private static readonly Size NaturalFittingBox = new Size(9, 6);

        /// <summary>
        /// Returns a configuration for building a wall in front of our natural expansion
        /// Null = No wall possible
        /// </summary>
        public static Wall GetNaturalWall()
        {
            Point2D estimatedLocation = GetNaturalWallEstimatedLocation();
            if (estimatedLocation == null)
            {
                return null;
            }

            // Build a list of candidates
#warning TODO: Invert box y if enemy spawning below us, x if to our left
            List<Wall> candidates = GetWallCandidatesAtLocationUsingBox(estimatedLocation, NaturalFittingBox, NaturalWallinConfigNoGap);
            if (candidates == null || candidates.Count == 0)
            {
                Log.SanityCheckFailed("Unable to find a combination of buildings that builds a wall at our natural");
                return null;
            }

            // Filter candidates
            List<Wall> validCandidates = new List<Wall>();
            Log.Warning("Build wall candidate list, searching for a valid blocking combination...");
            foreach (Wall iter in candidates)
            {
                bool isOk = IsGivenWallConfigurationViable(iter);
                if (isOk)
                {
                    //validCandidates.Add(iter);
                    Log.Info($"Determined natural wall location (from {validCandidates.Count} candidates) to be at {iter.GetCenterPosition().ToString2()}");
                    return iter;
                }
            }

            Log.Warning("No wall combination found");
            return null;
            //if (validCandidates.Count == 0)
            //{
            //    Log.SanityCheckFailed("Filtered out all natural wall candidates");
            //    return null;
            //}

            //// Return the candidate with the closest distance to our natural
            //float closestDistanceSq = float.MaxValue;
            //Wall closestRef = null;
            //Point2D naturalPos = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.NaturalExpansion.GetMineralCenter();
            //foreach (Wall iter in validCandidates)
            //{
            //    float distanceSq = iter.GetCenterPosition().AirDistanceSquared(naturalPos);
            //    if (distanceSq < closestDistanceSq)
            //    {
            //        closestDistanceSq = distanceSq;
            //        closestRef = iter;
            //    }
            //}

            //Log.Info($"Determined natural wall location (from {validCandidates.Count} candidates) to be at {closestRef.GetCenterPosition().ToString2()}");
            //return closestRef;
        }

        /// <summary>
        /// Checks that the given wall would completely block access to our natural for the enemy and that buildings can be placed there
        /// This check is fairly slow as it performs pathfinding
        /// </summary>
        private static bool IsGivenWallConfigurationViable(Wall wallObj)
        {
            ImageData gridMap = CurrentGameState.GameInformation.StartRaw.PlacementGrid;

            // Build override that disables the given tiles for pathfinding
            List<KeyValuePair<Point2D, bool>> overrideValues = new List<KeyValuePair<Point2D, bool>>();
            foreach (Wall.BuildingInWall iter in wallObj.Buildings)
            {
                Point2D buildingCenterPos = iter.BuildingPosition;
                Size buildingSize = iter.BuildingSize;

                int startX = (int)buildingCenterPos.X - ((buildingSize.Width - 1) / 2);
                int startY = (int)buildingCenterPos.Y - ((buildingSize.Height - 1) / 2);
                int endX = (int)buildingCenterPos.X + ((buildingSize.Width - 1) / 2);
                int endY = (int)buildingCenterPos.Y + ((buildingSize.Height - 1) / 2);
                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        // Optimization - Simply check building placement grid to start, this will filter away most calls
                        bool canBePlaced = gridMap.GetBit(x, y) != 0;
                        if (!canBePlaced)
                        {
                            return false;
                        }

                        var pair = new KeyValuePair<Point2D, bool>(new Point2D(x, y), false);
                        overrideValues.Add(pair);
                    }
                }
            }

            // See if a path is still possible
            ResourceCluster natural = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.NaturalExpansion;
            ResourceCluster enemy = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.EnemyMainBase;

            PathFinder pathFinderObj = new PathFinder();
            pathFinderObj.Initialize(overrideValues);
            LinkedList<BossePathNode> path = pathFinderObj.FindPath(natural.GetMineralCenter(), enemy.GetMineralCenter());
            if (path == null || path.Count == 0)
            {
                Log.Info("Found a wall config that completely blocks natural access");
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a list of possible wall combinations that can be used in the given area
        /// </summary>
        private static List<Wall> GetWallCandidatesAtLocationUsingBox(Point2D estimatedLocation, Size boxingParameters, List<Size> buildingSizesToUse)
        {
            List<Wall> workingList = new List<Wall>();

            // 1. Create placeholder wall
            workingList.Add(new Wall());
            //for (int index = 0; index < (boxingParameters.Width * boxingParameters.Height); index++)
            //{
            //    workingList.Add(new Wall());
            //}

            // 2. Take each existing wall and add each building, in every possible combination
            foreach (Size sizeIter in buildingSizesToUse)
            {
                List<Wall> newList = new List<Wall>();

                foreach (Wall existingWalliter in workingList)
                {
                    for (int baseXOffset = 0; baseXOffset < boxingParameters.Width; baseXOffset++)
                    {
                        for (int baseYOffset = 0; baseYOffset < boxingParameters.Height; baseYOffset++)
                        {
                            Wall newWall = new Wall(existingWalliter);

                            Wall.BuildingInWall buildingObj = new Wall.BuildingInWall(sizeIter, new Point2D(estimatedLocation.X + baseXOffset, estimatedLocation.Y + baseYOffset));
                            newWall.Buildings.Add(buildingObj);

                            if (IsThereBuildingOverlapInWallConfig(newWall))
                                continue;

                            newList.Add(newWall);
                        }
                    }
                }

                workingList = newList;
            }

            //// 3. Remove overlapping building
            //FilterOverlappingBuildings(workingList);

            // Done
            Log.Info("Built wall initial candidate list of size " + workingList.Count);
            return workingList;
        }

        //private static void FilterOverlappingBuildings(List<Wall> list)
        //{
        //    // Check overlap and that all buildings were placed
        //    List<Wall> wallsToRemove = new List<Wall>();
        //    foreach (Wall iter in list)
        //    {
        //        if (IsThereBuildingOverlapInWallConfig(iter))
        //        {
        //            wallsToRemove.Add(iter);
        //        }
        //    }

        //    foreach (Wall wallIter in wallsToRemove)
        //    {
        //        list.Remove(wallIter);
        //    }
        //    Log.Info("Removed " + wallsToRemove.Count + " invalid configurations from initial wall plan");
        //}

        private static bool IsThereBuildingOverlapInWallConfig(Wall config)
        {
            // outer = x, inner = y, if it exists something was built there
            Dictionary<int, Dictionary<int, object>> takentiles = new Dictionary<int, Dictionary<int, object>>();
            foreach (Wall.BuildingInWall iter in config.Buildings)
            {
                Point2D buildingCenterPos = iter.BuildingPosition;
                Size buildingSize = iter.BuildingSize;

                int startX = (int)buildingCenterPos.X - ((buildingSize.Width - 1) / 2);
                int startY = (int)buildingCenterPos.Y - ((buildingSize.Height - 1) / 2);
                int endX = (int)buildingCenterPos.X + ((buildingSize.Width - 1) / 2);
                int endY = (int)buildingCenterPos.Y + ((buildingSize.Height - 1) / 2);

                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        if (!takentiles.ContainsKey(x))
                        {
                            takentiles[x] = new Dictionary<int, object>();
                            takentiles[x][y] = true;
                            continue; // ok, first x
                        }

                        if (!takentiles[x].ContainsKey(y))
                        {
                            takentiles[x][y] = true;
                            continue; // ok, first y
                        }

                        // Another building used this coordiante
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns an estimated location for where to place our natural wall
        /// </summary>
        private static Point2D GetNaturalWallEstimatedLocation()
        {
            List<ChokePointGroup> naturalChokeGroups = BOSSE.MapAnalysisRef.AnalysedStaticMapRef.ChokePointCollections[BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.NaturalExpansion.ClusterId][BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.EnemyMainBase.ClusterId].ChokePointGroups;
            if (naturalChokeGroups == null || naturalChokeGroups.Count == 0)
            {
                Log.SanityCheckFailed("No chokepoint around natural found");
                return null;
            }

            ChokePointGroup naturalChokePoint = naturalChokeGroups[0];
            if (!naturalChokePoint.GetCenterOfChoke().IsWithinRange(BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.NaturalExpansion.GetMineralCenter(), 30))
            {
                Log.SanityCheckFailed("Natural chokepoint is too far away, or another chokepoint was detected");
                return null;
            }

            return naturalChokePoint.GetCenterOfChoke();
        }
    }
}
