namespace Bot
{
    using System;
    using SC2APIProtocol;

    internal class Program
    {
        // Debug settings (single player mode)
        private static readonly string mapName = "ThunderbirdLE.SC2Map";
        private static readonly Race opponentRace = Race.Protoss;
        private static readonly Difficulty opponentDifficulty = Difficulty.VeryEasy;

        private static void Main(string[] args)
        {
            try
            {
                Logger.Info("Started version " + GameConstants.ApplicationVersion);

                Globals.StarcraftRef = new GameConnection();
                if (args.Length == 0)
                {
                    Globals.StarcraftRef.readSettings();
                    Globals.StarcraftRef.RunSinglePlayer(Globals.BotRef, mapName, GameConstants.SpawnAsRace, opponentRace, opponentDifficulty).Wait();
                }
                else
                    Globals.StarcraftRef.RunLadder(Globals.BotRef, GameConstants.SpawnAsRace, args).Wait();
            }
            catch (Exception ex)
            {
                Logger.Info(ex.ToString());
            }

            Logger.Info("Done yo");
        }
    }
}