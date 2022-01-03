/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2022 Jesper Larsson

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
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// A set of instructions which together form an overall strategy
    /// </summary>
    public abstract class BuildOrder
    {
        public List<BuildStep> RemainingSteps = new List<BuildStep>();

        public void ResolveBuildOrder()
        {
            if (this.RemainingSteps.Count == 0)
                return;
            BuildStep nextStep = this.RemainingSteps[0];

            bool resolvedOk = nextStep.ResolveStep();
            if (resolvedOk == false)
                return; // try again next frame

            // Step finished successfully
            this.RemainingSteps.RemoveAt(0);

            // Go to next step
            // Removed for now, we want a single frame to pass between each update
            // Can otherwise cause two building orders to be placed in the spot on the same frame, as we poll the game for that information currently
            // This should not greatly affect bot performance as a single frame is not much on the scale of build orders
            //this.ResolveBuildOrder();
        }
    }
}
