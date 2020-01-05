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
namespace BOSSE.BuildOrderGenerator
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Threading;
    using System.Reflection;
    using System.Linq;
    using System.Diagnostics;

    using SC2APIProtocol;
    using MoreLinq;
    using Google.Protobuf.Collections;

    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    public class VirtualUnit
    {
        public UnitId Type;
        public bool IsBusy = false;

        /// <summary>
        /// If busy, this unit becomes available on the given frame
        /// </summary>
        public ulong BecomesAvailableAtTick = 0;

        /// <summary>
        /// Import constructor
        /// </summary>
        public VirtualUnit(Unit importedActualUnit)
        {
            this.Type = (UnitId)importedActualUnit.UnitType;
        }

        /// <summary>
        /// Planned unit as part of our build order
        /// </summary>
        public VirtualUnit(UnitId type)
        {
            this.Type = type;
        }

        public VirtualUnit Clone()
        {
            var obj = (VirtualUnit)this.MemberwiseClone();
            return obj;
        }

        public bool IsArmy()
        {
            return UnitConstants.ArmyUnits.Contains(this.Type);
        }

        public void PerformEffectsOnWorldState(VirtualWorldState worldState, ulong deltaFrames)
        {
            if (this.Type != BotConstants.WorkerUnit)
            {
                return;
            }

            if (this.IsBusy)
            {
                return; // busy workers don't mine
            }

            worldState.Minerals += (BuiltOrderConfig.WorkerMineralsPerFrameEstimate * deltaFrames);
        }

        public override string ToString()
        {
            return $"[VirtualUnit {this.Type} Busy={this.IsBusy}]";
        }
    }
}
