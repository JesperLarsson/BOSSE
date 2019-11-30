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
        private const int RefreshIntervalMs = 5000;
        private Graphics FormGraphics;

        private OverviewMap StandardMapRef;
        private TerrainMap TerraindMapRef;
        private InfluenceMapGui InfluenceMapRef;
        private TensionMapGui TensionMapRef;
        private VulnerabilityMapGui VulnerabilityMapRef;        

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

            Application.DoEvents();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Reduce flickering
            this.DoubleBuffered = true;

            // Init maps
            FormGraphics = this.CreateGraphics();
            StandardMapRef = new OverviewMap(FormGraphics, this.LabelStandardMap.Location.X, this.LabelStandardMap.Location.Y + this.LabelStandardMap.Size.Height);
            TerraindMapRef = new TerrainMap(FormGraphics, this.LabelTerrainMap.Location.X, this.LabelTerrainMap.Location.Y + this.LabelTerrainMap.Size.Height);
            InfluenceMapRef = new InfluenceMapGui(FormGraphics, this.LabelInfluenceMap.Location.X, this.LabelInfluenceMap.Location.Y + this.LabelInfluenceMap.Size.Height);
            TensionMapRef = new TensionMapGui(FormGraphics, this.LabelTensionMap.Location.X, this.LabelTensionMap.Location.Y + this.LabelTensionMap.Size.Height);
            VulnerabilityMapRef = new VulnerabilityMapGui(FormGraphics, this.LabelVulnerabilityMap.Location.X, this.LabelVulnerabilityMap.Location.Y + this.LabelVulnerabilityMap.Size.Height);

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
    }
}
