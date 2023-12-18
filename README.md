# CS2 Deathmatch plugin
CS2 Deathmatch plugin for [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp). It is also possible to set up a whitelist for specific steamid64, on which VPN or Country blocking will not take effect.
Custom Deathmatch CS2 plugin (Includes custom spawnpoints, multicfg, gun selection, spawn protection, etc).

### Main features
- Weapon selection
- Spawns editor
- Creating custom modes (multicfg)
- Weapon restrict for custom modes
- Free For All (FFA)
- Spawn protection
- Refill ammo or health per kill/headshot/killstreak

### to-do-list
- Client preferences

<h1 align="center">Wiki</h1>

### Installation
1. Unzip into your servers `csgo/addons/counterstrikesharp/plugins/` dir
2. Restart server
3. Configure config files and custom modes

### How Weapon Selection works
- Players can set their primary and secondary weapons using the buy menu.
- They just need to purchase a weapon once, and it will be saved.
- If the saved weapon is in the blocked weapons, the player will not receive it upon spawning.

### Spawns Editor
- Spawn points must be added more than originally present on the map, because the plugin overwrite the default spawns.
- So for example: If map de_mirage contains 10 CT spawns and 8 T spawns, o, you need to create at least 10 CT spawns and 8 T spawns.
<br>

- Commands (permission: @css/root): <br>
`css_dm_editor` - Enable/disabled editor mode<br>
`css_dm_addspawn_ct` - Add a new spawn for CT team<br>
`css_dm_addspawn_t` - Add a new spawn for T team<br>
`css_dm_removespawn` - Remove a nearest spawn

