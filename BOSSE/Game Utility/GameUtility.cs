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
    /// Various helper functions for interacting with StarCraft 2
    /// </summary>
    public static class GameUtility
    {
        /// <summary>
        /// Converts seconds to number of logical frames
        /// </summary>
        public static ulong SecsToFrames(int seconds)
        {
            return (ulong)(GameConstants.FRAMES_PER_SECOND * seconds);
        }

        public static string GetUnitName(uint unitType)
        {
            return CurrentGameState.GameData.Units[(int)unitType].Name;
        }

        public static bool CanAfford(uint unitType)
        {
            var unitData = GameData.Units[(int)unitType];
            return (Minerals >= unitData.MineralCost) && (Vespene >= unitData.VespeneCost);
        }

        public static List<Unit> GetUnits(uint unitType, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false)
        {
            var temp = new HashSet<uint>();
            temp.Add(unitType);
            return GetUnits(temp, alliance, onlyCompleted, onlyVisible);
        }

        public static List<Unit> GetUnits(HashSet<uint> hashset, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false)
        {
#warning TODO: ideally this should be cached in the future and cleared at each new frame
            var units = new List<Unit>();
            foreach (var unit in CurrentGameState.ObservationState.Observation.RawData.Units)
            {
                if (hashset.Contains(unit.UnitType) && unit.Alliance == alliance)
                {
                    if (onlyCompleted && unit.BuildProgress < 1)
                        continue;

                    if (onlyVisible && (unit.DisplayType != DisplayType.Visible))
                        continue;

                    units.Add(new Unit(unit));
                }
            }
            return units;
        }

        public static bool CanConstruct(uint unitType)
        {
            //we need worker for every structure
            if (GetUnits(Units.Workers).Count == 0) return false;

            //we need an RC for any structure
            var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
            if (resourceCenters.Count == 0) return false;

            if ((unitType == Units.COMMAND_CENTER) || (unitType == Units.SUPPLY_DEPOT))
                return CanAfford(unitType);

            //we need supply depots for the following structures
            var depots = GetUnits(Units.SupplyDepots, onlyCompleted: true);
            if (depots.Count == 0) return false;

            if (unitType == Units.BARRACKS)
                return CanAfford(unitType);

            return CanAfford(unitType);
        }

        public static int GetPendingCount(uint unitType, bool inConstruction = true)
        {
            var workers = GetUnits(Units.Workers);
            var abilityID = Abilities.GetID(unitType);

            var counter = 0;

            //count workers that have been sent to build this structure
            foreach (var worker in workers)
            {
                if (worker.order.AbilityId == abilityID)
                    counter += 1;
            }

            //count buildings that are already in construction
            if (inConstruction)
            {
                foreach (var unit in GetUnits(unitType))
                    if (unit.buildProgress < 1)
                        counter += 1;
            }

            return counter;
        }

        public static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
        {
            return (GetFirstInRange(targetPosition, units, maxDistance) != null);
        }

        public static Unit GetFirstInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
        {
            //squared distance is faster to calculate
            var maxDistanceSqr = maxDistance * maxDistance;
            foreach (var unit in units)
            {
                if (Vector3.DistanceSquared(targetPosition, unit.position) <= maxDistanceSqr)
                    return unit;
            }
            return null;
        }

        public static bool CanPlace(uint unitType, Vector3 targetPos)
        {
            //Note: this is a blocking call! Use it sparingly, or you will slow down your execution significantly!
            var abilityID = Abilities.GetID(unitType);

            RequestQueryBuildingPlacement queryBuildingPlacement = new RequestQueryBuildingPlacement();
            queryBuildingPlacement.AbilityId = abilityID;
            queryBuildingPlacement.TargetPos = new Point2D();
            queryBuildingPlacement.TargetPos.X = targetPos.X;
            queryBuildingPlacement.TargetPos.Y = targetPos.Y;

            Request requestQuery = new Request();
            requestQuery.Query = new RequestQuery();
            requestQuery.Query.Placements.Add(queryBuildingPlacement);

            var result = Globals.StarcraftRef.SendQuery(requestQuery.Query);
            if (result.Result.Placements.Count > 0)
                return (result.Result.Placements[0].Result == ActionResult.Success);
            return false;
        }

        public static void Construct(uint unitType)
        {
            Vector3 startingSpot;

            var resourceCenters = GetUnits(Units.ResourceCenters);
            if (resourceCenters.Count > 0)
                startingSpot = resourceCenters[0].position;
            else
            {
                Log.LogError("Unable to construct: {0}. No resource center was found.", GetUnitName(unitType));
                return;
            }

            const int radius = 12;

            //trying to find a valid construction spot
            var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
            Vector3 constructionSpot;
            while (true)
            {
                constructionSpot = new Vector3(startingSpot.X + Globals.Random.Next(-radius, radius + 1), startingSpot.Y + Globals.Random.Next(-radius, radius + 1), 0);

                //avoid building in the mineral line
                if (IsInRange(constructionSpot, mineralFields, 5)) continue;

                //check if the building fits
                if (!CanPlace(unitType, constructionSpot)) continue;

                //ok, we found a spot
                break;
            }

            var worker = GetAvailableWorker(constructionSpot);
            if (worker == null)
            {
                Log.LogError("Unable to find worker to construct: {0}", GetUnitName(unitType));
                return;
            }

            var abilityID = Abilities.GetID(unitType);
            var constructAction = CommandBuilder.CreateRawUnitCommand(abilityID);
            constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.tag);
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.X = constructionSpot.X;
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = constructionSpot.Y;
            GameOutput.QueuedActions.Add(constructAction);

            Log.LogInformation("Constructing: {0} @ {1} / {2}", GetUnitName(unitType), constructionSpot.X, constructionSpot.Y);
        }

        public static Unit GetAvailableWorker(Vector3 targetPosition)
        {
            var workers = GetUnits(Units.Workers);
            foreach (var worker in workers)
            {
                if (worker.order.AbilityId != Abilities.GATHER_MINERALS) continue;

                return worker;
            }

            return null;
        }
    }
}
