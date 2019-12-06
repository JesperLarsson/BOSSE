/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
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
