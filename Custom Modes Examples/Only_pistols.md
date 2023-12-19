<h1>IN THIS MODE, PLAYERS CAN USE ALL PISTOLS EXPECT THE DEAGLE</h1>

- Add this in your custom_modes.json file and edit YOUR MODE ID for valid value
```
"YOUR MODE ID": {
    "mode_name": "Only Pistols",
    "armor": 1,
    "only_hs": false,
    "primary_weapon": "",
    "secondary_weapon": "",
    "allow_select_weapons": true,
    "weapons_type": "pistols",
    "allow_knife_damage": true,
    "allow_center_message": true,
    "center_message_text": "<font class='fontSize-l' color='green'>Only Pistols</font>",
    "blocked_weapons": "deagle_list"
}
```
- Add this in your blocked_weapons file
```
  "deagle_list": [
      "weapon_deagle"
    ]
```
