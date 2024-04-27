<h1>IN THIS MODE, PLAYERS CAN USE ONLY AWP AND GET FLASH</h1>

- Add this in your config file (`counterstrikesharp/configs/plugins/Deathmatch/Deathmatch.json`) and edit YOUR MODE ID for valid value
```
"YOUR MODE ID": {
    "Name": "Only AWP",
    "Interval": 300,
    "Armor": 1,
    "OnlyHS": false,
    "KnifeDamage": false,
    "RandomWeapons": false,
    "CenterMessageText": "<font class='fontSize-l' color='red'>Only AWP</font>",
    "PrimaryWeapons": [
        "weapon_awp"
    ],
    "SecondaryWeapons": [],
    "Utilities": [
        "weapon_flashbang"
    ],
    "ExecuteCommands": []
}
```
