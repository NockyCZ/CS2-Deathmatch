using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using Newtonsoft.Json;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;

namespace Deathmatch
{
	public partial class Deathmatch
	{
		[GameEventHandler(HookMode.Post)]
		public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
		{
			//var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist").Concat(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")).Concat(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_deathmatch_spawn"));
			//SendConsoleMessage($"spawns entities: {spawns.Count()}", ConsoleColor.DarkCyan);
			var player = @event.Userid;
			if (player != null && player.IsValid && !player.IsBot && !player.IsHLTV && player.SteamID.ToString().Length == 17 && !playerData.ContainsKey(player.Slot))
			{
				var data = new DeathmatchPlayerData
				{
					BlockRandomWeaponsIntegeration = Server.CurrentTime,
				};
				playerData[player.Slot] = data;
				if (Config.SaveWeapons)
				{
					_ = UpdateOrLoadPlayerData(player, player.SteamID.ToString(), null, true);
				}
				else
				{
					bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);
					SetupDefaultWeapons(data, player.Team, IsVIP);
					SetupDefaultPreferences(data, IsVIP);
				}
			}
			return HookResult.Continue;
		}

		[GameEventHandler(HookMode.Pre)]
		public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
		{
			var player = @event.Userid;
			if (player != null && player.IsValid)
			{
				if (playerData.TryGetValue(player.Slot, out var data))
				{
					if (Config.SaveWeapons)
					{
						string[] preferences = {
							JsonConvert.SerializeObject(data.PrimaryWeapon),
							JsonConvert.SerializeObject(data.SecondaryWeapon),
							JsonConvert.SerializeObject(data.Preferences),
						};
						_ = UpdateOrLoadPlayerData(player, player.SteamID.ToString(), preferences, false);
					}
					playerData.Remove(player.Slot);
				}
				blockedSpawns.Remove(player.Slot);
				playersWaitingForRespawn.Remove(player.Slot);
				playersWithSpawnProtection.Remove(player.Slot);
			}

			return HookResult.Continue;
		}

		[GameEventHandler(HookMode.Post)]
		public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
		{
			if (GameRules == null)
				SetGameRules();

			var player = @event.Userid;
			if (player != null && player.IsValid)
			{
				//if (Config.SpawnSystem.SpawnsMethod != 2 && (CheckedEnemiesDistance > 100 || CheckSpawnVisibility) && GameRules?.WarmupPeriod == false)
				if (Config.SpawnSystem.SpawnsMethod != 2 && CheckedEnemiesDistance > 100 && GameRules?.WarmupPeriod == false)
					PerformRespawn(player, player.Team);

				GivePlayerWeapons(player, false);
			}

			return HookResult.Continue;
		}

