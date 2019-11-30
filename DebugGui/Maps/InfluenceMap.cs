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
    /// Influence minimap
    /// </summary>
    public class InfluenceMap : BaseMap
    {
        const int RenderScale = 2;

        public InfluenceMap(Graphics _formGraphics, int _baseX, int _baseY) : base(_formGraphics, _baseX, _baseY)
        {
        }

        public void Tick()
        {
            RectangleI playArea = BosseGui.GameInformation.StartRaw.PlayableArea;
            const int gridSize = 4;

            int xSize = playArea.P1.X - playArea.P0.X;
            int ySize = playArea.P1.Y - playArea.P0.Y;
            float[,] buffer = new float[xSize, ySize];

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    foreach (Unit unitIter in BosseGui.ObservationState.Observation.RawData.Units)
                    {
                        float standardInfluence = 1.0f;
                        float dissipationRate = 0.5f;
                        if (unitIter.Alliance == Alliance.Self)
                        {
                        }
                        //else if (unitIter.Alliance == Alliance.Enemy)
                        //{
                        //}
                        else
                        {
                            continue; // ignore
                        }

                        Vector2 tilePos = new Vector2(x, y);
                        Vector2 unitPos = new Vector2(unitIter.Pos.X, unitIter.Pos.Y);
                        float distanceToUnit = Vector2.Distance(tilePos, unitPos);

                        float fallOffValue = 1 - (dissipationRate / (dissipationRate - distanceToUnit));
                        float influenceContribution = standardInfluence * Math.Max(0, fallOffValue);

                        buffer[x, y] += influenceContribution;
                    }
                }
            }

            // Draw it
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    int value = (int)buffer[x, y] * 10;
                    SolidBrush unitBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, Math.Min(value, 255), Math.Min(value, 255), Math.Min(value, 255)));

                    float xPos = x;
                    float yPos = y;

                    FormGraphics.FillRectangle(unitBrush, (RenderScale * xPos) + BaseX, (RenderScale * yPos) + BaseY, RenderScale, RenderScale);
                }
            }
        }
    }
}
