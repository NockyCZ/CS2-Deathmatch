using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public bool CheckIsWeaponRestricted(string weaponName, bool isVIP, CsTeam team, bool bPrimary)
        {
            if (ActiveMode == null || GetAllDeathmatchPlayers().Count < 2)
                return false;

            var weaponsList = bPrimary ? ActiveMode.PrimaryWeapons : ActiveMode.SecondaryWeapons;
            if (weaponsList.Count == 1)
                return false;

            if (!RestrictedWeapons.ContainsKey(weaponName))
                return false;

            int restrictValue = GetWeaponRestrict(weaponName, isVIP, team);
            if (restrictValue == 0)
                return false;

            if (restrictValue < 0)
                return true;

            var playersList = Config.WeaponsRestrict.Global ? GetAllDeathmatchPlayers() : GetAllDeathmatchPlayers().Where(p => p.Team == team).ToList();
            int matchingCount = playersList.Count(p => playerData.ContainsPlayer(p) &&
                                                      ((bPrimary && playerData[p].PrimaryWeapon == weaponName) ||
                                                       (!bPrimary && playerData[p].SecondaryWeapon == weaponName)));

            return matchingCount >= restrictValue;
        }

        public CBasePlayerWeapon? GetWeaponFromSlot(CCSPlayerController player, gear_slot_t slot)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || pawn.WeaponServices == null)
                return null;

            return pawn.WeaponServices.MyWeapons
                .Select(weapon => weapon.Value?.As<CCSWeaponBase>())
                .FirstOrDefault(weaponBase => weaponBase != null && weaponBase.VData != null && weaponBase.VData.GearSlot == slot);
        }

        public bool IsHaveWeaponFromSlot(CCSPlayerController player, gear_slot_t slot)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || pawn.WeaponServices == null || !player.PawnIsAlive)
                return false;

            return pawn.WeaponServices.MyWeapons
                .Select(weapon => weapon.Value?.As<CCSWeaponBase>())
                .Any(weaponBase => weaponBase != null && weaponBase.VData != null && weaponBase.VData.GearSlot == slot);
        }

        public void RemovePlayerWeapon(CCSPlayerController player, string weaponName)
        {
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || pawn.WeaponServices == null || !player.PawnIsAlive)
                return;

            var replacements = new Dictionary<string, string>
            {
                { "weapon_m4a1_silencer", "weapon_m4a1" },
                { "weapon_usp_silencer", "weapon_hkp2000" },
                { "weapon_mp5sd", "weapon_mp7" }
            };

            if (replacements.TryGetValue(weaponName, out var replacement))
            {
                weaponName = replacement;
            }

            var weapon = pawn.WeaponServices.MyWeapons
                .Select(weapon => weapon.Value?.As<CCSWeaponBase>())
                .FirstOrDefault(weaponBase => weaponBase != null && weaponBase.DesignerName.Contains(weaponName));

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

        public (int, int) GetRestrictData(string weaponName, CsTeam team)
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