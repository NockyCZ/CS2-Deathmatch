using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI.Events;
using DeathmatchAPI.Helpers;

namespace DeathmatchAPI;

public interface IDeathmatchAPI
{
    public void StartCustomMode(int modeId);
    public void ChangeNextMode(int modeId);
    public void AddCustomMode(int modeId, ModeData mode);
    public void ChangeCheckDistance(int distance);

    /*
        Team String - Available values: ct | t
        Spawns Dictionary - Vector & QAngle
    */
    public void SetupCustomSpawns(string team, Dictionary<Vector, QAngle> spawns);
    public void SwapHudMessageVisibility(bool visible);
    public int GetActiveModeId();
    public int GetActiveModeRemainingTime();
    public Dictionary<string, ModeData> GetCustomModes();
    public event EventHandler<IDeathmatchEventsAPI> DeathmatchEventHandlers;
    public void TriggerEvent(IDeathmatchEventsAPI @event);
}