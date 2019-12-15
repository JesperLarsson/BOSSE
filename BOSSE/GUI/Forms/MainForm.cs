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
    using SC2APIProtocol;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public partial class MainForm : Form
    {
        const int standardScale = 2;
        const int bigScale = 8;
        private const int RefreshIntervalMs = 1000;
        private Graphics FormGraphics;

        private BaseMap BigMapRef;

        private OverviewMap StandardMapRef;
        private TerrainMap TerraindMapRef;
        private InfluenceMapGui InfluenceMapRef;
        private TensionMapGui TensionMapRef;
        private VulnerabilityMapGui VulnerabilityMapRef;
        //private PlacementGridMap PlacementGridMapRef;

        MainBaseChokeScore chokeForm = new MainBaseChokeScore();

        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Refreshes incoming GUI data from bot
        /// </summary>
        private void UpdateIncomingData(object sender, EventArgs e)
        {
            if (BosseGui.GameInformation == null)
                return; // Wait for data
            if (BosseGui.GameData == null)
                return; // Wait for data
            if (BosseGui.ObservationState == null)
                return; // Wait for data

            // Draw maps
            StandardMapRef.Tick();
            TerraindMapRef.Tick();
            InfluenceMapRef.Tick();
            TensionMapRef.Tick();
            VulnerabilityMapRef.Tick();
            //PlacementGridMapRef.Tick();

            BigMapRef.Tick();

            Application.DoEvents();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
            this.WindowState = FormWindowState.Maximized;

            // Init maps
            FormGraphics = this.CreateGraphics();
            StandardMapRef = new OverviewMap(FormGraphics, this.LabelStandardMap.Location.X, this.LabelStandardMap.Location.Y + this.LabelStandardMap.Size.Height, standardScale);
            TerraindMapRef = new TerrainMap(FormGraphics, this.LabelTerrainMap.Location.X, this.LabelTerrainMap.Location.Y + this.LabelTerrainMap.Size.Height, standardScale);
            InfluenceMapRef = new InfluenceMapGui(FormGraphics, this.LabelInfluenceMap.Location.X, this.LabelInfluenceMap.Location.Y + this.LabelInfluenceMap.Size.Height, standardScale);
            TensionMapRef = new TensionMapGui(FormGraphics, this.LabelTensionMap.Location.X, this.LabelTensionMap.Location.Y + this.LabelTensionMap.Size.Height, standardScale);
            VulnerabilityMapRef = new VulnerabilityMapGui(FormGraphics, this.LabelVulnerabilityMap.Location.X, this.LabelVulnerabilityMap.Location.Y + this.LabelVulnerabilityMap.Size.Height, standardScale);
            //PlacementGridMapRef = new PlacementGridMap(FormGraphics, this.LabelPlacementGrid.Location.X, this.LabelPlacementGrid.Location.Y + this.LabelPlacementGrid.Size.Height, standardScale);            

            BigMapRef = new OverviewMap(FormGraphics, 0, 0, bigScale);

            // Update maps periodically in GUI thread
            Timer timer = new Timer();
            timer.Interval = RefreshIntervalMs;
            timer.Tick += new EventHandler(UpdateIncomingData);
            timer.Start();
            UpdateIncomingData(null, null);

            // Show other forms
            chokeForm.Show(this);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void LabelTerrainMap_Click(object sender, EventArgs e)
        {
            BigMapRef = new TerrainMap(FormGraphics, 0, 0, bigScale);
        }

        private void LabelStandardMap_Click(object sender, EventArgs e)
        {
            BigMapRef = new OverviewMap(FormGraphics, 0, 0, bigScale);
        }

        private void LabelInfluenceMap_Click(object sender, EventArgs e)
        {
            BigMapRef = new InfluenceMapGui(FormGraphics, 0, 0, bigScale);
        }

        private void LabelTensionMap_Click(object sender, EventArgs e)
        {
            BigMapRef = new TensionMapGui(FormGraphics, 0, 0, bigScale);
        }

        private void LabelVulnerabilityMap_Click(object sender, EventArgs e)
        {
            BigMapRef = new VulnerabilityMapGui(FormGraphics, 0, 0, bigScale);
        }
    }
}
