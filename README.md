<p align="center">
<b>Deathmatch + MutliCFG</b> plugin is a game mode where players respawn immediately after being killed, allowing them to practice shooting and movement without the typical match constraints. MutliCFG allows you to set up special modes that change the gameplay to different types such as Only Pistols, Only AWP, Rifles, etc.
Use gamemodes like <b>Custom</b> , <b>Casual</b> or <b>Deathmatch</b> for the plugin to work properly.<br>
Designed for <a href="https://github.com/roflmuffin/CounterStrikeSharp">CounterStrikeSharp</a> framework<br>
<br>
<a href="https://buymeacoffee.com/sourcefactory">
<img src="https://img.buymeacoffee.com/button-api/?text=Support Me&emoji=🚀&slug=sourcefactory&button_colour=e6005c&font_colour=ffffff&font_family=Lato&outline_colour=000000&coffee_colour=FFDD00" />
</a>
</p>

## [Documentation/Wiki](https://docs.sourcefactory.eu/cs2-plugins/deathmatch)
### Discord Support Server
[<img src="https://discordapp.com/api/guilds/1149315368465211493/widget.png?style=banner2">](https://discord.gg/Tzmq98gwqF)

### Main features
- [x] [Weapon Selection](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/weapons-selection)
- [x] [Creating Custom Modes](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/creating-custom-modes)
  - [x] [With Examples](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/creating-custom-modes#examples)
- [x] [Weapons Restrict](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/weapons-restrict)
- [x] Spawns Editor
- [x] [Configuration](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/configuration)
  - [x] [General Settings](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/configuration#general-settings-1)
  - [x] [Gameplay Settings](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/configuration#gameplay-settings-1)
  - [x] [Sounds Settings](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/configuration#sounds-settings-1)
  - [x] [Custom Commands](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/configuration#custom-commands-1)
  - [x] [Players Gameplay Settings](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/configuration#players-gameplay-settings-1)
    - [x] Spawn Protection
    - [x] Respawn Time
    - [x] Refill Ammo
    - [x] Refill Health
  - [x] [Client Preferences](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/configuration#players-gameplay-settings-1)
    - [x] Kill Sound
    - [x] Headshot Kill Sound
    - [x] Knife Kill Sound
    - [x] Hit Sound
    - [x] Only Headshot
    - [x] Hud Messages
    - [x] Damage Info
- [x] VIP Support
- [x] [Admin & Players Commands](https://docs.sourcefactory.eu/cs2-plugins/deathmatch/commands)

### Addons/Modules
- [Limited Zones](https://docs.sourcefactory.eu/deathmatch/modules/limited-zones)

### Installation
1. Download the latest verison - https://github.com/NockyCZ/CS2-Deathmatch/releases
2. Unzip into your servers `csgo/addons/counterstrikesharp/` dir
3. Restart the server
4. Configure the config files and custom modes
<h1></h1>

<details>
<summary><h2>Configuration</h2></summary>

```
{
  "Save Players Weapons": false,
  "Database Connection": {
    "Host": "",
    "Port": 3306,
    "User": "",
    "Database": "",
    "Password": "",
    "SslMode": "Preferred"
  },
  "Gameplay Settings": {
    "Free For All": true,
    "Custom Modes": true,
    "Game Length": 30,
    "Random Selection Of Modes": true,
    "Map Start Custom Mode": 0,
    "New Mode Countdown": 10,
    "Hud Type": 1,
    "Check Enemies Distance": true,
    "Distance From Enemies for Respawn": 500,
    "Default Weapons": 2,
    "Switch Weapons": true,
    "Allow Buymenu": true,
    "Use Default Spawns": false,
    "Respawn Players After New Mode": false,
    "Fast Weapon Equip": true,
    "Spawn Protection Color": ""
  },
  "General Settings": {
    "Hide Round Seconds": true,
    "Hide New Mode Countdown": false,
    "Block Radio Messages": true,
    "Block Player Ping": true,
    "Block Player ChatWheel": true,
    "Remove Breakable Entities": true,
    "Remove Decals": true,
    "Remove Kill Points Message": true,
    "Remove Respawn Sound": true,
    "Force Map End": false,
    "Restart Map On Plugin Load": false
  },
  "Sounds Settings": {
    "Weapon Cant Equip Sound": "sounds/ui/weapon_cant_buy.vsnd_c",
    "New Mode Sound": "sounds/music/3kliksphilip_01/bombtenseccount.vsnd_c"
  },
  "Custom Commands": {
    "Deatmatch Menu Commands": "dm,deathmatch",
    "Weapons Select Commands": "gun,weapon,w,g",
    "Weapons Select Shortcuts": "weapon_ak47:ak,weapon_m4a1:m4,weapon_m4a1_silencer:m4a1,weapon_awp:awp,weapon_usp_silencer:usp,weapon_glock:glock,weapon_deagle:deagle"
  },
  "Players Gameplay Settings": {
    "VIP Flag": "@css/vip",
    "Non VIP Players": {
      "Respawn Time": 1.5,
      "Spawn Protection Time": 1.1,
      "Reffil Ammo Kill": false,
      "Reffil Ammo Headshot": false,
      "Reffil Ammo in All Weapons": false,
      "Reffil Health Kill": 20,
      "Reffil Health Headshot": 40
    },
    "VIP Players": {
      "Respawn Time": 1.1,
      "Spawn Protection Time": 1.2,
      "Reffil Ammo Kill": false,
      "Reffil Ammo Headshot": false,
      "Reffil Ammo in All Weapons": false,
      "Reffil Health Kill": 25,
      "Reffil Health Headshot": 50
    }
  },
  "Client Preferences": {
    "Kill Sound": {
      "Enabled": true,
      "Sound path": "sounds/ui/armsrace_kill_01.vsnd_c",
      "Default value": false,
      "Only for VIP": false,
      "Command Shortcuts": []
    },
    "Headshot Kill Sound": {
      "Enabled": true,
      "Sound path": "sounds/buttons/bell1.vsnd_c",
      "Default value": false,
      "Only for VIP": false,
      "Command Shortcuts": []
    },
    "Knife Kill Sound": {
      "Enabled": true,
      "Sound path": "sounds/ui/armsrace_final_kill_knife.vsnd_c",
      "Default value": false,
      "Only for VIP": false,
      "Command Shortcuts": []
    },
    "Hit Sound": {
      "Enabled": true,
      "Sound path": "sounds/ui/csgo_ui_contract_type2.vsnd_c",
      "Default value": false,
      "Only for VIP": false,
      "Command Shortcuts": []
    },
    "Only Headshot": {
      "Enabled": true,
      "Default value": false,
      "Only for VIP": false,
      "Command Shortcuts": [
        "hs",
        "onlyhs"
      ]
    },
    "Hud Messages": {
      "Enabled": true,
      "Default value": true,
      "Only for VIP": false,
      "Command Shortcuts": [
        "hud"
      ]
    },
    "Damage Info": {
      "Enabled": true,
      "Default value": false,
      "Only for VIP": false,
      "Command Shortcuts": [
        "damage",
        "dmg"
      ]
    }
  },
  "Custom Modes": {
    "0": {
      "Name": "Default",
      "Interval": 300,
      "Armor": 1,
      "OnlyHS": false,
      "KnifeDamage": true,
      "RandomWeapons": false,
      "CenterMessageText": "",
      "PrimaryWeapons": [
        "weapon_aug",
        "weapon_sg556",
        "weapon_xm1014",
        "weapon_ak47",
        "weapon_famas",
        "weapon_galilar",
        "weapon_m4a1",
        "weapon_m4a1_silencer",
        "weapon_mp5sd",
        "weapon_mp7",
        "weapon_p90",
        "weapon_awp",
        "weapon_ssg08",
        "weapon_scar20",
        "weapon_g3sg1",
        "weapon_m249",
        "weapon_negev",
        "weapon_nova",
        "weapon_sawedoff",
        "weapon_mag7",
        "weapon_ump45",
        "weapon_bizon",
        "weapon_mac10",
        "weapon_mp9"
      ],
      "SecondaryWeapons": [
        "weapon_usp_silencer",
        "weapon_p250",
        "weapon_glock",
        "weapon_fiveseven",
        "weapon_hkp2000",
        "weapon_deagle",
        "weapon_tec9",
        "weapon_revolver",
        "weapon_elite"
      ],
      "Utilities": [
        "weapon_flashbang"
      ],
      "ExecuteCommands": []
    },
    "1": {
      "Name": "Only Headshot",
      "Interval": 300,
      "Armor": 1,
      "OnlyHS": true,
      "KnifeDamage": false,
      "RandomWeapons": false,
      "CenterMessageText": "\u003Cfont class=\u0027fontSize-l\u0027 color=\u0027orange\u0027\u003EOnly Headshot\u003C/font\u003E\u003Cbr\u003ENext Mode: {NEXTMODE} in {REMAININGTIME}\u003Cbr\u003E",
      "PrimaryWeapons": [
        "weapon_aug",
        "weapon_sg556",
        "weapon_xm1014",
        "weapon_ak47",
        "weapon_famas",
        "weapon_galilar",
        "weapon_m4a1",
        "weapon_m4a1_silencer",
        "weapon_mp5sd",
        "weapon_mp7",
        "weapon_p90"
      ],
      "SecondaryWeapons": [
        "weapon_usp_silencer",
        "weapon_p250",
        "weapon_glock",
        "weapon_fiveseven",
        "weapon_hkp2000",
        "weapon_deagle"
      ],
      "Utilities": [],
      "ExecuteCommands": []
    },
    "2": {
      "Name": "Only Deagle",
      "Interval": 120,
      "Armor": 2,
      "OnlyHS": false,
      "KnifeDamage": true,
      "RandomWeapons": false,
      "CenterMessageText": "\u003Cfont class=\u0027fontSize-l\u0027 color=\u0027green\u0027\u003EOnly Deagle\u003C/font\u003E\u003Cbr\u003ENext Mode: {NEXTMODE} in {REMAININGTIME}\u003Cbr\u003E",
      "PrimaryWeapons": [],
      "SecondaryWeapons": [
        "weapon_deagle"
      ],
      "Utilities": [
        "weapon_flashbang",
        "weapon_healthshot"
      ],
      "ExecuteCommands": []
    },
    "3": {
      "Name": "Only Pistols",
      "Interval": 180,
      "Armor": 1,
      "OnlyHS": false,
      "KnifeDamage": true,
      "RandomWeapons": false,
      "CenterMessageText": "\u003Cfont class=\u0027fontSize-l\u0027 color=\u0027blue\u0027\u003EOnly Pistols\u003C/font\u003E\u003Cbr\u003ENext Mode: {NEXTMODE} in {REMAININGTIME}\u003Cbr\u003E",
      "PrimaryWeapons": [],
      "SecondaryWeapons": [
        "weapon_usp_silencer",
        "weapon_p250",
        "weapon_glock",
        "weapon_cz75a",
        "weapon_elite",
        "weapon_fiveseven",
        "weapon_tec9",
        "weapon_hkp2000"
      ],
      "Utilities": [],
      "ExecuteCommands": []
    },
    "4": {
      "Name": "Only SMG",
      "Interval": 200,
      "Armor": 2,
      "OnlyHS": false,
      "KnifeDamage": true,
      "RandomWeapons": true,
      "CenterMessageText": "\u003Cfont class=\u0027fontSize-l\u0027 color=\u0027yellow\u0027\u003EOnly SMG (Random Weapons)\u003C/font\u003E\u003Cbr\u003ENext Mode: {NEXTMODE} in {REMAININGTIME}\u003Cbr\u003E",
      "PrimaryWeapons": [
        "weapon_p90",
        "weapon_bizon",
        "weapon_mp5sd",
        "weapon_mp7",
        "weapon_mp9",
        "weapon_mac10",
        "weapon_ump45"
      ],
      "SecondaryWeapons": [],
      "Utilities": [
        "weapon_hegrenade",
        "weapon_flashbang",
        "weapon_healthshot"
      ],
      "ExecuteCommands": []
    }
  },
  "Weapons Restrict": {
    "Global Restrict": true,
    "Weapons": {
      "weapon_ak47": {
        "0": {
          "VIP": {
            "CT": 6,
            "T": 6,
            "Global": 12
          },
          "NonVIP": {
            "CT": 5,
            "T": 5,
            "Global": 10
          }
        },
        "1": {
          "VIP": {
            "CT": 5,
            "T": 5,
            "Global": 7
          },
          "NonVIP": {
            "CT": 4,
            "T": 4,
            "Global": 5
          }
        }
      },
      "weapon_awp": {
        "0": {
          "VIP": {
            "CT": 3,
            "T": 3,
            "Global": 4
          },
          "NonVIP": {
            "CT": 2,
            "T": 2,
            "Global": 3
          }
        }
      }
    }
  },
  "ConfigVersion": 1
}
```
</details>
