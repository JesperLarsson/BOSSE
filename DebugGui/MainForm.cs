using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DebugGui
{
    public partial class MainForm : Form
    {
        private const int RefreshIntervalMs = 5000;

        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Refreshes incoming GUI data from bot
        /// </summary>
        private void UpdateIncomingData(object sender, EventArgs e)
        {
            
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Timer timer = new Timer();
            timer.Interval = RefreshIntervalMs;
            timer.Tick += new EventHandler(UpdateIncomingData);
            timer.Start();
        }
    }
}
