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

    /// <summary>
    /// Standard minimap, only with the real coordinates as seen through the API
    /// </summary>
    public static class StandardMap
    {
        private static Graphics FormGraphics;
        private static int BaseX;
        private static int BaseY;

        const int RenderScale = 2;
        static readonly System.Drawing.SolidBrush BackgroundColor = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
        static readonly System.Drawing.SolidBrush SelfColor = new System.Drawing.SolidBrush(System.Drawing.Color.Blue);
        static readonly System.Drawing.SolidBrush EnemyColor = new System.Drawing.SolidBrush(System.Drawing.Color.Red);

        public static void Init(Graphics _formGraphics, int _baseX, int _baseY)
        {
            FormGraphics = _formGraphics;
            BaseX = _baseX;
            BaseY = _baseY + 10;
        }

        public static void Draw()
        {
            RectangleI playArea = BosseGui.GameInformation.StartRaw.PlayableArea;

            // Background, note that a border around the map is not usable (so we ignore it)
            int bgX = BaseX;
            int bgY = BaseY;
            int bgWidth = playArea.P1.X - playArea.P0.X;
            int bgHeight = playArea.P1.Y - playArea.P0.Y;
            FormGraphics.FillRectangle(BackgroundColor, bgX, bgY, bgWidth * RenderScale, bgHeight * RenderScale);

            // Units
            foreach (Unit unitIter in BosseGui.ObservationState.Observation.RawData.Units)
            {
                SolidBrush unitBrush;
                if (unitIter.Alliance == Alliance.Self)
                {
                    unitBrush = SelfColor;
                }
                else if (unitIter.Alliance == Alliance.Enemy)
                {
                    unitBrush = EnemyColor;
                }
                else
                {
                    continue; // ignore neutral etc
                }

                float x = unitIter.Pos.X - playArea.P0.X;
                float y = unitIter.Pos.Y - playArea.P0.Y;

                FormGraphics.FillRectangle(unitBrush, (RenderScale * x) + BaseX, (RenderScale * y) + BaseY, RenderScale, RenderScale);
            }
        }
    }
}
