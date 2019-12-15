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
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Threading;

    using SC2APIProtocol;

    public static class BosseGui
    {
        ///// <summary>
        ///// Set once during initialization
        ///// </summary>
        //public static ResponseGameInfo GameInformation;

        ///// <summary>
        ///// Set once during initialization
        ///// </summary>
        //public static ResponseData GameData;

        ///// <summary>
        ///// Set each frame
        ///// </summary>
        //public static ResponseObservation ObservationState;

        private static Thread ThreadMainForm;

        public static void StartGui()
        {
#if !DEBUG
            return;
#endif

            ThreadMainForm = new Thread(MainFormMain);
            ThreadMainForm.Start();
        }

        [STAThread]
        private static void MainFormMain()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}