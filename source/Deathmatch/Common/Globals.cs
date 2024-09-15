using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using DeathmatchAPI;
using DeathmatchAPI.Helpers;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        private static readonly Random Random = new Random();
        public static PluginCapability<IDeathmatchAPI> DeathmatchAPI { get; } = new("deathmatch");
        public DeathmatchConfig Config { get; set; } = new();
        public static CCSPlayerController? ActiveEditor = null;
        public static int NextMode;
        public static string ModeCenterMessage = "";
        public static string ActiveCustomMode = "";
        public static int ModeTimer = 0;
        public static int RemainingTime = 500;
        public static bool VisibleHud = true;
        public static int CheckedEnemiesDistance = 500;
        public static bool IsCasualGamemode;
        public static bool DefaultMapSpawnDisabled = false;
        public static bool IsLinuxServer;
        public static ModeData ActiveMode = new();
        public MemoryFunctionWithReturn<CCSPlayer_ItemServices, CEconItemView, AcquireMethod, NativeObject, AcquireResult>? CCSPlayer_CanAcquireFunc;
        public MemoryFunctionWithReturn<int, string, CCSWeaponBaseVData>? GetCSWeaponDataFromKeyFunc;
    }
}