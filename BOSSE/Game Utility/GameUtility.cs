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
    using static UnitConstants;
    using static AbilityConstants;

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
            return (ulong)(BotConstants.FRAMES_PER_SECOND * seconds);
        }

        /// <summary>
        /// Determines if we can afford to build/train the given unit
        /// </summary>
        public static bool CanAfford(UnitId unitType)
        {
            UnitTypeData unitData = GameData.Units[(int)unitType];
            return (CurrentMinerals >= unitData.MineralCost) && (CurrentVespene >= unitData.VespeneCost);
        }

        /// <summary>
        /// Get a list of active units for the given search parameter
        /// </summary>
        public static List<Unit> GetUnits(UnitId unitType, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false)
        {
            HashSet<UnitId> temp = new HashSet<UnitId>();
            temp.Add(unitType);

            return GetUnits(temp, alliance, onlyCompleted, onlyVisible);
        }

        /// <summary>
        /// Get a list of active units for the given search parameter
        /// </summary>
        public static List<Unit> GetUnits(HashSet<UnitId> unitTypesToFind, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false)
        {
            List<Unit> units = new List<Unit>();

            foreach (var unit in CurrentGameState.ObservationState.Observation.RawData.Units)
            {
                if (unitTypesToFind.Contains((UnitId)unit.UnitType) && unit.Alliance == alliance)
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

        /// <summary>
        /// Get number of buildings that are being built
        /// </summary>
        public static int GetPendingBuildingCount(UnitId unitType, bool inConstruction = true)
        {
            List<Unit> workers = GetUnits(UnitConstants.Workers);
            int abilityID = AbilityConstants.GetAbilityIdToBuildUnit(unitType);

            var counter = 0;

            // Find build orders to build this building
            foreach (var worker in workers)
            {
                if (worker.CurrentOrder != null && worker.CurrentOrder.AbilityId == abilityID)
                    counter += 1;
            }

            // Count buildings under construction
            if (inConstruction)
            {
                foreach (var unit in GetUnits(unitType))
                    if (unit.BuildProgress < 1)
                        counter += 1;
            }

            return counter;
        }

        /// <summary>
        /// Get if any unit in the given collection which is close to the given point
        /// </summary>
        public static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
        {
            return (GetFirstInRange(targetPosition, units, maxDistance) != null);
        }

        /// <summary>
        /// Get any unit in the given collection which is close to the given point
        /// </summary>
        public static Unit GetFirstInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
        {
            var maxDistanceSqr = maxDistance * maxDistance;

            foreach (var unit in units)
            {
                if (Vector3.DistanceSquared(targetPosition, unit.Position) <= maxDistanceSqr)
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if the given building type can be placed at the given point
        /// This is a BLOCKING call, ie very slow
        /// </summary>
        public static bool CanPlace(UnitId unitType, Vector3 targetPos)
        {
            var abilityID = AbilityConstants.GetAbilityIdToBuildUnit(unitType);

            RequestQueryBuildingPlacement queryBuildingPlacement = new RequestQueryBuildingPlacement();
            queryBuildingPlacement.AbilityId = abilityID;
            queryBuildingPlacement.TargetPos = new Point2D();
            queryBuildingPlacement.TargetPos.X = targetPos.X;
            queryBuildingPlacement.TargetPos.Y = targetPos.Y;

            Request requestQuery = new Request();
            requestQuery.Query = new RequestQuery();
            requestQuery.Query.Placements.Add(queryBuildingPlacement);

            var result = GameOutput.SendSynchronousRequest_BLOCKING(requestQuery.Query);
            if (result.Result.Placements.Count > 0)
                return (result.Result.Placements[0].Result == ActionResult.Success);
            return false;
        }

        /// <summary>
        /// Builds the given type
        /// </summary>
        public static void Construct(UnitId unitType)
        {
            const int radius = 12;
            Vector3 startingSpot;

            List<Unit> resourceCenters = GetUnits(UnitConstants.ResourceCenters);
            if (resourceCenters.Count > 0)
            {
                startingSpot = resourceCenters[0].Position;
            }
            else
            {
                Log.Error($"Unable to construct {unitType} - no resource center was found");
                return;
            }

            // Find a valid spot, the slow way
            List<Unit> mineralFields = GetUnits(UnitConstants.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
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

            Unit worker = GetAvailableWorker(constructionSpot);
            if (worker == null)
            {
                Log.Error($"Unable to find worker to construct {unitType}");
                return;
            }

            int abilityID = AbilityConstants.GetAbilityIdToBuildUnit(unitType);
            Action constructAction = CommandBuilder.CreateRawUnitCommand(abilityID);
            constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.Tag);
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.X = constructionSpot.X;
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = constructionSpot.Y;
            GameOutput.QueuedActions.Add(constructAction);

            Log.Info($"Constructing {unitType} at {constructionSpot.ToString2()} / {constructionSpot.Y}");
        }

        public static Unit GetAvailableWorker(Vector3 targetPosition)
        {
            var workers = GetUnits(UnitConstants.Workers);
            foreach (Unit worker in workers)
            {
                if (worker.CurrentOrder != null && worker.CurrentOrder.AbilityId != (uint)AbilityId.GATHER_MINERALS)
                    continue;

                return worker;
            }

            return null;
        }
    }
}
