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
    using System.Runtime.CompilerServices;
    using System.Diagnostics;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;
    using System.Runtime.Serialization;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// Signals a general bot exception where the game can continue
    /// </summary>
    public class BosseRecoverableException : Exception
    {
        public BosseRecoverableException()
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }

        public BosseRecoverableException(string msg) : base(msg)
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
    }
}
