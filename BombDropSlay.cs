using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Reflection;

namespace BombDropSlay;

public class BombDropSlayPlugin : BasePlugin, IPluginConfig<BombDropSlayConfig>
{
    private int _countdownId;
    private CCSPlayerController? _playerWhoDropped;
    private ulong _dropperSteamId;

    public BombDropSlayConfig Config { get; set; } = new();

    public override string ModuleName => "Bomb Drop Slay";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "GSM-RO";
    public override string ModuleDescription => "Slays a Terrorist who drops the C4 if nobody picks it up within 10 seconds.";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventBombDropped>(OnBombDropped);
        RegisterEventHandler<EventBombPickup>(OnBombPickup);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    }

    public void OnConfigParsed(BombDropSlayConfig config)
    {
        if (config.CountdownSeconds < 1)
        {
            throw new InvalidDataException("CountdownSeconds must be greater than zero.");
        }

        if (new[]
            {
                config.ChatPrefix,
                config.DropChatMessage,
                config.DropHudMessage,
                config.CountdownChatMessage,
                config.CountdownHudMessage,
                config.SlayChatMessage,
                config.SlayHudMessage
            }.Any(value => value == null))
        {
            throw new InvalidDataException("Configuration message values cannot be null.");
        }

        Config = config;
    }

    public override void Unload(bool hotReload)
    {
        CancelCountdown();
    }

    private HookResult OnBombDropped(EventBombDropped gameEvent, GameEventInfo info)
    {
        var player = gameEvent.Userid;
        if (player == null || !player.IsValid || player.TeamNum != 2 || !IsAlive(player))
        {
            return HookResult.Continue;
        }

        var countdownId = ++_countdownId;
        _playerWhoDropped = player;
        _dropperSteamId = player.SteamID;
        Announce(
            Config.DropChatMessage,
            Config.DropHudMessage,
            player,
            Config.CountdownSeconds);
        ScheduleCountdown(countdownId, Config.CountdownSeconds);

        return HookResult.Continue;
    }

    private HookResult OnBombPickup(EventBombPickup gameEvent, GameEventInfo info)
    {
        if (_playerWhoDropped == null)
        {
            return HookResult.Continue;
        }

        CancelCountdown();
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath gameEvent, GameEventInfo info)
    {
        var player = gameEvent.Userid;
        if (_playerWhoDropped != null &&
            player != null &&
            player.IsValid &&
            player.SteamID == _dropperSteamId)
        {
            CancelCountdown();
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd gameEvent, GameEventInfo info)
    {
        CancelCountdown();
        return HookResult.Continue;
    }

    private void ScheduleCountdown(int countdownId, int secondsRemaining)
    {
        AddTimer(1.0f, () =>
        {
            if (countdownId != _countdownId || _playerWhoDropped == null)
            {
                return;
            }

            if (!_playerWhoDropped.IsValid ||
                _playerWhoDropped.SteamID != _dropperSteamId ||
                !IsAlive(_playerWhoDropped))
            {
                CancelCountdown();
                return;
            }

            if (secondsRemaining <= 1)
            {
                var player = _playerWhoDropped;
                _playerWhoDropped = null;

                if (player.IsValid && player.SteamID == _dropperSteamId && IsAlive(player))
                {
                    player.PrintToChat(FormatChatMessage(Config.SlayChatMessage, player, 0));
                    player.PrintToCenterAlert(Format(Config.SlayHudMessage, player, 0));
                    player.CommitSuicide(false, true);
                }

                _dropperSteamId = 0;
                return;
            }

            secondsRemaining--;
            Announce(
                Config.CountdownChatMessage,
                Config.CountdownHudMessage,
                _playerWhoDropped,
                secondsRemaining);
            ScheduleCountdown(countdownId, secondsRemaining);
        });
    }

    private void CancelCountdown()
    {
        _countdownId++;
        _playerWhoDropped = null;
        _dropperSteamId = 0;
    }

    private static bool IsAlive(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        return pawn != null && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }

    private void Announce(string chatTemplate, string hudTemplate, CCSPlayerController? dropper, int seconds)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.TeamNum != 2)
            {
                continue;
            }

            player.PrintToChat(FormatChatMessage(chatTemplate, dropper, seconds));
            player.PrintToCenterAlert(Format(hudTemplate, dropper, seconds));
        }
    }

    private string FormatChatMessage(string template, CCSPlayerController? player, int seconds)
    {
        var message = $" {{Red}}{Config.ChatPrefix} - {{Default}}{Format(template, player, seconds)}";
        return Chat.FormatMessage(message);
    }

    private static string Format(string template, CCSPlayerController? player, int seconds)
    {
        return template
            .Replace("{player}", player?.PlayerName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{seconds}", seconds.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static class Chat
    {
        private static readonly Dictionary<string, char> PredefinedColors =
            typeof(ChatColors)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .ToDictionary(
                    field => $"{{{field.Name}}}",
                    field => (char)(field.GetValue(null) ?? '\x01'));

        public static string FormatMessage(string message) =>
            PredefinedColors.Aggregate(
                message,
                (current, color) => current.Replace(color.Key, color.Value.ToString()));
    }
}

public sealed class BombDropSlayConfig : BasePluginConfig
{
    public int CountdownSeconds { get; set; } = 10;
    public string ChatPrefix { get; set; } = "[BombDropSlay]";
    public string DropChatMessage { get; set; } =
        "C4 a fost aruncata de {player}! Daca nimeni nu o ridica in {seconds} secunde, va primi slay.";
    public string DropHudMessage { get; set; } =
        "C4 a fost aruncata de {player}! Daca nimeni nu o ridica in {seconds} secunde, va primi slay.";
    public string CountdownChatMessage { get; set; } = "C4 trebuie ridicata in {seconds} secunde!";
    public string CountdownHudMessage { get; set; } = "C4 trebuie ridicata in {seconds} secunde!";
    public string SlayChatMessage { get; set; } = "Ai primit slay pentru ca ai aruncat C4!";
    public string SlayHudMessage { get; set; } = "Ai primit slay pentru ca ai aruncat C4!";
}
