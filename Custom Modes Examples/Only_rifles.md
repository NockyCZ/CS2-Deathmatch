<h1>IN THIS MODE, PLAYERS CAN USE ALL RIFLES EXPECT THE FAMAS AND GALILAR</h1>

- Add this in your custom_modes.json file and edit YOUR MODE ID for valid value
```
"YOUR MODE ID": {
    "mode_name": "Only Rifles",
    "armor": 1,
    "only_hs": false,
    "allow_knife_damage": true,
    "random_weapons": false,
    "allow_center_message": true,
    "center_message_text": "<font class='fontSize-l' color='orange'>Only Rifles</font>",
    "primary_weapons": [
        "weapon_ak47",
        "weapon_m4a1",
        "weapon_m4a1_silencer",
        "weapon_aug",
        "weapon_sg556"
    ],
    "secondary_weapons": []
}
```