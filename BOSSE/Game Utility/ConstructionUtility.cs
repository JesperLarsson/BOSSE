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
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static GeneralGameUtility;
    using static AbilityConstants;

    /// <summary>
    /// Utility functions for placing buildings
    /// </summary>
    public static class ConstructionUtility
    {
        private static List<WallBuilderUtility.PlacementResult> defensiveBuildLocationsRequsted = new List<WallBuilderUtility.PlacementResult>();

        public static void Initialize()
        {
            //List<UnitId> rampConfig = new List<UnitId> { UnitId.SUPPLY_DEPOT, UnitId.SUPPLY_DEPOT, UnitId.SUPPLY_DEPOT };
            //List<UnitId> naturalConfig = new List<UnitId> { UnitId.BARRACKS, UnitId.BARRACKS, UnitId.SUPPLY_DEPOT, UnitId.BARRACKS };
            //defensiveBuildLocationsRequsted = WallBuilderUtility.DeterminePlacementsForRampWall(rampConfig);
            //defensiveBuildLocationsRequsted.AddRange(WallBuilderUtility.DeterminePlacementsForNaturalWall(naturalConfig));

            if (Tyr.Tyr.MapAnalyzer.GetMainRamp().Y > Globals.MainBaseLocation.Y)
            {
                // Down start location, upwards ramp
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.SUPPLY_DEPOT, new Vector3(152, 35, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.SUPPLY_DEPOT, new Vector3(154, 35, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.BARRACKS, new Vector3(155.5f, 37.5f, 0)));

                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.SUPPLY_DEPOT, new Vector3(140, 52, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.SUPPLY_DEPOT, new Vector3(140, 54, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.BARRACKS, new Vector3(140.5f, 46.5f, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.BARRACKS, new Vector3(140.5f, 49.5f, 0)));
            }
            else
            {
                // Top start location, downwards ramp
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.SUPPLY_DEPOT, new Vector3(37, 118, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.SUPPLY_DEPOT, new Vector3(37, 120, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.BARRACKS, new Vector3(39.5f, 121.5f, 0)));

                // Natural
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.SUPPLY_DEPOT, new Vector3(51, 104, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.SUPPLY_DEPOT, new Vector3(51, 102, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.BARRACKS, new Vector3(50.5f, 109.5f, 0)));
                defensiveBuildLocationsRequsted.Add(new WallBuilderUtility.PlacementResult(UnitId.BARRACKS, new Vector3(50.5f, 106.5f, 0)));
            }
        }

        /// <summary>
        /// Builds the given type anywhere, placeholder for a better solution
        /// Super slow, polls the game for a location
        /// </summary>
        public static void BuildGivenStructureAnyWhere_TEMPSOLUTION(UnitId unitType)
        {
            Point2D constructionSpot = null;

            // See if our defense config has requested a building of this type
            //Log.Debug("Running A");
            //foreach (WallBuilderUtility.PlacementResult defensiveLocationIter in defensiveBuildLocationsRequsted)
            //{
            //    //Log.Debug("Running B " + defensiveLocationIter.BuildingType + " vs " + unitType);
            //    if (defensiveLocationIter.BuildingType == unitType)
            //    {
            //        // Take this one
            //        //constructionSpot = new Vector3(defensiveLocationIter.Position.X - 1, defensiveLocationIter.Position.Y - 1, 0);
            //        constructionSpot = defensiveLocationIter.Position;
            //        //Log.Info("ConstructionUtility - Building ramp location " + defensiveLocationIter.Position.ToString2());
            //        defensiveBuildLocationsRequsted.Remove(defensiveLocationIter);
            //        break;
            //    }
            //}

            // Find a valid spot, the slow way
            if (constructionSpot == null)
            {
                //Log.Debug("Running backup solution...");
                const int radius = 12;
                Point2D startingSpot;

                List<Unit> resourceCenters = GetUnits(UnitConstants.ResourceCenters);
                if (resourceCenters.Count > 0)
                {
                    startingSpot = resourceCenters[0].Position;
                }
                else
                {
                    Log.Warning($"Unable to construct {unitType} - no resource center was found");
                    return;
                }

                List<Unit> mineralFields = GetUnits(UnitConstants.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
                while (true)
                {
                    constructionSpot = new Point2D(startingSpot.X + Globals.Random.Next(-radius, radius + 1), startingSpot.Y + Globals.Random.Next(-radius, radius + 1));

                    //avoid building in the mineral line
                    if (IsInRange(constructionSpot, mineralFields, 5)) continue;

                    //check if the building fits
                    //Log.Bulk("Running canplace hack...");
                    if (!CanPlaceRequest(unitType, constructionSpot)) continue;

                    //ok, we found a spot
                    break;
                }
            }

            Unit worker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(constructionSpot);
            if (worker == null)
            {
                Log.Warning($"Unable to find a worker to construct {unitType}");
                return;
            }

            Queue(CommandBuilder.ConstructAction(unitType, worker, constructionSpot));
            Log.Info($"Constructing {unitType} at {constructionSpot.ToString2()} using worker " + worker.Tag);
        }
    }
}
