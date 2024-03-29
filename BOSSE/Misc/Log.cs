﻿/*
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
    using System.IO;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Collections.Concurrent;
    using System.Diagnostics;

    /// <summary>
    /// Writes logging information to file/console/visual studio output
    /// </summary>
    public static class Log
    {
        private static string LogPathAbsolute;
        private static Thread ThreadInstance = null;
        private static readonly ConcurrentQueue<string> FileQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// Log file only, not to console
        /// </summary>
        public static void Bulk(string line)
        {
            FormatAndQueue("BULK ", line, false);
        }

        /// <summary>
        /// General information
        /// </summary>
        public static void Info(string line)
        {
            FormatAndQueue("INFO ", line, true);
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
            FormatAndQueue("WARN ", line, true);
        }

        /// <summary>
        /// Serious errors and unexepcted exceptions
        /// </summary>
        public static void Error(string line)
        {
            FormatAndQueue("ERR  ", line, true);
        }

        /// <summary>
        /// Will stop execution if called during a debugging session
        /// </summary>
        public static void SanityCheckFailed(string line, bool breakExe = true)
        {
            FormatAndQueue("SAN  ", line, true);

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
                Priority = ThreadPriority.AboveNormal // We don't consme much resources, but we don't want to risk getting overrun
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

                Thread.Sleep(100);
            }
        }

        private static void LoggingMainTick()
        {
            if (LogPathAbsolute == null)
                return;

            var fileStream = new StreamWriter(LogPathAbsolute, true);
            while (FileQueue.TryDequeue(out string msg))
            {
                fileStream.WriteLine(msg);
            }
            fileStream.Close();
        }

        private static void FormatAndQueue(string typePrefix, string lineToLog, bool traceToConsole)
        {
            // Get name of the caller
            string callingClassName = "N/A";
            var st = new StackTrace();
            for (int i = 2; i < st.FrameCount; i++)
            {
                var method = st.GetFrame(i).GetMethod();
                callingClassName = method.ReflectedType.Name;

                if (callingClassName.StartsWith("<>"))
                    continue; // Ignore anonymous names

                break;
            }
            
            // Format
            string fullPrefix = $"[{DateTime.Now.ToString("HH:mm:ss")} {typePrefix} {Globals.OnCurrentFrame} {callingClassName}]";
            string fullMessageRow = String.Format("{0, -55}", fullPrefix) + lineToLog;

            // File operations is done in a separate thread for better performance
            FileQueue.Enqueue(fullMessageRow);

            // Trace right away, it doesn't consume much resources and makes debugging easier
            if (traceToConsole)
            {
                System.Diagnostics.Debug.WriteLine(fullMessageRow);
                Console.WriteLine(fullMessageRow);
            }
        }
    }
}