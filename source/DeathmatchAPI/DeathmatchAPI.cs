using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI.Events;
using DeathmatchAPI.Helpers;

namespace DeathmatchAPI;

public interface IDeathmatchAPI
{
    public void StartCustomMode(int modeId);
    public void SetNextMode(int modeId);
    public void AddCustomMode(int modeId, ModeData mode);
    public void SetCheckEnemiesSpawnDistance(int distance);
    public void SetupCustomSpawns(List<SpawnData> spawns, bool clearSpawnsDictionary);
    public void SetHudMessageVisibility(bool visible);
    public int GetActiveModeId();
    public int GetActiveModeRemainingTime();
    public int GetDefaultCheckDistance();
    public void ToggleSpawnsDisplay(bool visible);
    public Dictionary<string, ModeData> GetCustomModes();
    public event EventHandler<IDeathmatchEventsAPI> DeathmatchEventHandlers;
    public void TriggerEvent(IDeathmatchEventsAPI @event);
}