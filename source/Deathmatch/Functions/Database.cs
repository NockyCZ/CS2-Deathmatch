using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using MySqlConnector;
using Newtonsoft.Json;

namespace Deathmatch
{
    public partial class Deathmatch
    {
        public static Dictionary<string, string>? GetPlayerWeaponsFromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Deathmatch] An error occurred while loading player weapons: '{ex.Message}'", ConsoleColor.Red);
                throw new Exception($"An error occurred while loading player weapons: {ex.Message}");
            }
        }

        public static Dictionary<string, object>? GetPlayerPreferencesFromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Deathmatch] An error occurred while loading player preferences: '{ex.Message}'", ConsoleColor.Red);
                throw new Exception($"An error occurred while loading player preferences: {ex.Message}");
            }
        }

        public async Task UpdateOrLoadPlayerData(CCSPlayerController player, string SteamID, string[]? data, bool load = true)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string sqlLoad = @"
                                INSERT INTO `dm_players` (`steamid`)
                                VALUES (
                                    @steamid
                                )
                                ON DUPLICATE KEY UPDATE
                                    `steamid` = @steamid;

                                SELECT
                                    `primary_weapons`,
                                    `secondary_weapons`,
                                    `preferences`
                                FROM
                                    `dm_players`
                                WHERE
                                    `steamid` = @steamid;
                            ";

                    string sqlUpdate = @"
                                INSERT INTO `dm_players` (`steamid`, `primary_weapons`, `secondary_weapons`, `preferences`)
                                VALUES (
                                    @steamid,
                                    @primary_weapons,
                                    @secondary_weapons,
                                    @preferences
                                )
                                ON DUPLICATE KEY UPDATE
                                    `primary_weapons` = @primary_weapons,
                                    `secondary_weapons` = @secondary_weapons,
                                    `preferences` = @preferences
                            ";

                    using (var cmd = new MySqlCommand(load ? sqlLoad : sqlUpdate, connection))
                    {
                        cmd.Parameters.AddWithValue("@steamid", SteamID);
                        if (load)
                        {
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    var primaryWeapons = reader["primary_weapons"] != DBNull.Value ? GetPlayerWeaponsFromJson(reader.GetString("primary_weapons")) : null;
                                    var secondaryWeapons = reader["secondary_weapons"] != DBNull.Value ? GetPlayerWeaponsFromJson(reader.GetString("secondary_weapons")) : null;
                                    var preferences = reader["preferences"] != DBNull.Value ? GetPlayerPreferencesFromJson(reader.GetString("preferences")) : null;

                                    Server.NextFrame(() =>
                                    {
                                        if (playerData.TryGetValue(player.Slot, out var data))
                                        {
                                            bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
                                            if (primaryWeapons == null || secondaryWeapons == null)
                                            {
                                                SetupDefaultWeapons(data, player.Team, IsVIP);
                                            }
                                            else
                                            {
                                                data.PrimaryWeapon = primaryWeapons;
                                                data.SecondaryWeapon = secondaryWeapons;
                                                SetupDefaultWeapons(data, player.Team, IsVIP);
                                            }

                                            if (preferences == null)
                                            {
                                                SetupDefaultPreferences(data, IsVIP);
                                            }
                                            else
                                            {
                                                data.Preferences = preferences;
                                                SetupDefaultPreferences(data, IsVIP);
                                            }
                                        }
                                    });
                                }
                            }
                        }
                        else
                        {
                            if (data != null)
                            {
                                cmd.Parameters.AddWithValue("@primary_weapons", data[0]);
                                cmd.Parameters.AddWithValue("@secondary_weapons", data[1]);
                                cmd.Parameters.AddWithValue("@preferences", data[2]);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Deathmatch] An error occurred while loading/updating player data: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        private MySqlConnection GetConnection()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = Config.Database.Host,
                Port = Config.Database.Port,
                UserID = Config.Database.User,
                Database = Config.Database.DatabaseName,
                Password = Config.Database.Password,
                Pooling = true
            };

            return new MySqlConnection(builder.ConnectionString);
        }
        public async Task CreateDatabaseConnection()
        {
            using MySqlConnection connection = GetConnection();
            try
            {
                await CreateTable();
                SendConsoleMessage("[Deathmatch] The database has been connected!", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Deathmatch] Unable to connect to the database: '{ex.Message}'", ConsoleColor.Red);
            }
        }

        public async Task CreateTable()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    using var cmd = new MySqlCommand(
                    @"CREATE TABLE IF NOT EXISTS dm_players (
                        steamid VARCHAR(32) PRIMARY KEY UNIQUE NOT NULL,
                        primary_weapons TEXT DEFAULT NULL,
                        secondary_weapons TEXT DEFAULT NULL,
                        preferences TEXT DEFAULT NULL
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;", connection);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                SendConsoleMessage($"[Deathmatch] An error occurred while creating database table: '{ex.Message}'", ConsoleColor.Red);
            }
        }
    }
}