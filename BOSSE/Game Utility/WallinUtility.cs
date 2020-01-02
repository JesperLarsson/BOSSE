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

        // Gap support could be added simply by inserting a 1x1 tile in the configuration
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
            Log.Bulk("Estimate = " + estimateX + "/" + estimateY);

            // 2. Build wall configurations
            const int yTestRadius = 5;
            var foundWallsAtY = new Dictionary<int, Wall>();
            for (int y = estimateY - yTestRadius; y <= estimateY + yTestRadius; y++)
            {
                Log.Bulk("Testing Y = " + y);
                Wall wallObj = TryGetWallAtExactY(estimateX, y);
                if (wallObj != null)
                {
                    bool blocksPathinOk = IsGivenWallConfigurationViable(wallObj);

                    if (blocksPathinOk)
                    {
                        Log.Bulk("Found natural wall " + wallObj);
                        foundWallsAtY[y] = wallObj;
                    }
                    else
                    {
                        Log.Bulk("Failed wall pathfinding test for Y=" + y);
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
                Log.Bulk("Using the only possible wall configuration: " + onlyWallObj);
                return onlyWallObj;
            }

            // 3. Choose the option that is most "in the middle"
            //   this somewhat works around an issue with our pathfinding that it doesn't consider diagonals
            var sortedY = foundWallsAtY.Keys.ToList();
            Log.Bulk("Possible natural wall configurations: " + sortedY.Count);
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
            Log.Bulk("  Starting nat wall at x = " + buildingStartsAtLeftX + " (estimate was " + estimateX + ")");

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
                    Log.Bulk("  Not possible to build a wall at test position");
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
                    Log.Bulk("  Builing too thick at " + buildingDef.BuildingCenterPosition.ToString2());
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

            int fromX = (int)building.BuildingCenterPosition.X - sizeXDiffToCenter;
            int fromY = (int)building.BuildingCenterPosition.Y - sizeYDiffToCenter;
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
                Point2D buildingCenterPos = iter.BuildingCenterPosition;
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
                Log.Bulk("Found a wall config that completely blocks natural access");
                return true;
            }
            else
            {
                return false;
            }
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
