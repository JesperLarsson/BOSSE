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

    /// <summary>
    /// Performs static analysis of the current map. All generated data does not change during runtime
    /// </summary>
    public static class StaticMapAnalyser
    {
        public static AnalysedStaticMap GenerateNewAnalysis()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            AnalysedStaticMap mapObject = new AnalysedStaticMap();
            PopulateMapData(mapObject);

            sw.Stop();
            Log.Info("Completed map analysis in " + sw.Elapsed.TotalMilliseconds + " ms");

            return mapObject;
        }

        private static void PopulateMapData(AnalysedStaticMap mapObject)
        {
            Point2D ourBase = Globals.MainBaseLocation;
            Point2D enemyBase = GeneralGameUtility.GuessEnemyBaseLocation();

            mapObject.MainBaseChokeScore = CalculateChokeScoreBetweenPoints(ourBase, enemyBase);
        }

        /// <summary>
        /// Calculates a score for each map tile based on how much of a chokepoint it is when going between the given points
        /// </summary>
        private static TileMap<byte> CalculateChokeScoreBetweenPoints(Point2D fromPoint, Point2D toPoint)
        {
            const int tileSkipCount = 2;
            const int selfXRadius = 4 * tileSkipCount;
            const int selfYRadius = 0;
            const int enemyXRadius = 1 * tileSkipCount;
            const int enemyYRadius = 0;

            Size2DI mapSize = CurrentGameState.GameInformation.StartRaw.MapSize;

            int selfStartX = (int)fromPoint.X - selfXRadius;
            int selfStartY = (int)fromPoint.Y - selfYRadius;
            int selfEndX = (int)fromPoint.X + selfXRadius;
            int selfEndY = (int)fromPoint.Y + selfYRadius;
            int enemyStartX = (int)toPoint.X - enemyXRadius;
            int enemyStartY = (int)toPoint.Y - enemyYRadius;
            int enemyEndX = (int)toPoint.X + enemyXRadius;
            int enemyEndY = (int)toPoint.Y + enemyYRadius;

            TileMap<byte> returnObj = new TileMap<byte>(mapSize.X, mapSize.Y);
            for (int selfX = selfStartX; selfX <= selfEndX; selfX += tileSkipCount)
            {
                for (int selfY = selfStartY; selfY <= selfEndY; selfY += tileSkipCount)
                {
                    Point2D from = new Point2D(selfX, selfY);

                    for (int enemyX = enemyStartX; enemyX <= enemyEndX; enemyX += tileSkipCount)
                    {
                        for (int enemyY = enemyStartY; enemyY <= enemyEndY; enemyY += tileSkipCount)
                        {
                            Point2D to = new Point2D(enemyX, enemyY);

                            // Find path between points
                            LinkedList<BossePathNode> path = BOSSE.PathFinderRef.FindPath(from, to);
                            if (path == null)
                                continue; // no path exists

                            // Add upp the points of the path
                            foreach (BossePathNode pathIter in path)
                            {
                                byte existingTileValue = returnObj.GetTile(pathIter.X, pathIter.Y);

                                if (existingTileValue == byte.MaxValue)
                                    continue; // Already reached ceiling

                                returnObj.SetTile(pathIter.X, pathIter.Y, (byte)(existingTileValue + 1));
                            }
                        }
                    }
                }
            }

            return returnObj;
        }
    }
}
