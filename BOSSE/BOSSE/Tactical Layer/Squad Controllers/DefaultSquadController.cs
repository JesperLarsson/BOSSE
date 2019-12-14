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
    /// Standard squad controller without special logic, this is the default fallback suitable for many situations
    /// </summary>
    public class DefaultSquadController : SquadControllerBase
    {
        /// <summary>
        /// Updates squad controller
        /// </summary>
        public override void Tick(MilitaryGoal currentGlobalGoal, Point2D targetPoint)
        {
            if (currentGlobalGoal == MilitaryGoal.DefendGeneral)
            {
                // Move units to our ramp
                Point2D ramp = Tyr.Tyr.MapAnalyzer.GetMainRamp();

                if (ramp == null)
                {
                    Log.Info("Unable to find ramp");
                    return;
                }

                Queue(CommandBuilder.AttackMoveAction(this.controlledSquad.AssignedUnits, ramp));
            }
            else if (currentGlobalGoal == MilitaryGoal.DefendPoint)
            {
                // Attack move to point
                Queue(CommandBuilder.AttackMoveAction(this.controlledSquad.AssignedUnits, targetPoint));
            }
            else if (currentGlobalGoal == MilitaryGoal.AttackGeneral)
            {
                // Attack move towards enemy main base
                Point2D enemyLocation = GuessEnemyBaseLocation();
                if (enemyLocation == null)
                {
                    Log.Warning("Unable to find enemy base location");
                    return;
                }

                Queue(CommandBuilder.AttackMoveAction(this.controlledSquad.AssignedUnits, enemyLocation));
            }
            else if (currentGlobalGoal == MilitaryGoal.AttackPoint)
            {
                // Attack move to point
                Queue(CommandBuilder.AttackMoveAction(this.controlledSquad.AssignedUnits, targetPoint));
            }
            else
            {
                throw new NotImplementedException("Military goal = " + currentGlobalGoal);
            }
        }
    }
}
