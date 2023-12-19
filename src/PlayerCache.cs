//THANKS - https://github.com/K4ryuu/K4-System/blob/9fce7f31f80ae9c55886a0b5c892a04bc5de5964/src/Plugin/PluginCache.cs
namespace Deathmatch
{
    using CounterStrikeSharp.API.Core;
	public class PlayerCache<T> : Dictionary<int, T>
	{
		public T this[CCSPlayerController controller]
		{
			get
			{
				if (controller is null || !controller.IsValid)
				{
					throw new ArgumentException("Invalid player controller");
				}

				if (controller.IsBot || controller.IsHLTV)
				{
					throw new ArgumentException("Player controller is BOT or HLTV");
				}

				if (!base.ContainsKey(controller.Slot))
				{
					throw new KeyNotFoundException($"Player with ID {controller.Slot} not found in cache");
				}

				if (base.TryGetValue(controller.Slot, out T? value))
				{
					return value;
				}

				return default(T)!;
			}
			set
			{
				if (controller is null || !controller.IsValid || !controller.PlayerPawn.IsValid)
				{
					throw new ArgumentException("Invalid player controller");
				}

				if (controller.IsBot || controller.IsHLTV)
				{
					throw new ArgumentException("Player controller is BOT or HLTV");
				}

				this[controller.Slot] = value;
			}
		}

		public bool ContainsPlayer(CCSPlayerController player)
		{
			if (player is null || !player.IsValid || !player.PlayerPawn.IsValid)
			{
				throw new ArgumentException("Invalid player controller");
			}

			if (player.IsBot || player.IsHLTV)
			{
				throw new ArgumentException("Player controller is BOT or HLTV");
			}

			return base.ContainsKey(player.Slot);
		}

		public bool RemovePlayer(CCSPlayerController player)
		{
			if (player is null || !player.IsValid || !player.PlayerPawn.IsValid){
				throw new ArgumentException("Invalid player controller");
			}

			if (player.IsBot || player.IsHLTV)
			{
				throw new ArgumentException("Player controller is BOT or HLTV");
			}

			return base.Remove(player.Slot);
		}
	}
}