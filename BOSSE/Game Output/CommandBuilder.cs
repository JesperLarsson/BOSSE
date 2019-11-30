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

    public static class CommandBuilder
    {
        public static Action RawCommand(int ability)
        {
            Action action = new Action();
            action.ActionRaw = new ActionRaw();
            action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
            action.ActionRaw.UnitCommand.AbilityId = ability;

            return action;
        }

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

        public static Action TrainAction(Unit fromCenter, UnitId unitTypeToBuild, bool allowQueue = false)
        {
            if (!allowQueue && fromCenter.QueuedOrders.Count > 0)
                return null;

            var abilityID = GetAbilityIdToBuildUnit(unitTypeToBuild);
            var action = CommandBuilder.RawCommand(abilityID);
            action.ActionRaw.UnitCommand.UnitTags.Add(fromCenter.Tag);

            return action;
        }
    }
}
