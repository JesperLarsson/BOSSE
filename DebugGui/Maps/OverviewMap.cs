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
    public class OverviewMap : BaseMap
    {
        const int RenderScale = 8;

        static readonly SolidBrush BackgroundColor = new SolidBrush(System.Drawing.Color.Black);
        static readonly SolidBrush SelfColor = new SolidBrush(System.Drawing.Color.Blue);
        static readonly SolidBrush EnemyColor = new SolidBrush(System.Drawing.Color.Red);
        static readonly SolidBrush MineralColor = new SolidBrush(System.Drawing.Color.White);

        SolidBrush noPathColor = new SolidBrush(System.Drawing.Color.Black);
        SolidBrush pathColor = new SolidBrush(System.Drawing.Color.DarkGray);

        public OverviewMap(Graphics _formGraphics, int _baseX, int _baseY) : base(_formGraphics, _baseX, _baseY)
        {
        }

        public void Tick()
        {
            RectangleI playArea = BosseGui.GameInformation.StartRaw.PlayableArea;

            // Background - Note that a border around the map is not usable (so we ignore it)
            //int bgX = BaseX;
            //int bgY = BaseY;
            //int bgWidth = playArea.P1.X - playArea.P0.X;
            //int bgHeight = playArea.P1.Y - playArea.P0.Y;
            //FormGraphics.FillRectangle(BackgroundColor, bgX, bgY, bgWidth * RenderScale, bgHeight * RenderScale);

            // Pathing overlay - input data contains 1 bit per pixel
            ImageData pathingMap = BosseGui.GameInformation.StartRaw.PathingGrid;
            for (int y = 0; y < pathingMap.Size.Y; y++)
            {
                for (int x = 0; x < (pathingMap.Size.X / 8); x++)
                {
                    byte value = pathingMap.Data[x + (y * pathingMap.Size.X / 8)];
                    //if (value == 0)
                    //    continue;

                    byte pixel1 = (byte)(value & 0x01);
                    byte pixel2 = (byte)(value & 0x02);
                    byte pixel3 = (byte)(value & 0x04);
                    byte pixel4 = (byte)(value & 0x08);
                    byte pixel5 = (byte)(value & 0x10);
                    byte pixel6 = (byte)(value & 0x20);
                    byte pixel7 = (byte)(value & 0x40);
                    byte pixel8 = (byte)(value & 0x80);

                    int xPos = x * 8;
                    int yPos = y;
                    
                    DrawPathingPixel(pixel1, xPos + 7, yPos, playArea);
                    DrawPathingPixel(pixel2, xPos + 6, yPos, playArea);
                    DrawPathingPixel(pixel3, xPos + 5, yPos, playArea);
                    DrawPathingPixel(pixel4, xPos + 4, yPos, playArea);
                    DrawPathingPixel(pixel5, xPos + 3, yPos, playArea);
                    DrawPathingPixel(pixel6, xPos + 2, yPos, playArea);
                    DrawPathingPixel(pixel7, xPos + 1, yPos, playArea);
                    DrawPathingPixel(pixel8, xPos + 0, yPos, playArea);
                }
            }

            // Units, updates every GUI frame
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

        private void DrawPathingPixel(byte pixelValue, int x, int y, RectangleI playArea)
        {
            SolidBrush pixelBrush;
            if (pixelValue == 0)
            {
                pixelBrush = noPathColor;
            }
            else
            {
                pixelBrush = pathColor;
            }

            float posX = x - playArea.P0.X;
            float poxY = y - playArea.P0.Y;

            if (posX > (playArea.P1.X - playArea.P0.X))
                return;
            if (poxY > playArea.P1.Y - playArea.P0.Y)
                return;

            FormGraphics.FillRectangle(pixelBrush, (RenderScale * posX) + BaseX, (RenderScale * poxY) + BaseY, RenderScale, RenderScale);
        }
    }
}
