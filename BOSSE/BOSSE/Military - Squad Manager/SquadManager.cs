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
        public MilitaryGoal CurrentMilitaryGoal = MilitaryGoal.NotSet;

        /// <summary>
        /// Military target, if any (can be null)
        /// </summary>
        public Vector3? TargetPoint = null;

        public enum MilitaryGoal
        {
            NotSet = 0,

            DefendGeneral,
            AttackGeneral,
            DefendPoint,
            AttackPoint
        }

        /// <summary>
        /// All active squads
        /// Name => Squad instance mapping
        /// </summary>
        private Dictionary<string, Squad> Squads = new Dictionary<string, Squad>();

        public void Initialize()
        {
            SetNewGoal(MilitaryGoal.DefendGeneral);
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
        /// Sets a new military goal
        /// </summary>
        public void SetNewGoal(MilitaryGoal newGoal)
        {
            if (newGoal == CurrentMilitaryGoal)
                return;

            Log.Info($"Setting new military goal = {newGoal} (was {this.CurrentMilitaryGoal})");
            this.CurrentMilitaryGoal = newGoal;
        }

        /// <summary>
        /// Updates all squad logic
        /// </summary>
        public void Tick()
        {
#if DEBUG
            // Sanity check
            if ((CurrentMilitaryGoal == MilitaryGoal.AttackPoint || CurrentMilitaryGoal == MilitaryGoal.DefendPoint) && TargetPoint == null)
            {
                Log.SanityCheckFailed("Point goal without a point specified");
                GeneralUtility.BreakIfAttached();
            }
#endif

            foreach (var squadIter in Squads.Values)
            {
                squadIter.ControlledBy.Tick(CurrentMilitaryGoal, TargetPoint);
            }
        }
    }
}
