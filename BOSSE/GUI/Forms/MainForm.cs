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
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using BOSSE;
    using SC2APIProtocol;

    public partial class MainForm : Form
    {
        private const int MainformRefreshIntervalMs = 100;

        /// <summary>
        /// List of all map instances
        /// </summary>
        private readonly List<BaseDebugMap> Maps = new List<BaseDebugMap>()
        {
            new OverviewDebugMap(),
            new TerrainDebugMap(),
            new PlacementGridDebugMap(),
            new InfluenceDebugMap(),

        };

        public MainForm()
        {
            InitializeComponent();
        }

        private void UpdateMainForm(object sender, EventArgs e)
        {
            int index = DropdownMapChoice.SelectedIndex;
            this.PictureMain.Image = Maps[index].GetMap();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            while (!BOSSE.HasCompletedFirstFrameInit)
                System.Threading.Thread.Sleep(10); // Wait for bot init

            this.DoubleBuffered = true;

            // Add maps to dropdown and start their respective threads
            foreach (BaseDebugMap iter in Maps)
            {
                DropdownMapChoice.Items.Add(iter.MapName);
                iter.Start();
            }
            DropdownMapChoice.SelectedIndex = 0;

            // Update maps periodically in GUI thread
            Timer timer = new Timer();
            timer.Interval = MainformRefreshIntervalMs;
            timer.Tick += new EventHandler(UpdateMainForm);
            timer.Start();
            UpdateMainForm(null, null);
        }
    }
}
