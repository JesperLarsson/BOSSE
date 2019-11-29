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

            DrawTestMap();
        }

        private void DrawTestMap()
        {
            const int scale = 2; // ex 2:1

            System.Drawing.SolidBrush backgroundBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
            System.Drawing.Graphics formGraphics;
            formGraphics = this.CreateGraphics();
            int baseX = this.LabelTestMap.Location.X;
            int baseY = this.LabelTestMap.Location.Y + this.LabelTestMap.Height + 10;

            // Background
            Size2DI size = BosseGui.GameInformation.StartRaw.MapSize;
            formGraphics.FillRectangle(backgroundBrush, baseX, baseY, size.X * scale, size.Y * scale);

            // Units
            foreach (Unit unitIter in BosseGui.ObservationState.Observation.RawData.Units)
            {
                System.Drawing.SolidBrush unitBrush;
                if (unitIter.Alliance == Alliance.Self)
                {
                    unitBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Blue);
                }
                else if (unitIter.Alliance == Alliance.Ally)
                {
                    unitBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Green);
                }
                else if (unitIter.Alliance == Alliance.Enemy)
                {
                    unitBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
                }
                else
                {
                    continue; // ignore neutral etc
                }

                float x = unitIter.Pos.X;
                float y = unitIter.Pos.Y;

                formGraphics.FillRectangle(unitBrush, (scale * x) + baseX, (scale * y) + baseY, scale, scale);
            }

            backgroundBrush.Dispose();
            formGraphics.Dispose();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;

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
