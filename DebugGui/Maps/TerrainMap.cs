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
    /// Terrain minimap
    /// </summary>
    public class TerrainMap : BaseMap
    {
        const int RenderScale = 2;
        bool renderedOnce = false;

        SolidBrush pixelBrush;

        public TerrainMap(Graphics _formGraphics, int _baseX, int _baseY) : base(_formGraphics, _baseX, _baseY)
        {
        }

        public void Draw()
        {
            if (renderedOnce)
                return; // Does not change

            RectangleI playArea = BosseGui.GameInformation.StartRaw.PlayableArea;

            // Terrain height
            ImageData terrainMap = BosseGui.GameInformation.StartRaw.TerrainHeight;
            for (int y = 0; y < terrainMap.Size.Y; y++)
            {
                for (int x = 0; x < terrainMap.Size.X; x++)
                {
                    byte heightValue = terrainMap.Data[x + (y * terrainMap.Size.X)];
                    if (heightValue == 0)
                        continue;

                    pixelBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, heightValue, heightValue, heightValue));

                    float posX = x - playArea.P0.X;
                    float poxY = y - playArea.P0.Y;

                    FormGraphics.FillRectangle(pixelBrush, (RenderScale * posX) + BaseX, (RenderScale * poxY) + BaseY, RenderScale, RenderScale);
                }
            }

            renderedOnce = true;
        }
    }
}
