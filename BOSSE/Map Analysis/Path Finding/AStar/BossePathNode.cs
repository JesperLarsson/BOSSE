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
    /// A single ingame tile which we can perform pathfinding on
    /// </summary>
    public class BossePathNode : IPathNode<Object>
    {
        public Int32 X { get; set; }
        public Int32 Y { get; set; }
        public Boolean IsWall { get; set; }

        public bool IsWalkable(Object unused)
        {
            return !IsWall;
        }
    }
}
