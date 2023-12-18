# CS2 Deathmatch plugin
CS2 Deathmatch plugin for [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp). It is also possible to set up a whitelist for specific steamid64, on which VPN or Country blocking will not take effect.
Custom Deathmatch CS2 plugin (Includes custom spawnpoints, multicfg, gun selection, spawn protection, etc).

### Main features
- [Weapon selection](#how-weapon-selection-works)
- (Spawns editor)(#spawns-editor)
- (Creating custom modes (multicfg))[#creating-custom-modes]
- (Weapon restrict for custom modes)[#blocked-weapons-list]
- Free For All (FFA)
- Spawn protection
- Refill ammo or health per kill/headshot/killstreak

### To-do list
- Client preferences

<h1 align="center">Wiki</h1>

### Installation
1. Unzip into your servers `csgo/addons/counterstrikesharp/plugins/` dir
2. Restart server
3. Configure config files and custom modes

### Configuration
```/game/csgo/addons/counterstrikesharp/configs/plugins/Deathmatch/Deathmatch.json```
|   | What it does |
| ------------- | ------------- |
| `free_for_all`  | If the game will be FFA or Team. - `true` or `false` |
| `custom_modes`  | Allow the custom modes (multicfg)? - `true` or `false` |
| `custom_modes_interval` | Works only if custom modes is enabled - `minutes` |
| `map_start_custom_mode` | Which custom mode will the map start with? - `ID of mode from custom_modes.json` |
| `spawn_protection_time` | How long will the spawn protection be? - `seconds` |
| `round_restart_time` | How long will it take for the round to restart when a new mode is selected - `seconds` |
| `hide_round_seconds` | Display the round timer? - `true` or `false` |
| `reffil_ammo_kill` | Refill ammo when a player eliminates someone? - `true` or `false` |
| `reffil_ammo_headshot` | Refill ammo when a player eliminates someone with headshot? - `true` or `false` |
| `refill_health_kill` | How much health does a player regenerate when they eliminate someone? - `number` |
| `refill_health_headshot` | How much health does a player regenerate when they eliminate someone with headshot? - `number` |

### How Weapon Selection works
- Players can set their primary and secondary weapons using the buy menu.
- They just need to purchase a weapon once, and it will be saved.
- If the saved weapon is in the blocked weapons, the player will not receive it upon spawning.

### Spawns Editor
- Spawn points must be added more than originally present on the map, because the plugin overwrite the default spawns.
- So for example: If map de_mirage contains 10 CT spawns and 8 T spawns, so you need to create at least 10 CT spawns and 8 T spawns.
<br>

- Commands (permission: @css/root): <br>
`css_dm_editor` - Enable/disabled editor mode<br>
`css_dm_addspawn_ct` - Add a new spawn for CT team<br>
`css_dm_addspawn_t` - Add a new spawn for T team<br>
`css_dm_removespawn` - Remove a nearest spawn

### Creating Custom Modes

### Blocked Weapons List
