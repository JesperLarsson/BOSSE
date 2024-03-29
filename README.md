# BOSSE

## About
BOSSE is a bot for the game StarCraft 2 written in C# as a fun hobby project.
Supports local debugging sessions (against the official AI or a human player), as well as online ladder play against other bots.

## Status
Development is mostly paused for now, but I will revisit the project every now and then as time allows :).

## Features
- Precalculated map analysis (expansion locations, chokepoints, wall locations, ramps, etc). Calculated once per map and stored to file
- Runtime map analysis - Influence, tension, vulnerability, etc
- Squad unit management with overridable logic
- Building placement planning
- Path finding through A*
- Debug GUI. Draws various maps and other debugging data

## Screenshot (Debug GUI)
The bot uses a simple GUI for debugging, which updates in real time:
![Alt text](/docs/UI2021-12-27.png?raw=true "Debug GUI")

## TODO / Major Limitations
- Currently only supports 2-player maps
- Does not support multiple build orders
- Prediction of enemy plans
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

## Rebuilding Game Schema
The protobuff schema needs to be rebuilt after each new game release, using the following steps:
1. Download latest schema from here by cloning the repo: https://github.com/Blizzard/s2client-proto
2. Download latest protobuff compiler, ex protoc-3.11.2-win64.zip, from here: https://github.com/protocolbuffers/protobuf/releases
3. Extract protoc.exe into the schema root folder
4. Open cmd and run:
<pre>protoc.exe --csharp_out=. s2clientprotocol/common.proto s2clientprotocol/data.proto s2clientprotocol/debug.proto s2clientprotocol/error.proto s2clientprotocol/query.proto s2clientprotocol/raw.proto s2clientprotocol/sc2api.proto s2clientprotocol/score.proto s2clientprotocol/spatial.proto s2clientprotocol/ui.proto</pre>
5. Copy the output .cs files to the solution
6. (Optional) Update the protobuff NuGet package in Visual studio, necessary if the schema contained any new protobuff features

## Credits
Based on the following starting template which bootstraps the game session: https://github.com/NikEyX/SC2-CSharpe-Starterkit. Otherwise everything has been implemented from scratch.
BOSSE is licensed under GPL, so feel free to use whatever you need.

## Links
- General info and wiki: http://wiki.sc2ai.net/Main_Page
- Interacts with StarCraft 2 using the official proto buff API: https://github.com/Blizzard/s2client-api
- API documentation: https://blizzard.github.io/s2client-api/annotated.html
