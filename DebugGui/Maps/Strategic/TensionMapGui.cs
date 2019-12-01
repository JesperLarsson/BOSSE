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
    using System.Numerics;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using BOSSE;
    using SC2APIProtocol;

    /// <summary>
    /// Tension minimap
    /// </summary>
    public class TensionMapGui : BaseMap
    {
        const int RenderScale = 2;

        protected static float[,] TensionMapInput = null;
        protected static int xSize;
        protected static int ySize;

        public TensionMapGui(Graphics _formGraphics, int _baseX, int _baseY) : base(_formGraphics, _baseX, _baseY)
        {
        }

        public static void NewTensionMapIsAvailable(float[,] _influenceMapInput, int _xSize, int _ySize)
        {
            xSize = _xSize;
            ySize = _ySize;
            TensionMapInput = _influenceMapInput;
        }

        public void Tick()
        {
            if (TensionMapInput == null)
                return;

            // Draw it
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    int value = (int)(TensionMapInput[x, y] * 10.0f);
                    SolidBrush brushColor;

                    brushColor = new SolidBrush(System.Drawing.Color.FromArgb(255, Math.Min(value, 255), Math.Min(value, 255), Math.Min(value, 255)));

                    float xPos = x;
                    float yPos = CompensateY(y);

                    FormGraphics.FillRectangle(brushColor, (RenderScale * xPos) + BaseX, (RenderScale * yPos) + BaseY, RenderScale, RenderScale);
                }
            }

            TensionMapInput = null; // do not redraw the same data again
        }
    }
}
