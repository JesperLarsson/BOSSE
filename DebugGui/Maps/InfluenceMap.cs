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
                        float dissipationRate = 10.0f;
                        if (unitIter.Alliance != Alliance.Self && unitIter.Alliance != Alliance.Enemy)
                        {
                            continue;
                        }

                        Vector2 tilePos = new Vector2(x, y);
                        Vector2 unitPos = new Vector2(unitIter.Pos.X - playArea.P0.X, unitIter.Pos.Y - playArea.P0.Y);
                        float distanceToUnit = Vector2.Distance(tilePos, unitPos);

                        float fallOffValue = 1 - (dissipationRate / (dissipationRate - distanceToUnit));
                        float influenceContribution = standardInfluence * Math.Max(0, fallOffValue);

                        if (unitIter.Alliance == Alliance.Enemy)
                        {
                            influenceContribution = -influenceContribution;
                        }

                        buffer[x, y] += influenceContribution;
                    }
                }
            }

            // Draw it
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    int value = (int)(buffer[x, y] * 10.0f);
                    SolidBrush brushColor;

                    if (value >= 0)
                    {
                        brushColor = new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0, Math.Min(value, 255)));
                    }
                    else
                    {
                        brushColor = new SolidBrush(System.Drawing.Color.FromArgb(255, Math.Min(-value, 255), 0, 0));
                    }

                    float xPos = x;
                    float yPos = y;

                    FormGraphics.FillRectangle(brushColor, (RenderScale * xPos) + BaseX, (RenderScale * yPos) + BaseY, RenderScale, RenderScale);
                }
            }
        }
    }
}
