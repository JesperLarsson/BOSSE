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
    using SC2APIProtocol;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static AbilityConstants;
    using static CurrentGameState;
    using static GeneralGameUtility;
    using static UnitConstants;

    /// <summary>
    /// Translates the given goal inte sc2 actions
    /// </summary>
    public class StrategicGoalExecutor : Manager
    {
        const int MinSupplyMargin = 4;
        const int TargetWorkerPerBase = 24;

        int unitCount = 0;

        /// <summary>
        /// Called once during start
        /// </summary>
        public override void Initialize()
        {
            // Create main squad
            BOSSE.SquadManagerRef.AddNewSquad(new Squad("MainSquad"));

            // Subscribe to all built marines and add them to main squad
            BOSSE.SensorManagerRef.GetSensor(typeof(OwnMilitaryUnitWasCompletedSensor)).AddHandler(
                ReceiveEventFinishedMilitaryUnit,
                unfilteredList => new HashSet<Unit>(unfilteredList.Where(unitIter => unitIter.UnitType == UnitId.MARINE || unitIter.UnitType == UnitId.SIEGE_TANK))
            );

            // Subscribe to finished buildings
            BOSSE.SensorManagerRef.GetSensor(typeof(OwnStructureWasCompletedSensor)).AddHandler(ReceiveEventBuildingFinished);

            BOSSE.WorkerManagerRef.SetNumberOfWorkersOnGas(3);
        }

        private void ReceiveEventFinishedMilitaryUnit(HashSet<Unit> newUnits)
        {
            foreach (Unit iter in newUnits)
            {
                Squad squad = BOSSE.SquadManagerRef.GetSquadOrNull("MainSquad");
                squad.AddUnit(iter);
                Log.Info("  Added unit to main squad: " + iter.Tag + " (" + iter.UnitType + ")");
                unitCount++;
            }

            if (unitCount > 10)
            {
                BOSSE.TacticalGoalRef.SetNewGoal(MilitaryGoal.AttackGeneral);
            }
        }

        /// <summary>
        /// Main function for the goal executor
        /// </summary>
        public override void OnFrameTick()
        {
            AllStrategiesPreRun();

            StrategicGoal currentGoal = BOSSE.StrategicGoalRef.GetCurrentGoal();
            if (currentGoal == StrategicGoal.EconomyFocus)
            {
                ExecuteEconomyFocus();
            }
            else if (currentGoal == StrategicGoal.BuildMilitary)
            {
                ExecuteBuildMilitary();
            }
            else
            {
                throw new NotImplementedException("Unsupported " + currentGoal.ToString());
            }

            AllStrategiesPostRun();
        }

        /// <summary>
        /// Called before all strategies are executed
        /// </summary>
        private void AllStrategiesPreRun()
        {

        }

        /// <summary>
        /// Called after all strategies are executed
        /// </summary>
        private void AllStrategiesPostRun()
        {
            const int MinSupplyMargin = 4;

            // Build depots as we need them
            UnitTypeData houseInfo = GetUnitInfo(UnitId.SUPPLY_DEPOT);
            uint currentAndPendingFood = GetCurrentAndPendingSupply();
            uint supplyDiff = currentAndPendingFood - CurrentGameState.UsedSupply;
            while (supplyDiff < MinSupplyMargin && CurrentMinerals >= houseInfo.MineralCost)
            {
                BOSSE.ConstructionManagerRef.BuildAutoSelectPosition(UnitConstants.UnitId.SUPPLY_DEPOT);
                supplyDiff += (uint)houseInfo.FoodProvided;
                CurrentMinerals -= houseInfo.MineralCost;
            }
        }

        /// <summary>
        /// Execute specific strategy
        /// </summary>
        private void ExecuteBuildMilitary()
        {
            const int RaxesWanted = 1;
            const int FactoriesWanted = 1;
            const int TechLabsWanted = 1;

            uint raxCount = GetUnitCountTotal(UnitConstants.BarracksVariations, includeEquivalents: true);
            uint techLabCount = GetUnitCountTotal(UnitConstants.TechlabVariations, includeEquivalents: true);
            uint factoryCount = GetUnitCountTotal(UnitConstants.FactoryVariations, includeEquivalents: true);

            // Expand
            //if (CanAfford(UnitId.COMMAND_CENTER))
            //{
            //    Point2D constructionSpot = BOSSE.MapAnalysisRef.AnalysedRuntimeMapRef.NaturalExpansion.GetCommandCenterPosition();
            //    Unit worker = BOSSE.WorkerManagerRef.RequestWorkerForJobCloseToPointOrNull(constructionSpot);
            //    Queue(CommandBuilder.ConstructAction(UnitId.COMMAND_CENTER, worker, constructionSpot));
            //}

            // Factory
            if (factoryCount < FactoriesWanted && CanAfford(UnitId.FACTORY) && HaveTechRequirementsToBuild(UnitId.FACTORY))
            {
                List<Unit> raxList = GetUnits(UnitConstants.BarracksVariations);
                if (raxList.Count > 0)
                {
                    Unit nearRax = raxList[0];
                    BOSSE.ConstructionManagerRef.BuildAtApproximatePosition(UnitId.FACTORY, nearRax.Position, 4);
                    SubtractCosts(UnitId.FACTORY);
                }
            }

            // Addons - Tech lab
            if (techLabCount < TechLabsWanted && CanAfford(UnitId.TECHLAB) && HaveTechRequirementsToBuild(UnitId.TECHLAB))
            {
                List<Unit> raxList = GetUnits(UnitConstants.BarracksVariations, onlyCompleted: true);
                if (raxList.Count > 0)
                {
                    Unit atRax = raxList[0];
                    Log.Info("Building tech lab at " + atRax);
                    Queue(CommandBuilder.UseAbility(AbilityConstants.AbilityId.BarracksBuildTechLab, atRax));
                    SubtractCosts(UnitId.TECHLAB);

                    // When techlab finishes, swap positions of the rax with a factory
                    BOSSE.SensorManagerRef.GetSensor(typeof(OwnStructureWasCompletedSensor)).AddHandler(new SensorEventHandler(delegate (HashSet<Unit> affectedUnits)
                    {
                        bool hasLifted = false;
                        Unit factory = null;
                        Point2D raxOrigin = atRax.Position;
                        Point2D factoryOrigin = null;

                        ContinuousUnitOrder swapOrder = new ContinuousUnitOrder(delegate ()
                        {
                            if (!hasLifted)
                            {
                                List<Unit> factories = GetUnits(UnitConstants.FactoryVariations, onlyCompleted: true);
                                if (factories.Count == 0)
                                    return false; // wait for factory
                                if (atRax.CurrentOrder != null)
                                    return false; // wait for rax to finish building

                                factory = factories[0];
                                factoryOrigin = factory.Position;

                                Log.Info("Lifting buildings to swap addons");
                                Queue(CommandBuilder.UseAbility(AbilityId.LIFT, atRax));
                                Queue(CommandBuilder.UseAbility(AbilityId.LIFT, factory));
                                hasLifted = true;
                                return false;
                            }
                            else
                            {
                                int landCount = 0;
                                if (atRax.UnitType == UnitId.BARRACKS_FLYING)
                                {
                                    Queue(CommandBuilder.UseAbilityOnGround(AbilityId.LAND, atRax, factoryOrigin));
                                }
                                else
                                {
                                    landCount++;
                                }
                                if (factory.UnitType == UnitId.FACTORY_FLYING)
                                {
                                    Queue(CommandBuilder.UseAbilityOnGround(AbilityId.LAND, factory, raxOrigin));
                                }
                                else
                                {
                                    landCount++;
                                }

                                return landCount == 2;
                            }
                        });

                        BOSSE.OrderManagerRef.AddOrder(swapOrder);
                    }),
                    unfilteredList => new HashSet<Unit>(unfilteredList.Where(unitIter => unitIter.UnitType.IsSame(UnitConstants.TechlabVariations))),
                    isOneShot: true);
                }
            }

            // Barracks
            if (raxCount < RaxesWanted && CanAfford(UnitId.BARRACKS) && HaveTechRequirementsToBuild(UnitId.BARRACKS))
            {
                // Build barracks
                BOSSE.ConstructionManagerRef.BuildAutoSelectPosition(UnitId.BARRACKS);
                SubtractCosts(UnitId.BARRACKS);
            }
            else
            {
                // Train marines
                UnitTypeData marineInfo = GetUnitInfo(UnitId.MARINE);
                List<Unit> activeSingleRaxes = GetUnits(UnitId.BARRACKS, onlyCompleted: true);

                foreach (Unit rax in activeSingleRaxes)
                {
                    if (CurrentMinerals < marineInfo.MineralCost || FreeSupply < marineInfo.FoodRequired)
                    {
                        break;
                    }
                    if (rax.CurrentOrder != null)
                    {
                        continue; // Do not queue unit
                    }

                    Queue(CommandBuilder.TrainAction(rax, UnitConstants.UnitId.MARINE));
                }
            }
        }

        /// <summary>
        /// Execute specific strategy
        /// </summary>
        private void ExecuteEconomyFocus()
        {
            List<Unit> commandCenters = GetUnits(UnitId.COMMAND_CENTER);

            // Check worker count
            int workerCount = GetUnits(UnitId.SCV).Count;
            if (workerCount >= 14)
            {
                BOSSE.StrategicGoalRef.SetNewGoal(StrategicGoal.BuildMilitary);
            }
        }

        private void ReceiveEventBuildingFinished(HashSet<Unit> buildings)
        {
            // We can upgrade our CC after the barracks finish
            StrategicGoal currentGoal = BOSSE.StrategicGoalRef.GetCurrentGoal();
            bool completedBarracks = buildings.Any(item => item.UnitType == UnitId.BARRACKS);
            if (!completedBarracks)
            {
                return;
            }

            // Upgrade to orbital commands
            List<Unit> commandCenters = GetUnits(UnitId.COMMAND_CENTER, onlyCompleted: true);
            foreach (Unit ccIter in commandCenters)
            {
                // We queue the upgrade action right away, even if building a worker, we will upgrade next after it's done building
                Queue(CommandBuilder.UseAbility(AbilityId.UPGRADE_TO_ORBITAL, ccIter));
            }
        }
    }
}
