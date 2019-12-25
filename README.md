# BOSSE

## About
BOSSE is a bot for the game StarCraft 2 written in C# as a hobby project.
Supports local debugging sessions (against Blizzard AI), as well as online ladder play against other bots.

## Features
- Static map analysis (finds chokepoints, natural expansion, etc). Performed once per map and saved to file
- Runtime map analysis - Influence, tension, vulnerability, etc
- High level separation of goals and game logic
- Squad unit management with overridable logic
- Building placement planning
- Path finding through A*
- Multithreading
- Capable of running in real time
- Debug GUI. Draws various maps and other debugging data
- More to come

## TODO / Major limitations
- Currently only supports 2-player maps
- Does not support multiple build orders
- Prediction of enemy plans
- Currently only plays as Terran
- Cheese detection
- Micro management

## Running BOSSE
1. Download StarCraft 2, start it at least once to initialize
2. Download the latest ladder map pack from here: https://github.com/Blizzard/s2client-proto#downloads
3. Extract the maps to your StarCraft 2 install directory under /Maps (create the folder if it doesn't exist)
4. Open solution in Visual Studio and click 'Run' (developed using VS 2019, but other versions will probably work)

## Links
- Interacts with StarCraft 2 using the official protocol buffer API by Blizzard, which can be found here: https://github.com/Blizzard/s2client-api
- API documentation: https://blizzard.github.io/s2client-api/annotated.html
- If you're looking for a nearly empty C# project to get started, this should work as-is: https://github.com/JesperLarsson/BOSSE/releases/tag/BasicStartingTemplate

## Troubleshooting
When starting a local debugging session BOSSE will use a hardcoded map name (currently ThunderbirdLE) to start, which you might not have.
What map is used can be configured in Main.cs along with some other settings.