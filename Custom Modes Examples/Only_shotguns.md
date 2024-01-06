<h1>IN THIS MODE, PLAYERS CAN USE ALL SHOTGUNS AND PLAYERS CANT SELECT THEIR WEAPONS (PLAYER WILL GET RANDOM SHOTGUN EVERY SPAWN)</h1>

- Add this in your custom_modes.json file and edit YOUR MODE ID for valid value
```
"YOUR MODE ID": {
    "mode_name": "Only Shotguns",
    "mode_interval": 300,
    "armor": 2,
    "only_hs": false,
    "allow_knife_damage": false,
    "random_weapons": true,
    "allow_center_message": true,
    "center_message_text": "<font class='fontSize-l' color='purple'>Only Shotguns</font>",
    "primary_weapons": [
        "weapon_mag7",
        "weapon_sawedoff",
        "weapon_nova",
        "weapon_xm1014"
    ],
    "secondary_weapons": []
}
```