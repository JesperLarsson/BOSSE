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
    /// Map data that is calculated during runtime
    /// </summary>
    public class AnalysedRuntimeMap
    {
        /// <summary>
        /// All bases on the map. Base id => instance mapping
        /// </summary>
        public Dictionary<int, BaseLocation> BaseLocations = new Dictionary<int, BaseLocation>();

        public BaseLocation MainBase;
        public BaseLocation NaturalExpansion;
        public BaseLocation ThirdExpansion;

        public BaseLocation EnemyMainBase;
        public BaseLocation EnemyNaturalExpansion;
        public BaseLocation EnemyThirdExpansion;

        public AnalysedRuntimeMap(Dictionary<int, BaseLocation> baseLocations, BaseLocation mainBase, BaseLocation naturalExpansion, BaseLocation thirdExpansion, BaseLocation enemyMainBase, BaseLocation enemyNaturalExpansion, BaseLocation enemyThirdExpansion)
        {
            BaseLocations = baseLocations;
            MainBase = mainBase;
            NaturalExpansion = naturalExpansion;
            ThirdExpansion = thirdExpansion;
            EnemyMainBase = enemyMainBase;
            EnemyNaturalExpansion = enemyNaturalExpansion;
            EnemyThirdExpansion = enemyThirdExpansion;
        }
    }
}
