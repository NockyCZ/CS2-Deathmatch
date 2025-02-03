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

    public static CBasePlayerWeapon? SetActiveWeapon(this CCSPlayerPawn pawn, CCSPlayerController player, gear_slot_t slot)
    {
        if (pawn.WeaponServices == null)
            return null;

        var weapon = pawn.GetWeaponFromSlot(slot);
        if (weapon == null || weapon.Value == null)
            return null;

        var model = weapon.Value.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName;
        if (!string.IsNullOrEmpty(model))
        {
            pawn.WeaponServices.ActiveWeapon.Raw = weapon.Raw;
            Utilities.SetStateChanged(player, "CPlayer_WeaponServices", "m_hActiveWeapon");
            pawn.GetViewModel()?.SetModel(model);
        }
        return weapon.Value;
    }

    public static CHandle<CBasePlayerWeapon>? GetWeaponFromSlot(this CCSPlayerPawn pawn, gear_slot_t slot)
    {
        return pawn?.WeaponServices?.MyWeapons
            .Select(weapon => weapon)
            .FirstOrDefault(weaponBase => weaponBase != null && weaponBase.Value?.As<CCSWeaponBase>().VData != null && weaponBase.Value?.As<CCSWeaponBase>().VData?.GearSlot == slot);
    }

    private static unsafe CBaseViewModel? GetViewModel(this CCSPlayerPawn pawn)
    {
        var handle = pawn.ViewModelServices?.Handle;
        if (handle == null || !handle.HasValue)
            return null;

        var viewModelServices = new CCSPlayer_ViewModelServices(handle.Value);

        var ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        var viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

        var viewModel = new CHandle<CBaseViewModel>(viewModels[0]);

        return viewModel.Value;
    }
}