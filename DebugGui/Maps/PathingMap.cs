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
    /// Pathing minimap, ie which ones can be found and not
    /// </summary>
    public class PathingMap : BaseMap
    {
        const int RenderScale = 2;
        bool renderedOnce = false;

        SolidBrush noPathColor = new SolidBrush(System.Drawing.Color.Black);
        SolidBrush pathColor = new SolidBrush(System.Drawing.Color.White);

        public PathingMap(Graphics _formGraphics, int _baseX, int _baseY) : base(_formGraphics, _baseX, _baseY)
        {
        }

        public void Draw()
        {
            if (renderedOnce)
                return; // Does not change

            RectangleI playArea = BosseGui.GameInformation.StartRaw.PlayableArea;

            // Background - Note that a border around the map is not usable (so we ignore it)
            int bgX = BaseX;
            int bgY = BaseY;
            int bgWidth = playArea.P1.X - playArea.P0.X;
            int bgHeight = playArea.P1.Y - playArea.P0.Y;
            FormGraphics.FillRectangle(noPathColor, bgX, bgY, bgWidth * RenderScale, bgHeight * RenderScale);

            // 1 bit per pixel
            ImageData pathingMap = BosseGui.GameInformation.StartRaw.PathingGrid;
            for (int y = 0; y < pathingMap.Size.Y; y++)
            {
                for (int x = 0; x < (pathingMap.Size.X / 8); x++)
                {
                    byte value = pathingMap.Data[x + (y * pathingMap.Size.X / 8)];
                    if (value == 0)
                        continue;

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

                    DrawPixel(pixel1, xPos + 7, yPos, playArea);
                    DrawPixel(pixel2, xPos + 6, yPos, playArea);
                    DrawPixel(pixel3, xPos + 5, yPos, playArea);
                    DrawPixel(pixel4, xPos + 4, yPos, playArea);
                    DrawPixel(pixel5, xPos + 3, yPos, playArea);
                    DrawPixel(pixel6, xPos + 2, yPos, playArea);
                    DrawPixel(pixel7, xPos + 1, yPos, playArea);
                    DrawPixel(pixel8, xPos + 0, yPos, playArea);
                }
            }

            renderedOnce = true;
        }

        private void DrawPixel(byte pixelValue, int x, int y, RectangleI playArea)
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

            FormGraphics.FillRectangle(pixelBrush, (RenderScale * posX) + BaseX, (RenderScale * poxY) + BaseY, RenderScale, RenderScale);
        }
    }
}
