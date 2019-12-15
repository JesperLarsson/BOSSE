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
}
