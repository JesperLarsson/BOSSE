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

        public static async Task<ResponseQuery> SendSynchronousRequest_BLOCKING(RequestQuery query)
        {
            var request = new Request();
            request.Query = query;
            var response = await Globals.GameConnection.SendRequest(request);

            return response.Query;
        }
    }
}
