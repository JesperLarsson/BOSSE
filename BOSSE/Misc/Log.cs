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
    using System.IO;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Collections.Concurrent;

    /// <summary>
    /// Writes logging information to file/console/visual studio output
    /// </summary>
    public static class Log
    {
        private static string LogPathAbsolute;
        private static Thread ThreadInstance = null;
        private static readonly ConcurrentQueue<string> TraceQueue = new ConcurrentQueue<string>();
        private static readonly ConcurrentQueue<string> FileQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// Log file only, not to console
        /// </summary>
        public static void Bulk(string line)
        {
            FormatAndQueue("BULK", line, false);
        }

        /// <summary>
        /// General information
        /// </summary>
        public static void Info(string line)
        {
            FormatAndQueue("INFO", line, true);
        }

        /// <summary>
        /// For temporary debugging of specific points of the code
        /// </summary>
        public static void Debug(string line)
        {
            FormatAndQueue("DEBUG", line, true);
        }

        /// <summary>
        /// Semi-serious issues
        /// </summary>
        public static void Warning(string line)
        {
            FormatAndQueue("WARNING", line, true);
        }

        /// <summary>
        /// Serious errors and unexepcted exceptions
        /// </summary>
        public static void Error(string line)
        {
            FormatAndQueue("ERROR", line, true);
        }

        /// <summary>
        /// Will stop execution if called during a debugging session
        /// </summary>
        public static void SanityCheckFailed(string line, bool breakExe = true)
        {
            FormatAndQueue("SANITY CHECK FAILED", line, true);

            if (breakExe && System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void Start()
        {
            LogPathAbsolute = "Logs/" + "BOSSE " + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".log";
            Directory.CreateDirectory(Path.GetDirectoryName(LogPathAbsolute));

            ThreadInstance = new Thread(new ThreadStart(LoggingMainLoop))
            {
                Name = "BosseLogger",
                Priority = ThreadPriority.BelowNormal
            };
            ThreadInstance.Start();
        }

        private static void LoggingMainLoop()
        {
            while (true)
            {
                try
                {
                    LoggingMainTick();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("EXCEPTION IN LOGGER: " + ex);
                }

                Thread.Sleep(1000);
            }
        }

        private static void LoggingMainTick()
        {
            if (LogPathAbsolute == null)
                return;

            while (FileQueue.TryDequeue(out string msg))
            {
                var fileStream = new StreamWriter(LogPathAbsolute, true);
                fileStream.WriteLine(msg);
                fileStream.Close();
            }

            while (TraceQueue.TryDequeue(out string msg))
            {
                System.Diagnostics.Debug.WriteLine(msg);
                Console.WriteLine(msg);
            }
        }

        private static void FormatAndQueue(string prefix, string line, bool trace)
        {
            var msg = "[" + DateTime.Now.ToString("HH:mm:ss") + " " + prefix + "] " + line;

            FileQueue.Enqueue(msg);

            if (trace)
                TraceQueue.Enqueue(msg);
        }
    }
}