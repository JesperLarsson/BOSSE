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
    using System.Runtime.CompilerServices;
    using System.Diagnostics;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

#warning TODO: Save static results to file and load it. Use folder ./data

    /// <summary>
    /// Container which holds the reference to the current analysed map
    /// Results can be saved between sessions for performance reasons
    /// </summary>
    public class MapAnalysisWrapper
    {
        public AnalysedStaticMap AnalysedStaticMapRef = null;
        public AnalysedRuntimeMap AnalysedRuntimeMapRef = null;

        public void Initialize()
        {
            // Perform runtime analysis
            this.AnalysedRuntimeMapRef = RuntimeMapAnalyser.AnalyseCurrentMap();

            // Load static analysis
            this.AnalysedStaticMapRef = StaticMapAnalyser.GenerateNewAnalysis();
        }
    }
}
