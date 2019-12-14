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
    using System.Runtime.CompilerServices;
    using System.Diagnostics;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;    

#warning TODO: Save results to file and load it. Use folder ./data

    /// <summary>
    /// Stores information about each tile of the ingame map
    /// </summary>
    public class TileMap<TileType>
    {
        public readonly int Width;
        public readonly int Height;
        private readonly TileType[,] Map;

        public TileMap(int xSize, int ySize)
        {
            this.Width = xSize;
            this.Height = ySize;
            this.Map = new TileType[xSize, ySize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TileType GetTile(int x, int y)
        {
            return this.Map[x, y];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTile(int x, int y, TileType value)
        {
            this.Map[x, y] = value;
        }
    }

    /// <summary>
    /// Contains various static metrics about the map that doesn't change between runs (chokepoints etc)
    /// </summary>
    public class AnalysedMap
    {
        /// <summary>
        /// Higher values indicate chokepoints between ours and the enemy main base
        /// </summary>
        public TileMap<byte> MainBaseChokeScore;
    }

    /// <summary>
    /// Analyses the current map and generates a result object
    /// </summary>
    public static class MapAnalyser
    {
        public static AnalysedMap GenerateNewAnalysis()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            AnalysedMap mapObject = new AnalysedMap();
            CalculateMainBaseChokeScore(mapObject);

            sw.Stop();
            Log.Info("Completed map analysis in " + sw.Elapsed.TotalMilliseconds + " ms");

            return mapObject;
        }

        /// <summary>
        /// Calculates a score for each map tile based on how much of a chokepoint it is, between our main bases
        /// Traces a radius around each spawn location to each other location around the enemy base
        /// </summary>
        private static void CalculateMainBaseChokeScore(AnalysedMap mapObject)
        {
            const int selfXRadius = 4;
            const int selfYRadius = 0;
            const int enemyXRadius = 1;
            const int enemyYRadius = 0;
            Point2D ourBase = Globals.MainBaseLocation;
            Point2D enemyBase = GeneralGameUtility.GuessEnemyBaseLocation();
            Size2DI mapSize = CurrentGameState.GameInformation.StartRaw.MapSize;

            int selfStartX = (int)ourBase.X - selfXRadius;
            int selfStartY = (int)ourBase.Y - selfYRadius;
            int selfEndX = (int)ourBase.X + selfXRadius;
            int selfEndY = (int)ourBase.Y + selfYRadius;
            int enemyStartX = (int)enemyBase.X - enemyXRadius;
            int enemyStartY = (int)enemyBase.Y - enemyYRadius;
            int enemyEndX = (int)enemyBase.X + enemyXRadius;
            int enemyEndY = (int)enemyBase.Y + enemyYRadius;

            mapObject.MainBaseChokeScore = new TileMap<byte>(mapSize.X, mapSize.Y);
            for (int selfX = selfStartX; selfX <= selfEndX; selfX++)
            {
                for (int selfY = selfStartY; selfY <= selfEndY; selfY++)
                {
                    Point2D from = new Point2D(selfX, selfY);

                    for (int enemyX = enemyStartX; enemyX <= enemyEndX; enemyX++)
                    {
                        for (int enemyY = enemyStartY; enemyY <= enemyEndY; enemyY++)
                        {
                            Point2D to = new Point2D(enemyX, enemyY);

                            // Find path between points
                            LinkedList<BossePathNode> path = BOSSE.PathFinderRef.FindPath(from, to);
                            if (path == null)
                                continue; // no path exists

                            // Add upp the points of the path
                            foreach (BossePathNode pathIter in path)
                            {
                                byte existingTileValue = mapObject.MainBaseChokeScore.GetTile(pathIter.X, pathIter.Y);

                                if (existingTileValue == byte.MaxValue)
                                    continue; // Already reached ceiling

                                mapObject.MainBaseChokeScore.SetTile(pathIter.X, pathIter.Y, (byte)(existingTileValue + 1));
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Container which holds the reference to the current analysed map
    /// Results can be saved between sessions for performance reasons
    /// </summary>
    public class MapAnalysisHandler
    {
        public AnalysedMap Map = null;

        public void Initialize()
        {
            this.Map = MapAnalyser.GenerateNewAnalysis();
        }
    }
}
