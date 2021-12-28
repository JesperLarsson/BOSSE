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
    using System.Linq;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Handles our ramps - Puts up / down supply depots etc as necessary
    /// </summary>
    public class RampManager : Manager
    {
        private HashSet<Unit> ManagedSupplyDepots = new HashSet<Unit>();

        public override void Initialize()
        {
        }

        public override void OnFrameTick()
        {
#warning TODO: Look for enemies that are close and put up depots
            foreach (Unit iter in this.ManagedSupplyDepots)
            {
                if (iter.UnitType != UnitId.SUPPLY_DEPOT_LOWERED)
                {
                    PutDownDepot(iter);
                }
            }
        }

        public void AddSupplyDepot(Unit newDepo)
        {
            ManagedSupplyDepots.Add(newDepo);
        }

        private void PutDownDepot(Unit depoToDown)
        {
            Log.Bulk("Putting down depot " + depoToDown);
            Queue(CommandBuilder.UseAbility(AbilityId.SupplyDepotLower, depoToDown));
        }

        private void PullUpDepot(Unit depoToDown)
        {
            Queue(CommandBuilder.UseAbility(AbilityId.SupplyDepotLower, depoToDown));
        }
    }
}
