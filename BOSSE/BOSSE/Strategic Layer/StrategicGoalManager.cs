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
    /// Our overall high level strategic goal
    /// </summary>
    public enum StrategicGoal
    {
        NotSet = 0,

        EconomyFocus,
        BuildMilitaryPlusEconomy,
        BuildMilitary,
        Expand
    }

    /// <summary>
    /// Holds our overall strategic goal
    /// </summary>
    public class StrategicGoalManager : Manager
    {
        private StrategicGoal CurrentStrategicGoal = StrategicGoal.NotSet;

        public override void Initialize()
        {
            
        }

        public override void OnFrameTick()
        {

        }

        public void SetNewGoal(StrategicGoal newGoal)
        {
            if (newGoal == CurrentStrategicGoal)
                return;

            Log.Info($"Setting new strategic goal = {newGoal} (was {CurrentStrategicGoal})");
            CurrentStrategicGoal = newGoal;
        }

        public StrategicGoal GetCurrentGoal()
        {
            return CurrentStrategicGoal;
        }
    }
}
