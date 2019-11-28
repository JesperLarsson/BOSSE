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
    using static GameUtility;

    /// <summary>
    /// A single StarCraft unit, this can be buildings etc as well
    /// </summary>
    public class Unit : GameObject
    {
        /// <summary>
        /// Original data as received from StarCraft
        /// </summary>
        private readonly SC2APIProtocol.Unit original;

        /// <summary>
        /// Looked up unit information
        /// </summary>
        private readonly UnitTypeData unitInformation;

        public string Name { get => unitInformation.Name; }
        public ulong Tag { get => original.Tag; }
        public uint UnitType { get => original.UnitType; }
        public float Integrity { get => (original.Health + original.Shield) / (original.HealthMax + original.ShieldMax); }
        public bool IsVisible { get => original.DisplayType == DisplayType.Visible; }
        public int IdealWorkers { get => original.IdealHarvesters; }
        public int AssignedWorkers { get => original.AssignedHarvesters; }
        public float BuildProgress { get => original.BuildProgress; }
        public RepeatedField<UnitOrder> QueuedOrders { get => original.Orders; }
        public UnitOrder CurrentOrder { get => QueuedOrders.Count > 0 ? QueuedOrders[0] : null; }

        public Unit(SC2APIProtocol.Unit unit) : base(position: new Vector3(unit.Pos.X, unit.Pos.Y, unit.Pos.Z))
        {
            this.original = unit;
            this.unitInformation = CurrentGameState.GameData.Units[(int)unit.UnitType];
        }
    }
}