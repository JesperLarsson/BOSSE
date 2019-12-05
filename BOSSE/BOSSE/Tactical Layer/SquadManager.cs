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
            BOSSE.TacticalGoalRef.Get(out MilitaryGoal currentMilitaryGoal, out Vector3? currentMilitaryGoalPoint);

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
