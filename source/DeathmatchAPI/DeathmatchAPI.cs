using CounterStrikeSharp.API.Core;
using DeathmatchAPI.Events;
using DeathmatchAPI.Helpers;
using static DeathmatchAPI.Preferences;

namespace DeathmatchAPI;

public interface IDeathmatchAPI
{
    public Preference? RegisterPreference(string name, PreferencesBooleanData data, bool vipOnly = false);
    public Preference? RegisterPreference(string name, PreferencesData data, bool vipOnly = false);
    public List<Preference> GetAllPreferences();
    public Preference? GetPreferenceByName(string name);
    public Categorie? RegisterMenuCategory(string name, string menuTitle, string menuOption, bool useLocalizer = false);
    public void RemoveMenuCategory(string name);
    public void RemoveMenuCategory(Categorie category);
    public Categorie? GetCategoryByName(string name);
    public void AddMenuPreferenceOption(Categorie? category, Preference preference);
    public void AddMenuOption(string name, Categorie? category, Action<CCSPlayerController, Menu> onChoose, string? flag = null);

    public void StartCustomMode(int modeId);
    public void SetNextMode(int modeId);
    public void AddCustomMode(int modeId, ModeData mode);
    public void SetCheckEnemiesSpawnDistance(int distance);
    public void SetCheckSpawnVisibility(bool value);
    public void SetupCustomSpawns(List<SpawnData> spawns, bool clearSpawnsDictionary);
    public void SetHudMessageVisibility(bool visible);
    public int GetActiveModeId();
    public int GetActiveModeRemainingTime();
    public int GetDefaultCheckDistance();
    public bool GetDefaultCheckSpawnVisibility();
    public void ToggleSpawnsDisplay(bool visible);
    public List<SpawnData> GetActiveSpawns();
    public List<SpawnData> GetDefaultSpawns();
    public Dictionary<string, ModeData> GetCustomModes();
    public event EventHandler<IDeathmatchEventsAPI> DeathmatchEventHandlers;
    public void TriggerEvent(IDeathmatchEventsAPI @event);
}