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
            Log.Info("Completed static map analysis in " + sw.Elapsed.TotalMilliseconds / 1000 + " s");

            return mapObject;
        }

        private static void PopulateMapData(AnalysedStaticMap mapObject)
        {
            Point2D ourBase = Globals.MainBaseLocation;
            Point2D enemyBase = GeneralGameUtility.GuessEnemyBaseLocation();

            mapObject.MainBaseChokeScore = CalculateChokeScoreBetweenPoints(ourBase, enemyBase);
            mapObject.GeneralChokeScore = CalculateGeneralChokeScore();
        }

        /// <summary>
        /// Calculates a general score of how "chokepointy" a point is
        /// 0 = not pathable
        /// 1 = not a chokepoint, pathable
        /// Higher values = more chokepointy, ie less valuable to stand in. Increases linearly to 255 at the most chokepointy part of the map
        /// </summary>
        private static TileMap<byte> CalculateGeneralChokeScore()
        {
            return null;

            Size2DI size = CurrentGameState.GameInformation.StartRaw.MapSize;
            TileMap<ulong> longMap = new TileMap<ulong>(size.X, size.Y);

            // Calculate score
            for (int fromX = 0; fromX < size.X; fromX++)
            {
                for (int fromY = 0; fromY < size.Y; fromY++)
                {
                    for (int innerX = 0; innerX < size.X; innerX++)
                    {
                        for (int innerY = 0; innerY < size.Y; innerY++)
                        {
                            Point2D innerPos = new Point2D(innerX, innerY);
                            Point2D fromPos = new Point2D(fromX, fromY);
                            if (innerPos == fromPos)
                                continue;

                            LinkedList<BossePathNode> path = BOSSE.PathFinderRef.FindPath(fromPos, innerPos);
                            if (path == null)
                                continue;

                            foreach (BossePathNode tileOnPath in path)
                            {
                                ulong oldVal = longMap.GetTile(tileOnPath.X, tileOnPath.Y);
                                if (oldVal == ulong.MaxValue)
                                    continue;
                                longMap.SetTile(tileOnPath.X, tileOnPath.Y, oldVal + 1);
                            }
                        }
                    }
                }
            }
            Log.Info("General choke: Completed main analysis");

            // Find minimum non-zero tile value + maxvalue
            ulong minValue = ulong.MaxValue;
            ulong maxValue = ulong.MinValue;
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    ulong iterVal = longMap.GetTile(x, y);
                    if (iterVal == 0)
                        continue;

                    minValue = Math.Min(iterVal, minValue);
                    maxValue = Math.Max(iterVal, maxValue);
                }
            }
            Log.Info("General choke: minValue = " + minValue + ", maxValue = " + maxValue);
            if (minValue == ulong.MaxValue)
            {
                Log.SanityCheckFailed("No min value found");
                throw new BosseFatalException("No max value found");
            }
            if (maxValue == ulong.MinValue)
            {
                Log.SanityCheckFailed("No max value found");
                throw new BosseFatalException("No max value found");
            }

            // Squash to byte values
            ulong valueDiff = maxValue - minValue;
            double longsPerByte = valueDiff / (byte.MaxValue - 1);
            TileMap<byte> byteMap = new TileMap<byte>();
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    ulong longVal = longMap.GetTile(x, y);
                    if (longVal == 0)
                        continue; // not pathable

                    ulong workingLong = (ulong)Math.Ceiling(((longVal - minValue) / longsPerByte));
                    workingLong += 1;
                    if (workingLong > byte.MaxValue)
                        workingLong = byte.MaxValue;

                    byte workingByte = (byte)workingLong;
                    byteMap.SetTile(x, y, workingByte);
                }
            }

            return byteMap;
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

            int selfStartX = (int)fromPoint.X - selfXRadius;
            int selfStartY = (int)fromPoint.Y - selfYRadius;
            int selfEndX = (int)fromPoint.X + selfXRadius;
            int selfEndY = (int)fromPoint.Y + selfYRadius;
            int enemyStartX = (int)toPoint.X - enemyXRadius;
            int enemyStartY = (int)toPoint.Y - enemyYRadius;
            int enemyEndX = (int)toPoint.X + enemyXRadius;
            int enemyEndY = (int)toPoint.Y + enemyYRadius;

            TileMap<byte> returnObj = new TileMap<byte>();
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
