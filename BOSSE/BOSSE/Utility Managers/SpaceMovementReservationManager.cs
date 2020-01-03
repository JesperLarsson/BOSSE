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
    using System.Linq;
    using System.Drawing;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// A single reserved space
    /// </summary>
    public class ReservedSpace
    {
        public Point2D CenterLocation;
        public Size Size;
        public string Name;
        //public UnitId? ReservedForUnitTypeOrNull;
        //public Unit ReservedForUnitOrNull;

        public ReservedSpace(Point2D centerLocation, Size size, string debugName)
        {
            CenterLocation = centerLocation;
            Size = size;
            Name = debugName;
        }

        public override string ToString()
        {
            return $"[ReservedSpace {Name} {CenterLocation.ToString2()} {Size.ToString2()}]";
        }
    }

    /// <summary>
    /// Allows us to reserve space for future usage in certain locations, blocks unit movement to the given locations
    /// </summary>
    public class SpaceMovementReservationManager : Manager
    {
        /// <summary>
        /// Name => Instance mapping
        /// </summary>
        private readonly Dictionary<string, ReservedSpace> ReservedSpaces = new Dictionary<string, ReservedSpace>();

        public void AddNew(ReservedSpace newSpace)
        {
            Log.Bulk("Reserving new space " + newSpace);

            if (ReservedSpaces.ContainsKey(newSpace.Name))
            {
                Log.SanityCheckFailed("Duplicate of reserved space with name " + newSpace.Name);
            }
            ReservedSpaces[newSpace.Name] = newSpace;
        }

        public void AddIfNew(ReservedSpace newSpace)
        {
            if (ReservedSpaces.ContainsKey(newSpace.Name))
            {
                return;
            }

            AddNew(newSpace);
        }

        public bool Remove(string name)
        {
            Log.Bulk("Un-reserving space " + name);

            return ReservedSpaces.Remove(name);
        }

        public bool IsPointInsideReservedSpace(Point2D pos)
        {
            foreach (ReservedSpace iter in ReservedSpaces.Values)
            {
                int sizeParam = Math.Max(iter.Size.Width, iter.Size.Height);
                int sizeDiffFromCenterPos = (sizeParam - 1) / 2;
                bool insideBuildingZone = iter.CenterLocation.IsWithinRange(pos, sizeDiffFromCenterPos);
                if (insideBuildingZone)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
