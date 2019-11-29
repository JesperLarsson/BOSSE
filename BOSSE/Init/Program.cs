/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System;
    using SC2APIProtocol;

    public class Program
    {
        // Debug settings (single player mode)
        private static readonly string mapName = "ThunderbirdLE.SC2Map";
        private static readonly Race opponentRace = Race.Protoss;
        private static readonly Difficulty opponentDifficulty = Difficulty.VeryEasy;

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

                Globals.StarcraftRef = new GameConnection();
                if (args.Length == 0)
                {
                    Globals.IsSinglePlayer = true;
                    Globals.Random = new Random(1234567); // use the same random number generation every time to make debugging problems easier

                    Globals.StarcraftRef.ReadSettings();
                    Globals.StarcraftRef.RunSinglePlayer(Globals.BotRef, mapName, BotConstants.SpawnAsRace, opponentRace, opponentDifficulty).Wait();
                }
                else
                {
                    Globals.IsSinglePlayer = false;
                    Globals.StarcraftRef.RunLadder(Globals.BotRef, BotConstants.SpawnAsRace, args).Wait();
                }
            }
            catch (Exception ex)
            {
                Log.Error("TOP LOOP EXCEPTION" + Environment.NewLine + ex.ToString());
            }

            Log.Info("Exiting...");
        }
    }
}