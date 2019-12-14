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
    using System.Diagnostics;
    using System.Linq;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using AStar;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Responsible for all pathfinding that we do
    /// </summary>
    public class PathFinder
    {
        private BossePathSolver<BossePathNode, Object> PathGrid;

        public void Initialize()
        {
            // Initialize A star grid
            ImageData pathingMap = CurrentGameState.GameInformation.StartRaw.PathingGrid;
            BossePathNode[,] grid = new BossePathNode[pathingMap.Size.X, pathingMap.Size.Y];

            for (int y = 0; y < pathingMap.Size.Y; y++)
            {
                for (int x = 0; x < pathingMap.Size.X; x++)
                {
                    int value = pathingMap.GetBit(x, y);

                    grid[x, y] = new BossePathNode()
                    {
                        IsWall = value == 0,
                        X = x,
                        Y = y,
                    };
                }
            }

            PathGrid = new BossePathSolver<BossePathNode, Object>(grid);
        }

        public SpatialAStar<BossePathNode, object>.AStarPath FindPath(Point2D from, Point2D to)
        {
            SpatialAStar<BossePathNode, object>.AStarPath path = PathGrid.Search(new System.Drawing.Point((int)from.X, (int)from.Y), new System.Drawing.Point((int)to.X, (int)to.Y), null);
            return path;
        }
    }
}
