﻿/*
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
        /// <summary>
        /// Pointer to our intended natural wall configuration, NULL = not possible to build a wall
        /// </summary>
        public Wall NaturalWallRef = null;

        public override void Initialize()
        {
            InitializeNaturalWall();
        }

        public bool BuildAtExactPosition(UnitId unitType, Point2D exactCoordinate)
        {
            if (exactCoordinate == null || (exactCoordinate.X == 0 && exactCoordinate.Y == 0))
            {
                Log.Warning($"Tried to build unit {unitType} without any coordinates set");
                return false;
            }

            Unit worker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(exactCoordinate, true);
            if (worker == null)
            {
                Log.Warning($"Unable to find a worker to construct {unitType}");
                return false;
            }

            Queue(CommandBuilder.ConstructAction(unitType, worker, exactCoordinate));
            Log.Info($"Constructing {unitType} at {exactCoordinate.ToString2()} using worker " + worker.Tag);
            return true;
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
        /// Tries to expand to the next available base expansion position, if possible
        /// </summary>
        public bool BuildNextExpansion( bool subtractCosts)
        {
            UnitId ccType = RaceCommandCenterUnitType();

            // Expand
            if (CanAfford(ccType))
            {
                Point2D constructionSpot = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.NaturalExpansion.GetCommandCenterPosition();

                // Try third base if we have already expanded once before
                if (IsOwnCcNear(constructionSpot))
                {
                    constructionSpot = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.ThirdExpansion.GetCommandCenterPosition();
                    if (IsOwnCcNear(constructionSpot))
                        return false;
                }

                Unit worker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(constructionSpot, true);
                if (worker == null)
                    return false;

                Queue(CommandBuilder.ConstructAction(ccType, worker, constructionSpot));

                if (subtractCosts)
                    SubtractCosts(ccType);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Builds the given structure anywhere - Note that this is slow since it polls the game for a valid location
        /// </summary>
        public bool BuildAutoSelectPosition(UnitId buildingType, bool subtractCosts, bool allowAsWallPart = true)
        {
            // Gas extractors are built by the worker manager
            if (GasExtractors.Contains(buildingType))
            {
                bool ok = BOSSE.WorkerManagerRef.BuildNewGasExtractors(1, subtractCosts);
                return ok;
            }

            // Command centers needs to be built at specific expansion slots
            if (CommandCenters.Contains(buildingType))
            {
                return BuildNextExpansion(subtractCosts);
            }

            // Check if it can be a part part of a building wall, typically at our natural expansion
            Point2D constructionSpot = null;
            if (allowAsWallPart && BotConstants.EnableWalling)
            {
                Wall.BuildingInWall partOfWall = FindAsPartOfWall(buildingType);
                if (partOfWall != null)
                {
                    partOfWall.IsReserved = true;
                    constructionSpot = partOfWall.BuildingCenterPosition;
                    Log.Info("Building wall part " + buildingType + " at " + constructionSpot.ToString2());

                    // Subscribe to it being placed so that we can update the unit reference
                    BOSSE.SensorManagerRef.GetSensor(typeof(OwnStructureWasPlacedSensor)).AddHandler(new SensorEventHandler(delegate (HashSet<Unit> affectedUnits)
                    {
                        if (affectedUnits.Count != 1)
                        {
                            Log.SanityCheckFailed("Could not assign back reference to wall part at " + constructionSpot.ToString2() + ", expected type = " + buildingType);
                            return;
                        }

                        Unit building = affectedUnits.First();
                        partOfWall.PlacedBuilding = building;
                        Log.Info("Updated wall back ref with placed building " + building);
                    }), unfilteredList => new HashSet<Unit>(unfilteredList.Where(unitIter => unitIter.Position.IsSameCoordinates(constructionSpot))), true);
                }
            }

            // Sanity check that wall coordinates are actually buildable
            // Check is disabled in real time mode due to performance concerns
            if (BotConstants.UseStepMode && constructionSpot != null && CanPlaceRequest(buildingType, constructionSpot) == false)
            {
                Log.SanityCheckFailed($"Wall coordinates {constructionSpot} are not buildable! Falling back on auto-placement logic");
                constructionSpot = null;
            }

            // Find a valid spot to build this building (slow)
            if (constructionSpot == null)
            {
                constructionSpot = AutoPickPosition(buildingType);
            }

            bool buildOk = BuildAtExactPosition(buildingType, constructionSpot);
            if (buildOk && subtractCosts)
                SubtractCosts(buildingType);

            return buildOk;
        }

        /// <summary>
        /// Attempts to automatically find a position to place the given building
        /// </summary>
        private Point2D AutoPickPosition(UnitId buildingType, Point2D argCloseToPosition = null, int searchRadius = 12)
        {
            // Pick a spot, we will search around this location
            Point2D startingSpot;
            if (argCloseToPosition == null)
            {
                startingSpot = Globals.MainBaseLocation;
            }
            else
            {
                startingSpot = argCloseToPosition;
            }

            // Protoss - Use pylon-based logic if possible, only falling back on the brute force method if necessary
            if (BOSSE.UseRace == Race.Protoss && (UnitConstants.ProtossBuildingsDoesNotRequirePylon.Contains(buildingType) == false))
            {
                Point2D point = AutoPickPositionNearPylon(buildingType, startingSpot);
                if (point != null)
                    return point;
            }

            // Try brute force method (slow)
            return AutoPickPositionBruteForce(buildingType, startingSpot, searchRadius);
        }

        /// <summary>
        /// "New" positioning style, only works for Protoss
        /// </summary>
        private Point2D AutoPickPositionNearPylon(UnitId buildingType, Point2D startingSpot)
        {
            List<Unit> pylons = GetUnits(UnitId.PYLON, onlyCompleted: true, onlyVisible: true, alliance: Alliance.Self);
            if (pylons == null || pylons.Count == 0)
                return null;

            // Sort by distance to target spot
            pylons = pylons.OrderBy(o => o.Position.AirDistanceAbsolute(startingSpot)).ToList();

            Size size = GetSizeOfBuilding(buildingType);
            List<Unit> mineralFields = GetUnits(UnitConstants.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
            const int PylonRange = 6;
            foreach (Unit pylonIter in pylons)
            {
                // Removed max search distance limit for now
                // This should make our build orders more reliable overall, as we will automatically fall back on another nearby pylon instead
                //if (pylonIter.Position.AirDistanceAbsolute(startingSpot) > searchRadius)
                //    break; // reached max search distance

                float xStart = pylonIter.Position.X - size.Width - PylonRange;
                float xEnd = pylonIter.Position.X + size.Width + PylonRange;
                float yStart = pylonIter.Position.Y - size.Height - PylonRange;
                float yEnd = pylonIter.Position.Y + size.Height + PylonRange;

                List<Point2D> candidateList = new List<Point2D>();
                for (float x = xStart; x <= xEnd; x++)
                {
                    for (float y = yStart; y <= yEnd; y++)
                    {
                        Point2D point = new Point2D(x, y);

                        // Can't collide with pylon
                        float distanceToPylon = point.AirDistanceAbsolute(pylonIter.Position);
                        if (distanceToPylon < size.Width || distanceToPylon < size.Height)
                            continue;

                        // Do not build close to mineral fields
                        if (IsInRangeAny(point, mineralFields, 5))
                            continue;

                        candidateList.Add(point);
                    }
                }

#warning IMPORTANT TODO: This should be sorted, but our placement logic just happens to work better when walling without this
                // Try to build as close to Pylon as possible
                //candidateList = candidateList.OrderBy(o => o.AirDistanceAbsolute(pylonIter.Position)).ToList();

                foreach (Point2D pointIter in candidateList)
                {
                    if (!CanPlaceRequest(buildingType, pointIter))
                        continue;

                    // Matched location that we can build at
                    return pointIter;
                }
            }

            Log.SanityCheckFailed("Unable to auto-place building " + buildingType + " near a pylon at " + startingSpot.ToString2());
            return null;
        }

        /// <summary>
        /// Tries to find a placement position by polling random coordinates, slow and unreliable
        /// </summary>
        private Point2D AutoPickPositionBruteForce(UnitId buildingType, Point2D startingSpot, int searchRadius)
        {
            List<Unit> mineralFields = GetUnits(UnitConstants.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
            List<Unit> pylons = null;
            if (BOSSE.UseRace == Race.Protoss && buildingType != UnitId.PYLON && buildingType != UnitId.NEXUS && buildingType != UnitId.ASSIMILATOR)
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
                if (!CanPlaceRequest(buildingType, constructionSpot))
                    continue;

                return constructionSpot;
            }

            Log.SanityCheckFailed("Unable to auto-place building " + buildingType + " near " + startingSpot.ToString2());
            return null;
        }

        /// <summary>
        /// Returns a position if the given building can be part of our wall, otherwise NULL
        /// </summary>
        private Wall.BuildingInWall FindAsPartOfWall(UnitId unitType)
        {
            if (BotConstants.EnableWalling == false)
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

        private bool IsOwnCcNear(Point2D point)
        {
            UnitId ccType = RaceCommandCenterUnitType();
            List<Unit> ccNearNatural = GetUnits(ccType);
            if (ccNearNatural == null || ccNearNatural.Count == 0)
                return false;

            ccNearNatural = ccNearNatural.OrderBy(o => o.Position.AirDistanceAbsolute(point)).ToList();

            float distance = ccNearNatural[0].Position.AirDistanceAbsolute(point);
            bool isNear = distance <= 8;

            return isNear; 
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

                bool first2x2IsReservedForAddon = BOSSE.UseRace == Race.Terran;

                if (first2x2IsReservedForAddon)
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
