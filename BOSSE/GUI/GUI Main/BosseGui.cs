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

    using SC2APIProtocol;

    public static class BosseGui
    {
        /// <summary>
        /// Set once during initialization
        /// </summary>
        public static ResponseGameInfo GameInformation;

        /// <summary>
        /// Set once during initialization
        /// </summary>
        public static ResponseData GameData;

        /// <summary>
        /// Set each frame
        /// </summary>
        public static ResponseObservation ObservationState;

        public static void StartGui()
        {
#if !DEBUG
            return;
#endif

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