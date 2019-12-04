/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;

    /// <summary>
    /// Writes logging information to file/console/visual studio
    /// </summary>
    public static class Log
    {
        private static string FilePath;
        private static bool StdoutClosed;
        private static object LogLock = new object();

        public static void Bulk(string line, params object[] parameters)
        {
            WriteLine("BULK", line, false, parameters);
        }

        public static void Info(string line, params object[] parameters)
        {
            WriteLine("INFO", line, true, parameters);
        }

        public static void Warning(string line, params object[] parameters)
        {
            WriteLine("WARNING", line, true, parameters);
        }

        public static void Error(string line, params object[] parameters)
        {
            WriteLine("ERROR", line, true, parameters);
        }

        public static void SanityCheckFailed(string line, bool breakExe = true, params object[] parameters)
        {
            WriteLine("SANITY CHECK FAILED", line, true, parameters);

            if (breakExe && System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        private static void Initialize()
        {
            FilePath = "Logs/" + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".log";
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        }

        private static void WriteLine(string prefix, string line, bool trace, params object[] parameters)
        {
            lock (LogLock)
            {
                if (FilePath == null)
                {
                    Initialize();
                }

                var msg = "[" + DateTime.Now.ToString("HH:mm:ss") + " " + prefix + "] " + string.Format(line, parameters);

                var fileStream = new StreamWriter(FilePath, true);
                fileStream.WriteLine(msg);
                fileStream.Close();

                if (!StdoutClosed && trace)
                {
                    try
                    {
                        Console.WriteLine(msg, parameters);
                    }
                    catch
                    {
                        StdoutClosed = true;
                    }
                }

                // To VS output
                if (trace)
                {
                    System.Diagnostics.Debug.WriteLine(msg, parameters);
                }
            }
        }
    }
}