/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using SC2APIProtocol;

    /// <summary>
    /// Manages all output to StarCraft
    /// </summary>
    public static class GameOutput
    {
        /// <summary>
        /// Queued outgoing actions to the game
        /// These will be sent to the game at the end of each logical frame
        /// </summary>
        public static List<SC2APIProtocol.Action> QueuedActions = new List<SC2APIProtocol.Action>();
    }
}
