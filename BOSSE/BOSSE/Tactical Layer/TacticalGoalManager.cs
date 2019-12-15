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
        private Point2D CurrentMilitaryGoalPoint = null;

        public override void Initialize()
        {
        }

        public override void OnFrameTick()
        {
        }

        public void SetNewGoal(MilitaryGoal newGoal, Point2D newPoint = null)
        {
            if (newGoal == CurrentMilitaryGoal)
                return;

            Log.Info($"Setting new military goal = {newGoal} (was {this.CurrentMilitaryGoal}) at {newPoint.ToString2()}");
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

        public Point2D GetTarget()
        {
            return CurrentMilitaryGoalPoint;
        }

        public void Get(out MilitaryGoal outGoal, out Point2D point)
        {
            outGoal = CurrentMilitaryGoal;
            point = CurrentMilitaryGoalPoint;
        }
    }
}
