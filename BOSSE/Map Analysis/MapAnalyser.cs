/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    //public class AnalysedMap
    //{
    //    public ulong[,]

    //}

    /// <summary>
    /// Parses the sc2 map, finds chokepoints, etc
    /// </summary>
    public class MapAnalyser
    {
            //        public AnalysedMap CurrentMap = null;

            //        public void Initialize()
            //        {
            //#warning TODO: Load from file
            //            this.CurrentMap = AnalyseCurrentMap();
            //        }

            //    private AnalysedMap AnalyseCurrentMap()
            //    {
            //        // Pathing overlay - input data contains 1 bit per pixel
            //        ImageData pathingMap = CurrentGameState.GameInformation.StartRaw.PathingGrid;
            //        for (int y = 0; y < pathingMap.Size.Y; y++)
            //        {
            //            for (int x = 0; x < (pathingMap.Size.X / 8); x++)
            //            {
            //                byte value = pathingMap.Data[x + (y * pathingMap.Size.X / 8)];
            //                //if (value == 0)
            //                //    continue;

            //                byte pixel1 = (byte)(value & 0x01);
            //                byte pixel2 = (byte)(value & 0x02);
            //                byte pixel3 = (byte)(value & 0x04);
            //                byte pixel4 = (byte)(value & 0x08);
            //                byte pixel5 = (byte)(value & 0x10);
            //                byte pixel6 = (byte)(value & 0x20);
            //                byte pixel7 = (byte)(value & 0x40);
            //                byte pixel8 = (byte)(value & 0x80);

            //                int xPos = x * 8;
            //                int yPos = y;

            //                DrawPathingPixel(pixel1, xPos + 7, yPos, playArea);
            //                DrawPathingPixel(pixel2, xPos + 6, yPos, playArea);
            //                DrawPathingPixel(pixel3, xPos + 5, yPos, playArea);
            //                DrawPathingPixel(pixel4, xPos + 4, yPos, playArea);
            //                DrawPathingPixel(pixel5, xPos + 3, yPos, playArea);
            //                DrawPathingPixel(pixel6, xPos + 2, yPos, playArea);
            //                DrawPathingPixel(pixel7, xPos + 1, yPos, playArea);
            //                DrawPathingPixel(pixel8, xPos + 0, yPos, playArea);
            //            }
            //        }


            //        return null;
            //    }
        }
}
