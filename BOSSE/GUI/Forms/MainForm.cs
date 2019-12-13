/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
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
            // Reduce flickering
            this.DoubleBuffered = true;

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
