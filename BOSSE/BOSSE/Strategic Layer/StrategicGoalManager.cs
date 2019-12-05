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