		[GameEventHandler(HookMode.Pre)]
		public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
		{
			var player = @event.Userid;
			var attacker = @event.Attacker;
			if (Config.Gameplay.DisplayAllKillFeed)
				info.DontBroadcast = true;

			if (player == null || !player.IsValid)
				return HookResult.Continue;

			var timer = 1.0f;
			bool IsVIP = AdminManager.PlayerHasPermissions(attacker, Config.PlayersSettings.VIPFlag);
			if (playerData.TryGetValue(player.Slot, out var data))
			{
				data.KillStreak = 0;
				if (Config.PlayersPreferences.DamageInfo.Enabled)
				{
					foreach (var kv in playerData)
					{
						if (kv.Key != attacker?.Slot)
							kv.Value.DamageInfo.Remove(player.Slot);
					}

					if (attacker != null && attacker.IsValid && attacker != player && GetPrefsValue(data, "DamageInfo", Config.PlayersPreferences.DamageInfo.DefaultValue))
					{
						if (data.DamageInfo.TryGetValue(attacker.Slot, out var damageInfo))
						{
							var givenDamageMessage = Localizer["Chat.GivenDamageVictim", attacker.PlayerName, damageInfo.Damage, damageInfo.Hits];
							if (!string.IsNullOrEmpty(givenDamageMessage))
								player.PrintToChat(Localizer["Chat.Prefix"] + " " + givenDamageMessage);
						}
						else
						{
							var givenDamageMessage = Localizer["Chat.NoDamageGiven", attacker.PlayerName];
							if (!string.IsNullOrEmpty(givenDamageMessage))
								player.PrintToChat(Localizer["Chat.Prefix"] + " " + givenDamageMessage);
						}
						data.DamageInfo.Clear();
					}
				}
				timer = IsVIP ? Config.PlayersSettings.VIP.RespawnTime : Config.PlayersSettings.NonVIP.RespawnTime;

				if (Config.Gameplay.DisplayAllKillFeed)
					@event.FireEventToClient(player);
			}

			// Only schedule respawn if the player is still tracked — disconnect handler clears playerData
			if (player.IsValid && playerData.ContainsKey(player.Slot))
			{
				playersWaitingForRespawn[player.Slot] = (timer, Server.CurrentTime);
			}
			if (attacker != null && attacker.IsValid && attacker != player && playerData.TryGetValue(attacker.Slot, out var attackerData) && attacker.PlayerPawn.Value != null)
			{
				attackerData.KillStreak++;
				if (GetPrefsValue(attackerData, "DamageInfo", Config.PlayersPreferences.DamageInfo.DefaultValue))
				{
					if (attackerData.DamageInfo.TryGetValue(player.Slot, out var damageInfo))
					{
						var givenDamageMessage = Localizer["Chat.GivenDamageAttacker", player.PlayerName, damageInfo.Damage, damageInfo.Hits];
						if (!string.IsNullOrEmpty(givenDamageMessage))
							attacker.PrintToChat(Localizer["Chat.Prefix"] + " " + givenDamageMessage);
						attackerData.DamageInfo.Remove(player.Slot);
					}
				}

				bool IsHeadshot = @event.Headshot;
				bool IsKnifeKill = @event.Weapon.Contains("knife") || @event.Weapon.Contains("bayonet");

				if (IsHeadshot && Config.PlayersPreferences.HSKillSound.Enabled && GetPrefsValue(attackerData, "HeadshotKillSound", Config.PlayersPreferences.HSKillSound.DefaultValue))
					PlaySound(attacker, Config.PlayersPreferences.HSKillSound.Path);
				else if (IsKnifeKill && Config.PlayersPreferences.KnifeKillSound.Enabled && GetPrefsValue(attackerData, "KnifeKillSound", Config.PlayersPreferences.KnifeKillSound.DefaultValue))
					PlaySound(attacker, Config.PlayersPreferences.KnifeKillSound.Path);
				else if (Config.PlayersPreferences.KillSound.Enabled && GetPrefsValue(attackerData, "KillSound", Config.PlayersPreferences.KillSound.DefaultValue))
					PlaySound(attacker, Config.PlayersPreferences.KillSound.Path);

				var Health = IsHeadshot
				? (IsVIP ? Config.PlayersSettings.VIP.HeadshotHealth : Config.PlayersSettings.NonVIP.HeadshotHealth)
				: (IsVIP ? Config.PlayersSettings.VIP.KillHealth : Config.PlayersSettings.NonVIP.KillHealth);

				var refillAmmo = IsHeadshot
				? (IsVIP ? Config.PlayersSettings.VIP.RefillAmmoHS : Config.PlayersSettings.NonVIP.RefillAmmoHS)
				: (IsVIP ? Config.PlayersSettings.VIP.RefillAmmo : Config.PlayersSettings.NonVIP.RefillAmmo);

				var giveHP = 100 >= attacker.PlayerPawn.Value.Health + Health ? Health : 100 - attacker.PlayerPawn.Value.Health;

				if (refillAmmo)
				{
					var allWeapons = IsVIP ? Config.PlayersSettings.VIP.ReffilAllWeapons : Config.PlayersSettings.NonVIP.ReffilAllWeapons;
					if (allWeapons)
					{
						var weapons = attacker.PlayerPawn.Value.WeaponServices?.MyWeapons.Where(w => w.Value != null && (ActiveMode.SecondaryWeapons.Contains(w.Value.DesignerName) || ActiveMode.PrimaryWeapons.Contains(w.Value.DesignerName))).ToList();
						if (weapons != null)
						{
							foreach (var weapon in weapons)
							{
								if (weapon.Value == null)
									continue;

								weapon.Value.Clip1 = 250;
								weapon.Value.ReserveAmmo[0] = 250;
								//Utilities.SetStateChanged(weapon.Value.As<CCSWeaponBase>(), "CBasePlayerWeapon", "m_iClip1");
							}
						}
					}
					else
					{
						var activeWeapon = attacker.PlayerPawn.Value.WeaponServices?.ActiveWeapon.Value;
						if (activeWeapon != null)
						{
							activeWeapon.Clip1 = 250;
							activeWeapon.ReserveAmmo[0] = 250;
							//Utilities.SetStateChanged(activeWeapon.As<CCSWeaponBase>(), "CBasePlayerWeapon", "m_iClip1");
						}
					}
				}
				if (giveHP > 0)
				{
					attacker.PlayerPawn.Value.Health += giveHP;
					Utilities.SetStateChanged(attacker.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
				}

				if (ActiveMode.Armor != 0)
				{
					string armor = ActiveMode.Armor == 1 ? "item_kevlar" : "item_assaultsuit";
					attacker.GiveNamedItem(armor);
				}

				if (Config.Gameplay.DisplayAllKillFeed)
					@event.FireEventToClient(attacker);
			}
			return HookResult.Continue;
		}

		[GameEventHandler(HookMode.Pre)]
		public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
		{
			var attacker = @event.Attacker;
			var player = @event.Userid;

			if (player == null || !player.IsValid || attacker == player)
				return HookResult.Continue;

			if (attacker != null && attacker.IsValid && playerData.TryGetValue(attacker.Slot, out var attackerData))
			{
				bool isKnife = @event.Weapon.Contains("knife") || @event.Weapon.Contains("bayonet");
				if (ActiveMode.OnlyHS)
				{
					if (@event.Hitgroup == 1)
					{
						if (Config.PlayersPreferences.DamageInfo.Enabled && GetPrefsValue(attackerData, "DamageInfo", Config.PlayersPreferences.DamageInfo.DefaultValue))
						{
							if (!attackerData.DamageInfo.TryGetValue(player.Slot, out var damageInfo))
							{
								damageInfo = new DamageData();
								attackerData.DamageInfo[player.Slot] = damageInfo;
							}
							damageInfo.Damage += @event.DmgHealth;
							damageInfo.Hits++;
						}
						if (Config.PlayersPreferences.HitSound.Enabled && GetPrefsValue(attackerData, "HitSound", Config.PlayersPreferences.HitSound.DefaultValue) && !isKnife)
							PlaySound(attacker, Config.PlayersPreferences.HitSound.Path);
					}
				}
				else
				{
					if (@event.Hitgroup != 1)
					{
						if (!isKnife && Config.PlayersPreferences.HitSound.Enabled && GetPrefsValue(attackerData, "HitSound", Config.PlayersPreferences.HitSound.DefaultValue))
						{
							if (!GetPrefsValue(attackerData, "OnlyHS", Config.PlayersPreferences.OnlyHS.DefaultValue))
								PlaySound(attacker, Config.PlayersPreferences.HitSound.Path);
						}
					}
					else if (!isKnife && Config.PlayersPreferences.HitSound.Enabled && GetPrefsValue(attackerData, "HitSound", Config.PlayersPreferences.HitSound.DefaultValue))
					{
						PlaySound(attacker, Config.PlayersPreferences.HitSound.Path);
					}

					if (Config.PlayersPreferences.DamageInfo.Enabled && GetPrefsValue(attackerData, "DamageInfo", Config.PlayersPreferences.DamageInfo.DefaultValue))
					{
						if (!attackerData.DamageInfo.TryGetValue(player.Slot, out var damageInfo))
						{
							damageInfo = new DamageData();
							attackerData.DamageInfo[player.Slot] = damageInfo;
						}
						damageInfo.Damage += @event.DmgHealth;
						damageInfo.Hits++;
					}
				}
			}
			return HookResult.Continue;
		}

		private HookResult OnTakeDamage(DynamicHook hook)
		{
			var entity = hook.GetParam<CEntityInstance>(0);
			if (entity == null || !entity.IsValid || !entity.DesignerName.Equals("player"))
				return HookResult.Continue;

			var pawn = entity.As<CCSPlayerPawn>();
			if (pawn == null || !pawn.IsValid)
				return HookResult.Continue;

			var player = pawn.OriginalController.Get();
			if (player == null || !player.IsValid)
				return HookResult.Continue;

			var damageInfo = hook.GetParam<CTakeDamageInfo>(1);
			if (playerData.TryGetValue(player.Slot, out var victimData) && victimData.SpawnProtection)
			{
				damageInfo.Damage = 0;
				return HookResult.Continue;
			}

			var attacker = damageInfo.Attacker.Value?.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();
			if (attacker == null || !attacker.IsValid)
				return HookResult.Continue;

			if (!ActiveMode.KnifeDamage && damageInfo.Ability.Value != null && (damageInfo.Ability.Value.DesignerName.Contains("knife") || damageInfo.Ability.Value.DesignerName.Contains("bayonet")))
			{
				attacker.PrintToCenter(Localizer["Hud.KnifeDamageIsDisabled"]);
				damageInfo.Damage = 0;
				return HookResult.Continue;
			}

			if (damageInfo.GetHitGroup() != HitGroup_t.HITGROUP_HEAD && playerData.TryGetValue(attacker.Slot, out var attackerData) && GetPrefsValue(attackerData, "OnlyHS", Config.PlayersPreferences.OnlyHS.DefaultValue))
			{
				damageInfo.Damage = 0;
				return HookResult.Continue;
			}

			return HookResult.Continue;
		}

		private HookResult OnEntityTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
		{
			// 1. Verify the entity receiving damage is a player pawn
			if (entity == null || !entity.IsValid || entity.DesignerName != "cs_player_pawn")
				return HookResult.Continue;

			CCSPlayerPawn pawn = new CCSPlayerPawn(entity.Handle);

			if (pawn == null || !pawn.IsValid)
				return HookResult.Continue;

			var player = pawn.OriginalController.Get();
			if (player == null || !player.IsValid)
				return HookResult.Continue;

			var damageInfo = info;
			if (playerData.TryGetValue(player.Slot, out var victimData) && victimData.SpawnProtection)
			{
				damageInfo.Damage = 0;
				return HookResult.Continue;
			}

			var attacker = damageInfo.Attacker.Value?.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();
			if (attacker == null || !attacker.IsValid)
				return HookResult.Continue;

			if (!ActiveMode.KnifeDamage && damageInfo.Ability.Value != null && (damageInfo.Ability.Value.DesignerName.Contains("knife") || damageInfo.Ability.Value.DesignerName.Contains("bayonet")))
			{
				attacker.PrintToCenter(Localizer["Hud.KnifeDamageIsDisabled"]);
				damageInfo.Damage = 0;
				return HookResult.Continue;
			}

			if (damageInfo.GetHitGroup() != HitGroup_t.HITGROUP_HEAD && playerData.TryGetValue(attacker.Slot, out var attackerData) && GetPrefsValue(attackerData, "OnlyHS", Config.PlayersPreferences.OnlyHS.DefaultValue))
			{
				damageInfo.Damage = 0;
				return HookResult.Continue;
			}

			return HookResult.Continue;
		}

		private HookResult OnWeaponCanAcquire(DynamicHook hook)
		{
			var item = hook.GetParam<CEconItemView>(1);
			if (item == null || item.Handle == IntPtr.Zero)
				return HookResult.Continue;

			var services = hook.GetParam<CCSPlayer_ItemServices>(0);
			// FIX: PointerTo requires checking .Value first
			if (services == null || services.Pawn.Value == null)
				return HookResult.Continue;

			var pawn = services.Pawn.Value.As<CCSPlayerPawn>();
			// FIX: Check pawn.IsValid and Controller.Value safely
			if (pawn == null || !pawn.IsValid || pawn.Controller.Value == null)
				return HookResult.Continue;

			var player = pawn.Controller.Value.As<CCSPlayerController>();
			if (player == null || !player.IsValid)
				return HookResult.Continue;

			string weaponName = GetSafeWeaponName(item.ItemDefinitionIndex);
			if (weaponName == "unknown")
			{
				// If it's a custom knife or unknown item, just let them have it to avoid crashing
				return HookResult.Continue;
			}

			// --- ORIGINAL LOGIC ---
			if (hook.GetParam<AcquireMethod>(2) == AcquireMethod.PickUp)
			{
				if (!ActiveMode.PrimaryWeapons.Contains(weaponName) && !ActiveMode.SecondaryWeapons.Contains(weaponName))
				{
					if (weaponName.Contains("knife") || weaponName.Contains("bayonet") || ActiveMode.Utilities.Contains(weaponName))
						return HookResult.Continue;

					hook.SetReturn(AcquireResult.AlreadyOwned);
					return HookResult.Handled;
				}
				return HookResult.Continue;
			}

			if (!IsCasualGamemode && !player.IsBot && IsHaveBlockedRandomWeaponsIntegration(player))
			{
				hook.SetReturn(AcquireResult.AlreadyPurchased);
				return HookResult.Handled;
			}

			if (ActiveMode.RandomWeapons)
			{
				PlaySound(player, Config.SoundSettings.CantEquipSound);
				player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsSelectIsDisabled"]}");
				hook.SetReturn(AcquireResult.AlreadyPurchased);
				return HookResult.Handled;
			}

			if (!ActiveMode.PrimaryWeapons.Contains(weaponName) && !ActiveMode.SecondaryWeapons.Contains(weaponName))
			{
				if (!player.IsBot)
				{
					PlaySound(player, Config.SoundSettings.CantEquipSound);

					string replacedweaponName = Localizer[weaponName];
					player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsDisabled", replacedweaponName]}");
				}
				hook.SetReturn(AcquireResult.AlreadyPurchased);
				return HookResult.Handled;
			}

