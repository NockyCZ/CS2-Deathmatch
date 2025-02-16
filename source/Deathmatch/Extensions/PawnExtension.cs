using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace Deathmatch;

public static class PawnExtension
{
    public static CBasePlayerWeapon? GetActiveWeapon(this CCSPlayerPawn pawn)
    {
        return pawn.WeaponServices?.ActiveWeapon.Value;
    }

    public static CBasePlayerWeapon? GetWeaponFromSlot(this CCSPlayerPawn pawn, gear_slot_t slot)
    {
        return pawn.WeaponServices?.MyWeapons
            .Select(weapon => weapon.Value?.As<CCSWeaponBase>())
            .FirstOrDefault(weaponBase => weaponBase?.VData?.GearSlot == slot);
    }

    public static bool IsHaveWeaponFromSlot(this CCSPlayerPawn pawn, gear_slot_t slot)
    {
        return pawn.WeaponServices?.MyWeapons
            .Any(weapon => weapon.Value?.As<CCSWeaponBase>()?.VData?.GearSlot == slot) ?? false;
    }
}