using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Newtonsoft.Json.Linq;

namespace Deathmatch
{
    public partial class DeathmatchCore
    {
        public class RestrictedWeaponsInfo
        {
            public int nonVIPRestrict { get; set; } = 0; //T or Global
            public int VIPRestrict { get; set; } = 0; //T or Global
            public int nonVIPRestrict_Team { get; set; } = 0; //CT
            public int VIPRestrict_Team { get; set; } = 0; //CT
        }

        readonly HashSet<string> SecondaryWeaponsList = new HashSet<string>
        {
            "weapon_hkp2000", "weapon_cz75a", "weapon_deagle", "weapon_elite",
            "weapon_fiveseven", "weapon_glock", "weapon_p250",
            "weapon_revolver", "weapon_tec9", "weapon_usp_silencer"
        };

        readonly HashSet<string> PrimaryWeaponsList = new HashSet<string>
        {
            "weapon_mag7", "weapon_nova", "weapon_sawedoff", "weapon_xm1014",
            "weapon_m249", "weapon_negev", "weapon_mac10", "weapon_mp5sd",
            "weapon_mp7", "weapon_mp9", "weapon_p90", "weapon_bizon",
            "weapon_ump45", "weapon_ak47", "weapon_aug", "weapon_famas",
            "weapon_galilar", "weapon_m4a1_silencer", "weapon_m4a1", "weapon_sg556",
            "weapon_awp", "weapon_g3sg1", "weapon_scar20", "weapon_ssg08"
        };

        readonly Dictionary<string, string> weaponSelectMapping = new Dictionary<string, string>
        {
            { "m4a4", "weapon_m4a1" },
            { "weapon_m4a1", "weapon_m4a1" },
            { "m4a1_silencer", "weapon_m4a1_silencer" },
            { "m4a1", "weapon_m4a1_silencer" }
        };

        List<string> AllowedPrimaryWeaponsList = new List<string>();
        List<string> AllowedSecondaryWeaponsList = new List<string>();
        public static Dictionary<string, RestrictedWeaponsInfo> RestrictedWeapons = new Dictionary<string, RestrictedWeaponsInfo>();
        public static JObject? weaponsRestrictData { get; private set; }

        public bool CheckIsWeaponRestricted(string weaponName, bool isVIP, int team)
        {
            bool bPrimary = PrimaryWeaponsList.Contains(weaponName);
            int matchingCount = 0;
            if (bPrimary)
            {
                if (AllowedPrimaryWeaponsList.Count == 1)
                    return false;
            }
            else
            {
                if (AllowedSecondaryWeaponsList.Count == 1)
                    return false;
            }
            int restrictValue = 0;
            if (RestrictedWeapons.ContainsKey(weaponName))
            {
                RestrictedWeaponsInfo restrict = RestrictedWeapons[weaponName];

                if (g_bWeaponRestrictGlobal)
                {
                    restrictValue = isVIP ? restrict.VIPRestrict : restrict.nonVIPRestrict;
                    if (restrictValue == 0)
                        return false;
                    else if (restrictValue < 0)
                        return true;

                    foreach (var p in Utilities.GetPlayers().Where(p => playerData.ContainsPlayer(p)))
                    {
                        if (playerData.ContainsPlayer(p))
                        {
                            if (bPrimary)
                            {
                                if (playerData[p].PrimaryWeapon == weaponName)
                                    matchingCount++;
                            }
                            else
                            {
                                if (playerData[p].SecondaryWeapon == weaponName)
                                    matchingCount++;
                            }
                        }
                    }
                }
                else
                {
                    switch (team)
                    {
                        case 2:
                            restrictValue = isVIP ? restrict.VIPRestrict_Team : restrict.nonVIPRestrict_Team;
                            if (restrictValue == 0)
                                return false;
                            else if (restrictValue < 0)
                                return true;
                            break;

                        case 3:
                            restrictValue = isVIP ? restrict.VIPRestrict : restrict.nonVIPRestrict;
                            if (restrictValue == 0)
                                return false;
                            else if (restrictValue < 0)
                                return true;
                            break;

                        default:
                            return true;
                    }

                    foreach (var p in Utilities.GetPlayers().Where(p => playerData.ContainsPlayer(p) && p.TeamNum == team))
                    {
                        if (bPrimary)
                        {
                            if (playerData[p].PrimaryWeapon == weaponName)
                                matchingCount++;
                        }
                        else
                        {
                            if (playerData[p].SecondaryWeapon == weaponName)
                                matchingCount++;
                        }
                    }
                }
                return matchingCount >= restrictValue;
            }
            return false;
        }

