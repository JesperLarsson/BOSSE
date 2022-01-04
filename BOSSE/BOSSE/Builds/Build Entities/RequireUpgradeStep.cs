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
    using System.Linq;
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
    using static global::BOSSE.UnitConstants;

    public class RequireUpgradeStep : BuildStep
    {
        public AbilityConstants.AbilityId UpgradeAbility;
        public bool ApplyChronoboost;

        public RequireUpgradeStep(AbilityConstants.AbilityId upgrade, bool applyChronoboost)
        {
            UpgradeAbility = upgrade;
            ApplyChronoboost = applyChronoboost;
        }

        public override bool ResolveStep()
        {
            bool foundUpgrade = GetUpgradeInfo(this.UpgradeAbility, out uint mineralCost, out uint gasCost, out UnitId researchedByBuilding);
            if (foundUpgrade == false)
            {
                GeneralUtility.BreakIfAttached();
                return false;
            }

            if (CanAfford(mineralCost, gasCost, 0) == false)
                return false;

            Unit fromBuilding = GeneralGameUtility.GetUnits(researchedByBuilding, onlyCompleted: true, onlyVisible: true).Where(o => o.CurrentOrder != null).ToList().FirstOrDefault();
            if (fromBuilding == null)
                return false;

            GeneralGameUtility.Queue(CommandBuilder.UseAbility(this.UpgradeAbility, fromBuilding));
            if (this.ApplyChronoboost)
                GeneralGameUtility.ApplyChronoBoostTo(fromBuilding);

            SubtractCosts(mineralCost, gasCost, 0);
            return true;
        }

        public override string ToString()
        {
            return $"Research {UpgradeAbility}";
        }
    }
}
