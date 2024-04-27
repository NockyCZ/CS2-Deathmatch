<h1>IN THIS MODE, PLAYERS CAN USE ALL RIFLES EXPECT THE FAMAS AND GALILAR</h1>

- Add this in your config file (`counterstrikesharp/configs/plugins/Deathmatch/Deathmatch.json`) and edit YOUR MODE ID for valid value
```
"YOUR MODE ID": {
    "Name": "Only Rifles",
    "Interval": 300,
    "Armor": 1,
    "OnlyHS": false,
    "KnifeDamage": true,
    "RandomWeapons": false,
    "CenterMessageText": "<font class='fontSize-l' color='orange'>Only Rifles</font>",
    "PrimaryWeapons": [
        "weapon_ak47",
        "weapon_m4a1",
        "weapon_m4a1_silencer",
        "weapon_aug",
        "weapon_sg556"
    ],
    "SecondaryWeapons": [],
    "Utilities": [],
    "ExecuteCommands": []
}
```