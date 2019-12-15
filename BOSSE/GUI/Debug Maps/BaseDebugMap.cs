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
namespace DebugGui
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using BOSSE;
    using SC2APIProtocol;

    /// <summary>
    /// Base class for GUI maps
    /// Each map is rendered in a seperate thread to keep GUI responsive
    /// </summary>
    public abstract class BaseDebugMap
    {
        public string MapName = "N/A";

        protected TimeSpan RenderInterval = TimeSpan.FromSeconds(1);
        protected int RenderScale = 8;
        protected Image CurrentOutputtedMap = null;

        /// <summary>
        /// Called for GUI thread periodically
        /// </summary>
        public Image GetMap()
        {
            return CurrentOutputtedMap;
        }

        public void Start()
        {
            Thread th = new Thread(new ThreadStart(MainLoop));
            th.Name = "GuiMapRenderer";
            th.Start();
        }

        private void MainLoop()
        {
            while (true)
            {
                try
                {
                    CurrentOutputtedMap = RenderMap();
                }
                catch (Exception ex)
                {
                    Log.Warning("Gui exception: " + ex);
                }

                Thread.Sleep(RenderInterval);
            }
        }

        protected abstract Image RenderMap();

        /// <summary>
        /// We need to compensate Y coordinates, game uses from the bottom left, and GUI from the top left
        /// </summary>
        protected float CompensateY(float y)
        {
            var playArea = CurrentGameState.GameInformation.StartRaw.PlayableArea;

            float temp = playArea.P1.Y - y - playArea.P0.Y;
            return temp;
        }
    }
}
