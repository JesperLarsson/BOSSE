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
    /// Determines which build order to follow
    /// </summary>
    public class CurrentBuildManager : Manager
    {
        /// <summary>
        /// Allowed build orders
        /// </summary>
        private List<BuildOrder> buildsAvailable = new List<BuildOrder>()
        {
            // Protoss
            new BlinkStalkers(),
            //new NexusRush(),

            // Terran
            //new MarineSpam(),
        };

        public BuildOrder CurrentBuildOrder;

        public CurrentBuildManager()
        {
            // Choose a random build order for this session
            this.buildsAvailable.Shuffle();
            this.CurrentBuildOrder = buildsAvailable[0];

            string chatString = $"Using build: {this.CurrentBuildOrder.GetType().Name}";
            GeneralGameUtility.Queue(CommandBuilder.Chat(chatString));
        }

        public override void OnFrameTick()
        {
            this.CurrentBuildOrder.ResolveBuildOrder();
        }

        public bool HasCompletedBuildOrder()
        {
            return this.CurrentBuildOrder.IsCompleted;
        }
    }
}
