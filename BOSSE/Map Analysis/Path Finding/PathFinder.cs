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

        /// <summary>
        /// <paramref name="overrideValues">Overrides the given points of our grid with new values, can be used to simulate behaviour other than the current game state</paramref>
        /// </summary>
        public void Initialize(List<KeyValuePair<Point2D, bool>> overrideValues = null)
        {
            // Initialize A star grid
#warning TODO: Include buildings, mineral fields, etc in pathfinding
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

            if (overrideValues != null)
            {
                foreach (var iter in overrideValues)
                {
                    Point2D pos = iter.Key;
                    bool newValue = iter.Value;
                    grid[(int)pos.X, (int)pos.Y].IsWall = newValue;
                }
            }

            PathGrid = new BossePathSolver<BossePathNode, Object>(grid);
        }

        /// <summary>
        /// Finds a path between the two points
        /// </summary>
        public LinkedList<BossePathNode> FindPath(Point2D from, Point2D to)
        {
            LinkedList<BossePathNode> path = PathGrid.Search(new System.Drawing.Point((int)from.X, (int)from.Y), new System.Drawing.Point((int)to.X, (int)to.Y), null);
            return path;
        }
    }
}
