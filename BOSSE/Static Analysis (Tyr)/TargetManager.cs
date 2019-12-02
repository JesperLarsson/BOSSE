/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 * Map analyzis based on Tyr bot
 */
namespace BOSSE.Tyr
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;
    using System.IO;
    using System.Diagnostics;

    using DebugGui;
    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static global::BOSSE.Tyr.UnitTypes;

    public class TargetManager
    {
        public List<Point2D> PotentialEnemyStartLocations = new List<Point2D>();
        //private ulong targetUnitTag = 0;
        //bool enemyMainFound = false;
        //public bool PrefferDistant { get; set; } = true;

        //public bool TargetAllBuildings = false;
        //public bool TargetCannons = false;
        //public bool TargetGateways = false;
        //public bool SkipPlanetaries = false;

        //public void OnFrame()
        //{
        //    if (PotentialEnemyStartLocations.Count > 1 && !enemyMainFound)
        //    {
        //        for (int i = PotentialEnemyStartLocations.Count - 1; i >= 0; i--)
        //            foreach (Unit unit in tyr.Enemies())
        //                if (SC2Util.DistanceSq(unit.Pos, PotentialEnemyStartLocations[i]) <= 6 * 6)
        //                {
        //                    for (; i > 0; i--)
        //                        PotentialEnemyStartLocations.RemoveAt(0);
        //                    while (PotentialEnemyStartLocations.Count > 1)
        //                        PotentialEnemyStartLocations.RemoveAt(PotentialEnemyStartLocations.Count - 1);
        //                }
        //    }
        //    if (PotentialEnemyStartLocations.Count > 1)
        //    {
        //        for (int i = PotentialEnemyStartLocations.Count - 1; i >= 0; i--)
        //            foreach (Unit unit in tyr.Observation.Observation.RawData.Units)
        //                if (unit.Owner == tyr.PlayerId && SC2Util.DistanceGrid(unit.Pos, PotentialEnemyStartLocations[i]) <= 5)
        //                {
        //                    PotentialEnemyStartLocations.RemoveAt(i);
        //                    break;
        //                }
        //    }

        //    if (PotentialEnemyStartLocations.Count == 1)
        //        enemyMainFound = true;

        //    if (PotentialEnemyStartLocations.Count == 1)
        //    {
        //        float dist = PrefferDistant ? -1 : 1000000;
        //        BuildingLocation target = null;
        //        foreach (BuildingLocation building in tyr.EnemyManager.EnemyBuildings.Values)
        //        {
        //            if (SkipPlanetaries && building.Type == UnitTypes.PLANETARY_FORTRESS)
        //                continue;
        //            if (UnitTypes.ResourceCenters.Contains(building.Type)
        //                || (TargetCannons && building.Type == UnitTypes.PHOTON_CANNON)
        //                || (TargetGateways && building.Type == UnitTypes.GATEWAY)
        //                || (TargetGateways && building.Type == UnitTypes.WARP_GATE)
        //                || TargetAllBuildings)
        //            {
        //                float newDist = SC2Util.DistanceSq(building.Pos, PotentialEnemyStartLocations[0]);
        //                if ((PrefferDistant && newDist > dist)
        //                    || (!PrefferDistant && newDist < dist))
        //                {
        //                    dist = newDist;
        //                    target = building;
        //                }
        //            }
        //        }

        //        if (target != null)
        //        {
        //            AttackTarget = SC2Util.To2D(target.Pos);
        //            targetUnitTag = target.Tag;
        //        }
        //    }

        //    Point2D lastTarget = AttackTarget;

        //    if (!tyr.EnemyManager.EnemyBuildings.ContainsKey(targetUnitTag))
        //    {
        //        AttackTarget = null;
        //        targetUnitTag = 0;
        //        foreach (BuildingLocation enemyBuilding in tyr.EnemyManager.EnemyBuildings.Values)
        //        {
        //            AttackTarget = SC2Util.To2D(enemyBuilding.Pos);
        //            targetUnitTag = enemyBuilding.Tag;
        //            break;
        //        }

        //        if (AttackTarget == null)
        //        {
        //            float dist = 1000000;
        //            foreach (Point2D location in PotentialEnemyStartLocations)
        //            {
        //                if (lastTarget == null)
        //                {
        //                    AttackTarget = location;
        //                    break;
        //                }

        //                float newDist = SC2Util.DistanceSq(lastTarget, location);
        //                if (newDist < dist)
        //                {
        //                    dist = newDist;
        //                    AttackTarget = location;
        //                }
        //            }
        //        }
        //    }
        //    else AttackTarget = SC2Util.To2D(tyr.EnemyManager.EnemyBuildings[targetUnitTag].Pos);

        //    if (tyr.EnemyManager.EnemyBuildings.Count == 0 && PotentialEnemyStartLocations.Count == 1)
        //    {
        //        bool cleared = false;
        //        foreach (Agent agent in tyr.UnitManager.Agents.Values)
        //        {
        //            if (SC2Util.DistanceSq(agent.Unit.Pos, PotentialEnemyStartLocations[0]) <= 6 * 6)
        //            {
        //                cleared = true;
        //                break;
        //            }
        //        }
        //        if (cleared)
        //        {
        //            PotentialEnemyStartLocations.RemoveAt(0);
        //            foreach (Base b in tyr.BaseManager.Bases)
        //                PotentialEnemyStartLocations.Add(b.BaseLocation.Pos);
        //        }
        //    }
        //}

        public void OnStart()
        {
            foreach (Point2D location in Tyr.GameInfo.StartRaw.StartLocations)
                if (SC2Util.DistanceGrid(Tyr.MapAnalyzer.StartLocation, location) > 20)
                    PotentialEnemyStartLocations.Add(location);

            Log.Info("Possible enemy start locations: " + PotentialEnemyStartLocations.Count);
        }

        public Point2D AttackTarget { get; internal set; }
    }
}
