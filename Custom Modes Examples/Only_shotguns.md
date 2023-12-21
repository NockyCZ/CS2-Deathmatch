<h1>IN THIS MODE, PLAYERS CAN USE ALL SHOTGUNS</h1>

- Add this in your custom_modes.json file and edit YOUR MODE ID for valid value
```
"YOUR MODE ID": {
    "mode_name": "Only Shotguns",
    "armor": 2,
    "only_hs": false,
    "primary_weapon": "",
    "secondary_weapon": "",
    "allow_select_weapons": true,
    "weapons_type": "shotguns",
    "allow_knife_damage": false,
    "allow_center_message": true,
    "center_message_text": "<font class='fontSize-l' color='purple'>Only Shotguns</font>",
    "blocked_weapons": "",
    "bot_settings": "OnlyShotgunsBOTS"
}
```
- Add this in your `bot_settings.json` file
```
    "OnlyShotgunsBOTS": {
      "primary weapons": [
        "weapon_mag7",
        "weapon_sawedoff",
        "weapon_nova",
        "weapon_xm1014"
      ],
      "secondary weapons": []
    }
```