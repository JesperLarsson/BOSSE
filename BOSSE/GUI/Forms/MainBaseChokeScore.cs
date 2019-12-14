/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
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

    public partial class MainBaseChokeScore : Form
    {
        private const int RefreshIntervalMs = 5000;
        const int RenderScale = 8;

        public MainBaseChokeScore()
        {
            InitializeComponent();
        }

        private void MainBaseChokeScore_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;

            // Update periodically
            Timer timer = new Timer();
            timer.Interval = RefreshIntervalMs;
            timer.Tick += new EventHandler(UpdateMap);
            timer.Start();
            UpdateMap(null, null);
        }

        private void UpdateMap(object sender, EventArgs e)
        {
            // Wait for initialization
            while (!BOSSE.BOSSE.HasCompletedFirstFrameInit)
            {
                System.Threading.Thread.Sleep(1000);
            }

            var formGraphics = this.CreateGraphics();
            BOSSE.TileMap<byte> map = BOSSE.BOSSE.MapAnalysisHandlerRef.Map.MainBaseChokeScore;
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    byte value = map.GetTile(x, y);
                    int sqValue = value * value;
                    if (sqValue > 255)
                    {
                        value = 255;
                    }
                    else
                    {
                        value = (byte)sqValue;
                    }

                    var pixelBrush = new SolidBrush(Color.FromArgb(255, value, 0, 0));
                    int yPos = map.Height - y;

                    formGraphics.FillRectangle(pixelBrush, (RenderScale * x), (RenderScale * yPos), RenderScale, RenderScale);
                }
            }

            var font = new Font("Arial", 12);
            formGraphics.DrawString("Slask plask task", font, Brushes.Black, new PointF(10, 10));

        }
    }
}
