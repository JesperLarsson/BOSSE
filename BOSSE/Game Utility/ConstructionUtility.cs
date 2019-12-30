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
        /// <summary>
        /// Builds the given type anywhere, placeholder for a better solution
        /// Super slow, polls the game for a location
        /// </summary>
        public static void BuildGivenStructureAnyWhere_TEMPSOLUTION(UnitId unitType)
        {
            Point2D constructionSpot = null;

            // Check if part of wall
            System.Drawing.Size size = GetSizeOfBuilding(unitType);
            if (size.Width != 0 && size.Height != 0)
            {
                Wall naturalWall = WallinUtility.GetNaturalWall();
                if (naturalWall != null)
                {
                    foreach (Wall.BuildingInWall iter in naturalWall.Buildings)
                    {
                        if (iter.BuildingType.HasValue)
                            continue;

                        if (iter.BuildingSize.Width == size.Width && iter.BuildingSize.Height == size.Height)
                        {
                            // Building is a match
                            if (!CanPlaceRequest(unitType, iter.BuildingPosition))
                            {
                                Log.SanityCheckFailed("Cannot place wall part as intended at " + iter.BuildingPosition);
                                continue;
                            }

                            iter.BuildingType = unitType;
                            constructionSpot = iter.BuildingPosition; // new Point2D(((float)iter.BuildingPosition.X) + 0.5f, ((float)iter.BuildingPosition.Y) + 0.5f);
                            Log.Info("Building wall part " + unitType + " at " + constructionSpot.ToString2());
                            
                            break;
                        }
                    }
                }
            }

            // Find a valid spot, the slow way
            if (constructionSpot == null)
            {
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
