using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public bool CheckIsWeaponRestricted(string weaponName, bool isVIP, CsTeam team)
        {
            bool bPrimary = PrimaryWeaponsList.Contains(weaponName);
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
            if (RestrictedWeapons.ContainsKey(weaponName))
            {
                int restrictValue = GetWeaponRestrict(weaponName, isVIP, team);
                int matchingCount = 0;

                if (Config.WeaponsRestrict.Global)
                {
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
                    if (restrictValue == 0)
                        return false;
                    else if (restrictValue < 0)
                        return true;

                    foreach (var p in Utilities.GetPlayers().Where(p => playerData.ContainsPlayer(p) && p.Team == team))
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

        public CBasePlayerWeapon? GetWeaponFromSlot(CCSPlayerController player, gear_slot_t slot)
        {
            if (player.PlayerPawn == null || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value.WeaponServices == null)
                return null;

            return player.PlayerPawn.Value.WeaponServices.MyWeapons
                .Select(weapon => weapon.Value?.As<CCSWeaponBase>())
                .Where(weaponBase => weaponBase != null && weaponBase.VData != null && weaponBase.VData.GearSlot == slot)
                .FirstOrDefault();
        }

        public void GiveOrReplaceWeapons(CCSPlayerController player, string PrimaryWeapon, string SecondaryWeapon)
        {
            if (player == null || !player.IsValid || player.PlayerPawn == null || player.PlayerPawn.Value == null || !player.PawnIsAlive)
                return;

            var weaponServices = player.PlayerPawn.Value.WeaponServices;
            if (weaponServices == null)
                return;
            var weaponsList = weaponServices.MyWeapons.Select(weapon => weapon.Value?.As<CCSWeaponBase>()).ToList();
            if (weaponsList == null)
                return;

            var weapon = weaponsList
                .Where(weaponBase => weaponBase != null && weaponBase.VData != null && weaponBase.VData.GearSlot == gear_slot_t.GEAR_SLOT_RIFLE)
                .FirstOrDefault();

            if (weapon == null)
            {
                if (string.IsNullOrEmpty(PrimaryWeapon))
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SetupWeaponsCommand"]}");
                else
                    player.GiveNamedItem(PrimaryWeapon);
            }
            else
            {
                if (string.IsNullOrEmpty(PrimaryWeapon))
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SetupWeaponsCommand"]}");
                else
                {
                    player.RemoveItemByDesignerName(weapon.DesignerName, false);
                    player.GiveNamedItem(PrimaryWeapon);
                }
            }
            
            weapon = weaponsList
                .Where(weaponBase => weaponBase != null && weaponBase.VData != null && weaponBase.VData.GearSlot == gear_slot_t.GEAR_SLOT_PISTOL)
                .FirstOrDefault();

            if (weapon == null)
            {
                if (string.IsNullOrEmpty(SecondaryWeapon))
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SetupWeaponsCommand"]}");
                else
                    player.GiveNamedItem(SecondaryWeapon);
            }
            else
            {
                if (string.IsNullOrEmpty(SecondaryWeapon))
                    player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SetupWeaponsCommand"]}");
                else
                {
                    player.RemoveItemByDesignerName(weapon.DesignerName, false);
                    player.GiveNamedItem(SecondaryWeapon);
                }
            }

            weapon = weaponsList
                .Where(weaponBase => weaponBase != null && weaponBase.VData != null && weaponBase.VData.GearSlot == gear_slot_t.GEAR_SLOT_KNIFE)
                .FirstOrDefault();
            if (weapon == null)
                player.GiveNamedItem("weapon_knife");
        }

        private string GetRandomWeaponFromList(List<string> weaponsList, bool isVIP, CsTeam team)
        {
            if (Config.Gameplay.RemoveRestrictedWeapons)
            {
                foreach (var weapon in weaponsList)
                {
                    if (CheckIsWeaponRestricted(weapon, isVIP, team))
                        weaponsList.Remove(weapon);
                }
            }
            int index = Random.Next(weaponsList.Count);
            return weaponsList[index];
        }

        public int GetWeaponRestrict(string weaponName, bool isVIP, CsTeam team)
        {
            if (!RestrictedWeapons.ContainsKey(weaponName))
                return 0;
            if (!RestrictedWeapons[weaponName].ContainsKey(ActiveCustomMode.ToString()))
                return 0;

            var restrictInfo = RestrictedWeapons[weaponName][ActiveCustomMode.ToString()][isVIP ? RestrictType.VIP : RestrictType.NonVIP];
            return Config.WeaponsRestrict.Global ? restrictInfo.Global : (team == CsTeam.CounterTerrorist ? restrictInfo.CT : restrictInfo.T);
        }

        public (int, int) GetRestrictData(string weaponName, bool isVIP, CsTeam team)
        {
            if (!RestrictedWeapons.ContainsKey(weaponName))
                return (0, 0);
            if (!RestrictedWeapons[weaponName].ContainsKey(ActiveCustomMode.ToString()))
                return (0, 0);

            var restrictDataVIP = RestrictedWeapons[weaponName][ActiveCustomMode.ToString()][RestrictType.VIP];
            var restrictDataNonVIP = RestrictedWeapons[weaponName][ActiveCustomMode.ToString()][RestrictType.NonVIP];

            if (Config.WeaponsRestrict.Global)
                return (restrictDataNonVIP.Global, restrictDataVIP.Global);

            return team == CsTeam.CounterTerrorist
                ? (restrictDataNonVIP.CT, restrictDataVIP.CT)
                : (restrictDataNonVIP.T, restrictDataVIP.T);
        }
    }
}