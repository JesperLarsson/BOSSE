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
    using System.Linq;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Various helper functions for interacting with StarCraft 2
    /// </summary>
    public static class GeneralGameUtility
    {
        /// <summary>
        /// Returns which ability builds the given unit
        /// </summary>
        public static int GetAbilityIdToBuildUnit(UnitConstants.UnitId unitType)
        {
            return (int)CurrentGameState.State.GameData.Units[(int)unitType].AbilityId;
        }

        /// <summary>
        /// Returns static information (HP, cost, etc) about the given unit
        /// </summary>
        public static UnitTypeData GetUnitInfo(UnitId unitType)
        {
            UnitTypeData info = CurrentGameState.State.GameData.Units[(int)unitType];
            return info;
        }

        /// <summary>
        /// Determines if we currently have the requirements to build the given unit or not 
        /// </summary>
        public static bool HaveTechRequirementsToBuild(UnitId unitType)
        {
            UnitTypeData data = GetUnitInfo(unitType);
            if (data.TechRequirement == 0)
                return true; // no requirements

            UnitId requirement = (UnitId)data.TechRequirement;
            List<Unit> activeUnitsOftype = GetUnits(requirement, onlyCompleted: true);
            if (activeUnitsOftype.Count == 0)
            {
                // Check for equivalent units
                HashSet<UnitId> equivalentTech = GetEquivalentTech(requirement);
                foreach (UnitId equivalentIter in equivalentTech)
                {
                    List<Unit> activeUnitsOfEquivalent = GetUnits(equivalentIter, onlyCompleted: true);
                    if (activeUnitsOfEquivalent.Count > 0)
                        return true;
                }

                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns a list of units that are the same as the given unit in terms of satisfying tech requirements
        /// Example: Lowered supply depot is a separate unit from supply depots, but are treated the same
        /// </summary>
        public static HashSet<UnitId> GetEquivalentTech(UnitId requiredTech)
        {
#warning Optimization: This reverse mapping could be cached
            uint techId = (uint)requiredTech;
            HashSet<UnitId> resultList = new HashSet<UnitId>();

            foreach (UnitTypeData unitDataIter in CurrentGameState.State.GameData.Units)
            {
                if (unitDataIter.TechAlias == null)
                    continue;

                foreach (uint alias in unitDataIter.TechAlias)
                {
                    if (alias == techId)
                    {
                        resultList.Add((UnitId)alias);
                    }
                }
            }

            // Not in tech tree for some reason
            if (requiredTech == UnitId.SUPPLY_DEPOT)
                resultList.Add(UnitId.SUPPLY_DEPOT_LOWERED);

            return resultList;
        }

        /// <summary>
        /// Queues the given action for output to sc2
        /// </summary>
        public static void Queue(Action action, bool allowOrderOverride = false)
        {
            if (action == null)
            {
                Log.Warning("Tried to queue invalid action");
                return;
            }

            if (action.ActionRaw != null && action.ActionRaw.UnitCommand != null)
            {
                foreach (ulong iter in action.ActionRaw.UnitCommand.UnitTags)
                {
                    bool existed = Unit.AllUnitInstances.TryGetValue(iter, out Unit unitData);
                    if (!existed)
                    {
                        Log.SanityCheckFailed("Queued an action for a unit that doesn't existing in our managed cache (" + iter + ")");
                        continue;
                    }

                    if (unitData.HasNewOrders && (!allowOrderOverride))
                    {
                        // This indicates an issue with the code somewhere, everything should check the current order and the HasNewOrders field
                        Log.Warning("NOTE: Queued duplicate orders for unit " + iter + ", they will be overriden");
                        continue;
                    }

                    //Log.Bulk("Issued a new command to unit " + iter);
                    unitData.HasNewOrders = true;
                }
            }

            GameOutput.QueuedActions.Add(action);
        }

        /// <summary>
        /// Returns the size of a building once placed
        /// </summary>
        public static System.Drawing.Size GetSizeOfBuilding(UnitId buildingId)
        {
            if (buildingId == UnitId.SUPPLY_DEPOT)
            {
                return new System.Drawing.Size(2, 2);
            }
            else if (buildingId == UnitId.PYLON)
            {
                return new System.Drawing.Size(2, 2);
            }
            else if (buildingId == UnitId.CYBERNETICS_CORE || buildingId == UnitId.TWILIGHT_COUNSEL)
            {
                return new System.Drawing.Size(3, 3);
            }
            else if (buildingId == UnitId.BARRACKS)
            {
                return new System.Drawing.Size(3, 3);
            }
            else if (buildingId == UnitId.FACTORY)
            {
                return new System.Drawing.Size(3, 3);
            }
            else if (buildingId == UnitId.WARP_GATE || buildingId == UnitId.GATEWAY)
            {
                return new System.Drawing.Size(3, 3);
            }
            else if (buildingId == UnitId.COMMAND_CENTER || buildingId == UnitId.NEXUS)
            {
                return new System.Drawing.Size(5, 5);
            }
            else
            {
                Log.Bulk("Building size not supported for " + buildingId);
                return new System.Drawing.Size(0, 0);
            }
        }

        /// <summary>
        /// Guesstimates the enemy base location, null = no decent guess
        /// </summary>
        public static Point2D GuessEnemyBaseLocation()
        {
#warning TODO: Replace function with a better system
            foreach (var startLocation in CurrentGameState.State.GameInformation.StartRaw.StartLocations)
            {
                Point2D enemyLocation = new Point2D(startLocation.X, startLocation.Y);
                if (!enemyLocation.IsWithinRange(Globals.MainBaseLocation, 30))
                    return enemyLocation;
            }

            Log.SanityCheckFailed("Unable to find enemy position");
            return null;
        }

        /// <summary>
        /// Determines if we can afford to build/train the given unit
        /// </summary>
        public static bool CanAfford(UnitId unitType)
        {
            UnitTypeData unitData = State.GameData.Units[(int)unitType];
            int foodConsumed = (int)(unitData.FoodRequired - unitData.FoodProvided);

            bool enoughFood = FreeSupply >= foodConsumed;
            bool enoughMinerals = CurrentMinerals >= unitData.MineralCost;
            bool enoughGas = CurrentVespene >= unitData.VespeneCost;

            return enoughMinerals && enoughGas && enoughFood;
        }

        /// <summary>
        /// Subtracts the costs for the unit from our current total. Called after queuing an order to buy/construct
        /// </summary>
        public static void SubtractCosts(UnitId unitType)
        {
            UnitTypeData unitData = State.GameData.Units[(int)unitType];
            int foodConsumed = (int)(unitData.FoodProvided - unitData.FoodRequired);

            CurrentMinerals -= unitData.MineralCost;
            CurrentVespene -= unitData.VespeneCost;
            UsedSupply = (uint)(UsedSupply - foodConsumed);
        }

        /// <summary>
        /// Returns all game units matching certain criteria
        /// </summary>
        public static List<Unit> GetUnits(UnitId unitType, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false, bool includeWorkersTaskedToBuildUnit = false, bool includeBuildingOrdersBuildingUnit = false)
        {
            HashSet<UnitId> temp = new HashSet<UnitId>
            {
                unitType
            };

            return GetUnits(temp, alliance, onlyCompleted, onlyVisible, includeWorkersTaskedToBuildUnit, includeBuildingOrdersBuildingUnit);
        }

        /// <summary>
        /// Returns all game units matching certain criteria
        /// </summary>
        public static List<Unit> GetUnits(HashSet<UnitId> unitTypesToFind, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false, bool includeWorkersTaskedToBuildUnit = false, bool includeBuildingOrdersBuildingUnit = false)
        {
            List<Unit> units = new List<Unit>();

            foreach (SC2APIProtocol.Unit unitIter in CurrentGameState.State.ObservationState.Observation.RawData.Units)
            {
                // Check building orders
                bool addUnit = false;
                if (includeBuildingOrdersBuildingUnit && unitIter.Orders != null)
                {
                    foreach (UnitOrder orderIter in unitIter.Orders)
                    {
                        foreach (UnitId searchParamUnit in unitTypesToFind)
                        {
                            int abilityID = GetAbilityIdToBuildUnit(searchParamUnit);
                            if (orderIter.AbilityId == abilityID)
                            {
                                addUnit = true;
                                break;
                            }
                        }
                        if (addUnit)
                            break;
                    }
                }

                // Check if this unit is what we're searching for
                if (unitTypesToFind.Contains((UnitId)unitIter.UnitType) && unitIter.Alliance == alliance && addUnit == false)
                {
                    if (onlyCompleted && unitIter.BuildProgress < 1)
                        continue;
                    if (onlyVisible && (unitIter.DisplayType != DisplayType.Visible))
                        continue;

                    addUnit = true;
                }

                if (addUnit)
                {
                    // Get managed unit instance (adds things on top of sc2 api entity)
                    Unit managedUnit;
                    if (Unit.AllUnitInstances.ContainsKey(unitIter.Tag))
                    {
                        // Re-use existing instance
                        managedUnit = Unit.AllUnitInstances[unitIter.Tag];
                    }
                    else
                    {
                        managedUnit = new Unit(unitIter);
                    }

                    units.Add(managedUnit);
                }

                if (includeWorkersTaskedToBuildUnit)
                {
                    List<Unit> worksBuildingUnit = GetAllWorkersTaskedToBuildType((UnitId)unitIter.UnitType);
                    foreach (Unit workerIter in worksBuildingUnit)
                    {
                        if (workerIter.Alliance != alliance)
                            continue;
                        if (onlyVisible && (workerIter.IsVisible == false))
                            continue;

                        units.Add(workerIter);
                    }
                }
            }

            return units;
        }

        public static uint GetUnitCountTotal(UnitId unitTypeToFind, bool includePending = true, bool onlyCompleted = false, HashSet<ulong> excludeUnitTags = null, bool includeEquivalents = false)
        {
            var temp = new HashSet<UnitId>() { unitTypeToFind };
            return GetUnitCountTotal(temp, includePending, onlyCompleted, excludeUnitTags, includeEquivalents);
        }

        public static uint GetUnitCountTotal(HashSet<UnitId> unitTypesToFind, bool includePending = true, bool onlyCompleted = false, HashSet<ulong> excludeUnitTags = null, bool includeEquivalents = false)
        {
            // Add equivalents to list 
            if (includeEquivalents)
            {
                foreach (UnitId typeToFindOriginal in unitTypesToFind)
                {
                    HashSet<UnitId> equivalentIter = GetEquivalentTech(typeToFindOriginal);
                    foreach (UnitId equivalentUnitId in equivalentIter)
                    {
                        unitTypesToFind.Add(equivalentUnitId);
                    }
                }
            }

            List<Unit> activeUnits = GetUnits(unitTypesToFind, onlyCompleted: onlyCompleted);
            uint count = (uint)activeUnits.Count;

            // Check for pending units
            if (includePending)
            {
                foreach (UnitId unitType in unitTypesToFind)
                {
                    List<Unit> workersBuildingType = GetAllWorkersTaskedToBuildType(unitType);

                    foreach (Unit workerIter in workersBuildingType)
                    {
                        if (workerIter.CurrentOrder != null && (!UnitListContainsTag(activeUnits, workerIter.CurrentOrder.TargetUnitTag)))
                        {
                            if (excludeUnitTags != null && excludeUnitTags.Contains(workerIter.CurrentOrder.TargetUnitTag))
                                continue;

                            count++;
                        }
                    }
                }
            }

            return count;
        }

        private static bool UnitListContainsTag(List<Unit> unitList, ulong tag)
        {
            foreach (var iter in unitList)
            {
                if (iter.Tag == tag)
                    return true;
            }

            return false;
        }

        private static List<Unit> GetAllWorkersTaskedToBuildType(UnitId unitType)
        {
            int abilityID = GetAbilityIdToBuildUnit(unitType);
            List<Unit> allWorkers = GetUnits(RaceWorkerUnitType(), onlyCompleted: true);
            List<Unit> returnList = new List<Unit>();

            foreach (Unit worker in allWorkers)
            {
                if (worker.CurrentOrder != null && worker.CurrentOrder.AbilityId == abilityID)
                {
                    returnList.Add(worker);
                }
            }

            return returnList;
        }

        ///// <summary>
        ///// Get total number of buildings of a certain type that we own / are in progress of being built
        ///// </summary>
        //public static uint GetBuildingCountTotal(UnitId unitType, bool includingPending = true)
        //{
        //    uint count = (uint)GetUnits(unitType).Count;

        //    if (includingPending)
        //    {
        //        count += (uint)GetPendingBuildingCount(unitType, false);
        //    }

        //    return count;
        //}

        ///// <summary>
        ///// Get number of buildings that are being built
        ///// </summary>
        //public static int GetPendingBuildingCount(UnitId unitType, bool inConstruction = true)
        //{
        //    List<Unit> workers = GetUnits(UnitConstants.Workers);
        //    int abilityID = GetAbilityIdToBuildUnit(unitType);

        //    var counter = 0;

        //    // Find build orders to build this building
        //    foreach (var worker in workers)
        //    {
        //        if (worker.CurrentOrder != null && worker.CurrentOrder.AbilityId == abilityID)
        //            counter += 1;
        //    }

        //    // Count buildings under construction
        //    if (inConstruction)
        //    {
        //        foreach (var unit in GetUnits(unitType))
        //            if (unit.BuildProgress < 1)
        //                counter += 1;
        //    }

        //    return counter;
        //}

        /// <summary>
        /// Get if any unit in the given collection which is close to the given point
        /// </summary>
        public static bool IsInRangeAny(Point2D targetPosition, List<Unit> units, float maxDistance)
        {
            return (GetFirstInRange(targetPosition, units, maxDistance) != null);
        }

        /// <summary>
        /// Get any unit in the given collection which is close to the given point
        /// </summary>
        public static Unit GetFirstInRange(Point2D targetPosition, List<Unit> units, float maxDistance)
        {
            foreach (var unit in units)
            {
                if (targetPosition.IsWithinRange(unit.Position, maxDistance))
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if the given building type can be placed at the given point by sending a request to sc2
        /// This is a BLOCKING call, ie very slow
        /// </summary>
        public static bool CanPlaceRequest(UnitId unitType, Point2D targetPos)
        {
            var abilityID = GetAbilityIdToBuildUnit(unitType);

            RequestQueryBuildingPlacement queryBuildingPlacement = new RequestQueryBuildingPlacement();
            queryBuildingPlacement.AbilityId = abilityID;
            queryBuildingPlacement.TargetPos = new Point2D();
            queryBuildingPlacement.TargetPos.X = targetPos.X;
            queryBuildingPlacement.TargetPos.Y = targetPos.Y;

            Request requestQuery = new Request();
            requestQuery.Query = new RequestQuery();
            requestQuery.Query.Placements.Add(queryBuildingPlacement);

            var result = GameOutput.SendSynchronousRequest_BLOCKING(requestQuery.Query);
            if (result == null)
            {
                Log.Warning("Did not receive a reply to sc2 synchronous request");
                return false;
            }

            if (result.Result.Placements.Count > 0)
                return (result.Result.Placements[0].Result == ActionResult.Success);

            Log.SanityCheckFailed("No response from sc2 in regards to placement of " + unitType);
            return false;
        }

        /// <summary>
        /// Returns any mineral field in our main base
        /// </summary>
        public static Unit GetMineralInMainMineralLine()
        {
            Point2D posVector = new Point2D(Globals.MainBaseLocation.X, Globals.MainBaseLocation.Y);

            List<Unit> allMinerals = GetUnits(UnitConstants.MineralFields, Alliance.Neutral, false, true);
            foreach (var iter in allMinerals)
            {
                if (iter.Position.IsWithinRange(posVector, 15))
                    return iter;
            }

            return null;
        }

        public static UnitId RaceWorkerUnitType()
        {
            if (BOSSE.UseRace == Race.Terran)
                return UnitId.SCV;
            if (BOSSE.UseRace == Race.Protoss)
                return UnitId.PROBE;
            if (BOSSE.UseRace == Race.Zerg)
                return UnitId.DRONE;

            throw new Exception("Unable to determine worker unit type");
        }

        public static UnitId RaceCommandCenterUnitType()
        {
            if (BOSSE.UseRace == Race.Terran)
                return UnitId.COMMAND_CENTER;
            if (BOSSE.UseRace == Race.Protoss)
                return UnitId.NEXUS;
            if (BOSSE.UseRace == Race.Zerg)
                return UnitId.HATCHERY;

            throw new Exception("Unable to determine command center type");
        }

        public static UnitId RaceHouseType()
        {
            if (BOSSE.UseRace == Race.Terran)
                return UnitId.SUPPLY_DEPOT;
            if (BOSSE.UseRace == Race.Protoss)
                return UnitId.PYLON;
            if (BOSSE.UseRace == Race.Zerg)
                return UnitId.OVERLORD;

            throw new Exception("Unable to determine house type");
        }

        public static UnitId RaceGasExtractor()
        {
            if (BOSSE.UseRace == Race.Terran)
                return UnitId.REFINERY;
            if (BOSSE.UseRace == Race.Protoss)
                return UnitId.ASSIMILATOR;
            if (BOSSE.UseRace == Race.Zerg)
                return UnitId.EXTRACTOR;

            throw new Exception("Unable to determine gas type");
        }

        public static AbilityId RaceMiningAction()
        {
            if (BOSSE.UseRace == Race.Terran)
                return AbilityId.HARVEST_GATHER_SCV;
            if (BOSSE.UseRace == Race.Protoss)
                return AbilityId.HARVEST_GATHER_PROBE;
            if (BOSSE.UseRace == Race.Zerg)
                return AbilityId.HARVEST_GATHER_DRONE;

            throw new Exception("Unable to determine mining type");
        }

        /// <summary>
        /// Converts the given logical frame number into a timestamp, as if played at human speed
        /// </summary>
        public static TimeSpan TicksToHumanTime(ulong frameNumber)
        {
            Double doubleFrame = frameNumber;
            doubleFrame = doubleFrame / 22.4d;

            TimeSpan time = TimeSpan.FromSeconds(doubleFrame);
            return time;
        }

        public static TimeSpan GameUptime()
        {
            return TicksToHumanTime(Globals.OnCurrentFrame);
        }

        public static ulong StartGameTimer()
        {
            return Globals.OnCurrentFrame;
        }

        public static TimeSpan MeasureGameTimer(ulong frame)
        {
            ulong frameDiff = Globals.OnCurrentFrame - frame;
            return TicksToHumanTime(frameDiff);
        }

        public static void ApplyChronoBoostTo(Unit targetUnit)
        {
            const int ChronoCost = 50;

            if (BOSSE.UseRace != Race.Protoss)
                return;

            // Use the nexus with the most energy
            List<Unit> allNexus = GetUnits(UnitId.NEXUS, onlyCompleted: true);
            if (allNexus == null || allNexus.Count == 0)
                return;

            allNexus.OrderByDescending(o => o.Energy).ToList();
            Unit useNexus = allNexus[0];
            if (useNexus.Energy < ChronoCost)
                return;

            Queue(CommandBuilder.UseAbilityOnOtherUnit(AbilityId.ChronoBoost, useNexus, targetUnit));
            useNexus.Energy -= ChronoCost;
        }

        /// <summary>
        /// Returns a set of buildings which can produce the given unit
        /// </summary>
        public static UnitId WhichBuildingsProductUnit(UnitId unitToProduce)
        {
            // Protoss
            if (unitToProduce == UnitId.PROBE)
                return UnitId.NEXUS;

            else if (unitToProduce == UnitId.ZEALOT)
                return UnitId.GATEWAY;
            else if (unitToProduce == UnitId.SENTRY)
                return UnitId.GATEWAY;
            else if (unitToProduce == UnitId.STALKER)
                return UnitId.GATEWAY;

            else if (unitToProduce == UnitId.DARK_TEMPLAR)
                return UnitId.DARK_SHRINE;
            else if (unitToProduce == UnitId.HIGH_TEMPLAR)
                return UnitId.TEMPLAR_ARCHIVE;

            else if (unitToProduce == UnitId.IMMORTAL)
                return UnitId.ROBOTICS_BAY;
            else if (unitToProduce == UnitId.COLOSSUS)
                return UnitId.ROBOTICS_BAY;
            else if (unitToProduce == UnitId.WARP_PRISM || unitToProduce == UnitId.WARP_PRISM_PHASING)
                return UnitId.ROBOTICS_BAY;
            else if (unitToProduce == UnitId.OBSERVER)
                return UnitId.ROBOTICS_BAY;

            else if (unitToProduce == UnitId.PHOENIX)
                return UnitId.STARGATE;
            else if (unitToProduce == UnitId.VOID_RAY)
                return UnitId.STARGATE;
            else if (unitToProduce == UnitId.CARRIER)
                return UnitId.STARGATE;

            else if (unitToProduce == UnitId.MOTHERSHIP)
                return UnitId.NEXUS;
            else if (unitToProduce == UnitId.ARCHON)
                throw new Exception("It's complicated, archons not supported"); // should not get here, is not built
            else
                throw new Exception($"Tried to build unsupported unit {unitToProduce}");
        }

        /// <summary>
        /// Tries to train the given unit inside an available production structure
        /// </summary>
        public static bool TryBuildUnit(UnitId unitToBuild, bool onlyFromFreeStructure, bool allowChronoBoost)
        {
            if (CanAfford(unitToBuild) == false)
                return false;
            if (HaveTechRequirementsToBuild(unitToBuild) == false)
                return false;

            UnitId producedFrom = WhichBuildingsProductUnit(unitToBuild);
            List<Unit> productionFacilities = GetUnits(producedFrom, onlyCompleted: true, onlyVisible: true);

            if (productionFacilities == null || productionFacilities.Count == 0)
                return false;

            foreach (Unit buildingIter in productionFacilities)
            {
                if (buildingIter.HasNewOrders)
                    continue;
                if (onlyFromFreeStructure && buildingIter.CurrentOrder != null)
                    continue;

                Queue(CommandBuilder.TrainAction(buildingIter, unitToBuild));
                ApplyChronoBoostTo(buildingIter);

                return true;
            }

            return false;
        }
    }
}
