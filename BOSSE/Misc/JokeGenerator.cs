/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 * Map analyzis based on Tyr bot
 */
namespace BOSSE
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Threading;
    using System.IO;
    using System.Diagnostics;

    using DebugGui;
    using SC2APIProtocol;
    using Action = SC2APIProtocol.Action;
    using static CurrentGameState;
    using static UnitConstants;

    /// <summary>
    /// Generates some simple jokes for the chat
    /// </summary>
    public static class JokeGenerator
    {
        /// <summary>
        /// Outer instance is a single joke, each inner list is each line of the joke
        /// </summary>
        private static List<List<string>> jokes = new List<List<string>>(){
            new List<String>() { "glhf" },
            //new List<String>() { "Good bot (；^＿^)ッ" },
            //new List<String>() { "What happens when you cross a werewolf with Santa?", "Santa Claws" },
            //new List<String>() { "A Mexican magician says he will disappear on the count of 3", "\"uno, dos...\" poof", "He disappeared without a tres" },
            //new List<String>() { "What's the difference between a good joke and a bad joke timing" },
            //new List<String>() { "How do you find Will Smith in the snow?", "You look for the fresh prints" },
            new List<String>() { "I'd tell you a UDP joke", "But I'm not sure if you would get it" },
            new List<String>() { "How did the three wise men find Jesus? ", "A*" },
        };

        public static List<string> GetJoke()
        {
#if DEBUG
            // Only ladder gets the jokes
            return new List<string>();
#endif

            Random rand = new Random();
            int index = rand.Next(0, jokes.Count - 1);

            return jokes[index];
        }
    }
}
