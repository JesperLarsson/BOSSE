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
        public SquadControllerBase ControlledBy;

        public Squad(string name, SquadControllerBase controller = null)
        {
            if (controller == null)
            {
                controller = new DefaultSquadController();
            }
            controller.AssignSquad(this);

            ControlledBy = controller;
            Name = name;

            // Subscribe to "unit died" event - Remove it from the squad
            BOSSE.SensorManagerRef.GetSensor(Sensor.SensorId.OwnMilitaryUnitDiedSensor).AddHandler(new EventHandler(delegate (Object sensorRef, EventArgs args)
            {
                OwnMilitaryUnitDiedSensor.Details details = (OwnMilitaryUnitDiedSensor.Details)args;

                var ownedUnits = AssignedUnits.ToList();
                foreach (var killedIter in details.KilledUnits)
                {
                    ownedUnits = ownedUnits.Where(x => x.Tag != killedIter.Tag).ToList();
                }

                HashSet<Unit> newUnitSet = new HashSet<Unit>();
                foreach (var iter in ownedUnits)
                {
                    Log.Info($"Removed unit {iter.Tag} from squad {Name} (unit died)");
                    newUnitSet.Add(iter);
                }
            }));
        }

        public virtual void IsBeingDeleted()
        {
            Log.Bulk("Deleted squad " + Name);
        }

        public virtual void AddUnit(Unit newUnit)
        {
            AssignedUnits.Add(newUnit);
        }
    }
}
