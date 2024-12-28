using System.Diagnostics.Eventing.Reader;
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

            var playersList = Config.RestrictedWeapons.Global ? players : players.Where(p => p.Team == team);
            int matchingCount = playersList.Count(p => bPrimary
                ? playerData[p].PrimaryWeapon.TryGetValue(ActiveCustomMode, out var primary) && primary == weaponName
                : playerData[p].SecondaryWeapon.TryGetValue(ActiveCustomMode, out var secondary) && secondary == weaponName);

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

        private string GetRandomWeaponFromList(List<string> weaponsList, ModeData modeData, bool isVIP, CsTeam team, bool bPrimary)
        {
            if (weaponsList.Any())
            {
                if (!modeData.RandomWeapons && (Config.Gameplay.DefaultModeWeapons != 1 || Config.Gameplay.DefaultModeWeapons != 2))
                    weaponsList.RemoveAll(weapon => CheckIsWeaponRestricted(weapon, isVIP, team, bPrimary));

                int index = Random.Next(weaponsList.Count);
                return weaponsList[index];
            }
            return "";
        }

        public int GetWeaponRestrict(string weaponName, bool isVIP, CsTeam team)
        {
            if (!Config.RestrictedWeapons.Restrictions.TryGetValue(weaponName, out var weaponRestrictions) || !weaponRestrictions.TryGetValue(ActiveCustomMode, out var restrictTypes))
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
            switch (bullets)
            {
                case -1:
                    return Localizer["Chat.Disabled"];
                case 0:
                    return Localizer["Chat.Unlimited"];
                default:
                    return bullets.ToString();
            }
        }
    }
}