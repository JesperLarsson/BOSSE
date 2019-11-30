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
        public enum StrategicGoal
        {
            EconomyFocus,
            BuildMilitaryPlusEconomy,
            BuildMilitary,
            Expand
        }
        public StrategicGoal CurrentGoal;

        const int MinSupplyMargin = 4;
        const int TargetWorkerPerBase = 24;

        /// <summary>
        /// Called once during start
        /// </summary>
        public void Initialize()
        {
            Log.Info("Setting initial strategic goal");
            SetNewGoal(StrategicGoal.EconomyFocus);
        }

        /// <summary>
        /// Main function for the goal executor
        /// </summary>
        public void Tick()
        {
            AllStrategiesPreRun();

            if (CurrentGoal == StrategicGoal.EconomyFocus)
            {
                ExecuteEconomyFocus();
            }
            else if (CurrentGoal == StrategicGoal.BuildMilitary)
            {
                ExecuteEconomyFocus();
            }
            else
            {
                throw new NotImplementedException("Unsupported " + CurrentGoal.ToString());
            }

            AllStrategiesPostRun();
        }

        public void SetNewGoal(StrategicGoal newGoal)
        {
            if (newGoal == CurrentGoal)
                return;

            Log.Info($"Setting new strategic goal = {newGoal} (was {this.CurrentGoal})");
            this.CurrentGoal = newGoal;
        }

        private void AllStrategiesPreRun()
        {

        }

        private void AllStrategiesPostRun()
        {
            // Build depots as we need them
            UnitTypeData houseInfo = GetUnitInfo(UnitId.SUPPLY_DEPOT);
            uint supplyDiff = MaxSupply - CurrentSupply - BotConstants.MinSupplyMargin;
            while (supplyDiff > 0 && CurrentMinerals >= houseInfo.MineralCost)
            {
                BuildStructureAnyWhere(UnitConstants.UnitId.SUPPLY_DEPOT);
                supplyDiff -= (uint)houseInfo.FoodProvided;
                CurrentMinerals -= houseInfo.MineralCost;
            }
        }

        private void ExecuteBuildMilitary()
        {
            const int RaxesWanted = 2;

            UnitTypeData raxInfo = GetUnitInfo(UnitId.BARRACKS);
            List<Unit> activeRaxes = GetUnits(UnitId.BARRACKS);

            if (activeRaxes.Count < RaxesWanted && CurrentMinerals >= raxInfo.MineralCost)
            {
                // Build barracks
                BuildStructureAnyWhere(UnitConstants.UnitId.BARRACKS);
                CurrentMinerals -= raxInfo.MineralCost;
            }
            else
            {
                // Train marines
                foreach (Unit rax in activeRaxes)
                {
                    if (CurrentMinerals >= GetUnitInfo(UnitId.MARINE).MineralCost)
                    {
                        Queue(CommandBuilder.TrainAction(rax, UnitConstants.UnitId.MARINE));
                    }
                }
            }
        }

        private void ExecuteEconomyFocus()
        {
            List<Unit> commandCenters = GetUnits(UnitId.COMMAND_CENTER);

            // Check worker count
            int workerCount = GetUnits(UnitId.SCV).Count;
            if (workerCount < (BotConstants.TargetWorkerPerBase * commandCenters.Count))
            {
                // Build more workers
                foreach (Unit cc in commandCenters)
                {
                    if (CurrentMinerals >= GetUnitInfo(UnitId.SCV).MineralCost)
                    {
                        Queue(CommandBuilder.TrainAction(cc, UnitConstants.UnitId.SCV));
                    }
                }
            }
            else
            {
                this.SetNewGoal(StrategicGoal.BuildMilitary);
            }
        }

        /// <summary>
        /// Builds the given type
        /// </summary>
        public static void BuildStructureAnyWhere(UnitId unitType)
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

            int abilityID = GetAbilityIdToBuildUnit(unitType);
            Action constructAction = CommandBuilder.RawCommand(abilityID);
            constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.Tag);
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.X = constructionSpot.X;
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = constructionSpot.Y;
            Queue(constructAction);

            Log.Info($"Constructing {unitType} at {constructionSpot.ToString2()} / {constructionSpot.Y}");
        }
    }
}
