<h1>IN THIS MODE, PLAYERS CAN USE ALL PISTOLS EXPECT THE DEAGLE AND GET GRENADE+FLASH</h1>

- Add this in your config file (`counterstrikesharp/configs/plugins/Deathmatch/Deathmatch.json`) and edit YOUR MODE ID for valid value
```
"YOUR MODE ID": {
    "Name": "Only Pistols",
    "Interval": 300,
    "Armor": 1,
    "OnlyHS": false,
    "KnifeDamage": true,
    "RandomWeapons": false,
    "CenterMessageText": "<font class='fontSize-l' color='green'>Only Pistols</font>",
    "PrimaryWeapons": [],
    "SecondaryWeapons": [
        "weapon_usp_silencer",
        "weapon_p250",
        "weapon_glock",
        "weapon_cz75a",
        "weapon_elite",
        "weapon_fiveseven",
        "weapon_hkp2000",
        "weapon_tec9"
    ],
    "Utilities": [
        "weapon_flashbang",
        "weapon_hegrenade
    ],
    "ExecuteCommands": []
}
```

