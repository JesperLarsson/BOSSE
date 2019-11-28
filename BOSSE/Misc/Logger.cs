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

    using SC2APIProtocol;
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GameUtility;

    /// <summary>
    /// Writes logging information
    /// </summary>
    public static class Log
    {
        private static string logFile;
        private static bool stdoutClosed;

        public static void Info(string line, params object[] parameters)
        {
            WriteLine("INFO", line, parameters);
        }

        public static void Warning(string line, params object[] parameters)
        {
            WriteLine("WARNING", line, parameters);
        }

        public static void Error(string line, params object[] parameters)
        {
            WriteLine("ERROR", line, parameters);
        }

        private static void Initialize()
        {
            logFile = "Logs/" + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".log";
            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
        }

        private static void WriteLine(string prefix, string line, params object[] parameters)
        {
            if (!Globals.IsSinglePlayer)
                return;

            if (logFile == null)
                Initialize();

            var msg = "[" + DateTime.Now.ToString("HH:mm:ss") + " " + prefix + "] " + string.Format(line, parameters);

            var fileStream = new StreamWriter(logFile, true);
            fileStream.WriteLine(msg);
            fileStream.Close();

            if (!stdoutClosed)
            {
                try
                {
                    Console.WriteLine(msg, parameters);
                }
                catch
                {
                    stdoutClosed = true;
                }
            }
        }
    }
}