			if (playerData.TryGetValue(player.Slot, out var data))
			{
				string localizerWeaponName = Localizer[weaponName];
				bool IsVIP = AdminManager.PlayerHasPermissions(player, Config.PlayersSettings.VIPFlag);

				bool IsPrimary = PrimaryWeaponsList.Contains(weaponName);
				if (CheckIsWeaponRestricted(weaponName, IsVIP, player.Team, ActiveMode.PrimaryWeapons, ActiveCustomMode, IsPrimary))
				{
					PlaySound(player, Config.SoundSettings.CantEquipSound);

					(int NonVIP, int VIP) restrictInfo = GetRestrictData(weaponName, player.Team);
					player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponIsRestricted", localizerWeaponName, GetWeaponRestrictLozalizer(restrictInfo.NonVIP), GetWeaponRestrictLozalizer(restrictInfo.VIP)]}");
					hook.SetReturn(AcquireResult.NotAllowedByMode);
					return HookResult.Handled;
				}

				var p = player.PlayerPawn.Value;
				if (IsPrimary)
				{
					if (data.PrimaryWeapon.TryGetValue(ActiveCustomMode, out var primaryWeapon) && weaponName == primaryWeapon)
					{
						player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
						hook.SetReturn(AcquireResult.AlreadyOwned);
						return HookResult.Handled;
					}
					data.PrimaryWeapon[ActiveCustomMode] = weaponName;
					player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.PrimaryWeaponSet", localizerWeaponName]}");

