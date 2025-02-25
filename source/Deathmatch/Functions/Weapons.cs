using System.Diagnostics.Eventing.Reader;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI.Helpers;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public bool CheckIsWeaponRestricted(string weaponName, bool isVIP, CsTeam team, List<string> weaponsList, string modeId, bool isPrimary)
        {
            if (weaponsList.Count <= 1)
                return false;

            if (!Config.RestrictedWeapons.Restrictions.ContainsKey(weaponName))
                return false;

            int restrictValue = GetWeaponRestrict(weaponName, isVIP, team, modeId);
            if (restrictValue == 0)
                return false;

            if (restrictValue < 0)
                return true;

            var players = Utilities.GetPlayers().Where(p => playerData.ContainsKey(p.Slot));
            if (players.Count() < 2)
                return false;

            var playersList = Config.RestrictedWeapons.Global ? players : players.Where(p => p.Team == team);
            int matchingCount = playersList.Count(p => isPrimary
                ? playerData[p.Slot].PrimaryWeapon.TryGetValue(modeId, out var primary) && primary == weaponName
                : playerData[p.Slot].SecondaryWeapon.TryGetValue(modeId, out var secondary) && secondary == weaponName);

            return matchingCount >= restrictValue;
        }

        private string GetRandomWeaponFromList(List<string> weaponsList, ModeData modeData, bool isVIP, CsTeam team, string modeId, bool bPrimary)
        {
            if (!weaponsList.Any())
                return "";

            var filteredWeapons = !modeData.RandomWeapons && Config.Gameplay.DefaultModeWeapons != 1 && Config.Gameplay.DefaultModeWeapons != 2
                ? weaponsList.Where(weapon => !CheckIsWeaponRestricted(weapon, isVIP, team, weaponsList, modeId, bPrimary)).ToList()
                : weaponsList;

            return filteredWeapons.Any() ? filteredWeapons[Random.Shared.Next(filteredWeapons.Count)] : "";
        }

        public int GetWeaponRestrict(string weaponName, bool isVIP, CsTeam team, string modeId)
        {
            if (!Config.RestrictedWeapons.Restrictions.TryGetValue(weaponName, out var weaponRestrictions) || !weaponRestrictions.TryGetValue(modeId, out var restrictTypes))
                return 0;

            var restrictInfo = restrictTypes[isVIP ? RestrictType.VIP : RestrictType.NonVIP];
            return Config.RestrictedWeapons.Global ? restrictInfo.Global : (team == CsTeam.CounterTerrorist ? restrictInfo.CT : restrictInfo.T);
        }

        public (int NonVIP, int VIP) GetRestrictData(string weaponName, CsTeam team)
        {
            if (!Config.RestrictedWeapons.Restrictions.TryGetValue(weaponName, out var weaponRestrictions) || !weaponRestrictions.TryGetValue(ActiveCustomMode, out var restrictTypes))
                return (0, 0);

            var restrictDataNonVIP = restrictTypes[RestrictType.NonVIP];
            var restrictDataVIP = restrictTypes[RestrictType.VIP];

            return Config.RestrictedWeapons.Global
                ? (restrictDataNonVIP.Global, restrictDataVIP.Global)
                : team == CsTeam.CounterTerrorist
                    ? (restrictDataNonVIP.CT, restrictDataVIP.CT)
                    : (restrictDataNonVIP.T, restrictDataVIP.T);
        }

        public string GetWeaponRestrictLozalizer(int bullets)
        {
            return bullets switch
            {
                -1 => Localizer["Chat.Disabled"],
                0 => Localizer["Chat.Unlimited"],
                _ => bullets.ToString()
            };
        }
    }
}