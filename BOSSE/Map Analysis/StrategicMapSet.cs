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

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// A set of maps that can be used for planning or presenting current world state at a high level
    /// </summary>
    public class StrategicMapSet
    {
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
        public static StrategicMapSet CalculateNewFromCurrentMapState()
        {
            StrategicMapSet outObj = new StrategicMapSet();
            RectangleI playArea = CurrentGameState.GameInformation.StartRaw.PlayableArea;
            const int gridSize = 4;

            int xSize = playArea.P1.X - playArea.P0.X;
            int ySize = playArea.P1.Y - playArea.P0.Y;

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
                        const float dissipationRateFalloff = 10.0f;
                        if (unitIter.Alliance != Alliance.Self && unitIter.Alliance != Alliance.Enemy)
                        {
                            continue;
                        }

                        Vector2 tilePos = new Vector2(x, y);
                        Vector2 unitPos = new Vector2(unitIter.Pos.X - playArea.P0.X, unitIter.Pos.Y - playArea.P0.Y);
                        float distanceToUnit = Vector2.Distance(tilePos, unitPos);

                        float fallOffValue = 1 - (dissipationRateFalloff / (dissipationRateFalloff - distanceToUnit));
                        float influenceContribution = standardInfluence * Math.Max(0, fallOffValue);

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

            // Calculate vulnerability map = tension - Abs(influence)
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
            return outObj;
        }
    }
}
