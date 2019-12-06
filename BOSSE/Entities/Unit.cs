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
        /// Contained for all available unit objects
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

        // Property lookups
        public string Name { get => unitInformation.Name; }
        public ulong Tag { get => original.Tag; }
        public uint UnitType { get => original.UnitType; }
        public float Integrity { get => (original.Health + original.Shield) / (original.HealthMax + original.ShieldMax); }
        public bool IsVisible { get => original.DisplayType == DisplayType.Visible; }
        public int IdealWorkers { get => original.IdealHarvesters; }
        public int AssignedWorkers { get => original.AssignedHarvesters; }
        public float BuildProgress { get => original.BuildProgress; }
        public float Energy { get => original.Energy; set => original.Energy = value; }
        public float MineralCost { get => unitInformation.MineralCost; }
        public float VespeneCost { get => unitInformation.VespeneCost; }
        public RepeatedField<UnitOrder> QueuedOrders { get => original.Orders; }
        public UnitOrder CurrentOrder { get => QueuedOrders.Count > 0 ? QueuedOrders[0] : null; }
        public Vector3 Position { get => new Vector3(original.Pos.X, original.Pos.Y, original.Pos.Z); }

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
        /// Refresh with latest observational data
        /// </summary>
        public void RefreshData(SC2APIProtocol.Unit newOriginal)
        {
            this.original = newOriginal;
        }

        /// <summary>
        /// Refreshes current position etc of all units
        /// </summary>
        public static void RefreshAllUnitData()
        {
            foreach (SC2APIProtocol.Unit sc2UnitData in CurrentGameState.ObservationState.Observation.RawData.Units)
            {
                if (!Unit.AllUnitInstances.ContainsKey(sc2UnitData.Tag))
                    continue;

                Unit.AllUnitInstances[sc2UnitData.Tag].RefreshData(sc2UnitData);
            }
        }

        public double GetDistance(Unit otherUnit)
        {
            return GetDistance(otherUnit.Position);
        }

        public double GetDistance(Vector3 location)
        {
            return Vector3.Distance(new Vector3(Position.X, Position.Y, 0), new Vector3(location.X, location.Y, 0));
        }
    }
}