/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace Bot
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static GameUtility;

    internal class JesperBot : Bot
    {
        private List<SC2APIProtocol.Action> actions = new List<SC2APIProtocol.Action>();

        public IEnumerable<Action> OnFrame()
        {
            actions.Clear();



            return actions;
        }
    }
}