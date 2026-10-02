using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
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
        private CCSGameRules? GameRules;
        public static int NextMode;
        public static string ModeCenterMessage = "";
        public static float ModeMessageHideTime = 0;
        public static string ActiveCustomMode = "";
        public static int ModeTimer = 0;
        public static int RemainingTime = 500;
        public static bool VisibleHud = true;
        public static int CheckedEnemiesDistance = 500;
        public static bool CheckSpawnVisibility;
        public static bool IsCasualGamemode;
        private ConVar? _gameTypeCvar;
        private ConVar? _gameModeCvar;
        // Soundevent guids of the currently playing New Mode Sound per player slot, so it can be stopped
        private readonly Dictionary<int, uint> _newModeSoundGuids = new();
        public static bool DefaultMapSpawnDisabled = false;
        public static string SpawnsPath = "";
        public static ModeData ActiveMode = new();

        // HUD render caches — rebuilt in 1s tick branch, consumed by OnTick to avoid per-tick string allocation
        public static string HudCenterHtml = "";
        public static string HudCenterMsg = "";
        public static bool HudHasContent = false;
        private List<CCSPlayerController> _hudPlayerCache = new();
        private float _lastHudPlayerCacheTime = -10f;

        // Cached delegate refs so Unload can RemoveListener/UnhookUserMessage symmetrically
        private CounterStrikeSharp.API.Core.Listeners.OnMapStart? _onMapStartDelegate;
        private CounterStrikeSharp.API.Core.Listeners.OnMapEnd? _onMapEndDelegate;
        private CounterStrikeSharp.API.Core.Listeners.OnTick? _onTickDelegate;
        private CounterStrikeSharp.API.Modules.UserMessages.UserMessage.UserMessageHandler? _hookDecals;
        private CounterStrikeSharp.API.Modules.UserMessages.UserMessage.UserMessageHandler? _hookPoints;
        private CounterStrikeSharp.API.Modules.UserMessages.UserMessage.UserMessageHandler? _hookRespawnSound;
        private CounterStrikeSharp.API.Modules.UserMessages.UserMessage.UserMessageHandler? _hookHudMessages;
        private readonly List<(string cmd, CommandInfo.CommandListenerCallback cb)> _registeredCommandListeners = new();
    }
}