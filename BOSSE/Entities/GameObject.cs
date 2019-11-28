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

    /// <summary>
    /// A single in-game object of any type (building etc)
    /// </summary>
    public abstract class GameObject
    {
        public Vector3 Position;

        public GameObject(Vector3 position)
        {
            this.Position = position;
        }

        public double GetDistance(Unit otherUnit)
        {
            return Vector3.Distance(Position, otherUnit.Position);
        }

        public double GetDistance(Vector3 location)
        {
            return Vector3.Distance(Position, location);
        }
    }
}
