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
    using System.Linq;

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
        //private static readonly List<Size> NaturalWallinConfigNoGap = new List<Size>()
        //{
        //    new Size(3, 3),
        //    new Size(3, 3),
        //    new Size(3, 3),
        //};
        private static readonly List<Size> NaturalWallinConfigNoGap = new List<Size>()
        {
            new Size(3, 3),
            new Size(2, 2),
            new Size(2, 2),
            new Size(2, 2),
        };

        private static Wall CachedNaturalWall = null;

        /// <summary>
        /// Returns a configuration for building a wall in front of our natural expansion
        /// Null = No wall possible
        /// </summary>
        public static Wall GetNaturalWall()
        {
            if (CachedNaturalWall == null)
            {
                CachedNaturalWall = CalculateNaturalWall();
            }
            return CachedNaturalWall;
        }

        private static Wall CalculateNaturalWall()
        {
            // 1. Estimate wall position
            Point2D estimatedLocation = GetNaturalWallEstimatedLocation();
            if (estimatedLocation == null)
            {
                return null;
            }
            int estimateX = (int)estimatedLocation.X;
            int estimateY = (int)estimatedLocation.Y;
            Log.Info("Estimate = " + estimateX + "/" + estimateY);

            // 2. Build wall configurations
            const int yTestRadius = 5;
            var foundWallsAtY = new Dictionary<int, Wall>();
            for (int y = estimateY - yTestRadius; y <= estimateY + yTestRadius; y++)
            {
                Log.Info("Testing Y = " + y);
                Wall wallObj = TryGetWallAtExactY(estimateX, y);
                if (wallObj != null)
                {
                    bool blocksPathinOk = IsGivenWallConfigurationViable(wallObj);

                    if (blocksPathinOk)
                    {
                        Log.Info("Found natural wall " + wallObj);
                        foundWallsAtY[y] = wallObj;
                    }
                    else
                    {
                        Log.Info("Failed wall pathfinding test for Y=" + y);
                    }
                }
            }

            if (foundWallsAtY.Count == 0)
            {
                Log.SanityCheckFailed("Unable to find natural wall");
                return null;
            }
            if (foundWallsAtY.Count == 1)
            {
                Wall onlyWallObj = foundWallsAtY[0];
                Log.Info("Using the only possible wall configuration: " + onlyWallObj);
                return onlyWallObj;
            }

            // 3. Choose the option that is most "in the middle"
            //   this somewhat works around an issue with our pathfinding that it doesn't consider diagonals
            var sortedY = foundWallsAtY.Keys.ToList();
            Log.Info("Possible natural wall configurations: " + sortedY.Count);
            sortedY.Sort();
            int medianIndex = (int)(sortedY.Count / 2);
            int usedY = sortedY[medianIndex];

            Wall usedWall = foundWallsAtY[usedY];
            Log.Info("Using median natural wall: " + usedWall);
            return usedWall;
        }

        private static Wall TryGetWallAtExactY(int estimateX, int y)
        {
            var wallConfig = NaturalWallinConfigNoGap;
            ImageData gridMap = CurrentGameState.GameInformation.StartRaw.PlacementGrid;

            // 1. Search for starting position = first buildable tile
            const int radiusSearchX = 5;
            int buildingStartsAtLeftX = -1;
            for (int x = estimateX - radiusSearchX; x < estimateX + radiusSearchX; x++)
            {
                // Is this location buildable?
                bool canBePlaced = gridMap.GetBit(x, y) != 0;
                if (canBePlaced)
                {
                    buildingStartsAtLeftX = x;
                    break;
                }
            }
            if (buildingStartsAtLeftX == -1)
            {
                Log.SanityCheckFailed("  Unable to determine wall start X");
                return null;
            }
            Log.Info("  Starting nat wall at x = " + buildingStartsAtLeftX + " (estimate was " + estimateX + ")");

            // 2. Make sure we can fit our buildings here
            int exactWidthRequired = 0;
            foreach (Size iter in wallConfig)
            {
                exactWidthRequired += iter.Width;
            }

            for (int x = buildingStartsAtLeftX; x < (buildingStartsAtLeftX + exactWidthRequired); x++)
            {
                bool canBePlaced = gridMap.GetBit(x, y) != 0;
                if (!canBePlaced)
                {
                    Log.Info("  Not possible to build a wall at test position");
                    return null;
                }
            }

            // 3. Next X should not be buildable in order to create a wall
            //bool nextTileBuildable = gridMap.GetBit(buildingStartsAtLeftX + exactWidthRequired + 1, y) != 0;
            //if (nextTileBuildable)
            //{
            //    Log.Info("  Gap too wide at y = " + y);
            //    return null;
            //}

            // 4. Place buildings
            Wall wallObj = new Wall();
            int nextBuildingLeftX = buildingStartsAtLeftX;
            foreach (Size buildingSize in wallConfig)
            {
                int sizeDiffToCenter = (buildingSize.Width - 1) / 2;
                Wall.BuildingInWall buildingDef = new Wall.BuildingInWall(buildingSize, new Point2D(nextBuildingLeftX + sizeDiffToCenter + 0.5f, y));
                wallObj.Buildings.Add(buildingDef);

                if (!BuildingFitsInPosition(buildingDef))
                {
                    Log.Info("  Builing too thick at " + buildingDef.BuildingPosition.ToString2());
                    return null;
                }

                nextBuildingLeftX += buildingSize.Width;
            }

            return wallObj;
        }

        private static bool BuildingFitsInPosition(Wall.BuildingInWall building)
        {
            int sizeYDiffToCenter = (building.BuildingSize.Height - 1) / 2;
            int sizeXDiffToCenter = (building.BuildingSize.Width - 1) / 2;

            int fromX = (int)building.BuildingPosition.X - sizeXDiffToCenter;
            int fromY = (int)building.BuildingPosition.Y - sizeYDiffToCenter;
            int toX = fromX + building.BuildingSize.Width;
            int toY = fromY + building.BuildingSize.Height;

            ImageData gridMap = new ImageData(CurrentGameState.GameInformation.StartRaw.PlacementGrid);
            for (int x = fromX; x < toX; x++)
            {
                for (int y = fromY; y < toY; y++)
                {
                    bool canBePlaced = gridMap.GetBit(x, y) != 0;
                    if (!canBePlaced)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks that the given wall would completely block access to our natural for the enemy and that buildings can be placed there
        /// This check is fairly slow as it performs pathfinding
        /// </summary>
        private static bool IsGivenWallConfigurationViable(Wall wallObj)
        {
            // Build override that disables the given tiles for pathfinding
            List<KeyValuePair<Point2D, bool>> overrideValues = new List<KeyValuePair<Point2D, bool>>();
            foreach (Wall.BuildingInWall iter in wallObj.Buildings)
            {
                Point2D buildingCenterPos = iter.BuildingPosition;
                Size buildingSize = iter.BuildingSize;

                int startX = (int)buildingCenterPos.X - ((buildingSize.Width - 1) / 2);
                int startY = (int)buildingCenterPos.Y - ((buildingSize.Height - 1) / 2);
                int endX = startX + buildingSize.Width;
                int endY = startY + buildingSize.Height;
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        bool isWall = true;
                        var pair = new KeyValuePair<Point2D, bool>(new Point2D(x, y), isWall);
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

        ///// <summary>
        ///// Gets a list of possible wall combinations that can be used in the given area
        ///// </summary>
        //private static List<Wall> GetWallCandidatesAtLocationUsingBox(Point2D estimatedLocation, Size boxingParameters, List<Size> buildingSizesToUse)
        //{
        //    List<Wall> workingList = new List<Wall>();

        //    // 1. Create placeholder wall
        //    workingList.Add(new Wall());
        //    //for (int index = 0; index < (boxingParameters.Width * boxingParameters.Height); index++)
        //    //{
        //    //    workingList.Add(new Wall());
        //    //}

        //    // 2. Take each existing wall and add each building, in every possible combination
        //    foreach (Size sizeIter in buildingSizesToUse)
        //    {
        //        List<Wall> newList = new List<Wall>();

        //        foreach (Wall existingWalliter in workingList)
        //        {
        //            for (int baseXOffset = 0; baseXOffset < boxingParameters.Width; baseXOffset++)
        //            {
        //                for (int baseYOffset = 0; baseYOffset < boxingParameters.Height; baseYOffset++)
        //                {
        //                    Wall newWall = new Wall(existingWalliter);

        //                    Wall.BuildingInWall buildingObj = new Wall.BuildingInWall(sizeIter, new Point2D(estimatedLocation.X + baseXOffset, estimatedLocation.Y + baseYOffset));
        //                    newWall.Buildings.Add(buildingObj);

        //                    if (IsThereBuildingOverlapInWallConfig(newWall))
        //                        continue;

        //                    newList.Add(newWall);
        //                }
        //            }
        //        }

        //        workingList = newList;
        //    }

        //    //// 3. Remove overlapping building
        //    //FilterOverlappingBuildings(workingList);

        //    // Done
        //    Log.Info("Built wall initial candidate list of size " + workingList.Count);
        //    return workingList;
        //}

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
            return BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.GetNaturalDefensePos();
        }
    }
}
