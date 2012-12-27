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

		public bool Deployed;
		public Position Position;
		public double Strength;

		public int? MovementPoints;
		public bool? CanPerformAction;
		public bool? WasUsedInCurrentTurn;
		public bool? WasUsedInCurrentMicroTurn;

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

			Deployed = false;
			Position = null;
			Strength = 1.0;

			MovementPoints = null;
			CanPerformAction = null;
			WasUsedInCurrentTurn = null;
			WasUsedInCurrentMicroTurn = null;
		}

		public void ResetUnitForNewTurn()
		{
			MovementPoints = Stats.Movement;
			CanPerformAction = true;
			WasUsedInCurrentTurn = false;
			WasUsedInCurrentMicroTurn = false;
		}
	}
}