        public void CheckIsValidWeaponsInList<T>(List<T> setupedWeaponsList, HashSet<T> weaponsList)
        {
            foreach (var weapon in setupedWeaponsList)
            {
                if (!weaponsList.Contains(weapon))
                {
                    SendConsoleMessage($"[Deathmatch] Invalid weapon name: {weapon} (Mode ID: {g_iActiveMode})", ConsoleColor.Red);
                }
                else
                {
                    if (weapon != null)
                    {
                        string restrictValues = GetWeaponRestrict(weapon.ToString()!, g_iActiveMode.ToString());
                        if (!string.IsNullOrEmpty(restrictValues))
                        {
                            if (restrictValues.Contains("ct") && restrictValues.Contains("t"))
                            {
                                g_bWeaponRestrictGlobal = false;
                                string[] teamValues = restrictValues.Split('|');
                                foreach (string teamValue in teamValues)
                                {
                                    string[] parts = teamValue.Split(':');

                                    if (parts.Length == 2)
                                    {
                                        string team = parts[0].ToLower();
                                        string[] values = parts[1].Split(',');

                                        if (values.Length == 2)
                                        {
                                            if (team == "ct")
                                            {
                                                RestrictedWeapons.Add(weapon.ToString()!, new RestrictedWeaponsInfo { nonVIPRestrict = int.Parse(values[0]), VIPRestrict = int.Parse(values[1]) });
                                                //SendConsoleMessage($"[Deathmatch] Weapon '{weapon}' is restricted for CT Team ({values[0]} | VIP: {values[1]})", ConsoleColor.Green);
                                            }
                                            else if (team == "t")
                                            {
                                                RestrictedWeapons.Add(weapon.ToString()!, new RestrictedWeaponsInfo { nonVIPRestrict_Team = int.Parse(values[0]), VIPRestrict_Team = int.Parse(values[1]) });
                                                //SendConsoleMessage($"[Deathmatch] Weapon '{weapon}' is restricted for T Team ({values[0]} | VIP: {values[1]})", ConsoleColor.Green);
                                            }
                                        }
                                        else
                                        {
                                            SendConsoleMessage($"[Deathmatch] Wrong configuration in weapons_restrict.json for '{weapon}'", ConsoleColor.Red);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                g_bWeaponRestrictGlobal = true;
                                string[] globalValues = restrictValues.Split(',');
                                if (globalValues.Length == 2)
                                {
                                    RestrictedWeapons.Add(weapon.ToString()!, new RestrictedWeaponsInfo { nonVIPRestrict = int.Parse(globalValues[0]), VIPRestrict = int.Parse(globalValues[1]) });
                                    //SendConsoleMessage($"[Deathmatch] Weapon '{weapon}' is restricted for All players ({globalValues[0]} | VIP: {globalValues[1]})", ConsoleColor.Green);
                                }
                                else
                                {
                                    SendConsoleMessage($"[Deathmatch] Wrong configuration in weapons_restrict.json for '{weapon}'", ConsoleColor.Red);
                                }
                            }
                        }
                        /*else
                        {
                            Server.PrintToConsole($"{weapon} is not restricted for this mode (ID: {g_iActiveMode})");
                        }*/
                    }
                }
            }
        }
        public string GetWeaponFromSlot(CCSPlayerController player, int slot)
        {
            if (player.PlayerPawn == null || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value.WeaponServices == null)
                return null!;

            foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
            {
                if (weapon != null && weapon.IsValid)
                {
                    if (slot == 1)
                    {
                        if (PrimaryWeaponsList.Contains(weapon.Value!.DesignerName))
                        {
                            return weapon.Value!.DesignerName;
                        }
                    }
                    else if (slot == 2)
                    {
                        if (SecondaryWeaponsList.Contains(weapon.Value!.DesignerName))
                        {
                            return weapon.Value!.DesignerName;
                        }
                    }
                }
            }
            return null!;
        }
        public int IsHaveWeaponFromSlot(CCSPlayerController player, int slot)
        {
            if (!player.IsValid && player == null && player!.PlayerPawn == null || player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null || !player.PawnIsAlive)
                return 3;

            foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
            {
                if (weapon != null && weapon.IsValid)
                {
                    switch (slot)
                    {
                        case 0:
                            if (SecondaryWeaponsList.Contains(weapon.Value!.DesignerName))
                                return 2;
                            else if (PrimaryWeaponsList.Contains(weapon.Value!.DesignerName))
                                return 1;
                            break;
                        case 1:
                            if (PrimaryWeaponsList.Contains(weapon.Value!.DesignerName))
                                return 1;
                            break;
                        case 2:
                            if (SecondaryWeaponsList.Contains(weapon.Value!.DesignerName))
                                return 2;
                            break;
                    }
                }
            }
            return 3;
        }
        private string GetRandomWeaponFromList(List<string> weaponsList)
        {
            Random rand = new Random();
            int index = rand.Next(weaponsList.Count);
            return weaponsList[index];
        }

        public void LoadWeaponsRestrict(string filepath)
        {
            if (!File.Exists(filepath))
            {
                //SendConsoleMessage($"[Deathmatch] No weapons_restrict file found! (Deathmatch/weapons_restrict.json)", ConsoleColor.Red);
                JObject weaponData = new JObject
                {
                    ["weapon_ak47"] = new JArray
                    {
                        new JObject
                        {
                            ["0"] = "7,0",
                            ["1"] = "8,10"
                        }
                    },
                    ["weapon_usp_silencer"] = new JArray
                    {
                        new JObject
                        {
                            ["0"] = "5,10",
                            ["1"] = "10,0",
                            ["3"] = "6,8"
                        }
                    }
                };

                File.WriteAllText(filepath, weaponData.ToString());
                var jsonData = File.ReadAllText(filepath);
                weaponsRestrictData = JObject.Parse(jsonData);
            }
            else
            {
                var jsonData = File.ReadAllText(filepath);
                weaponsRestrictData = JObject.Parse(jsonData);
            }
        }
        public string GetWeaponRestrict(string weapon, string modetype)
        {
            if (weaponsRestrictData != null)
            {
                return weaponsRestrictData[weapon]?[0]?[modetype]?.ToString() ?? "";
            }
            return "";
        }
    }
}