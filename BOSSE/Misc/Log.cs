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

        /// <summary>
        /// Log file only, not to console
        /// </summary>
        public static void Bulk(string line)
        {
            WriteLine("BULK", line, false);
        }

        /// <summary>
        /// General information
        /// </summary>
        public static void Info(string line)
        {
            WriteLine("INFO", line, true);
        }

        /// <summary>
        /// For temporary debugging of specific points of the code
        /// </summary>
        public static void Debug(string line)
        {
            WriteLine("DEBUG", line, true);
        }

        /// <summary>
        /// Semi-serious issues
        /// </summary>
        public static void Warning(string line)
        {
            WriteLine("WARNING", line, true);
        }

        /// <summary>
        /// Serious errors and unexepcted exceptions
        /// </summary>
        public static void Error(string line)
        {
            WriteLine("ERROR", line, true);
        }

        /// <summary>
        /// Will stop execution if called during a debugging session
        /// </summary>
        public static void SanityCheckFailed(string line, bool breakExe = true)
        {
            WriteLine("SANITY CHECK FAILED", line, true);

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

        private static void WriteLine(string prefix, string line, bool trace)
        {
            lock (LogLock)
            {
                if (FilePath == null)
                {
                    Initialize();
                }

                var msg = "[" + DateTime.Now.ToString("HH:mm:ss") + " " + prefix + "] " + line;

                var fileStream = new StreamWriter(FilePath, true);
                fileStream.WriteLine(msg);
                fileStream.Close();

                if (!StdoutClosed && trace)
                {
                    try
                    {
                        Console.WriteLine(msg);
                    }
                    catch
                    {
                        StdoutClosed = true;
                    }
                }

                // To VS output
                if (trace)
                {
                    System.Diagnostics.Debug.WriteLine(msg);
                }
            }
        }
    }
}