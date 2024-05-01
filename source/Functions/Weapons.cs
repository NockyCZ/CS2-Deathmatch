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
            if (player == null || !player.IsValid || player.PlayerPawn == null || player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null || !player.PawnIsAlive)
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
            Random rand = new Random();
            int index = rand.Next(weaponsList.Count);
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