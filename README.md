# BOSSE
BOt Starcraft(2) -e

## About
BOSSE is an experimental Bot / Ai for the game StarCraft 2 written in C#, made by Jesper Larsson.
Supports local debugging sessions (against Blizzard AI) as well as online ladder play.

## Features:
- Separation of goals from game logic (separated into military/scouting/economic and tactical goals)
- Squad unit management with overridable logic. Squads may also be given their own goals
- Static map analysis (finds cliffs, base location, chokepoints, ramps and more)
- Building placement logic (defensive walls around natural expansion)
- 'Sensors' use the subscriber design pattern to provide callbacks for abstracted inputs ('a cloaked unit was seen', 'a unit has died', etc)
- Strategic maps are calculated periodically which calculate influence, tension and vulnerability for each game tile
- Debug GUI which draws minimaps and internal data
- More to come

## Running BOSSE
1. Download StarCraft 2
2. Download the latest ladder map pack from here: https://github.com/Blizzard/s2client-proto#downloads
3. Extract the maps to your StarCraft 2 install directory under /Maps
4. Open solution in Visual Studio and click 'Run'

## Troubleshooting
When starting a local debugging session BOSSE will use a hardcoded map name (currently ThunderbirdLE) to start, which you might not have.
What map is used can be configured in Main.cs along with some other settings.