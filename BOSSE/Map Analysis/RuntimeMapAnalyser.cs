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
    /// Performs runtime map analysis
    /// </summary>
    public static class RuntimeMapAnalyser
    {
        public static AnalysedRuntimeMap AnalyseCurrentMap()
        {
            var resourceClusters = FindBaseLocations();

            AnalysedRuntimeMap completedMap = new AnalysedRuntimeMap(
                allClusters: resourceClusters,
                mainBase: null,
                naturalExpansion: null,
                thirdExpansion: null,
                enemyMainBase: null,
                enemyNaturalExpansion: null,
                enemyThirdExpansion: null
                );
            return completedMap;
        }

        private static Dictionary<int, ResourceCluster> FindBaseLocations()
        {
            Point2D baseLoc = Globals.MainBaseLocation;

            var temp = GetUnits(UnitConstants.MineralFields, alliance: Alliance.Neutral);
            Log.Debug("Our base = " + baseLoc.ToString2());
            foreach (var iter in temp)
            {
                double dist = iter.Position.DistanceAbsolute(baseLoc);
                Log.Debug("Geyser at " + iter.Position.ToString2() + " distSq=" + dist);
            }

            //CurrentGameState.GameInformation.StartRaw.StartLocations;
            return null;
        }
    }
}
