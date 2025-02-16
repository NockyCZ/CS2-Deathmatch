using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using DeathmatchAPI;
using DeathmatchAPI.Events;
using DeathmatchAPI.Helpers;
using static DeathmatchAPI.Preferences;

namespace Deathmatch;

public partial class Deathmatch : IDeathmatchAPI
{
    public event EventHandler<IDeathmatchEventsAPI>? DeathmatchEventHandlers;
    public void TriggerEvent(IDeathmatchEventsAPI @event)
    {
        DeathmatchEventHandlers?.Invoke(this, @event);
    }

    public void ToggleSpawnsDisplay(bool visible)
    {
        if (visible)
            ShowAllSpawnPoints();
        else
            RemoveSpawnModels();
    }

    public void StartCustomMode(int modeId)
    {
        if (!Config.CustomModes.ContainsKey(modeId.ToString()))
            throw new Exception($"A Custom mode with ID '{modeId}' cannot be started, because this mode does not exist!");

        SetupCustomMode(modeId.ToString());
    }

    public void SetNextMode(int modeId)
    {
        if (!Config.CustomModes.ContainsKey(modeId.ToString()))
            throw new Exception($"A Custom mode with ID '{modeId}' cannot be set as next mode, because this mode does not exist!");

        NextMode = modeId;
    }

    public void AddCustomMode(int modeId, ModeData mode)
    {
        if (Config.CustomModes.ContainsKey(modeId.ToString()))
            throw new Exception($"A Custom mode with ID '{modeId}' cannot be added, because this mode already exists!");

        Config.CustomModes.Add(modeId.ToString(), mode);
    }

    public void SetCheckEnemiesSpawnDistance(int distance)
    {
        CheckedEnemiesDistance = distance;
    }

     public void SetCheckSpawnVisibility(bool value)
    {
        CheckSpawnVisibility = value;
    }

    public void SetHudMessageVisibility(bool visible)
    {
        VisibleHud = visible;
    }

    public int GetActiveModeId()
    {
        return int.Parse(ActiveCustomMode);
    }

    public int GetActiveModeRemainingTime()
    {
        return RemainingTime;
    }

    public Dictionary<string, ModeData> GetCustomModes()
    {
        return Config.CustomModes;
    }

    public int GetDefaultCheckDistance()
    {
        return Config.SpawnSystem.DistanceRespawn;
    }

    public bool GetDefaultCheckSpawnVisibility()
    {
        return Config.SpawnSystem.CheckVisible;
    }

    public void SetupCustomSpawns(List<SpawnData> spawns, bool clearSpawnsDictionary)
    {
        if (clearSpawnsDictionary)
        {
            spawnPositionsCT.Clear();
            spawnPositionsT.Clear();
        }

        foreach (var data in spawns)
        {
            switch (data.Team)
            {
                case CsTeam.CounterTerrorist:
                    spawnPositionsCT[data.Position] = data.Angle;
                    break;
                case CsTeam.Terrorist:
                    spawnPositionsT[data.Position] = data.Angle;
                    break;
            }
        }
    }

    public Preference? RegisterPreference(string name, PreferencesBooleanData data, bool vipOnly = false)
    {
        var preference = Preference.RegisterPreference(name, data, vipOnly);
        if (preference == null)
            return null;

        if (!data.CommandShortcuts.Any())
            return preference;

        foreach (var cmd in data.CommandShortcuts)
        {
            var cmdName = cmd;
            if (!cmdName.Contains("css_"))
                cmdName = $"css_{cmdName}";

            AddCommand(cmdName, "Switch Boolean Player Preferences", (player, info) =>
            {
                if (player == null || !player.IsValid || !playerData.ContainsKey(player.Slot))
                    return;

                if (preference.VipOnly && !AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag))
                    return;

                SwitchBooleanPrefsValue(player, name);
            });
        }
        return preference;
    }

    public Preference? RegisterPreference(string name, PreferencesData data, bool vipOnly = false)
    {
        var preference = Preference.RegisterPreference(name, data, vipOnly);
        if (preference == null)
            return null;

        if (!data.CommandShortcuts.Any())
            return preference;

        foreach (var cmd in data.CommandShortcuts)
        {
            var cmdName = cmd;
            if (!cmdName.Contains("css_"))
                cmdName = $"css_{cmdName}";

            AddCommand(cmdName, "Switch String Player Preferences", (player, info) =>
            {
                if (player == null || !player.IsValid || !playerData.TryGetValue(player.Slot, out var PlayerData))
                    return;

                if (preference.VipOnly && !AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag))
                    return;

                var currentValue = GetPrefsValue(PlayerData, name, data.DefaultValue);
                SwitchStringPrefsValue(player, name, data.Options, currentValue);
            });
        }
        return preference;
    }

    public List<Preference> GetAllPreferences()
    {
        return Preference.GetAllPreferences();
    }

    public Preference? GetPreferenceByName(string name)
    {
        return Preference.GetPreferenceByName(name);
    }

    public Categorie? RegisterMenuCategory(string name, string menuTitle, string menuOption, bool useLocalizer = false)
    {
        return Categorie.AddCustomCategory(name, menuTitle, menuOption, useLocalizer);
    }

    public void RemoveMenuCategory(string name)
    {
        Categorie.RemoveCategory(name);
    }

    public void RemoveMenuCategory(Categorie category)
    {
        Categorie.RemoveCategory(category);
    }

    public Categorie? GetCategoryByName(string name)
    {
        return Categorie.GetCategoryByName(name);
    }

    public void AddMenuPreferenceOption(Categorie? category, Preference preference)
    {
        Menu.AddPreferenceOption(category, preference);
    }

    public void AddMenuOption(string name, Categorie? category, Action<CCSPlayerController, Menu> onChoose, string? flag = null)
    {
        Menu.AddOption(name, category, onChoose);
    }
}