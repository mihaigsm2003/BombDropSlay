# BombDropSlay

CounterStrikeSharp plugin that warns the Terrorist team when a living T player drops the C4. If nobody picks it up before the countdown ends, the player who dropped it receives a slay.

## Features

- Sends the warning and countdown to Terrorists only, in chat and HUD.
- Cancels the countdown when the C4 is picked up, the dropper dies, or the round ends.
- Sends the slay message to the dropper and slays them only if they are still alive when the timer expires.
- Uses CounterStrikeSharp's automatic plugin configuration system.

## Requirements

- CounterStrikeSharp installed on a CS2 server.
- .NET 10 SDK to build from source.

## Build

From the repository directory, run:

```powershell
dotnet build .\BombDropSlay.csproj -c Release
```

The plugin assembly is created at `bin\Release\net10.0\BombDropSlay.dll`.

## Install

Copy `BombDropSlay.dll` to the CounterStrikeSharp plugins directory on the server:

```text
/game/csgo/addons/counterstrikesharp/plugins/BombDropSlay/BombDropSlay.dll
```

Load or reload the plugin using the normal CounterStrikeSharp plugin-management procedure. CounterStrikeSharp creates the plugin configuration on first load at:

```text
/game/csgo/addons/counterstrikesharp/configs/plugins/BombDropSlay/BombDropSlay.json
```

## Configuration

The generated configuration has these defaults:

```json
{
  "ConfigVersion": 1,
  "CountdownSeconds": 10,
  "ChatPrefix": "[BombDropSlay]",
  "DropChatMessage": "{player} dropped the C4! If nobody picks it up within {seconds} seconds, they will be slain.",
  "DropHudMessage": "{player} dropped the C4! If nobody picks it up within {seconds} seconds, they will be slain.",
  "CountdownChatMessage": "The C4 must be picked up within {seconds} seconds!",
  "CountdownHudMessage": "The C4 must be picked up within {seconds} seconds!",
  "SlayChatMessage": "You were slain for dropping the C4!",
  "SlayHudMessage": "You were slain for dropping the C4!"
}
```

`{player}` is replaced by the dropper's name and `{seconds}` by the remaining countdown. `CountdownSeconds` must be at least `1`. Chat messages support CounterStrikeSharp color tags such as `{Red}`, `{DarkRed}`, and `{Default}`.

Edit the file and reload the plugin for configuration changes to take effect. If a configuration file was created previously, update its message values too; existing settings are not overwritten automatically.
