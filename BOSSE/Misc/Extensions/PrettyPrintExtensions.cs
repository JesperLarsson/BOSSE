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
