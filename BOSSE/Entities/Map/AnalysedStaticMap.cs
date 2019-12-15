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
    using Google.Protobuf.Collections;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GeneralGameUtility;

    /// <summary>
    /// Contains various static metrics about the map that doesn't change between runs (chokepoints etc)
    /// </summary>
    public class AnalysedStaticMap
    {
        /// <summary>
        /// Higher values indicate chokepoints between ours and the enemy main base
        /// </summary>
        public TileMap<byte> MainBaseChokeScore;
    }
}
