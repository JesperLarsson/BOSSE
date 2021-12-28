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
    /// Manages our squads consisting of army units
    /// </summary>
    public class SquadManager : Manager
    {
        /// <summary>
        /// All active squads
        /// Name => Squad instance mapping
        /// </summary>
        private readonly Dictionary<string, Squad> Squads = new Dictionary<string, Squad>();
        private bool SquadsModified = false;

        /// <summary>
        /// Initializes the squad manager
        /// </summary>
        public override void Initialize()
        {

        }

        /// <summary>
        /// Adds the squad to the manager
        /// </summary>
        public void AddNewSquad(Squad newSquad)
        {
            if (Squads.ContainsKey(newSquad.Name))
            {
                Log.Warning("Squad already exists: " + newSquad.Name);
                return;
            }

            Squads[newSquad.Name] = newSquad;
        }

        /// <summary>
        /// Removes the squad from the manager, it will no longer receive update ticks
        /// </summary>
        public void DeleteExistingSquad(string name)
        {
            if (!Squads.ContainsKey(name))
            {
                Log.Warning("Tried to delete squad that doesn't exist: " + name);
                return;
            }

            Squads[name].IsBeingDeleted();
            Squads.Remove(name);
            SquadsModified = true;
        }

        /// <summary>
        /// Gets the given squad name
        /// </summary>
        public Squad GetSquadOrNull(string squadName)
        {
            if (!Squads.ContainsKey(squadName))
            {
                return null;
            }

            return Squads[squadName];
        }

        /// <summary>
        /// Updates all squad logic
        /// </summary>
        public override void OnFrameTick()
        {
            BOSSE.TacticalGoalRef.Get(out MilitaryGoal currentMilitaryGoal, out Point2D currentMilitaryGoalPoint);

#if DEBUG
            // Sanity check
            if ((currentMilitaryGoal == MilitaryGoal.AttackPoint || currentMilitaryGoal == MilitaryGoal.DefendPoint) && currentMilitaryGoalPoint == null)
            {
                Log.SanityCheckFailed("Point goal without a point specified");
                GeneralUtility.BreakIfAttached();
            }
#endif

            foreach (var squadIter in Squads.Values)
            {
                squadIter.ControlledBy.Tick(currentMilitaryGoal, currentMilitaryGoalPoint);

                // Re-run if we modified our controllers
                if (SquadsModified)
                {
                    SquadsModified = false;
                    OnFrameTick();
                    return;
                }
            }
        }
    }
}
