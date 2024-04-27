using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public DeathmatchConfig Config { get; set; } = null!;
        public static int ActiveCustomMode = 0;
        public static int g_iModeTimer = 0;
        public static int g_iRemainingTime = 500;
        public static bool g_bIsActiveEditor = false;
        public static bool g_bDefaultMapSpawnDisabled = false;
        public static bool IsCasualGamemode;
        public ModeData? ActiveMode;
        public MemoryFunctionWithReturn<CCSPlayer_ItemServices, CEconItemView, AcquireMethod, NativeObject, AcquireResult>? CCSPlayer_CanAcquireFunc;
        public MemoryFunctionWithReturn<int, string, CCSWeaponBaseVData>? GetCSWeaponDataFromKeyFunc;
    }
}