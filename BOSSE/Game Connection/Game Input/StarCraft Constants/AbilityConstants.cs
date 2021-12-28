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

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;

    /// <summary>
    /// Constant values used to identify specific unit abilities
    /// Latest values can be found in sc2 directory as stableid.json, ex: C:\Users\Username\Documents\StarCraft II
    /// </summary>
    public static class AbilityConstants
    {
        public enum AbilityId
        {
            RESEARCH_BANSHEE_CLOAK = 790,
            RESEARCH_INFERNAL_PREIGNITER = 761,
            RESEARCH_UPGRADE_MECH_AIR = 3699,
            RESEARCH_UPGRADE_MECH_ARMOR = 3700,
            RESEARCH_UPGRADE_MECH_GROUND = 3701,

            UPGRADE_TO_ORBITAL = 1516,

            CANCEL_CONSTRUCTION = 314,
            CANCEL = 3659,
            CANCEL_LAST = 3671,
            LIFT = 3679,
            LAND = 3678,

            SMART = 1,
            STOP = 4,
            ATTACK = 23,
            MOVE = 16,
            PATROL = 17,
            RALLY = 3673,
            REPAIR = 316,

            THOR_SWITCH_AP = 2362,
            THOR_SWITCH_NORMAL = 2364,
            SCANNER_SWEEP = 399,
            YAMATO = 401,
            CALL_DOWN_MULE = 171,
            CLOAK = 3676,
            REAPER_GRENADE = 2588,
            DEPOT_RAISE = 558,
            DEPOT_LOWER = 556,
            SIEGE_TANK = 388,
            UNSIEGE_TANK = 390,
            TRANSFORM_TO_HELLBAT = 1998,
            TRANSFORM_TO_HELLION = 1978,
            UNLOAD_BUNKER = 408,
            SALVAGE_BUNKER = 32,

            GATHER_RESOURCES = 295,
            RETURN_RESOURCES = 296,

            GATHER_MINERALS = 295,
            RETURN_MINERALS = 296,

            SupplyDepotLower = 556,
            SupplyDepotRaise = 558,

            BarracksBuildTechLab = 421,
            BarracksBuildReactor = 422,
            FactoryBuildTechLab = 454,
            FactoryBuildReactor = 455,
        }
    }
}