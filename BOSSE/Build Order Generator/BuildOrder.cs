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

    public class BuildOrder
    {
        private List<PlannedAction> ActionOrder = new List<PlannedAction>();

        public bool IsEmpty()
        {
            return this.ActionOrder.Count == 0;
        }

        public void Add(PlannedAction newAction)
        {
            ActionOrder.Add(newAction);
        }

        public bool ContainsActionOfType(Type actionType)
        {
            return ActionOrder.Where(a => a.GetType() == actionType).ToList().Count > 0;
        }

        public void SimulateAll(VirtualWorldState onWorldState, ulong currentFrame, ulong deltaFrames)
        {
            // Take all actions
            foreach (PlannedAction actioniter in ActionOrder)
            {
                actioniter.SimulateActionEffects(onWorldState, currentFrame, deltaFrames);
            }

            // Simulate effects of existing units
            foreach (VirtualUnit unitIter in onWorldState.Units)
            {
                unitIter.PerformEffectsOnWorldState(onWorldState, deltaFrames);
            }
        }

        public BuildOrder Clone()
        {
            BuildOrder obj = (BuildOrder)this.MemberwiseClone();
            obj.ActionOrder = new List<PlannedAction>();

            foreach (var iter in this.ActionOrder)
            {
                obj.ActionOrder.Add(iter.Clone());
            }
            return obj;
        }

        /// <summary>
        /// Returns a scalar value with an estimate of how good our general game position is during the given time
        /// We use our total value as a good-enough heuristic
        /// </summary>
        public ulong Evaluate(VirtualWorldState worldState, BuildOrderWeights weights)
        {
            uint totalMinerals = 0;
            uint totalGas = 0;

            foreach (var iter in worldState.Units)
            {
                var unitInfo = GetUnitInfo(iter.Type);

                uint unitMinerals = unitInfo.MineralCost;
                uint unitGas = unitInfo.VespeneCost;
                if (iter.Type == BotConstants.WorkerUnit)
                {
                    unitMinerals = (uint)(unitMinerals * weights.Worker);
                    unitGas = (uint)(unitGas * weights.Worker);
                }
                else if (iter.Type == BotConstants.CommandCenterUnit)
                {
                    unitMinerals = (uint)(unitMinerals * weights.CommandCenter);
                    unitGas = (uint)(unitGas * weights.CommandCenter);
                }
                else if (iter.IsArmy())
                {
                    unitMinerals = (uint)(unitMinerals * weights.Military);
                    unitGas = (uint)(unitGas * weights.Military);
                }

                totalMinerals += unitMinerals;
                totalGas += unitGas;
            }

            return totalMinerals + totalGas;
        }

        public override string ToString()
        {
            string str = "";
            foreach (var iter in ActionOrder)
            {
                str += iter.GetType().Name + ", ";
            }

            return $"[BuildOrder {ActionOrder.Count} - {str}]";
        }
    }
}
