/*
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
    using System.Linq;
    using SC2APIProtocol;

    /// <summary>
    /// Reads commandline arguments passed to the bot
    /// </summary>
    public class CommandLine
    {
        public CommandLine(string[] args)
        {
            for (var i = 0; i < args.Count(); i += 2)
                if (args[i] == "-g" || args[i] == "--GamePort")
                {
                    GamePort = int.Parse(args[i + 1]);
                }
                else if (args[i] == "-o" || args[i] == "--StartPort")
                {
                    StartPort = int.Parse(args[i + 1]);
                }
                else if (args[i] == "-l" || args[i] == "--LadderServer")
                {
                    LadderServer = args[i + 1];
                }
                else if (args[i] == "-c" || args[i] == "--ComputerOpponent")
                {
                    if (ComputerRace == Race.NoRace)
                        ComputerRace = Race.Random;
                    i--;
                }
                else if (args[i] == "-a" || args[i] == "--ComputerRace")
                {
                    if (args[i + 1] == "Protoss")
                        ComputerRace = Race.Protoss;
                    else if (args[i + 1] == "Terran")
                        ComputerRace = Race.Terran;
                    else if (args[i + 1] == "Zerg")
                        ComputerRace = Race.Zerg;
                    else if (args[i + 1] == "Random")
                        ComputerRace = Race.Random;
                }
                else if (args[i] == "-d" || args[i] == "--ComputerDifficulty")
                {
                    if (args[i + 1] == "VeryEasy") ComputerDifficulty = Difficulty.VeryEasy;
                    else if (args[i + 1] == "Easy") ComputerDifficulty = Difficulty.Easy;
                    else if (args[i + 1] == "Medium") ComputerDifficulty = Difficulty.Medium;
                    else if (args[i + 1] == "MediumHard") ComputerDifficulty = Difficulty.MediumHard;
                    else if (args[i + 1] == "Hard") ComputerDifficulty = Difficulty.Hard;
                    else if (args[i + 1] == "Harder") ComputerDifficulty = Difficulty.Harder;
                    else if (args[i + 1] == "VeryHard") ComputerDifficulty = Difficulty.VeryHard;
                    else if (args[i + 1] == "CheatVision") ComputerDifficulty = Difficulty.CheatVision;
                    else if (args[i + 1] == "CheatMoney") ComputerDifficulty = Difficulty.CheatMoney;
                    else if (args[i + 1] == "CheatInsane") ComputerDifficulty = Difficulty.CheatInsane;
                    else ComputerDifficulty = Difficulty.Easy;
                }
        }

        public int GamePort { get; set; }
        public int StartPort { get; set; }
        public string LadderServer { get; set; }
        public Race ComputerRace { get; set; } = Race.NoRace;
        public Difficulty ComputerDifficulty { get; set; } = Difficulty.VeryEasy;
    }
}