/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2022 Jesper Larsson

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
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
    /// Provides helper functions for <see cref="ImageData"/> sc2 struct. See sc2-provided bits_per_pixel for which function to use
    /// </summary>
    public static class ImageDataExtensions
    {
        /// <summary>
        /// Returns a specific bit of the given <see cref="ImageData"/>
        /// </summary>
        public static byte GetBit(this ImageData imageObj, int pixelXOffset, int pixelYOffset)
        {
            int pixelOffset = pixelXOffset + pixelYOffset * imageObj.Size.X;
            int byteOffset = pixelOffset / 8;
            int bitOffset = pixelOffset % 8;

            return ((imageObj.Data[byteOffset] & 1 << (7 - bitOffset)) == 0) ? (byte)0 : (byte)1;
        }

        /// <summary>
        /// Returns a specific byte of the given <see cref="ImageData"/>
        /// </summary>
        public static int GetByte(this ImageData imageObj, int byteXOffset, int byteYOffset)
        {
            int byteOffset = byteXOffset + byteYOffset * imageObj.Size.X;
            return imageObj.Data[byteOffset];
        }
    }
}
