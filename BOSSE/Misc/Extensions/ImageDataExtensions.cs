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
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// Provides helper functions for <see cref="ImageData"/> sc2 struct
    /// </summary>
    public static class ImageDataExtensions
    {
        /// <summary>
        /// Returns a specific bit of the given image
        /// </summary>
        public static byte GetBit(this ImageData imageObj, int pixelXOffset, int pixelYOffset)
        {
            int pixelOffset = pixelXOffset + pixelYOffset * imageObj.Size.X;
            int byteOffset = pixelOffset / 8;
            int bitOffset = pixelOffset % 8;

            return ((imageObj.Data[byteOffset] & 1 << (7 - bitOffset)) == 0) ? (byte)0 : (byte)1;
        }

        /// <summary>
        /// Returns a specific byte of the given image
        /// </summary>
        public static int GetByte(this ImageData imageObj, int byteXOffset, int byteYOffset)
        {
            int byteOffset = byteXOffset + byteYOffset * imageObj.Size.X;
            return imageObj.Data[byteOffset];
        }
    }
}
