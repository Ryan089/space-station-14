using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    sealed class JoinGameCommand : IConsoleCommand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string Command => "joingame";
        public string Description => "";
        public string Help => "";

        public JoinGameCommand()
        {
            IoCManager.InjectDependencies(this);
        }
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var player = shell.Player as IPlayerSession;

            if (player == null)
            {
                return;
            }

            var ticker = EntitySystem.Get<GameTicker>();
            var stationSystem = EntitySystem.Get<StationSystem>();
            var stationJobs = EntitySystem.Get<StationJobsSystem>();

            if (!ticker.PlayersInLobby.ContainsKey(player) || ticker.PlayersInLobby[player] == LobbyPlayerStatus.Observer)
            {
                Logger.InfoS("security", $"{player.Name} ({player.UserId}) attempted to latejoin while in-game.");
                shell.WriteError($"{player.Name} is not in the lobby.   This incident will be reported.");
                return;
            }

            if (ticker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine("Round has not started.");
                return;
            }
            else if (ticker.RunLevel == GameRunLevel.InRound)
            {
                string id = args[0];

                if (!int.TryParse(args[1], out var sid))
                {
                    shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                }

                var station = new EntityUid(sid);
                var jobPrototype = _prototypeManager.Index<JobPrototype>(id);
                if(stationJobs.TryGetJobSlot(station, jobPrototype, out var slots) == false || slots == 0)
                {
                    shell.WriteLine($"{jobPrototype.Name} has no available slots.");
                    return;
                }
                ticker.MakeJoinGame(player, station, id);
                return;
            }

            ticker.MakeJoinGame(player, EntityUid.Invalid);
        }
    }
}
