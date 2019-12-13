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
    /// Building placement grid minimap
    /// </summary>
    public class PlacementGridMap : BaseMap
    {
        protected static int xSize;
        protected static int ySize;

        public PlacementGridMap(Graphics _formGraphics, int _baseX, int _baseY, int renderScale) : base(_formGraphics, _baseX, _baseY, renderScale)
        {
        }

        public override void Tick()
        {
            SolidBrush pixelBrush;
            RectangleI playArea = BosseGui.GameInformation.StartRaw.PlayableArea;
            ImageData gridMap = BosseGui.GameInformation.StartRaw.PlacementGrid;
            //for (int y = 0; y < gridMap.Size.Y; y++)
            //{
            //    for (int x = 0; x < gridMap.Size.X; x++)
            //    {
            //        byte heightValue = gridMap.Data[x + (y * gridMap.Size.X)];
            //        if (heightValue == 0)
            //            continue;

            //        pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, heightValue, heightValue, heightValue));

            //        float posX = x - playArea.P0.X;
            //        float posY = CompensateY(y - playArea.P0.Y);

            //        FormGraphics.FillRectangle(pixelBrush, (RenderScale * posX) + BaseX, (RenderScale * posY) + BaseY, RenderScale, RenderScale);
            //    }
            //}
        }
    }
}
