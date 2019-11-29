/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace DebugGui
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Threading;

    public static class BosseGui
    {
        public static void StartGui()
        {
            Thread guiThread = new Thread(GuiEnterLoop);
            guiThread.Start();
        }

        [STAThread]
        private static void GuiEnterLoop()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
