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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;

    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;
    using System.Linq;

    /// <summary>
    /// Basic 2 base build order, focusing on producing blink stalkers
    /// Loosely based on https://www.youtube.com/watch?v=aPPHx7GAVUo
    /// </summary>
    public class BlinkStalkers : BuildOrder
    {
        public BlinkStalkers()
        {
            /*
                1x pylon
                1x gateway
                1x gas
                1x cyber core
                1x nexus
                2x pylon
                2x gas
                1x stalker från gateway (chrono boost)

                vänta på cyber core
                    välj warp gate tech (chrono boost)

                1x twilight countil
                3x gateways

                vänta på twilight council
                    välj blink tech (chrono boost)

                4x gateways
                Pylon, foward position, ev x2
                Pylon
                3x Gas
             */

            RemainingSteps.Add(new CustomStep(() =>
            {
                // Disable auto-building of Pylons, it is a hardcoded part of our starting build
                BOSSE.HouseProviderManagerRef.Disable();

#warning TODO: Re-enable walling
                // Wallin seems to be unreliable sometimes, disabled for now
                //; testa igen ;
                //BOSSE.ConstructionManagerRef.AllowNaturalWallinIn = false;
            }));

            RemainingSteps.Add(new RequireBuilding(UnitId.PYLON, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.GATEWAY, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.ASSIMILATOR, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.CYBERNETICS_CORE, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.NEXUS, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.PYLON, 2));
            RemainingSteps.Add(new RequireBuilding(UnitId.ASSIMILATOR, 2));
            RemainingSteps.Add(new RequireUnit(UnitId.STALKER, 1) { AllowChronoBoost = true });

            RemainingSteps.Add(new WaitForCompletion(UnitId.CYBERNETICS_CORE, 1));
            RemainingSteps.Add(new WaitForCondition(() =>
            {
                // Save resources for warp gate upgrade
                return CurrentMinerals >= 50 && CurrentVespene >= 50;
            }));
            RemainingSteps.Add(new CustomStep(() =>
            {
                // Buy warp upgrade
                Unit cyberCore = GeneralGameUtility.GetUnits(UnitId.CYBERNETICS_CORE, onlyCompleted: true, onlyVisible: true).FirstOrDefault();
                GeneralGameUtility.Queue(CommandBuilder.UseAbility(AbilityConstants.AbilityId.CyberneticsCoreResearch_ResearchWarpGate, cyberCore));

                // Boost out the upgrade
                GeneralGameUtility.ApplyChronoBoostTo(cyberCore);
            }));

            RemainingSteps.Add(new RequireBuilding(UnitId.TWILIGHT_COUNSEL, 1));
            RemainingSteps.Add(new RequireBuilding(UnitId.GATEWAY, 3));

            RemainingSteps.Add(new WaitForCompletion(UnitId.TWILIGHT_COUNSEL, 1));
            RemainingSteps.Add(new WaitForCondition(() =>
            {
                // Save resources for blink upgrade
                return CurrentMinerals >= 150 && CurrentVespene >= 150;
            }));
            RemainingSteps.Add(new CustomStep(() =>
            {
                // Buy blink upgrade
                Unit twilightCouncil = GeneralGameUtility.GetUnits(UnitId.TWILIGHT_COUNSEL, onlyCompleted: true, onlyVisible: true).FirstOrDefault();
                GeneralGameUtility.Queue(CommandBuilder.UseAbility(AbilityConstants.AbilityId.ResearchBlinkUpgrade, twilightCouncil));

                // Boost out the upgrade
                GeneralGameUtility.ApplyChronoBoostTo(twilightCouncil);
            }));

            RemainingSteps.Add(new RequireBuilding(UnitId.GATEWAY, 4));
            RemainingSteps.Add(new RequireBuilding(UnitId.PYLON, 4));
            RemainingSteps.Add(new RequireBuilding(UnitId.ASSIMILATOR, 3));

            // Build finished
            RemainingSteps.Add(new CustomStep(() =>
            {
                // Re-enable auto-building of Pylons
                BOSSE.HouseProviderManagerRef.Enable();
            }));
        }
    }
}
