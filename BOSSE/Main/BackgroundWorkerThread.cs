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

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// Runs background tasks while the main bot is running to offload the work on another CPU core
    /// </summary>
    public class BackgroundWorkerThread
    {
        private Thread ThreadInstance;

        public void StartThread()
        {
            ThreadInstance = new Thread(new ThreadStart(MainLoop));
            ThreadInstance.Name = "BotBackgroundThread";
            ThreadInstance.Start();
        }

        private void MainLoop()
        {
            while (true)
            {
                try
                {
                    StrategicMapSet.CalculateNewFromCurrentMapState();
                }
                catch (Exception ex)
                {
                    Log.Error("PERIODICAL THREAD EXCEPTION: " + ex);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
