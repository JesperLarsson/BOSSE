/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace BOSSE
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    using SC2APIProtocol;

    /// <summary>
    /// Handles setting up a proto buff connection to StarCraft 2 via a local proxy
    /// </summary>
    public class GameBootstrapper
    {
        private const string address = "127.0.0.1";
        private readonly ProtobufProxy proxy = new ProtobufProxy();

        private string starcraftDir;
        private string starcraftExe;
        private string starcraftMaps;

        private void StartSC2Instance(int port)
        {
            var processStartInfo = new ProcessStartInfo(starcraftExe);
            processStartInfo.Arguments = string.Format("-listen {0} -port {1} -displayMode 0", address, port);
            processStartInfo.WorkingDirectory = Path.Combine(starcraftDir, "Support64");

            Process.Start(processStartInfo);
        }

        private async Task Connect(int port)
        {
            const int timeout = 60;
            for (var i = 0; i < timeout * 2; i++)
            {
                try
                {
                    await proxy.Connect(address, port);
                    return;
                }
                catch (WebSocketException)
                {
                    // Try again
                }

                Thread.Sleep(500);
            }

            Log.Info($"Unable to connect to SC2 after {timeout} seconds.");
            throw new Exception("Unable to make a connection.");
        }

        private async Task CreateGame(string mapName, Race opponentRace, Difficulty opponentDifficulty)
        {
            var createGame = new RequestCreateGame();
            createGame.Realtime = !BotConstants.TickLockMode;

            var mapPath = Path.Combine(starcraftMaps, mapName);

            if (!File.Exists(mapPath))
            {
                Log.Error("Unable to locate map: " + mapPath);
                throw new Exception("Unable to locate map: " + mapPath);
            }

            createGame.LocalMap = new LocalMap();
            createGame.LocalMap.MapPath = mapPath;

            var player1 = new PlayerSetup();
            createGame.PlayerSetup.Add(player1);
            player1.Type = PlayerType.Participant;

            var player2 = new PlayerSetup();
            createGame.PlayerSetup.Add(player2);
            player2.Race = opponentRace;
            player2.Type = PlayerType.Computer;
            player2.Difficulty = opponentDifficulty;

            var request = new Request();
            request.CreateGame = createGame;
            var response = CheckResponse(await proxy.SendRequest(request));

            if (response.CreateGame.Error != ResponseCreateGame.Types.Error.Unset)
            {
                Log.Error("CreateGame error: " + response.CreateGame.Error.ToString());
                if (!String.IsNullOrEmpty(response.CreateGame.ErrorDetails))
                {
                    Log.Error(response.CreateGame.ErrorDetails);
                }
            }
        }

        private void ReadSettings()
        {
            var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var executeInfo = Path.Combine(myDocuments, "StarCraft II", "ExecuteInfo.txt");
            if (!File.Exists(executeInfo))
                executeInfo = Path.Combine(myDocuments, "StarCraftII", "ExecuteInfo.txt");

            if (File.Exists(executeInfo))
            {
                var lines = File.ReadAllLines(executeInfo);
                foreach (var line in lines)
                {
                    var argument = line.Substring(line.IndexOf('=') + 1).Trim();
                    if (line.Trim().StartsWith("executable"))
                    {
                        starcraftExe = argument;
                        starcraftDir =
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(Path.GetDirectoryName(starcraftExe))); //we need 2 folders down
                        if (starcraftDir != null)
                            starcraftMaps = Path.Combine(starcraftDir, "Maps");
                    }
                }
            }
            else
            {
                throw new Exception("Unable to find:" + executeInfo +
                                    ". Make sure you started the game successfully at least once.");
            }
        }

        private async Task<uint> JoinGame(Race race)
        {
            var joinGame = new RequestJoinGame();
            joinGame.Race = race;

            joinGame.Options = new InterfaceOptions();
            joinGame.Options.Raw = true;
            joinGame.Options.Score = true;

            var request = new Request();
            request.JoinGame = joinGame;
            var response = CheckResponse(await proxy.SendRequest(request));

            if (response.JoinGame.Error != ResponseJoinGame.Types.Error.Unset)
            {
                Log.Error("JoinGame error:" + response.JoinGame.Error.ToString());
                if (!String.IsNullOrEmpty(response.JoinGame.ErrorDetails))
                {
                    Log.Error(response.JoinGame.ErrorDetails);
                }
            }

            return response.JoinGame.PlayerId;
        }

        private async Task<uint> JoinGameLadder(Race race, int startPort)
        {
            var joinGame = new RequestJoinGame();
            joinGame.Race = race;

            joinGame.SharedPort = startPort + 1;
            joinGame.ServerPorts = new PortSet();
            joinGame.ServerPorts.GamePort = startPort + 2;
            joinGame.ServerPorts.BasePort = startPort + 3;

            joinGame.ClientPorts.Add(new PortSet());
            joinGame.ClientPorts[0].GamePort = startPort + 4;
            joinGame.ClientPorts[0].BasePort = startPort + 5;

            joinGame.Options = new InterfaceOptions();
            joinGame.Options.Raw = true;
            joinGame.Options.Score = true;

            var request = new Request();
            request.JoinGame = joinGame;

            var response = CheckResponse(await proxy.SendRequest(request));

            if (response.JoinGame.Error != ResponseJoinGame.Types.Error.Unset)
            {
                Log.Error("JoinGame error: " + response.JoinGame.Error.ToString());
                if (!String.IsNullOrEmpty(response.JoinGame.ErrorDetails))
                {
                    Log.Error(response.JoinGame.ErrorDetails);
                }
            }

            return response.JoinGame.PlayerId;
        }

        public async Task Ping()
        {
            await proxy.Ping();
        }

        private async Task RequestLeaveGame()
        {
            var requestLeaveGame = new Request();
            requestLeaveGame.LeaveGame = new RequestLeaveGame();
            await proxy.SendRequest(requestLeaveGame);
        }

        public async Task SendRequest(Request request)
        {
            await proxy.SendRequest(request);
        }

        public async Task<ProtobufProxy> RunSinglePlayer(string map, Race myRace, Race opponentRace, Difficulty opponentDifficulty)
        {
            ReadSettings();

            const int port = 5678;

            StartSC2Instance(port);
            await Connect(port);
            await CreateGame(map, opponentRace, opponentDifficulty);

            Globals.PlayerId = await JoinGame(myRace);

            return this.proxy;
        }

        public async Task<ProtobufProxy> RunLadder(Race myRace, string[] args)
        {
            var commandLineArgs = new CommandLine(args);

            await Connect(commandLineArgs.GamePort);

            Globals.PlayerId = await JoinGameLadder(myRace, commandLineArgs.StartPort);

            return this.proxy;
        }

        private Response CheckResponse(Response response)
        {
            if (response.Error.Count > 0)
            {
                Log.Error("Starcraft errors received:");
                foreach (var error in response.Error)
                {
                    Log.Error(error);
                }
            }
            return response;
        }
    }
}
