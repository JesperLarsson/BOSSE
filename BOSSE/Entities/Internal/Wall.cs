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
    using System.Drawing;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static GeneralGameUtility;
    using static AbilityConstants;

    /// <summary>
    /// A group of buildings intended as a wall
    /// </summary>
    public class Wall
    {
        /// <summary>
        /// A specific part of a <see cref="Wall"/>
        /// </summary>
        public class BuildingInWall
        {
            public Size BuildingSize;
            public Point2D BuildingPosition;

            /// <summary>
            /// Assigned as we build the wall, null = slot is unused
            /// </summary>
            public UnitId? BuildingType = null;

            public BuildingInWall(Size buildingSize, Point2D buildingPosition)
            {
                this.BuildingSize = buildingSize;
                this.BuildingPosition = buildingPosition;
            }

            public bool DoesGivenTypeFit(UnitId unitType)
            {
                UnitTypeData info = GetUnitInfo(unitType);
                throw new Exception();
                //info.
                //// todo
            }
        }

        public Wall()
        {

        }
        public Wall(Wall other)
        {
            this.Buildings.AddRange(other.Buildings);
        }

        public List<BuildingInWall> Buildings = new List<BuildingInWall>();

        /// <summary>
        /// Returns an approximate center location of the wall, useful for calculating distances but not placing buildings
        /// </summary>
        public Point2D GetCenterPosition()
        {
            if (Buildings.Count == 0)
                throw new BosseFatalException("Can't calculate wall center without building allocations");

            float xTotal = 0;
            float yTotal = 0;

            foreach (var iter in this.Buildings)
            {
                xTotal += iter.BuildingPosition.X;
                yTotal += iter.BuildingPosition.Y;
            }

            float x = xTotal / this.Buildings.Count;
            float y = yTotal / this.Buildings.Count;
            Point2D resultPos = new Point2D(x, y);

            return resultPos;
        }
    }
}
