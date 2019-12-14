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
            AnalysedMap mapObject = new AnalysedMap();

            CalculateMainBaseChokeScore(mapObject);

            return mapObject;
        }

        private static void CalculateMainBaseChokeScore(AnalysedMap mapObject)
        {













            mapObject.MainBaseChokeScore = ,,,;
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
