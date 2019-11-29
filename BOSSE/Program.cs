/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System;

    public class Program
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        private static void Main(string[] args)
        {
            try
            {
                Log.Info($"****************");
                Log.Info($"StarCraft 2 Bot - BOSSE - Version {BotConstants.ApplicationVersion}");
                Log.Info($"****************");

                MainLoop mainLoop = new MainLoop();
                mainLoop.Start(args);
            }
            catch (Exception ex)
            {
                Log.Error("TOP LOOP EXCEPTION" + Environment.NewLine + ex.ToString());
            }

            Log.Info("Exiting BOSSE");
        }
    }
}