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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Determines our overall military goal at this moment
    /// Squad may have goals which override this behaviour
    /// </summary>
    public enum MilitaryGoal
    {
        NotSet = 0,

        DefendGeneral,
        AttackGeneral,
        DefendPoint,
        AttackPoint
    }

    /// <summary>
    /// Holds our high level tactical goal
    /// </summary>
    public class TacticalGoalManager : Manager
    {
        private MilitaryGoal CurrentMilitaryGoal = MilitaryGoal.NotSet;

        /// <summary>
        /// Military target, if any (can be null)
        /// </summary>
        private Vector3? CurrentMilitaryGoalPoint = null;

        public override void Initialize()
        {
        }

        public override void OnFrameTick()
        {
        }

        public void SetNewGoal(MilitaryGoal newGoal, Vector3? newPoint = null)
        {
            if (newGoal == CurrentMilitaryGoal)
                return;

            Log.Info($"Setting new military goal = {newGoal} (was {this.CurrentMilitaryGoal}) at {newPoint.ToStringSafe2()}");
            this.CurrentMilitaryGoal = newGoal;
        }

        public MilitaryGoal GetGoal()
        {
            return CurrentMilitaryGoal;
        }

        public bool GoalHasTarget()
        {
            return CurrentMilitaryGoalPoint != null;
        }

        public Vector3? GetTarget()
        {
            return CurrentMilitaryGoalPoint;
        }

        public void Get(out MilitaryGoal outGoal, out Vector3? point)
        {
            outGoal = CurrentMilitaryGoal;
            point = CurrentMilitaryGoalPoint;
        }
    }
}
