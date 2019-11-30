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
    /// A single squad of units
    /// </summary>
    public class Squad
    {
        public string Name;
        public HashSet<Unit> AssignedUnits = new HashSet<Unit>();
        public ISquadController ControlledBy;

        public Squad(string name, ISquadController controller = null)
        {
            if (controller == null)
                controller = new DefaultSquadController(this);

            ControlledBy = controller;
            Name = name;
        }

        public void AddUnit(Unit newUnit)
        {
            AssignedUnits.Add(newUnit);
        }
    }
}
