# CS2 Deathmatch plugin
CS2 Deathmatch plugin for [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp). Minimum API version: 166<br>
Use gamemodes like `Custom` or `Casual` for the plugin to work properly.<br>
Set the map duration according to `mp_timelimit`, the roundtime is also determined by this cvar.

> LAST UPDATE OF WIKI IS FOR VERSION 1.0.4 (Will be updated soon)

### Main features
1. [Weapon Selection](#how-weapon-selection-works)
2. [Spawns Editor](#spawns-editor)
3. [Creating Custom Modes](#creating-custom-modes)
4. [Weapons Restrict](#weapons-restrict)
5. [Deathmatch Cvars](#deathmatch-cvars)
6. [Admin](#admin-commands-permission-cssroot) & [Player](#player-commands) Commands
7. Free For All (FFA)
8. Spawn protection
9. Refill ammo or health per kill/headshot
10. VIP Support
etc...

### To-do list
- Client preferences
- Storing players weapons for different modes in the database

<h1 align="center">Wiki</h1>

### Installation
1. Download the latest verison - https://github.com/NockyCZ/CS2-Deathmatch/releases
2. Unzip into your servers `csgo/addons/counterstrikesharp/plugins/` dir
3. Restart the server
4. Configure the config files and custom modes
<h1></h1>

### Configuration
```/game/csgo/addons/counterstrikesharp/configs/plugins/Deathmatch/Deathmatch.json```
| Deathmatch Settings                 | What it does                                                                                    |
| ----------------------------------- | ----------------------------------------------------------------------------------------------- |
| `free_for_all`                      | If the game will be FFA or Team. - `true` or `false`                                            |
| `custom_modes`                      | Allow the custom modes (multicfg)? - `true` or `false`                                          |
| `random_selection_of_modes`         | Will the modes be selected randomly? - `true` or `false`                                        |
| `map_start_custom_mode`             | Which custom mode will the map start with? - `ID of mode from custom_modes.json`                |
| `new_mode_countdown`                | At what second does the countdown for the new mode appear? - `seconds`                          |
| `check_enemies_distance`            | Checking the distance from opponents to disable a specific spawn? - `true` or `false`           |
| `distance_from_enemies_for_respawn` | If <b>check_enemies_distance</b> is true, what distance will be checked? - `number`             |
| `default_weapons`                   | ┌ What weapons will a player get if they don't have any weapons configured?                     |
|                                     | ├ `0` - None                                                                                    |
|                                     | ├ `1` - The first weapon from primary/secondary weapons list in the current mode                |
|                                     | └ `2` - A Random weapon from primary/secondary weapons list in the current mode                 |
| `respawn_players_after_new_mode`    | Respawn all players at the start of a new mode? - `true` or `false`                             |
| `hide_round_seconds`                | Display the round timer? - `true` or `false`                                                    |
| `block_radio_messages`              | Block radio messages? - `true` or `false`                                                       |
| `remove_breakable_entities`         | Remove breakable entities at the round start? - `true` or `false`                               |
| `remove_decals_after_death`         | Remove all decals upon player death? (Blood, bullets, etc.) - `true` or `false`                 |
| `weapons_select_shortcuts`          | Create your own commands for setting weapons - `weapon_name:shortcut` example: `weapon_ak47:ak` |


| Players Settings               | What it does                                                                                   |
| ------------------------------ | ---------------------------------------------------------------------------------------------- |
| `VIP_Flag`                     | Flag for VIP players                                                                           |
| `(VIP_)respawn_time`           | How long does it take to respawn a player? - `float`                                           |
| `(VIP_)spawn_protection_time`  | How long will the spawn protection be? - `float`                                               |
| `(VIP_)reffil_ammo_kill`       | Refill ammo when a player eliminates someone? - `true` or `false`                              |
| `(VIP_)reffil_ammo_headshot`   | Refill ammo when a player eliminates someone with headshot? - `true` or `false`                |
| `(VIP_)refill_health_kill`     | How much health does a player regenerate when they eliminate someone? - `number`               |
| `(VIP_)refill_health_headshot` | How much health does a player regenerate when they eliminate someone with headshot? - `number` |

<h1></h1>

### Player Commands:
`css_gun <WEAPON_NAME>` - Setup a weapon (alias /w; /weapon; /guns)<br>
`css_gun` - Show the list of allowed weapons for the current mode (alias /w; /weapon; /guns)<br>
And if you have created custom command shortcuts, all players will be able to use them.

### Admin Commands (permission: @css/root):
`css_dm_startmode <ID>` - Start a custom mode<br>
<h1></h1>

### How Weapon Selection works
1. Players can set their primary and secondary weapons using the `/gun <WEAPON_NAME>` command. 
- The weapon name doesn't have to be complete. For example, to set the 'AK47', you only need to type 'ak'. 
- To display the list of allowed weapons for the current mode, simply type /gun without specifying a weapon name.

<br>

- <b>Additional informations about weapons:</b>
- If a player doesn't have any weapon set for the current mode, they will get a weapon according to your configuration in the <b>default_weapons</b> section.
- If a player tries to set a blocked weapon, it will not be assigned to them.
- If a saved weapon is not in a allowed weapons list, a player will not get it upon spawning.
- Bots will always randomly get weapons from your primary/secondary weapons list for the current mode.
<h1></h1>

### Spawns Editor
- The spawns will be correctly loaded only upon map reload. Therefore, after setting up your spawns, refresh the map.
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
  
<br>

- <b>Parameters for custom modes</b>
1. `mode_name` - What will the mode be named?
2. `mode_interval` - Duration of individual modes? <b>seconds</b> - If custom_modes are false, then the interval is turned off.
3. `armor` - What type of armor will players receive at spawn? <b>0</b> - None | <b>1</b> - Armor Only | <b>2</b> - Armor and Helmet
4. `only_hs` - Will this mode be only headshots? <b>true</b> or <b>false</b>
5. `allow_knife_damage` - Will knife damage be enabled? <b>true</b> or <b>false</b>
6. `random_weapons` - If you set this value to true, players won't be able to customize their weapons for this custom mode, and upon each spawn, they will receive a random weapon from primary/secondary_weapons. <b>true</b> or <b>false</b>. Also, if default_weapons is set to 0 or 1, players will still randomly get weapons
7. `allow_center_message` - Allow center message? <b>true</b> or <b>false</b>
8. `center_message_text` - What message will be displayed in the center message during the mode if allow_center_message is true? [Preview](https://i.imgur.com/rNNGcpa.png)
9. `primary_weapons` - List of available primary weapons for the custom mode
10. `secondary_weapons` - List of available secondary weapons for the custom mode

<br>

- <b>List of weapons that must be in the same list:</b>
1. `weapon_usp_silencer` and `weapon_hkp2000`
2. `weapon_mp5sd` and `weapon_mp7`
- If these weapons are not in the same weapons list (primary/secondary), an error may occur where players are not given any weapons.

<b>Some examples:</b> [Only Pistols expect Deagle](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_pistols.md) , [Only AK47 & Headshot](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_AK47.md) , [Only AWP](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_awp.md) , [Only Rifles expect FAMAS and GALILAR](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_rifles.md) , [Only Shotguns with random weapons](https://github.com/NockyCZ/CS2-Deathmatch/blob/main/Custom%20Modes%20Examples/Only_shotguns.md)
<h1></h1>

### Weapons Restrict
```/game/csgo/addons/counterstrikesharp/plugins/Deathmatch/weapons_restrict.json```
- You can set weapons restrictions for individual modes. 
- You can set values for individual teams or for all. 
- Additionally, the restriction is divided into nonVIP and VIP players.
- Weapons restrict does not apply to bots!

<b>How to restrict certain weapon ?</b>
- If the weapon is not in your primary/secondary weapons list for current custom mode, you don't need to add that weapon to the restrict.
- You cannot combine Team or All weapons restrict! It must always be set either to Team or All.
- If you set the value to 0, it means the weapon is not restricted.
- If `random_weapons` are true in a specific custom mode, then all weapons in that custom mode are not restricted.

<b>1. Weapon restrict for all players:</b>
```  
  "weapon_awp": [
    {
      "MODE ID": "nonVIP,VIP",
      "1": "1,2",    // IN MODE WITH ID 1 - THE WEAPON IS RESTRICTED TO 1 FOR NONVIP PLAYERS | THE WEAPON IS RESTRICTED TO 2 FOR VIP PLAYERS
      "2": "2,4",    // IN MODE WITH ID 2 - THE WEAPON IS RESTRICTED TO 2 FOR NONVIP PLAYERS | THE WEAPON IS RESTRICTED TO 4 FOR VIP PLAYERS
                     // For example, in the mode with ID 3, this weapon is not in the primary/secondary weapons list, so we don't need to write it!
      "4": "5,0"     // IN MODE WITH ID 4 - THE WEAPON IS RESTRICTED TO 5 FOR NONVIP PLAYERS | THE WEAPON IS NOT RESTRICTED FOR VIP PLAYERS
    }
  ]
```
<b>2. Weapon restrict for teams:</b>
```  
  "weapon_awp": [
    {
      "MODE ID": "ct:nonVIP,VIP|t:nonVIP,VIP",
      "1": "ct:1,2|t:2,3",    // IN MODE WITH ID 1 - THE WEAPON IS RESTRICTED FOR CT TEAM TO 1 FOR NONVIP AND TO 2 FOR VIP | FOR T TEAM TO 2 FOR NONVIP AND TO 3 FOR VIP
      "2": "ct:2,3|t:3,0",    // IN MODE WITH ID 2 - THE WEAPON IS RESTRICTED FOR CT TEAM TO 2 FOR NONVIP AND TO 3 FOR VIP | FOR T TEAM TO 3 FOR NONVIP AND FOR VIP IS NOT RESTRICTED
                              // For example, in the mode with ID 3, this weapon is not in the primary/secondary weapons list, so we don't need to write it!
      "4": "ct:2,3|t:0,0"     // IN MODE WITH ID 4 - THE WEAPON IS RESTRICTED FOR CT TEAM TO 2 FOR NONVIP AND TO 3 FOR VIP | THE WEAPON IS NOT RESTRICTED FOR T TEAM
    }
  ]
```
<h1></h1>

### Deathmatch Cvars
```/game/csgo/addons/counterstrikesharp/plugins/Deathmatch/deathmatch_cvars.txt```
- The plugin automatically creates a `deathmatch_cvars.txt` file with pre-configured basic cvars for the proper functioning of your DM server. You can edit and set your own cvars in this file
