using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI.Helpers;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public bool CheckIsWeaponRestricted(string weaponName, bool isVIP, CsTeam team, bool bPrimary)
        {
            var players = Utilities.GetPlayers().Where(p => playerData.ContainsPlayer(p)).ToList();
            if (players == null || players.Count < 2)
                return false;

            var weaponsList = bPrimary ? ActiveMode.PrimaryWeapons : ActiveMode.SecondaryWeapons;
            if (weaponsList.Count == 1)
                return false;

            if (!Config.RestrictedWeapons.Restrictions.ContainsKey(weaponName))
                return false;

            int restrictValue = GetWeaponRestrict(weaponName, isVIP, team);
            if (restrictValue == 0)
                return false;

            if (restrictValue < 0)
                return true;

            var playersList = Config.RestrictedWeapons.Global ? players : players.Where(p => p.Team == team).ToList();
            int matchingCount = playersList.Count(p => (bPrimary && playerData[p].PrimaryWeapon.TryGetValue(ActiveCustomMode, out var primary) && primary == weaponName) || (!bPrimary && playerData[p].SecondaryWeapon.TryGetValue(ActiveCustomMode, out var secondary) && secondary == weaponName));

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

        private string GetRandomWeaponFromList(List<string> weaponsList, ModeData modeData, bool isVIP, CsTeam team, bool bPrimary)
        {
            if (weaponsList.Count > 0)
            {
                if (!modeData.RandomWeapons && Config.Gameplay.RemoveRestrictedWeapons)
                    weaponsList.RemoveAll(weapon => CheckIsWeaponRestricted(weapon, isVIP, team, bPrimary));

                //if (weaponsList.Count == 1)
                //    return weaponsList[0];

                int index = Random.Next(weaponsList.Count);
                return weaponsList[index];
            }
            return "";
        }

        public int GetWeaponRestrict(string weaponName, bool isVIP, CsTeam team)
        {
            if (!Config.RestrictedWeapons.Restrictions.ContainsKey(weaponName) || !Config.RestrictedWeapons.Restrictions[weaponName].ContainsKey(ActiveCustomMode))
                return 0;

            var restrictInfo = Config.RestrictedWeapons.Restrictions[weaponName][ActiveCustomMode][isVIP ? RestrictType.VIP : RestrictType.NonVIP];
            return Config.RestrictedWeapons.Global ? restrictInfo.Global : (team == CsTeam.CounterTerrorist ? restrictInfo.CT : restrictInfo.T);
        }

        public (int, int) GetRestrictData(string weaponName, CsTeam team)
        {
            if (!Config.RestrictedWeapons.Restrictions.ContainsKey(weaponName))
                return (0, 0);
            if (!Config.RestrictedWeapons.Restrictions[weaponName].ContainsKey(ActiveCustomMode))
                return (0, 0);

            var restrictDataVIP = Config.RestrictedWeapons.Restrictions[weaponName][ActiveCustomMode][RestrictType.VIP];
            var restrictDataNonVIP = Config.RestrictedWeapons.Restrictions[weaponName][ActiveCustomMode][RestrictType.NonVIP];

            if (Config.RestrictedWeapons.Global)
                return (restrictDataNonVIP.Global, restrictDataVIP.Global);

            return team == CsTeam.CounterTerrorist
                ? (restrictDataNonVIP.CT, restrictDataVIP.CT)
                : (restrictDataNonVIP.T, restrictDataVIP.T);
        }
    }
}