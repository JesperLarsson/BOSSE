/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System;
    using SC2APIProtocol;

    public class Program
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        private static void Main(string[] args)
        {
            try
            {
                Log.Info("****************");
                Log.Info("Started BOSSE version " + BotConstants.ApplicationVersion);
                Log.Info("****************");

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