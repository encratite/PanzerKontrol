using System.Collections.Generic;

namespace PanzerKontrol
{
	public class Unit
	{
		public readonly UnitType Type;
		public readonly List<UnitUpgrade> Upgrades;
		public readonly UnitStats Stats;
		public readonly int Points;

		public Unit(UnitConfiguration configuration, GameServer server)
		{
			Faction faction = server.GetFaction(configuration.FactionId);
			Type = faction.GetUnitType(configuration.UnitTypeId);
			Upgrades = new List<UnitUpgrade>();
			int points = Type.Points;
			Stats = Type.Stats.Clone();
			foreach (var upgradeId in configuration.Upgrades)
			{
				UnitUpgrade upgrade = Type.GetUpgrade(upgradeId);
				Upgrades.Add(upgrade);
				Stats.Combine(upgrade.Effect);
				points += upgrade.Points;
			}
			Points = points;
		}
	}
}
