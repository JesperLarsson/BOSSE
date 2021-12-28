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

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// A set of maps that can be used for planning or presenting current world state at a high level
    /// Thread safety: This is calculated in a background thread, but a full instance is swapped in place to LatestMapSet that is valid when it's written. Reading from LatestMapSet is safe
    /// </summary>
    public class StrategicMapSet
    {
        /// <summary>
        /// Pointer to the latest version of our strategy layer maps
        /// </summary>
        public static StrategicMapSet LatestMapSet = null;

        /// <summary>
        /// Map width, in float count
        /// </summary>
        public int xSize;

        /// <summary>
        /// Map height, in float count
        /// </summary>
        public int ySize;

        /// <summary>
        /// Strategic influence map, each entry corresponds to a tile in game
        /// </summary>
        public float[,] InfluenceMap;

        /// <summary>
        /// Strategic tension map, each entry corresponds to a tile in game
        /// </summary>
        public float[,] TensionMap;

        /// <summary>
        /// Strategic vulnerability map, each entry corresponds to a tile in game
        /// </summary>
        public float[,] VulnerabilityMap;

        /// <summary>
        /// Calculates a new set of strategic maps. This is an expensive calculation CPU-wise
        /// </summary>
        public static void CalculateNewFromCurrentMapState()
        {
            StrategicMapSet outObj = new StrategicMapSet();
            //RectangleI playArea = CurrentGameState.GameInformation.StartRaw.PlayableArea;

            int xSize = CurrentGameState.GameInformation.StartRaw.MapSize.X;
            int ySize = CurrentGameState.GameInformation.StartRaw.MapSize.Y;

            // Calculate per-side influence for each tile, depending on distance to their units
            float[,] SelfInfluence = new float[xSize, ySize];
            float[,] EnemyInfluence = new float[xSize, ySize];
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    foreach (SC2APIProtocol.Unit unitIter in CurrentGameState.ObservationState.Observation.RawData.Units)
                    {
                        const float standardInfluence = 1.0f;
                        const float dissipationRateFalloff = 0.9f;
                        if (unitIter.Alliance != Alliance.Self && unitIter.Alliance != Alliance.Enemy)
                        {
                            continue;
                        }

                        Vector2 tilePos = new Vector2(x, y);
                        Vector2 unitPos = new Vector2(unitIter.Pos.X, unitIter.Pos.Y);
                        float distanceToUnit = Vector2.Distance(tilePos, unitPos);

                        float fallOffValue = 1 - (dissipationRateFalloff / (dissipationRateFalloff - distanceToUnit));
                        float influenceContribution = standardInfluence * Math.Max(0, fallOffValue);

                        if (distanceToUnit > 9) // 9 = vision of a marine and decent average of all units
                        {
                            influenceContribution = 0;
                        }

                        if (unitIter.Alliance == Alliance.Self)
                        {
                            SelfInfluence[x, y] += influenceContribution;
                        }
                        else if (unitIter.Alliance == Alliance.Enemy)
                        {
                            EnemyInfluence[x, y] += influenceContribution;
                        }
                    }
                }
            }

            // Calculate influence map = my influence - enemy influence
            outObj.InfluenceMap = new float[xSize, ySize];
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    outObj.InfluenceMap[x, y] = SelfInfluence[x, y] - EnemyInfluence[x, y];
                }
            }

            // Calculate tension map = my influence + enemy influence
            outObj.TensionMap = new float[xSize, ySize];
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    outObj.TensionMap[x, y] = SelfInfluence[x, y] + EnemyInfluence[x, y];
                }
            }

            // Calculate vulnerability map = tension - abs(influence)
            outObj.VulnerabilityMap = new float[xSize, ySize];
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    outObj.VulnerabilityMap[x, y] = outObj.TensionMap[x, y] - Math.Abs(outObj.InfluenceMap[x, y]);
                }
            }

            outObj.xSize = xSize;
            outObj.ySize = ySize;

            LatestMapSet = outObj;
        }
    }
}
