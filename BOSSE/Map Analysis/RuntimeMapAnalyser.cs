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
    /// Performs runtime map analysis
    /// </summary>
    public static class RuntimeMapAnalyser
    {
        public static AnalysedRuntimeMap AnalyseCurrentMap()
        {
            var baseLocations = FindBaseLocations();


            AnalysedRuntimeMap completedMap = new AnalysedRuntimeMap(
                baseLocations: baseLocations,
                mainBase: null,
                naturalExpansion: null,
                thirdExpansion: null,
                enemyMainBase: null,
                enemyNaturalExpansion: null,
                enemyThirdExpansion: null
                );
            return completedMap;
        }

        private static Dictionary<int, BaseLocation> FindBaseLocations()
        {




        }
    }
}
