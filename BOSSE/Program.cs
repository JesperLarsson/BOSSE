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