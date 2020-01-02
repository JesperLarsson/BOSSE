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
        private Wall naturalWall = null;

        public override void Initialize()
        {
            // Find a configuration to build a wall at our natural
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.naturalWall = WallinUtility.GetNaturalWall();
            sw.Stop();
            Log.Bulk("Found natural wall in " + sw.Elapsed.TotalMilliseconds / 1000 + " s");

            if (this.naturalWall == null)
            {
                Log.SanityCheckFailed("Unable to find a configuration to build natural wall");
            }
            else
            {
                Log.Info("OK - Found natural wall location");
            }
        }

        public Wall GetNaturalWall()
        {
            return naturalWall;
        }

        /// <summary>
        /// Builds the given structure anywhere - Note that this is slow since it polls the game for a valid location
        /// </summary>
        public void BuildAutoSelect(UnitId unitType)
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
                            if (!CanPlaceRequest(unitType, iter.BuildingCenterPosition))
                            {
                                Log.SanityCheckFailed("Cannot place wall part as intended at " + iter.BuildingCenterPosition);
                                continue;
                            }

                            iter.BuildingType = unitType;
                            constructionSpot = iter.BuildingCenterPosition; // new Point2D(((float)iter.BuildingPosition.X) + 0.5f, ((float)iter.BuildingPosition.Y) + 0.5f);
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

        public override void OnFrameTick()
        {
            // Not necessary
        }
    }
}
