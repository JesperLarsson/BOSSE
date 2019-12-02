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
    using static GameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Manages our squads consisting of army units
    /// </summary>
    public class SquadManager
    {        
        /// <summary>
        /// All active squads
        /// Name => Squad instance mapping
        /// </summary>
        private readonly Dictionary<string, Squad> Squads = new Dictionary<string, Squad>();

        /// <summary>
        /// Initializes the squad manager
        /// </summary>
        public void Initialize()
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
        public void Tick()
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
            }
        }
    }
}
