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
    using System.Linq;
    using System.Drawing;
    using System.Diagnostics;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Responsible for building construction and placement
    /// </summary>
    public class ConstructionManager : Manager
    {
        public volatile bool AllowNaturalWallinIn = true;

        /// <summary>
        /// Pointer to our intended natural wall configuration, NULL = not possible to build a wall
        /// </summary>
        public Wall NaturalWallRef = null;

        /// <summary>
        /// First 2x2 tile in our wall is intended for use with a building addon
        /// </summary>
        private const bool First2x2IsReservedForAddon = true;

        public override void Initialize()
        {
            InitializeNaturalWall();
        }

        public void BuildAtExactPosition(UnitId unitType, Point2D exactCoordinate)
        {
            if (exactCoordinate == null || (exactCoordinate.X == 0 && exactCoordinate.Y == 0))
            {
                Log.Warning($"Tried to build unit {unitType} without any coordinates set");
                return;
            }

            Unit worker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(exactCoordinate);
            if (worker == null)
            {
                Log.Warning($"Unable to find a worker to construct {unitType}");
                return;
            }

            Queue(CommandBuilder.ConstructAction(unitType, worker, exactCoordinate));
            Log.Info($"Constructing {unitType} at {exactCoordinate.ToString2()} using worker " + worker.Tag);
        }

        public void BuildAtApproximatePosition(UnitId unitType, Point2D approximatePosition, int searchRadius = 12)
        {
            Point2D actualPosition = this.AutoPickPosition(unitType, approximatePosition, searchRadius);
            if (actualPosition == null)
            {
                Log.SanityCheckFailed("Unable to find a valid position close to " + approximatePosition.ToString2());
                return;
            }

            BuildAtExactPosition(unitType, actualPosition);
        }

        /// <summary>
        /// Builds the given structure anywhere - Note that this is slow since it polls the game for a valid location
        /// </summary>
        public void BuildAutoSelectPosition(UnitId unitType, bool allowAsWallPart = true)
        {
            Point2D constructionSpot = null;

            // Check if it can be a part part of a building wall, typically at our natural expansion
            if (allowAsWallPart)
            {
                Wall.BuildingInWall partOfWall = FindAsPartOfWall(unitType);
                if (partOfWall != null)
                {
                    partOfWall.IsReserved = true;
                    constructionSpot = partOfWall.BuildingCenterPosition;
                    Log.Info("Building wall part " + unitType + " at " + constructionSpot.ToString2());

                    // Subscribe to it being placed so that we can update the unit reference
                    BOSSE.SensorManagerRef.GetSensor(typeof(OwnStructureWasPlacedSensor)).AddHandler(new SensorEventHandler(delegate (HashSet<Unit> affectedUnits)
                    {
                        if (affectedUnits.Count != 1)
                        {
                            Log.SanityCheckFailed("Could not assign back reference to wall part at " + constructionSpot.ToString2() + ", expected type = " + unitType);
                            return;
                        }

                        Unit building = affectedUnits.First();
                        partOfWall.PlacedBuilding = building;
                        Log.Info("Updated wall back ref with placed building " + building);
                    }), unfilteredList => new HashSet<Unit>(unfilteredList.Where(unitIter => unitIter.Position.IsSameCoordinates(constructionSpot))), true);
                }
            }

            // Find a valid spot, the slow way
            if (constructionSpot == null)
            {
                constructionSpot = AutoPickPosition(unitType);
            }

            BuildAtExactPosition(unitType, constructionSpot);
        }

        private Point2D AutoPickPosition(UnitId unitType, Point2D argCloseToPosition = null, int searchRadius = 12)
        {
            Point2D startingSpot;
            if (argCloseToPosition == null)
            {
                startingSpot = Globals.MainBaseLocation;
            }
            else
            {
                startingSpot = argCloseToPosition;
            }

            List<Unit> mineralFields = GetUnits(UnitConstants.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
            List<Unit> pylons = null;
            if (BOSSE.UseRace == Race.Protoss && unitType != UnitId.PYLON && unitType != UnitId.NEXUS && unitType != UnitId.ASSIMILATOR)
                pylons = GetUnits(UnitId.PYLON, onlyCompleted: true, onlyVisible: true, alliance: Alliance.Self);

            for (int _ = 0; _ < 10000; _++)
            {
                Point2D constructionSpot = new Point2D(startingSpot.X + Globals.Random.Next(-searchRadius, searchRadius + 1), startingSpot.Y + Globals.Random.Next(-searchRadius, searchRadius + 1));

                // Do not build close to mineral fields
                if (IsInRangeAny(constructionSpot, mineralFields, 5))
                    continue;

                // Protoss must build close to Pylons
                //   Game range is actually 6.5 units, but we model it as 6 which is close enough
                if (pylons != null && IsInRangeAny(constructionSpot, pylons, 6) == false)
                    continue;

                // Must be buildable (polls game)
                if (!CanPlaceRequest(unitType, constructionSpot))
                    continue;

                return constructionSpot;
            }

            Log.SanityCheckFailed("Unable to auto-place building " + unitType + " near " + startingSpot.ToString2());
            return null;
        }

        /// <summary>
        /// Returns a position if the given building can be part of our wall, otherwise NULL
        /// </summary>
        private Wall.BuildingInWall FindAsPartOfWall(UnitId unitType)
        {
            if (this.AllowNaturalWallinIn == false)
                return null;

            Size size = GetSizeOfBuilding(unitType);
            if (size.Width == 0 || size.Height == 0)
            {
                return null;
            }

            Wall naturalWall = WallinUtility.GetNaturalWall();
            if (naturalWall == null)
            {
                return null;
            }

            foreach (Wall.BuildingInWall iter in naturalWall.Buildings)
            {
                if (iter.IsReserved)
                    continue;

                if (iter.BuildingSize.Width == size.Width && iter.BuildingSize.Height == size.Height)
                {
                    // Building is a match
                    if (!CanPlaceRequest(unitType, iter.BuildingCenterPosition))
                    {
                        Log.SanityCheckFailed("Cannot place wall part as intended at " + iter.BuildingCenterPosition);
                        continue;
                    }

                    return iter;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a configuration to build a wall at our natural
        /// </summary>
        private void InitializeNaturalWall()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.NaturalWallRef = WallinUtility.GetNaturalWall();
            sw.Stop();
            Log.Bulk("Found natural wall in " + sw.Elapsed.TotalMilliseconds / 1000 + " s");

            if (this.NaturalWallRef == null)
            {
                Log.SanityCheckFailed("Unable to find a configuration to build natural wall");
            }
            else
            {
                Log.Info("OK - Found natural wall location");

                if (First2x2IsReservedForAddon)
                {
                    foreach (var iter in this.NaturalWallRef.Buildings)
                    {
                        if (iter.BuildingSize.IsSameAsSize(new Size(2, 2)))
                        {
                            iter.IsReserved = true;
                            Log.Info("Reserved natural part for an addon: " + iter);
                            break;
                        }
                    }
                }
            }
        }
    }
}
