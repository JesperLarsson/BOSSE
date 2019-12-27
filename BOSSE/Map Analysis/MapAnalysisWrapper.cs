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
    using System.Runtime.CompilerServices;
    using System.Diagnostics;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;
    using System.Runtime.Serialization;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

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
            // Perform runtime analysis on each startup
            Log.Info("Performing runtime map analysis");
            this.AnalysedRuntimeMapRef = RuntimeMapAnalyser.AnalyseCurrentMap();

            // Load static analysis if available
            if (!LoadStaticAnalysisFromFile())
            {
                CreateMapFolder();

                Log.Info("Generating new map analysis (this will take multiple hours)...");
                this.AnalysedStaticMapRef = StaticMapAnalyser.GenerateNewAnalysis();

                Log.Info("Map analysis generated, saving to file");
                SaveStaticAnalysisToFile();
            }
        }

        private void SaveStaticAnalysisToFile()
        {
            string filePath = GetMapFilePath();
            FileStream fs = new FileStream(filePath, FileMode.Create);

            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(fs, AnalysedStaticMapRef);
            }
            catch (SerializationException ex)
            {
                Log.SanityCheckFailed("Unable to save map data: " + ex);
            }
            finally
            {
                fs.Close();
            }
        }

        private bool LoadStaticAnalysisFromFile()
        {
            string filePath = GetMapFilePath();
            if (!File.Exists(filePath))
                return false;

            FileStream fs = new FileStream(filePath, FileMode.Open);
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                AnalysedStaticMapRef = (AnalysedStaticMap)formatter.Deserialize(fs);
            }
            catch (SerializationException)
            {
                Log.Warning("Error reading map data from file (likely outdated)");
                return false;
            }
            finally
            {
                fs.Close();
            }

            // Validate version
            if (AnalysedStaticMapRef.FileFormatVersion != AnalysedStaticMap.LatestFileFormatVersion)
            {
                Log.Warning("Saved map data is outdated (version " + AnalysedStaticMapRef.FileFormatVersion + ", expected " + AnalysedStaticMap.LatestFileFormatVersion + ")");
                return false;
            }

            Log.Info("Loaded static map data for current map (" + CurrentGameState.GameInformation.MapName + ")");
            return true;
        }

        private string GetMapFilePath()
        {
            const string mapFolder = "StaticMapData";

            string mapName = CurrentGameState.GameInformation.MapName;
            mapName = Path.Combine(mapFolder, mapName);
            mapName = Path.ChangeExtension(mapName, ".bossemap");

            return mapName;
        }

        /// <summary>
        /// Creates map folder if it doesn't exist
        /// </summary>
        private void CreateMapFolder()
        {
            FileInfo file = new FileInfo(GetMapFilePath());
            file.Directory.Create();
        }
    }
}
