namespace Bot
{
    using System;
    using SC2APIProtocol;

    internal class Program
    {
        // Settings for your bot.
        private static readonly Bot bot = new JesperBot();
        private const Race race = Race.Terran;

        // Settings for single player mode
        private static readonly string mapName = "ThunderbirdLE.SC2Map";
        private static readonly Race opponentRace = Race.Protoss;
        private static readonly Difficulty opponentDifficulty = Difficulty.VeryEasy;

        public static GameConnection gc;

        private static void Main(string[] args)
        {
            try
            {
                gc = new GameConnection();
                if (args.Length == 0)
                {
                    gc.readSettings();
                    gc.RunSinglePlayer(bot, mapName, race, opponentRace, opponentDifficulty).Wait();
                }
                else
                    gc.RunLadder(bot, race, args).Wait();
            }
            catch (Exception ex)
            {
                Logger.Info(ex.ToString());
            }

            Logger.Info("Terminated.");
        }
    }
}