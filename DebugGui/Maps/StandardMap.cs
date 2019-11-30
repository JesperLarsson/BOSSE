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

    using BOSSE;
    using SC2APIProtocol;

    /// <summary>
    /// Standard minimap, only with the real coordinates as seen through the API
    /// </summary>
    public class StandardMap : BaseMap
    {
        const int RenderScale = 4;

        static readonly SolidBrush BackgroundColor = new SolidBrush(System.Drawing.Color.Black);
        static readonly SolidBrush SelfColor = new SolidBrush(System.Drawing.Color.Blue);
        static readonly SolidBrush EnemyColor = new SolidBrush(System.Drawing.Color.Red);
        static readonly SolidBrush MineralColor = new SolidBrush(System.Drawing.Color.White);

        public StandardMap(Graphics _formGraphics, int _baseX, int _baseY) : base(_formGraphics, _baseX, _baseY)
        {
        }

        public void Draw()
        {
            RectangleI playArea = BosseGui.GameInformation.StartRaw.PlayableArea;

            // Background - Note that a border around the map is not usable (so we ignore it)
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
                else if (UnitConstants.MineralFields.Contains((UnitConstants.UnitId)unitIter.UnitType))
                {
                    unitBrush = MineralColor;
                }
                else
                {
                    continue; // ignore
                }

                float x = unitIter.Pos.X - playArea.P0.X;
                float y = unitIter.Pos.Y - playArea.P0.Y;

                FormGraphics.FillRectangle(unitBrush, (RenderScale * x) + BaseX, (RenderScale * y) + BaseY, RenderScale, RenderScale);
            }
        }
    }
}