					var weapon = p?.GetWeaponFromSlot(gear_slot_t.GEAR_SLOT_RIFLE);
					if (!Config.Gameplay.SwitchWeapons && weapon != null)
					{
						hook.SetReturn(AcquireResult.AlreadyOwned);
						return HookResult.Handled;
					}
				}
				else
				{
					if (data.SecondaryWeapon.TryGetValue(ActiveCustomMode, out var secondaryWeapon) && weaponName == secondaryWeapon)
					{
						player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.WeaponsIsAlreadySet", localizerWeaponName]}");
						hook.SetReturn(AcquireResult.AlreadyOwned);
						return HookResult.Handled;
					}
					data.SecondaryWeapon[ActiveCustomMode] = weaponName;
					player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SecondaryWeaponSet", localizerWeaponName]}");

					var weapon = p?.GetWeaponFromSlot(gear_slot_t.GEAR_SLOT_PISTOL);
					if (!Config.Gameplay.SwitchWeapons && weapon != null)
					{
						hook.SetReturn(AcquireResult.AlreadyOwned);
						return HookResult.Handled;
					}
				}
			}
			return HookResult.Continue;
		}

		private string GetSafeWeaponName(ushort index)
		{
			return index switch
			{
				1 => "weapon_deagle",
				2 => "weapon_elite",
				3 => "weapon_fiveseven",
				4 => "weapon_glock",
				7 => "weapon_ak47",
				8 => "weapon_aug",
				9 => "weapon_awp",
				10 => "weapon_famas",
				11 => "weapon_g3sg1",
				13 => "weapon_galilar",
				14 => "weapon_m249",
				16 => "weapon_m4a1",
				17 => "weapon_mac10",
				19 => "weapon_p90",
				23 => "weapon_mp5sd",
				24 => "weapon_ump45",
				25 => "weapon_xm1014",
				26 => "weapon_bizon",
				27 => "weapon_mag7",
				28 => "weapon_negev",
				29 => "weapon_sawedoff",
				30 => "weapon_tec9",
				31 => "weapon_taser",
				32 => "weapon_hkp2000",
				33 => "weapon_mp7",
				34 => "weapon_mp9",
				35 => "weapon_nova",
				36 => "weapon_p250",
				38 => "weapon_scar20",
				39 => "weapon_sg556",
				40 => "weapon_ssg08",
				42 => "weapon_knife",
				43 => "weapon_flashbang",
				44 => "weapon_hegrenade",
				45 => "weapon_smokegrenade",
				46 => "weapon_molotov",
				47 => "weapon_decoy",
				48 => "weapon_incgrenade",
				49 => "weapon_c4",
				59 => "weapon_knife_t",
				60 => "weapon_m4a1_silencer",
				61 => "weapon_usp_silencer",
				63 => "weapon_cz75a",
				64 => "weapon_revolver",
				_ => "unknown"
			};
		}

		[GameEventHandler]
		public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
		{
			if (Config.General.RemoveBreakableEntities)
				RemoveBreakableEntities();

			return HookResult.Continue;
		}

		[GameEventHandler]
		public HookResult OnNewMatchBegin(EventBeginNewMatch @event, GameEventInfo info)
		{
			SetupCustomMode(Config.Gameplay.MapStartMode.ToString());
			return HookResult.Continue;
		}

		private HookResult OnPlayerRadioMessage(CCSPlayerController? player, CommandInfo info)
		{
			if (Config.General.BlockRadioMessage)
				return HookResult.Handled;
			return HookResult.Continue;
		}

		private HookResult OnPlayerChatwheel(CCSPlayerController? player, CommandInfo info)
		{
			if (Config.General.BlockPlayerChatWheel)
				return HookResult.Handled;
			return HookResult.Continue;
		}

		private HookResult OnPlayerPing(CCSPlayerController? player, CommandInfo info)
		{
			if (Config.General.BlockPlayerPing)
				return HookResult.Handled;
			return HookResult.Continue;
		}

		private HookResult OnRandomWeapons(CCSPlayerController? player, CommandInfo info)
		{
			return HookResult.Handled;
		}
	}
}
