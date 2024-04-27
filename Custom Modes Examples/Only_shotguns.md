<h1>IN THIS MODE, PLAYERS CAN USE ALL SHOTGUNS AND PLAYERS CANT SELECT THEIR WEAPONS (PLAYER WILL GET RANDOM SHOTGUN EVERY SPAWN)</h1>

- Add this in your config file (`counterstrikesharp/configs/plugins/Deathmatch/Deathmatch.json`) and edit YOUR MODE ID for valid value
```
"YOUR MODE ID": {
    "Name": "Only Shotguns",
    "Interval": 300,
    "Armor": 2,
    "OnlyHS": false,
    "KnifeDamage": false,
    "RandomWeapons": true,
    "CenterMessageText": "<font class='fontSize-l' color='purple'>Only Shotguns</font>",
    "PrimaryWeapons": [
        "weapon_mag7",
        "weapon_sawedoff",
        "weapon_nova",
        "weapon_xm1014"
    ],
    "SecondaryWeapons": [],
    "Utilities": [],
    "ExecuteCommands": []
}
```