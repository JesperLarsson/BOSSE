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
            // General
            new TerrainDebugMap(),
            new OverviewDebugMap(),            
            new PlacementGridDebugMap(),

            // Strategic
            new InfluenceDebugMap(),
            new VulnerabilityDebugMap(),
            new TensionDebugMap(),

            // Map analysis data
            //new GeneralChokepointDebugMap(),
            new ResourceClusterDebugMap(),

            // Chokepoints are appended during runtime
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

        protected override bool ShowWithoutActivation
        {
            // Prevents form from taking focus when starting. From: https://stackoverflow.com/a/157843/645155
            get { return true; }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            while (!BOSSE.HasCompletedFirstFrameInit)
                System.Threading.Thread.Sleep(10); // Wait for bot init

            // reduce flickering
            this.DoubleBuffered = true;

            // Add chopkepoint maps
            this.Maps.Add(new ChokepointDebugMap("Main bases", BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.MainBase, BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.EnemyMainBase));
            this.Maps.Add(new ChokepointDebugMap("Natural to enemy", BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.NaturalExpansion, BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.EnemyMainBase));
            this.Maps.Add(new ChokepointDebugMap("Third to enemy", BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.ThirdExpansion, BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.EnemyMainBase));

            // Add maps to dropdown and start their respective threads
            foreach (BaseDebugMap iter in Maps)
            {
                DropdownMapChoice.Items.Add(iter.MapName);
                iter.Start();
            }
            DropdownMapChoice.SelectedIndex = 0;

            // Update maps periodically
            Timer timer = new Timer();
            timer.Interval = MainformRefreshIntervalMs;
            timer.Tick += new EventHandler(UpdateMainForm);
            timer.Start();
            UpdateMainForm(null, null);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
