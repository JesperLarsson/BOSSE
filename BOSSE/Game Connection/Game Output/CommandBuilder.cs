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
    using System.Linq;
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
            Action action = RawCommand((int)RaceMiningAction());
            action.ActionRaw.UnitCommand.TargetUnitTag = mineralPatch.Tag;

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
            AbilityId ability = GetAbilityIdToBuildUnit(structureToBuild);
            Action actionObj = CommandBuilder.RawCommand((int)ability);

            actionObj.ActionRaw.UnitCommand.UnitTags.Add(unitThatBuilds.Tag);
            actionObj.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            actionObj.ActionRaw.UnitCommand.TargetWorldSpacePos.X = targetPos.X;
            actionObj.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = targetPos.Y;

            unitThatBuilds.HasNewOrders = true;

            return actionObj;
        }

        /// <summary>
        /// Build sc2 action to make the given unit construct the given building at a on another location (gas geyser)
        /// </summary>
        public static Action ConstructActionOnTarget(UnitId structureToBuild, Unit unitThatBuilds, Unit onUnit)
        {
            Log.Bulk($"ConstructOnTarget {onUnit.ToString()} {unitThatBuilds.ToString()} {structureToBuild}");
            AbilityId ability = GetAbilityIdToBuildUnit(structureToBuild);
            Action actionObj = CommandBuilder.RawCommand((int)ability);

            actionObj.ActionRaw.UnitCommand.UnitTags.Add(unitThatBuilds.Tag);
            actionObj.ActionRaw.UnitCommand.TargetUnitTag = onUnit.Tag;

            return actionObj;
        }

        /// <summary>
        /// Build sc2 action to train the specified unit from the given resource center
        /// </summary>
        public static Action TrainActionAndSubtractCosts(Unit fromCenter, UnitId unitTypeToBuild, bool updateResourcesAvailable = true)
        {
            if (fromCenter.QueuedOrders.Count > 0)
                Log.SanityCheckFailed("No queueing is expected");

            // Special case - Warp gates need special logic when training
            if (fromCenter.UnitType == UnitId.WARP_GATE)
                return TrainActionAndSubtractCostsWarpTech(fromCenter, unitTypeToBuild, updateResourcesAvailable);

            Log.Bulk($"Train {fromCenter.ToString()} {unitTypeToBuild}");
            
            AbilityId ability = GetAbilityIdToBuildUnit(unitTypeToBuild);
            Action action = CommandBuilder.RawCommand((int)ability);
            action.ActionRaw.UnitCommand.UnitTags.Add(fromCenter.Tag);

            if (updateResourcesAvailable)
            {
                SubtractCosts(unitTypeToBuild);
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

        public static Action TrainActionAndSubtractCostsWarpTech(Unit fromCenter, UnitId unitTypeToBuild, bool updateResourcesAvailable = true)
        {
            if (fromCenter.UnitType != UnitId.WARP_GATE)
                Log.SanityCheckFailed("Tried to warp in units from non-warpgate building");

            AbilityId ability = GetAbilityIdToBuildUnit(unitTypeToBuild);
            ability = GetWarpInAbility(ability);

            Point2D targetPos = GetWarpInSpot();
            if (targetPos == null)
                return null;

            Log.Bulk($"WarpIn {unitTypeToBuild} at {targetPos}");
            
            Action action = CommandBuilder.RawCommand((int)ability);
            action.ActionRaw.UnitCommand.UnitTags.Add(fromCenter.Tag);
            action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = targetPos.X;
            action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = targetPos.Y;

            if (updateResourcesAvailable)
            {
                SubtractCosts(unitTypeToBuild);
            }

            return action;
        }

        private static Point2D GetWarpInSpot()
        {
            // Use the pylon that is closest to the enemy base
            List<Unit> pylons = GetUnits(UnitId.PYLON, onlyCompleted: true, onlyVisible: true, alliance: Alliance.Self);
            if (pylons == null || pylons.Count == 0)
                return null;

            Point2D enemyPos = GuessEnemyBaseLocation();
            pylons = pylons.OrderBy(o => o.Position.AirDistanceAbsolute(enemyPos)).ToList();
            Unit targetPylon = pylons[0];

            // Using a random offset should be good-enough to start with
            // If we collide with something pre-existing, we will try to build another unit next frame anyway
            int xOffset = Globals.Random.Next(-6, 6);
            int yOffset = Globals.Random.Next(-6, 6);

            Point2D warpPos = new Point2D(targetPylon.Position.X + xOffset, targetPylon.Position.Y + yOffset);
            return warpPos;
        }
    }
}