/*
    BOSSE - Starcraft 2 Bot
    Copyright (C) 2020 Jesper Larsson

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
