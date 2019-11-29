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
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;

    /// <summary>
    /// Constant values used to identify specific unit abilities
    /// </summary>
    public enum AbilityConstants
    {
        RESEARCH_BANSHEE_CLOAK = 790,
        RESEARCH_INFERNAL_PREIGNITER = 761,
        RESEARCH_UPGRADE_MECH_AIR = 3699,
        RESEARCH_UPGRADE_MECH_ARMOR = 3700,
        RESEARCH_UPGRADE_MECH_GROUND = 3701,

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
    }
}