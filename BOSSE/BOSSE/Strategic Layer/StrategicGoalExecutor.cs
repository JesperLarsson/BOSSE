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
    using System.Linq;

    using SC2APIProtocol;
    using Google.Protobuf.Collections;

    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GameUtility;
    using static UnitConstants;
    using static AbilityConstants;

    /// <summary>
    /// Translates the given goal inte sc2 actions
    /// </summary>
    public class StrategicGoalExecutor
    {
        const int MinSupplyMargin = 4;
        const int TargetWorkerPerBase = 24;

        int marineCount = 0;

        /// <summary>
        /// Called once during start
        /// </summary>
        public void Initialize()
        {
            // Create main squad
            BOSSE.SquadManagerRef.AddNewSquad(new Squad("MainSquad"));

            // Subscribe to all built marines and add them to main squad
            BOSSE.SensorManagerRef.GetSensor(Sensor.SensorId.OwnMilitaryUnitWasCompletedSensor).AddHandler(
                ReceiveEventRecruitedMarine,
                unfilteredList => new HashSet<Unit>(unfilteredList.Where(unitIter => unitIter.UnitType == (uint)UnitId.MARINE))
            );

            // Subscribe to finished buildings
            BOSSE.SensorManagerRef.GetSensor(Sensor.SensorId.OwnStructureWasCompletedSensor).AddHandler(ReceiveEventBuildingFinished);
        }

        private void ReceiveEventRecruitedMarine(HashSet<Unit> newMarines)
        {
            foreach (Unit iter in newMarines)
            {
                if (iter.UnitType != (uint)UnitId.MARINE)
                    continue;

                Squad squad = BOSSE.SquadManagerRef.GetSquadOrNull("MainSquad");
                squad.AddUnit(iter);
                Log.Info("  Added marine to main squad: " + iter.Tag);
                marineCount++;
            }

            if (marineCount > 10)
            {
                BOSSE.TacticalGoalRef.SetNewGoal(MilitaryGoal.AttackGeneral);
            }
        }

        /// <summary>
        /// Main function for the goal executor
        /// </summary>
        public void Tick()
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
            // Build depots as we need them
            UnitTypeData houseInfo = GetUnitInfo(UnitId.SUPPLY_DEPOT);
            uint pendingFood = (uint)(GetPendingBuildingCount(UnitId.SUPPLY_DEPOT) * houseInfo.FoodProvided);
            uint supplyDiff = MaxSupply - CurrentSupply - pendingFood;
            while (supplyDiff < BotConstants.MinSupplyMargin && CurrentMinerals >= houseInfo.MineralCost)
            {
                BuildGivenStructureAnyWhere_TEMPSOLUTION(UnitConstants.UnitId.SUPPLY_DEPOT);
                supplyDiff += (uint)houseInfo.FoodProvided;
                CurrentMinerals -= houseInfo.MineralCost;
            }
        }

        /// <summary>
        /// Execute specific strategy
        /// </summary>
        private void ExecuteBuildMilitary()
        {
            const int RaxesWanted = 2;

            UnitTypeData raxInfo = GetUnitInfo(UnitId.BARRACKS);
            uint raxCount = GetBuildingCountTotal(UnitId.BARRACKS);

            if (raxCount < RaxesWanted && CurrentMinerals >= raxInfo.MineralCost)
            {
                // Build barracks
                BuildGivenStructureAnyWhere_TEMPSOLUTION(UnitConstants.UnitId.BARRACKS);
                CurrentMinerals -= raxInfo.MineralCost;
            }
            else
            {
                // Train marines
                UnitTypeData marineInfo = GetUnitInfo(UnitId.MARINE);
                List<Unit> activeRaxes = GetUnits(UnitId.BARRACKS, onlyCompleted: true);

                foreach (Unit rax in activeRaxes)
                {
                    if (CurrentMinerals < marineInfo.MineralCost || AvailableSupply < marineInfo.FoodRequired)
                    {
                        break;
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
            if (workerCount < (BotConstants.TargetWorkerPerBase * commandCenters.Count))
            {
                // Build more workers
                UnitTypeData workerInfo = GetUnitInfo(UnitId.SCV);
                foreach (Unit cc in commandCenters)
                {
                    if (CurrentMinerals >= workerInfo.MineralCost && AvailableSupply >= workerInfo.FoodRequired && cc.CurrentOrder == null)
                    {
                        Queue(CommandBuilder.TrainAction(cc, UnitConstants.UnitId.SCV));
                        workerCount++;
                    }
                }
            }
            else
            {
                BOSSE.StrategicGoalRef.SetNewGoal(StrategicGoal.BuildMilitary);
            }
        }

        private void ReceiveEventBuildingFinished(HashSet<Unit> buildings)
        {
            // We can upgrade our CC after the barracks finish
            StrategicGoal currentGoal = BOSSE.StrategicGoalRef.GetCurrentGoal();
            bool completedBarracks = buildings.Any(item => item.UnitType == (uint)UnitId.BARRACKS);
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
