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

    /// <summary>
    /// A single step in a <see cref="BuildOrder"/>
    /// </summary>
    public abstract class BuildOrderStep
    {
        /// <summary>
        /// Set after this step has been performed
        /// </summary>
        public bool Completed = false;

        /// <summary>
        /// Set if this step depends on another step to be completed first
        /// </summary>
        public BuildOrderStep DependsOnStepOrNull;

        /// <summary>
        /// If set, the next step of the build order is allowed to begin before this one has started
        /// </summary>
        public bool AllowPrematureProceed = false;
    }

    public abstract class BuildOrderStepBuilding
    {
        /// <summary>
        /// Type of building to place
        /// </summary>
        public UnitConstants.UnitId BuildingType;

        /// <summary>
        /// Indicates which base to build the building at, or null if it can be placed anywhere
        /// </summary>
        public BaseLocation BuildAtBaseOrNull;
    }

    public enum BuildOrderState
    {
        Unknown,
        NeedResources,
        InProgress,
        Completed
    }

    /// <summary>
    /// A single build order, indicates which units and structures to build
    /// </summary>
    public abstract class BuildOrder
    {
    }
}