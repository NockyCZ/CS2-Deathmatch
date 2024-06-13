using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public bool CheckIsWeaponRestricted(string weaponName, bool isVIP, CsTeam team, bool bPrimary)
        {
            if (ActiveMode == null)
                return false;

            if (bPrimary)
            {
                if (ActiveMode.PrimaryWeapons.Count == 1)
                    return false;
            }
            else
            {
                if (ActiveMode.SecondaryWeapons.Count == 1)
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

        public bool IsHaveWeaponFromSlot(CCSPlayerController player, gear_slot_t slot)
        {
            if (player == null || !player.IsValid || player.PlayerPawn == null || player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null || !player.PawnIsAlive)
                return false;

            return player.PlayerPawn.Value.WeaponServices.MyWeapons
                .Select(weapon => weapon.Value?.As<CCSWeaponBase>())
                .Where(weaponBase => weaponBase != null && weaponBase.VData != null && weaponBase.VData.GearSlot == slot)
                .Count() > 0;
        }

        public void RemovePlayerWeapon(CCSPlayerController player, string weaponName)
        {
            if (player.PlayerPawn == null || player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null || !player.PawnIsAlive)
                return;

            switch (weaponName)
            {
                case "weapon_m4a1_silencer":
                    weaponName = "weapon_m4a1";
                    break;

                case "weapon_usp_silencer":
                    weaponName = "weapon_hkp2000";
                    break;

                case "weapon_mp5sd":
                    weaponName = "weapon_mp7";
                    break;
            }

            var weapon = player.PlayerPawn.Value.WeaponServices.MyWeapons
                .Select(weapon => weapon.Value?.As<CCSWeaponBase>())
                .Where(weaponBase => weaponBase != null && weaponBase.VData != null && weaponBase.DesignerName.Contains(weaponName)).FirstOrDefault();

            if (weapon != null)
                weapon.Remove();
        }

        private string GetRandomWeaponFromList(List<string> weaponsList, bool isVIP, CsTeam team, bool bPrimary)
        {
            if (weaponsList.Count > 0)
            {
                if (!ActiveMode!.RandomWeapons && Config.Gameplay.RemoveRestrictedWeapons)
                    weaponsList.RemoveAll(weapon => CheckIsWeaponRestricted(weapon, isVIP, team, bPrimary));

                int index = Random.Next(weaponsList.Count);
                return weaponsList[index];
            }
            return "";
        }

        public int GetWeaponRestrict(string weaponName, bool isVIP, CsTeam team)
        {
            if (!RestrictedWeapons.ContainsKey(weaponName) || !RestrictedWeapons[weaponName].ContainsKey(ActiveCustomMode.ToString()))
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