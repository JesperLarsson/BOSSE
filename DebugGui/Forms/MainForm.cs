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
            StandardMap.Draw();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Reduce flickering
            this.DoubleBuffered = true;
            
            // Init maps
            FormGraphics = this.CreateGraphics();
            StandardMap.Init(FormGraphics, this.LabelStandardMap.Location.X, this.LabelStandardMap.Location.Y + this.LabelStandardMap.Size.Height);

            // Update maps periodically in GUI thread
            Timer timer = new Timer();
            timer.Interval = RefreshIntervalMs;
            timer.Tick += new EventHandler(UpdateIncomingData);
            timer.Start();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
