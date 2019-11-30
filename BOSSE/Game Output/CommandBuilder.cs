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
    /// Builds sc2 actions that can be sent to the game
    /// </summary>
    public static class CommandBuilder
    {
        /// <summary>
        /// Returns a raw sc2 command without any actions in it
        /// </summary>
        public static Action RawCommand(int ability)
        {
            Action action = new Action();
            action.ActionRaw = new ActionRaw();
            action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
            action.ActionRaw.UnitCommand.AbilityId = ability;

            return action;
        }

        /// <summary>
        /// Build sc2 action to attack move to the given location
        /// </summary>
        public static Action AttackMoveAction(List<Unit> units, Vector3 target)
        {
            Action action = RawCommand((int)AbilityConstants.AbilityId.ATTACK);

            action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;

            foreach (var unit in units)
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);

            return action;
        }

        /// <summary>
        /// Build sc2 action to gather the given mineral field
        /// </summary>
        public static Action MineMineralsAction(List<Unit> units, Unit mineralPatch)
        {
            Action action = RawCommand((int)AbilityConstants.AbilityId.GATHER_RESOURCES);

            action.ActionRaw.UnitCommand.TargetUnitTag = mineralPatch.Tag;
            foreach (var unit in units)
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);

            return action;
        }

        /// <summary>
        /// Build sc2 action to move to the given location
        /// </summary>
        public static Action MoveAction(List<Unit> units, Vector3 target)
        {
            Action action = RawCommand((int)AbilityConstants.AbilityId.MOVE);

            action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;

            foreach (var unit in units)
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);

            return action;
        }

        /// <summary>
        /// Build sc2 action to make the given unit construct the given building at a location
        /// </summary>
        public static Action ConstructAction(UnitId structureToBuild, Unit unitThatBuilds, Vector3 location)
        {
            int abilityID = GetAbilityIdToBuildUnit(structureToBuild);

            Action actionObj = CommandBuilder.RawCommand(abilityID);
            actionObj.ActionRaw.UnitCommand.UnitTags.Add(unitThatBuilds.Tag);
            actionObj.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            actionObj.ActionRaw.UnitCommand.TargetWorldSpacePos.X = location.X;
            actionObj.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = location.Y;

            return actionObj;
        }

        /// <summary>
        /// Build sc2 action to train the specified unit from the given resource center
        /// </summary>
        public static Action TrainAction(Unit fromCenter, UnitId unitTypeToBuild, bool allowQueue = false, bool updateResourcesAvailable = true)
        {
            if (!allowQueue && fromCenter.QueuedOrders.Count > 0)
                return null;

            var abilityID = GetAbilityIdToBuildUnit(unitTypeToBuild);
            var action = CommandBuilder.RawCommand(abilityID);
            action.ActionRaw.UnitCommand.UnitTags.Add(fromCenter.Tag);

            if (updateResourcesAvailable)
            {
                var info = GetUnitInfo(unitTypeToBuild);
                CurrentMinerals -= info.MineralCost;
                CurrentVespene -= info.VespeneCost;

                CurrentSupply += (uint)info.FoodProvided;
                CurrentSupply -= (uint)info.FoodRequired;
            }

            return action;
        }
    }
}
