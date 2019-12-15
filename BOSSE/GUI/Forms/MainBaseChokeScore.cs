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
            BOSSE.TileMap<byte> map = BOSSE.BOSSE.MapAnalysisRef.AnalysedStaticMapRef.MainBaseChokeScore;
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
        }
    }
}
