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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Helper functions for building walls (at ramps / in front of natural / etc
    /// </summary>
    public static class WallBuilderUtility
    {
        public class PlacementResult
        {
            public UnitId BuildingType;
            public Vector3 Position;

            public PlacementResult(UnitId targetUnitid, Vector3 position)
            {
                BuildingType = targetUnitid;
                Position = position;
            }
        }

        private static Tyr.WallInCreator WallInCreator = new Tyr.WallInCreator();

        public static List<PlacementResult> DeterminePlacementsForRampWall(List<UnitId> unitTypes)
        {
            List<uint> types = new List<uint>();
            foreach (UnitId unitType in unitTypes)
            {
                types.Add((uint)unitType);
            }
            WallInCreator.Create(types);

            // Pop each result
            List<PlacementResult> resultList = new List<PlacementResult>();
            while (WallInCreator.Wall.Count > 0)
            {
                Tyr.WallBuilding tyrResult = WallInCreator.Wall[0];

                // Find in input data
                foreach (UnitId searchIter in unitTypes)
                {
                    if ((uint)searchIter == tyrResult.Type)
                    {
                        PlacementResult obj = new PlacementResult(searchIter, new Vector3(tyrResult.Pos.X, tyrResult.Pos.Y, 0));
                        resultList.Add(obj);
                        unitTypes.Remove(searchIter);
                        break;
                    }
                }

                WallInCreator.Wall.RemoveAt(0);
            }

            return resultList;
        }

        public static List<PlacementResult> DeterminePlacementsForNaturalWall(List<UnitId> unitTypes)
        {
            List<uint> types = new List<uint>();
            foreach (UnitId unitType in unitTypes)
            {
                types.Add((uint)unitType);
            }
            WallInCreator.CreateFullNatural(types);

            // Pop each result
            List<PlacementResult> resultList = new List<PlacementResult>();
            while (WallInCreator.Wall.Count > 0)
            {
                Tyr.WallBuilding tyrResult = WallInCreator.Wall[0];

                // Find in input data
                foreach (UnitId searchIter in unitTypes)
                {
                    if ((uint)searchIter == tyrResult.Type)
                    {
                        PlacementResult obj = new PlacementResult(searchIter, new Vector3(tyrResult.Pos.X, tyrResult.Pos.Y, 0));
                        resultList.Add(obj);
                        unitTypes.Remove(searchIter);
                        break;
                    }
                }

                WallInCreator.Wall.RemoveAt(0);
            }

            return resultList;
        }
    }
}
