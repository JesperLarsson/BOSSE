/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2022 Jesper Larsson

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
        /// Original data as received from StarCraft this frame
        /// </summary>
        public SC2APIProtocol.Unit Original;

        /// <summary>
        /// Original data as received from StarCraft previous frame
        /// </summary>
        public SC2APIProtocol.Unit LastFrameData;

        /// <summary>
        /// Looked up unit information
        /// </summary>
        private readonly UnitTypeData unitInformation;

        /// <summary>
        /// If set, this unit is reserved for a specific use and should not be touched by general management classes
        /// </summary>
        public bool IsReserved = false;

        /// <summary>
        /// If set, will not automatically be moved back to mining
        /// </summary>
        public bool IsBuilder = false;

        /// <summary>
        /// Set if this unit was given new orders this tick
        /// This prevents other parts of the code from issuing duplicate orders
        /// </summary>
        public bool HasNewOrders = false;

        // Property lookup helper functions
        public string Name { get => unitInformation.Name; }
        public ulong Tag { get => Original.Tag; }
        public UnitConstants.UnitId UnitType { get => (UnitConstants.UnitId)Original.UnitType; }
        public float Integrity { get => (Original.Health + Original.Shield) / (Original.HealthMax + Original.ShieldMax); }
        public bool IsVisible { get => Original.DisplayType == DisplayType.Visible; }
        public int IdealWorkers { get => Original.IdealHarvesters; }
        public int AssignedWorkers { get => Original.AssignedHarvesters; }
        public Alliance Alliance { get => Original.Alliance; }
        public float BuildProgress { get => Original.BuildProgress; }
        public float Energy { get => Original.Energy; set => Original.Energy = value; }
        public float MineralCost { get => unitInformation.MineralCost; }
        public float VespeneCost { get => unitInformation.VespeneCost; }
        public RepeatedField<UnitOrder> QueuedOrders { get => Original.Orders; }
        public UnitOrder CurrentOrder { get => QueuedOrders.Count > 0 ? QueuedOrders[0] : null; }
        public Point2D Position { get => new Point2D(Original.Pos.X, Original.Pos.Y); }

        /// <summary>
        /// Warp gates only. Game frame when we last warped in from this warp gate
        /// </summary>
        private ulong LastWarpInFrame = 0;

        /// <summary>
        /// Create a new instance from sc2 instance, we wrap around it and add some functionality
        /// </summary>
        public Unit(SC2APIProtocol.Unit unit) : base()
        {
            this.Original = unit;
            this.unitInformation = CurrentGameState.State.GameData.Units[(int)unit.UnitType];

#if DEBUG
            if (AllUnitInstances.ContainsKey(this.Tag))
            {
                Log.SanityCheckFailed("Already have an instance of unit " + this.Tag);
            }
#endif

            AllUnitInstances.Add(this.Tag, this);

            Log.Bulk("Created new unit: " + this);
        }

        /// <summary>
        /// Create placeholder instance, only used as a temporary placeholder during events
        /// </summary>
        public Unit(ulong tag) : base()
        {
            this.Original = new SC2APIProtocol.Unit();
            this.Original.Tag = tag;
        }

        /// <summary>
        /// Called on each new frame, refresh data etc
        /// </summary>
        public void UpdateDataEachTick(SC2APIProtocol.Unit newOriginal)
        {
            this.HasNewOrders = false;

            this.LastFrameData = this.Original;
            this.Original = newOriginal;

            this.ReapplyChronoBoostToSelf();
        }

        /// <summary>
        /// Updates all units each tick
        /// </summary>
        public static void OnTick()
        {
            // Refresh unit data with new input
            foreach (SC2APIProtocol.Unit sc2UnitData in CurrentGameState.State.ObservationState.Observation.RawData.Units)
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

        public bool CanWarpIn()
        {
#warning TODO: There must be a better way of determining whether we can warp in or not, but the charge-mechanic does not seem to be exposed through the API. Is there a better workaround?
            ulong framesSinceWarpIn = Globals.OnCurrentFrame - this.LastWarpInFrame;
            TimeSpan time = TicksToHumanTime(framesSinceWarpIn);

            bool canWarp = time.TotalMilliseconds >= 11000;
            return canWarp;
        }

        public void PerformedWarpIn()
        {
            this.LastWarpInFrame = Globals.OnCurrentFrame;
        }

        private void ReapplyChronoBoostToSelf()
        {
            // Special case, re-apply chrono boost if it ran out
            // If it was worth boosting before, it's worth boosting again!
            bool hadChronoboost = this.LastFrameData.BuffIds.Contains((uint)BuffId.CHRONOBOOSTENERGYCOST);
            bool hasChronoboost = this.Original.BuffIds.Contains((uint)BuffId.CHRONOBOOSTENERGYCOST);
            bool hasKeptOrder = this.LastFrameData.Orders.Count == 1 && this.Original.Orders.Count == 1 && this.LastFrameData.Orders[0].AbilityId == this.Original.Orders[0].AbilityId;

            if (hadChronoboost && (!hasChronoboost) && hasKeptOrder)
            {
                GeneralGameUtility.ApplyChronoBoostTo(this);
            }
        }
    }
}