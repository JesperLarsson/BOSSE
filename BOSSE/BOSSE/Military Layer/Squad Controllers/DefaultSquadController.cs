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
    /// Standard squad controller without special logic, this is the default fallback suitable for many situations
    /// </summary>
    public class DefaultSquadController : ISquadController
    {
        /// <summary>
        /// The squad that we're controlling
        /// </summary>
        private readonly Squad controlledSquad;

        public DefaultSquadController(Squad squadToControl)
        {
            controlledSquad = squadToControl;
        }

        /// <summary>
        /// Updates squad controller
        /// </summary>
        public void Tick(SquadManager.MilitaryGoal currentGlobalGoal, Vector3? TargetPoint)
        {
            if (currentGlobalGoal == SquadManager.MilitaryGoal.DefendGeneral)
            {
                // Move units to our ramp
                Point2D ramp = Tyr.Tyr.MapAnalyzer.GetMainRamp();

                if (ramp == null)
                {
                    Log.Info("Unable to find ramp");
                    return;
                }

                Queue(CommandBuilder.AttackMoveAction(this.controlledSquad.AssignedUnits, new Vector3(ramp.X, ramp.Y, 0)));
            }
            else if (currentGlobalGoal == SquadManager.MilitaryGoal.DefendPoint)
            {
                // Attack move to point
                Queue(CommandBuilder.AttackMoveAction(this.controlledSquad.AssignedUnits, new Vector3(TargetPoint.Value.X, TargetPoint.Value.Y, 0)));
            }
            else if (currentGlobalGoal == SquadManager.MilitaryGoal.AttackGeneral)
            {
                // Attack move towards enemy main base
                Vector3? enemyLocation = GuessEnemyBaseLocation();
                if (enemyLocation == null)
                {
                    Log.Warning("Unable to find enemy base location");
                    return;
                }

                Queue(CommandBuilder.AttackMoveAction(this.controlledSquad.AssignedUnits, new Vector3(enemyLocation.Value.X, enemyLocation.Value.Y, 0)));
            }
            else if (currentGlobalGoal == SquadManager.MilitaryGoal.AttackPoint)
            {
                // Attack move to point
                Queue(CommandBuilder.AttackMoveAction(this.controlledSquad.AssignedUnits, new Vector3(TargetPoint.Value.X, TargetPoint.Value.Y, 0)));
            }
            else
            {
                throw new NotImplementedException("Military goal = " + currentGlobalGoal);
            }
        }
    }
}
