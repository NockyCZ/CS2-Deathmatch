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
    "blocked_weapons": "rifles_list"
}
```
- Add this in your blocked_weapons file
```
  "rifles_list": [
      "weapon_galilar",
      "weapon_famas"
    ]
```
