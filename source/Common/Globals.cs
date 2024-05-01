using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public DeathmatchConfig Config { get; set; } = null!;
        public static int NextMode;
        public static string ModeCenterMessage = "";
        public static int ActiveCustomMode = 0;
        public static int ModeTimer = 0;
        public static int RemainingTime = 500;
        public static bool IsActiveEditor = false;
        public static bool IsCasualGamemode;
        public static bool DefaultMapSpawnDisabled = false;
        public ModeData? ActiveMode;
        public MemoryFunctionWithReturn<CCSPlayer_ItemServices, CEconItemView, AcquireMethod, NativeObject, AcquireResult>? CCSPlayer_CanAcquireFunc;
        public MemoryFunctionWithReturn<int, string, CCSWeaponBaseVData>? GetCSWeaponDataFromKeyFunc;
    }
}