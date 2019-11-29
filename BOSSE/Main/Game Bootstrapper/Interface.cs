/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System.Collections.Generic;
    using SC2APIProtocol;

    /// <summary>
    /// Bot interface abstraction layer
    /// </summary>
    public interface IBot
    {
        void Update();
    }
}