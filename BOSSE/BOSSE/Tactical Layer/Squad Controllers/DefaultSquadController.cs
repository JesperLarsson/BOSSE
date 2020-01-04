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
                Point2D natDef = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.GetNaturalDefensePosition();

                Queue(CommandBuilder.AttackMoveAction(this.controlledSquad.AssignedUnits, natDef));
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
