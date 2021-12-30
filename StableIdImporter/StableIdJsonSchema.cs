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
namespace StableIdImporter.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class Rootobject
    {
        public Ability[] Abilities { get; set; }
        public Buff[] Buffs { get; set; }
        public Effect[] Effects { get; set; }
        public Unit[] Units { get; set; }
        public Upgrade[] Upgrades { get; set; }
    }

    public class Ability
    {
        public string buttonname { get; set; }
        public int id { get; set; }
        public int index { get; set; }
        public string name { get; set; }
        public string friendlyname { get; set; }
        public int remapid { get; set; }
    }

    public class Buff
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class Effect
    {
        public int id { get; set; }
        public string name { get; set; }
        public string friendlyname { get; set; }
        public float radius { get; set; }
    }

    public class Unit
    {
        public int id { get; set; }
        public string name { get; set; }
        public string friendlyname { get; set; }
    }

    public class Upgrade
    {
        public int id { get; set; }
        public string name { get; set; }
        public string friendlyname { get; set; }
    }
}
