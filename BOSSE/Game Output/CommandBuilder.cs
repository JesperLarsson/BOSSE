﻿/*
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
    using static GeneralGameUtility;
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
        private static Action RawCommand(int ability)
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
        public static Action AttackMoveAction(IEnumerable<Unit> units, Point2D targetPos, bool allowPositionCorrection = true)
        {
            Log.Bulk($"AttackMove {targetPos.ToString2()} {units.ToString2()}");
            Action action = RawCommand((int)AbilityConstants.AbilityId.ATTACK);

            // Find another nearby position if we need to
            if (allowPositionCorrection && BOSSE.SpaceMovementReservationManagerRef.IsPointInsideReservedSpace(targetPos))
            {
                targetPos = FindPathableLocationCloseTo(targetPos);
            }

            action.ActionRaw.UnitCommand.TargetWorldSpacePos = targetPos;
            foreach (var unit in units)
            {
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
            }
            return action;
        }

        /// <summary>
        /// Build sc2 action to gather the given mineral field
        /// </summary>
        public static Action MineMineralsAction(IEnumerable<Unit> units, Unit mineralPatch)
        {
            Log.Bulk($"MineMinerals {mineralPatch.ToString()} {units.ToString2()}");
            Action action = RawCommand((int)AbilityConstants.AbilityId.GATHER_MINERALS);
            action.ActionRaw.UnitCommand.TargetUnitTag = mineralPatch.Tag;
            //action.ActionRaw.UnitCommand.TargetWorldSpacePos = mineralPatch.Position;

            foreach (var unit in units)
            {
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
            }

            return action;
        }

        /// <summary>
        /// Build sc2 action to move to the given location
        /// </summary>
        public static Action MoveAction(IEnumerable<Unit> units, Point2D targetPos)
        {
            Log.Bulk($"Move {targetPos.ToString2()} {units.ToString2()}");
            Action action = RawCommand((int)AbilityConstants.AbilityId.MOVE);

            action.ActionRaw.UnitCommand.TargetWorldSpacePos = targetPos;

            foreach (var unit in units)
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);

            return action;
        }

        /// <summary>
        /// Build sc2 action to move to the given location
        /// </summary>
        public static Action MoveAction(IEnumerable<Unit> units, BossePathNode targetPos)
        {
            Log.Bulk($"Move {targetPos.ToString()} {units.ToString2()}");
            Action action = RawCommand((int)AbilityConstants.AbilityId.MOVE);

            action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = targetPos.X;
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = targetPos.Y;

            foreach (var unit in units)
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);

            return action;
        }

        /// <summary>
        /// Build sc2 action to make the given unit construct the given building at a location
        /// </summary>
        public static Action ConstructAction(UnitId structureToBuild, Unit unitThatBuilds, Point2D targetPos)
        {
            Log.Bulk($"Construct {targetPos.ToString2()} {unitThatBuilds.ToString()} {structureToBuild}");
            int abilityID = GetAbilityIdToBuildUnit(structureToBuild);
            Action actionObj = CommandBuilder.RawCommand(abilityID);

            actionObj.ActionRaw.UnitCommand.UnitTags.Add(unitThatBuilds.Tag);
            actionObj.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            actionObj.ActionRaw.UnitCommand.TargetWorldSpacePos.X = targetPos.X;
            actionObj.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = targetPos.Y;

            return actionObj;
        }

        /// <summary>
        /// Build sc2 action to make the given unit construct the given building at a on another location (gas geyser)
        /// </summary>
        public static Action ConstructActionOnTarget(UnitId structureToBuild, Unit unitThatBuilds, Unit onUnit)
        {
            Log.Bulk($"ConstructOnTarget {onUnit.ToString()} {unitThatBuilds.ToString()} {structureToBuild}");
            int abilityID = GetAbilityIdToBuildUnit(structureToBuild);
            Action actionObj = CommandBuilder.RawCommand(abilityID);

            actionObj.ActionRaw.UnitCommand.UnitTags.Add(unitThatBuilds.Tag);
            actionObj.ActionRaw.UnitCommand.TargetUnitTag = onUnit.Tag;

            return actionObj;
        }

        /// <summary>
        /// Build sc2 action to train the specified unit from the given resource center
        /// </summary>
        public static Action TrainAction(Unit fromCenter, UnitId unitTypeToBuild, bool updateResourcesAvailable = true)
        {
            Log.Bulk($"Train {fromCenter.ToString()} {unitTypeToBuild}");
            if (fromCenter.QueuedOrders.Count > 0)
                Log.SanityCheckFailed("No queueing is expected");

            int abilityID = GetAbilityIdToBuildUnit(unitTypeToBuild);
            Action action = CommandBuilder.RawCommand(abilityID);
            action.ActionRaw.UnitCommand.UnitTags.Add(fromCenter.Tag);

            if (updateResourcesAvailable)
            {
                var info = GetUnitInfo(unitTypeToBuild);
                CurrentMinerals -= info.MineralCost;
                CurrentVespene -= info.VespeneCost;

                UsedSupply += (uint)info.FoodProvided;
                UsedSupply -= (uint)info.FoodRequired;
            }

            return action;
        }

        /// <summary>
        /// Build sc2 action for the given unit to use an ability
        /// </summary>
        public static Action UseAbility(AbilityConstants.AbilityId ability, Unit unitToUseAbility)
        {
            Log.Bulk($"UseAbility {unitToUseAbility.ToString()} {ability}");
            Action action = CommandBuilder.RawCommand((int)ability);

            action.ActionRaw.UnitCommand.UnitTags.Add(unitToUseAbility.Tag);

            return action;
        }

        /// <summary>
        /// Build sc2 action for the given unit to use an ability on another unit
        /// </summary>
        public static Action UseAbilityOnOtherUnit(AbilityConstants.AbilityId ability, Unit unitToUseAbility, Unit targetUnit)
        {
            Log.Bulk($"UseAbilityOnUnit {unitToUseAbility.ToString()} {targetUnit} {ability}");
            Action action = CommandBuilder.RawCommand((int)ability);

            action.ActionRaw.UnitCommand.UnitTags.Add(unitToUseAbility.Tag);
            action.ActionRaw.UnitCommand.TargetUnitTag = targetUnit.Tag;

            return action;
        }

        /// <summary>
        /// Build sc2 action for the given unit to use an ability at a target location
        /// </summary>
        public static Action UseAbilityOnGround(AbilityConstants.AbilityId ability, Unit unitToUseAbility, Point2D location)
        {
            Log.Bulk($"UseAbilityOnUnit {unitToUseAbility.ToString()} {location.ToString2()} {ability}");
            Action action = CommandBuilder.RawCommand((int)ability);

            action.ActionRaw.UnitCommand.UnitTags.Add(unitToUseAbility.Tag);
            action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = location.X;
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = location.Y;

            return action;
        }

        /// <summary>
        /// Build sc2 action for inserting the given message in ingame chat
        /// </summary>
        public static Action Chat(string message)
        {
            Log.Bulk($"Chat {message}");
            var actionChat = new ActionChat();
            actionChat.Channel = ActionChat.Types.Channel.Broadcast;
            actionChat.Message = message;

            var action = new Action();
            action.ActionChat = actionChat;

            return action;
        }

        private static Point2D FindPathableLocationCloseTo(Point2D closeToPos, bool checkReserveration = true, bool checkTilePathing = true)
        {
            if ((!checkReserveration) && (!checkTilePathing))
                Log.SanityCheckFailed("Invalid arguments given to FindPathableLocationCloseTo");

            const int SearchRadius = 6;
            for (int xOffset = -SearchRadius; xOffset < SearchRadius; xOffset++)
            {
                for (int yOffset = -SearchRadius; yOffset < SearchRadius; yOffset++)
                {
                    float searchX = closeToPos.X + xOffset;
                    float searchY = closeToPos.Y + yOffset;
                    Point2D searchPos = new Point2D(searchX, searchY);

                    if (checkTilePathing && (!searchPos.IsPathable()))
                    {
                        continue;
                    }
                    if (checkReserveration && BOSSE.SpaceMovementReservationManagerRef.IsPointInsideReservedSpace(searchPos))
                    {
                        continue;
                    }

                    // Done
                    return searchPos;
                }
            }

            throw new BosseFatalException("Unable to find a replacement position close to " + closeToPos);
        }
    }
}