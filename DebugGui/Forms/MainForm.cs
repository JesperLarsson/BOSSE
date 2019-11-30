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
        private const int RefreshIntervalMs = 1000;
        private Graphics FormGraphics;

        private StandardMap StandardMapRef;
        private TerrainMap TerraindMapRef;
        private PathingMap PathingMapRef;
        private InfluenceMap InfluenceMapRef;        

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
            PathingMapRef.Tick();
            InfluenceMapRef.Tick();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Reduce flickering
            this.DoubleBuffered = true;
            
            // Init maps
            FormGraphics = this.CreateGraphics();
            StandardMapRef = new StandardMap(FormGraphics, this.LabelStandardMap.Location.X, this.LabelStandardMap.Location.Y + this.LabelStandardMap.Size.Height);
            TerraindMapRef = new TerrainMap(FormGraphics, this.LabelTerrainMap.Location.X, this.LabelTerrainMap.Location.Y + this.LabelTerrainMap.Size.Height);
            PathingMapRef = new PathingMap(FormGraphics, this.LabelPathMap.Location.X, this.LabelPathMap.Location.Y + this.LabelPathMap.Size.Height);
            InfluenceMapRef = new InfluenceMap(FormGraphics, this.LabelInfluenceMap.Location.X, this.LabelInfluenceMap.Location.Y + this.LabelInfluenceMap.Size.Height);

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
