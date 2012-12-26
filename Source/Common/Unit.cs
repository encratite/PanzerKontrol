using System.Collections.Generic;

namespace PanzerKontrol
{
	public class Unit
	{
		public readonly int Id;
		public readonly Faction Faction;
		public readonly UnitType Type;
		public readonly List<UnitUpgrade> Upgrades;
		public readonly UnitStats Stats;
		public readonly int Points;

		public Position Position;

		public Unit(int id, UnitConfiguration configuration, GameServer server)
		{
			Id = id;
			Faction = server.GetFaction(configuration.FactionId);
			Type = Faction.GetUnitType(configuration.UnitTypeId);
			Upgrades = new List<UnitUpgrade>();
			int points = Type.Points;
			Stats = Type.Stats.Clone();
			HashSet<int> SlotsOccupied = new HashSet<int>();
			foreach (var upgradeId in configuration.Upgrades)
			{
				UnitUpgrade upgrade = Type.GetUpgrade(upgradeId);
				if (SlotsOccupied.Contains(upgrade.Slot))
					throw new ClientException("An upgrade slot was already occupied");
				SlotsOccupied.Add(upgrade.Slot);
				Upgrades.Add(upgrade);
				Stats.Combine(upgrade.Effect);
				points += upgrade.Points;
			}
			Points = points;
			Position = null;
		}
	}
}
