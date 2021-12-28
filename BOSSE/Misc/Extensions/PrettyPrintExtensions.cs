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
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// Adds extension methods for dumping objects as strings, useful for debugging
    /// </summary>
    public static class PrettyPrintExtensions
    {
        public static string DumpObject(this IEnumerable<object> list)
        {
            string outStr = "";

            foreach (var iter in list)
            {
                outStr += DumpObject(iter) + Environment.NewLine;
            }

            return outStr;
        }

        public static string DumpObject(this object obj)
        {
            Type type = obj.GetType();

            string outString = "[";
            outString += type.Name + " ";

            foreach (var iter in type.GetFields())
            {
                var val = iter.GetValue(obj);
                string str;

                if (val == null)
                {
                    str = "NULL";
                }
                else
                {
                    str = val.ToString();
                }

                outString += $"{iter.Name} = {str}, ";
            }
            foreach (var iter in type.GetProperties())
            {
                var val = iter.GetValue(obj);
                string str;

                if (val == null)
                {
                    str = "NULL";
                }
                else
                {
                    str = val.ToString();
                }

                outString += $"{iter.Name} = {str}, ";
            }

            outString = outString.TrimSeparatorFromEnd();
            outString += "]";

            return outString;
        }

        public static string TrimSeparatorFromEnd(this string str, string separator = ",")
        {
            str = str.Trim();

            if (str.EndsWith(separator))
            {
                str = str.Substring(0, str.Length - separator.Length);
            }

            return str;
        }
    }
}
