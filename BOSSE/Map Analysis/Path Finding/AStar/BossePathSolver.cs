namespace AStar
{
    // Based 2019-12-14 on (MIT license): https://www.codeproject.com/Articles/118015/Fast-A-Star-2D-Implementation-for-C
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;

    using SC2APIProtocol;

    /// <summary>
    /// A Star pathfinding solver
    /// </summary>
    public class BossePathSolver<TPathNode, TUserContext> : SpatialAStar<TPathNode, TUserContext> where TPathNode : IPathNode<TUserContext>
    {
        protected override Double Heuristic(PathNode inStart, PathNode inEnd)
        {
            return Math.Abs(inStart.X - inEnd.X) + Math.Abs(inStart.Y - inEnd.Y);
        }

        protected override Double NeighborDistance(PathNode inStart, PathNode inEnd)
        {
            return Heuristic(inStart, inEnd);
        }

        public BossePathSolver(TPathNode[,] inGrid)
            : base(inGrid)
        {
        }
    }
}
