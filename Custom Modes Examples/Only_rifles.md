<h1>IN THIS MODE, PLAYERS CAN USE ALL RIFLES EXPECT THE FAMAS AND GALILAR</h1>

- Add this in your custom_modes.json file and edit YOUR MODE ID for valid value
```
"YOUR MODE ID": {
    "mode_name": "Only Rifles",
    "armor": 1,
    "only_hs": false,
    "primary_weapon": "",
    "secondary_weapon": "",
    "allow_select_weapons": true,
    "weapons_type": "rifles",
    "allow_knife_damage": true,
    "allow_center_message": true,
    "center_message_text": "<font class='fontSize-l' color='orange'>Only Rifles</font>",
    "blocked_weapons": "rifles_list",
    "bot_settings": "OnlyRiflesBOTS"
}
```
- Add this in your `blocked_weapons.json` file
```
  "rifles_list": [
      "weapon_galilar",
      "weapon_famas"
    ]
```
- Add this in your `bot_settings.json` file
```
    "OnlyRiflesBOTS": {
      "primary weapons": [
        "weapon_ak47",
        "weapon_m4a1",
        "weapon_m4a1_silencer",
        "weapon_aug",
        "weapon_sg556"
      ],
      "secondary weapons": []
    }
```
