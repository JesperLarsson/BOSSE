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
    using System.Drawing;
    using System.Diagnostics;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;
    using static AbilityConstants;
    
    /// <summary>
    /// Responsible for building construction and placement
    /// </summary>
    public class ConstructionManager : Manager
    {
        private Wall naturalWall = null;

        public override void Initialize()
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //this.naturalWall = WallinUtility.GetNaturalWall();
            //sw.Stop();
            //Log.Info("Found natural wall in " + sw.Elapsed.TotalMilliseconds / 1000 + " s");

            //if (this.naturalWall == null)
            //{
            //    Log.SanityCheckFailed("Unable to find a config to build natural wall");
            //}
            //else
            //{
            //    Log.Info("OK - Found natural wall");
            //}
        }

        public override void OnFrameTick()
        {

        }
    }
}
