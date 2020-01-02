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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// A single StarCraft unit, this can be buildings etc as well
    /// </summary>
    public class Unit : GameObject
    {
        /// <summary>
        /// Container for all unit objects Tag => Instance mapping
        /// </summary>
        public static Dictionary<ulong, Unit> AllUnitInstances = new Dictionary<ulong, Unit>();

        /// <summary>
        /// Original data as received from StarCraft
        /// </summary>
        private SC2APIProtocol.Unit original;

        /// <summary>
        /// Looked up unit information
        /// </summary>
        private readonly UnitTypeData unitInformation;

        /// <summary>
        /// If set, this unit is reserved for a specific use and should not be touched by general management classes
        /// </summary>
        public bool IsReserved = false;

        /// <summary>
        /// Set if this unit was given new orders this tick
        /// This prevents other parts of the code from issuing duplicate orders
        /// </summary>
        public bool HasNewOrders = false;
        
        // Property lookup helper functions
        public string Name { get => unitInformation.Name; }
        public ulong Tag { get => original.Tag; }
        public UnitConstants.UnitId UnitType { get => (UnitConstants.UnitId)original.UnitType; }
        public float Integrity { get => (original.Health + original.Shield) / (original.HealthMax + original.ShieldMax); }
        public bool IsVisible { get => original.DisplayType == DisplayType.Visible; }
        public int IdealWorkers { get => original.IdealHarvesters; }
        public int AssignedWorkers { get => original.AssignedHarvesters; }
        public Alliance Alliance { get => original.Alliance; }
        public float BuildProgress { get => original.BuildProgress; }
        public float Energy { get => original.Energy; set => original.Energy = value; }
        public float MineralCost { get => unitInformation.MineralCost; }
        public float VespeneCost { get => unitInformation.VespeneCost; }
        public RepeatedField<UnitOrder> QueuedOrders { get => original.Orders; }
        public UnitOrder CurrentOrder { get => QueuedOrders.Count > 0 ? QueuedOrders[0] : null; }
        public Point2D Position { get => new Point2D(original.Pos.X, original.Pos.Y); }

        /// <summary>
        /// Create a new instance from sc2 instance, we wrap around it and add some functionality
        /// </summary>
        public Unit(SC2APIProtocol.Unit unit) : base()
        {
            this.original = unit;
            this.unitInformation = CurrentGameState.GameData.Units[(int)unit.UnitType];

#if DEBUG
            if (AllUnitInstances.ContainsKey(this.Tag))
            {
                Log.SanityCheckFailed("Already have an instance of unit " + this.Tag);
            }
#endif

            AllUnitInstances.Add(this.Tag, this);
        }

        /// <summary>
        /// Create placeholder instance, only used as a temporary placeholder during events
        /// </summary>
        public Unit(ulong tag) : base()
        {
            this.original = new SC2APIProtocol.Unit();
            this.original.Tag = tag;
        }

        /// <summary>
        /// Called on each new frame, refresh data etc
        /// </summary>
        public void UpdateDataEachTick(SC2APIProtocol.Unit newOriginal)
        {
            this.HasNewOrders = false;
            this.original = newOriginal;
        }

        /// <summary>
        /// Updates all units each tick
        /// </summary>
        public static void OnTick()
        {
            // Refresh unit data with new input
            foreach (SC2APIProtocol.Unit sc2UnitData in CurrentGameState.ObservationState.Observation.RawData.Units)
            {
                if (!Unit.AllUnitInstances.ContainsKey(sc2UnitData.Tag))
                    continue;

                Unit.AllUnitInstances[sc2UnitData.Tag].UpdateDataEachTick(sc2UnitData);
            }
        }

        public override string ToString()
        {
            return $"[{this.UnitType} {this.Tag} {this.Position.ToString2()}]";
        }
    }
}