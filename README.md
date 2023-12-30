# CS2 Deathmatch plugin
CS2 Deathmatch plugin for [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp). Minimum API version: 142

### Main features
- [CS2 Deathmatch plugin](#cs2-deathmatch-plugin)
    - [Main features](#main-features)
    - [To-do list](#to-do-list)
    - [Installation](#installation)
    - [Configuration](#configuration)
    - [Player Commands:](#player-commands)
    - [Admin Commands (permission: @css/root):](#admin-commands-permission-cssroot)
    - [How Weapon Selection works](#how-weapon-selection-works)
    - [Spawns Editor](#spawns-editor)
    - [Creating Custom Modes](#creating-custom-modes)
    - [Deathmatch Cvars](#deathmatch-cvars)

### To-do list
- Client preferences

<h1 align="center">Wiki</h1>

### Installation
1. Download the latest verison - https://github.com/NockyCZ/CS2-Deathmatch/releases
2. Unzip into your servers `csgo/addons/counterstrikesharp/plugins/` dir
3. Restart the server
4. Configure the config files and custom modes
<h1></h1>

### Configuration
```/game/csgo/addons/counterstrikesharp/configs/plugins/Deathmatch/Deathmatch.json```
|                             | What it does                                                                                   |
| --------------------------- | ---------------------------------------------------------------------------------------------- |
| `free_for_all`              | If the game will be FFA or Team. - `true` or `false`                                           |
| `custom_modes`              | Allow the custom modes (multicfg)? - `true` or `false`                                         |
| `custom_modes_interval`     | Works only if custom modes is enabled - `minutes`                                              |
| `random_selection_of_modes` | Will the modes be selected randomly? - `true` or `false`                                       |
| `map_start_custom_mode`     | Which custom mode will the map start with? - `ID of mode from custom_modes.json`               |
| `spawn_protection_time`     | How long will the spawn protection be? - `seconds`                                             |
| `round_restart_time`        | How long will it take for the round to restart when a new mode is selected - `seconds`         |
| `hide_round_seconds`        | Display the round timer? - `true` or `false`                                                   |
| `block_radio_messages`      | Block radio messages? - `true` or `false`                                                      |
| `remove_breakable_entities` | Remove breakable entities at the round start? - `true` or `false`                              |
| `reffil_ammo_kill`          | Refill ammo when a player eliminates someone? - `true` or `false`                              |
| `reffil_ammo_headshot`      | Refill ammo when a player eliminates someone with headshot? - `true` or `false`                |
| `refill_health_kill`        | How much health does a player regenerate when they eliminate someone? - `number`               |
| `refill_health_headshot`    | How much health does a player regenerate when they eliminate someone with headshot? - `number` |
<h1></h1>

### Player Commands:
`css_gun <WEAPON_NAME>` - Setup a weapon (alias /w; /weapon).<br>
`css_gun` - Show the list of allowed weapons for the current mode (alias /w; /weapon).

### Admin Commands (permission: @css/root):
`css_dm_startmode <ID>` - Start a custom mode<br>
<h1></h1>

### How Weapon Selection works
1. Players can set their primary and secondary weapons using the `/gun <WEAPON_NAME>` command. 
- The weapon name doesn't have to be complete. For example, to set the 'AK47', you only need to type 'ak'. 
- To display the list of allowed weapons for the current mode, simply type /gun without specifying a weapon name.
- If a player tries to set a blocked weapon, it will not be assigned to them.
- If a saved weapon is not in a allowed weapons list, a player will not receive it upon spawning.
<h1></h1>

### Spawns Editor
- Spawn points must be added more than originally present on the map, because the plugin overwrite the default spawns.
- So for example: If map de_mirage contains 10 CT spawns and 8 T spawns, so you need to create at least 10 CT spawns and 8 T spawns.
<br>

- Commands (permission: @css/root): <br>
`css_dm_editor` - Enable/disabled editor mode<br>
`css_dm_addspawn_ct` - Add a new CT spawn at your current position <br>
`css_dm_addspawn_t` - Add a new T spawn at your current position<br>
`css_dm_removespawn` - Remove a nearest spawn
<h1></h1>

### Creating Custom Modes
```/game/csgo/addons/counterstrikesharp/plugins/Deathmatch/custom_modes.json```
- The default mode is always [ID 0](https://i.imgur.com/mbmiOF6.png), so even if you have custom_modes turned off in the config, the game will follow the data set for the mode with [ID 0](https://i.imgur.com/mbmiOF6.png).
- When creating a custom mode, pay attention to the mode ID. Modes must be consecutive starting from 0, and no numbers should be skipped; otherwise, an error will occur.

- <b>Parameters for custom modes</b>
1. `mode_name` - What will the mode be named?
2. `armor` - What type of armor will players receive at spawn? <b>0</b> - None | <b>1</b> - Armor Only | <b>2</b> - Armor and Helmet
3. `only_hs` - Will this mode be only headshots? <b>true</b> or <b>false</b>
4. `allow_knife_damage` - Will knife damage be enabled? <b>true</b> or <b>false</b>
5. `random_weapons` - If you set this value to true, players won't be able to customize their weapons for this custom mode, and upon each spawn, they will receive a random weapon from primary/secondary_weapons. <b>true</b> or <b>false</b>
6. `allow_center_message` - Allow center message? <b>true</b> or <b>false</b>
7. `center_message_text` - What message will be displayed in the center message during the mode if allow_center_message is true? [Preview](https://i.imgur.com/rNNGcpa.png)
8. `primary_weapons` - List of available primary weapons for the custom mode
9. `secondary_weapons` - List of available secondary weapons for the custom mode

- <b>Some examples:</b> [Only Pistols but deagle is disabled](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_pistols.md) , [Only AK47 & Headshot](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_AK47.md) , [Only AWP](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_awp.md) , [Only Rifles but FAMAS and GALILAR is disabled](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_rifles.md) , [Only Shotguns with random weapons](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_shotguns.md)
<h1></h1>

### Deathmatch Cvars
```/game/csgo/addons/counterstrikesharp/plugins/Deathmatch/deathmatch_cvars.txt```
- The plugin automatically creates a `deathmatch_cvars.txt` file with pre-configured basic cvars for the proper functioning of your DM server. You can edit and set your own cvars in this file