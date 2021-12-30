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
namespace StableIdImporter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Text;

    using Newtonsoft.Json;

    /// <summary>
    /// Quick-and-dirty application to convert the official stableid.json file into a custom enum
    /// This contains the game IDs for all abilities in the game
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            string remoteUrl = @"https://raw.githubusercontent.com/Blizzard/s2client-proto/master/stableid.json";

            HttpClient client = new HttpClient();
            System.Threading.Tasks.Task<HttpResponseMessage> request = client.GetAsync(remoteUrl);

            request.Wait();
            HttpResponseMessage result = request.Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"Error {result.StatusCode}");
            }
            else
            {
                string body = result.Content.ReadAsStringAsync().Result;
                Schema.Rootobject file = JsonConvert.DeserializeObject<Schema.Rootobject>(body);

#warning TODO: Implement unit import which is backwards compatible with current enum sets
                File.WriteAllText("AbilityConstants.cs", BuildAbilitiesEnums(file.Abilities), Encoding.UTF8);
                //File.WriteAllText("UnitConstants.cs", BuildUnitsEnums(file.Units), Encoding.UTF8); // Not working with current enums

                Console.WriteLine("Done, outputs must be copied manually to main project");
            }

            Console.ReadLine();
        }

        private static string BuildUnitsEnums(Schema.Unit[] units)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("public enum UnitId\r\n{\r\n");

            HashSet<string> usedNames = new HashSet<string>();

            foreach (Schema.Unit iter in units)
            {
                string name = iter.name;
                string friendlyname = iter.friendlyname;
                int id = iter.id;

                if (String.IsNullOrWhiteSpace(name))
                    continue;
                if (char.IsDigit(name[0]))
                    name = "_" + name;

                name = name.ToUpperInvariant();

                string finalName = name;
                finalName = finalName.Replace(" ", "_").Trim();
                if (String.IsNullOrWhiteSpace(friendlyname) == false)
                    finalName = friendlyname.Replace(" ", "_").Trim().ToUpperInvariant();

                string fullRow = $"    {finalName} = {id},";
                //if (String.IsNullOrWhiteSpace(friendlyname) == false)
                //    fullRow += $" // {friendlyname}";
                fullRow += "\r\n";

                if (usedNames.Contains(finalName))
                    fullRow = "    // " + fullRow.Trim() + "\r\n";

                usedNames.Add(finalName);
                sb.Append(fullRow);
            }

            sb.Append("}\r\n");
            return sb.ToString();
        }

        private static string BuildAbilitiesEnums(Schema.Ability[] abilities)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("public enum AbilityId\r\n{\r\n");

            HashSet<string> usedNames = new HashSet<string>();

            foreach (Schema.Ability iter in abilities)
            {
                string name = iter.name;
                string buttonname = iter.buttonname;
                string friendlyname = iter.friendlyname;
                int id = iter.id;

                if (String.IsNullOrWhiteSpace(name) || String.IsNullOrWhiteSpace(buttonname))
                    continue;
                if (char.IsDigit(name[0]))
                    name = "_" + name;

                name = name.ToUpperInvariant();
                buttonname = buttonname.ToUpperInvariant();

                string finalName = name;
                if (name != buttonname)
                    finalName += "_" + buttonname;
                finalName = finalName.Replace(" ", "").Trim();

                string fullRow = $"    {finalName} = {id},";
                if (String.IsNullOrWhiteSpace(friendlyname) == false)
                    fullRow += $" // {friendlyname}";
                fullRow += "\r\n";

                if (usedNames.Contains(finalName))
                    fullRow = "    // " + fullRow.Trim() + "\r\n";

                usedNames.Add(finalName);
                sb.Append(fullRow);
            }

            sb.Append("}\r\n");
            return sb.ToString();
        }
    }
}
