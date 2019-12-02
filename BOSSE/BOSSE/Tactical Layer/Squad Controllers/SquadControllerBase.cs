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
    /// Interface for all squad controllers
    /// </summary>
    public abstract class SquadControllerBase
    {
        /// <summary>
        /// The squad that we're controlling
        /// </summary>
        protected Squad controlledSquad;

        public abstract void Tick(MilitaryGoal currentGlobalGoal, Vector3? TargetPoint);

        public void AssignSquad(Squad newSquad)
        {
            if (controlledSquad != newSquad && controlledSquad != null)
            {
                Log.SanityCheckFailed("Assigned new squad to existing squad controller");
            }

            controlledSquad = newSquad;
        }
    }
}
