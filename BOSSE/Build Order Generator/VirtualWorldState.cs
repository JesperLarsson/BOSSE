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

    public class VirtualWorldState
    {
        public List<VirtualUnit> Units = new List<VirtualUnit>();
        public float Minerals;
        public float Gas;
        //public uint Supply;

        /// <summary>
        /// From current sc2 state
        /// </summary>
        public VirtualWorldState(ResponseObservation actualCurrentGameState)
        {
            foreach (var unitIter in actualCurrentGameState.Observation.RawData.Units)
            {
                if (unitIter.Alliance != Alliance.Self)
                    continue;

                VirtualUnit obj = new VirtualUnit(unitIter);
                this.Units.Add(obj);
            }
            this.Minerals = actualCurrentGameState.Observation.PlayerCommon.Minerals;
            this.Gas = actualCurrentGameState.Observation.PlayerCommon.Vespene;
            //this.Supply = actualCurrentGameState.Observation.PlayerCommon.FoodCap; ;
        }

        public VirtualWorldState Clone()
        {
            var obj = (VirtualWorldState)this.MemberwiseClone();
            obj.Units = new List<VirtualUnit>();

            foreach (var iter in this.Units)
            {
                obj.Units.Add(iter.Clone());
            }
            return obj;
        }

        public float EstimateMineralIncomePerFrame()
        {
            return GetUnitsOfType(BotConstants.WorkerUnit).Count * BuiltOrderConfig.WorkerMineralsPerFrameEstimate;
        }

        public List<VirtualUnit> GetUnitsOfType(HashSet<UnitId> types)
        {
            List<VirtualUnit> list = new List<VirtualUnit>();

            foreach (var iter in Units)
            {
                if (types.Contains(iter.Type))
                {
                    list.Add(iter);
                }
            }

            return list;
        }
        public List<VirtualUnit> GetUnitsOfType(UnitId type)
        {
            return GetUnitsOfType(new HashSet<UnitId>() { type });
        }
    }
}
