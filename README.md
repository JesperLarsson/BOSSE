# BOSSE

## About
BOSSE is a bot for the game StarCraft 2 written in C# as a hobby project.
Supports local debugging sessions (against Blizzard AI), as well as online ladder play against other bots.

## Status
Development mostly paused for now.

## Features
- Precalculated map analysis (chokepoints, expansion locations, ramps, etc). Calculated once per map and stored to file
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
- Can only play on pre-analysed maps (which takes a couple of hours)
- Does not support multiple build orders
- Prediction of enemy plans
- Currently only plays as Terran
- Cheese detection
- Micro management

## Running BOSSE
1. Download the latest version of StarCraft 2 via Battle.net, start it at least once to initialize everything
2. Download the latest ladder map pack from here: https://github.com/Blizzard/s2client-proto#downloads
3. Extract the maps to your StarCraft 2 install directory under Maps (create the folder if it doesn't exist). Ex: C:\Program Files (x86)\StarCraft II\Maps\
4. Open solution in Visual Studio and click 'Run', it will automatically open up the game with the correct settings

### Troubleshooting:
When starting a local debugging session BOSSE will use a hardcoded map name (currently ThunderbirdLE) to start, which you might not have.
What map is used can be configured in Main.cs along with some other settings.
Developed using Visual Studio 2019, but other versions will likely work.

## Credits
Based on the following starting template: https://github.com/NikEyX/SC2-CSharpe-Starterkit

Otherwise everything has been implemented from scratch.

## Links
- Interacts with StarCraft 2 using the official proto buff API by Blizzard: https://github.com/Blizzard/s2client-api
- API documentation: https://blizzard.github.io/s2client-api/annotated.html
- If you're looking for a nearly empty C# project to get started, this should work as-is: https://github.com/JesperLarsson/BOSSE/releases/tag/BasicStartingTemplate
- General info and wiki: http://wiki.sc2ai.net/Main_Page
