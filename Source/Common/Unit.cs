using System;
using System.Collections.Generic;

namespace PanzerKontrol
{
	public class Unit
	{
		public readonly PlayerState Owner;
		public readonly int Id;
		public readonly Faction Faction;
		public readonly UnitType Type;
		public readonly List<UnitUpgrade> Upgrades;
		public readonly int Points;

		public UnitStats Stats;

		public bool Deployed;
		public Hex Hex;
		public double Strength;

		public int MovementPoints;
		public bool CanPerformAction;

		public bool Entrenched;

		// The number of small turns a unit has been out of supply
		public int AttritionDuration;

		public Unit(PlayerState owner, int id, UnitConfiguration configuration, Server server)
		{
			Owner = owner;
			Id = id;
			Faction = server.GetFaction(configuration.FactionId);
			Type = Faction.GetUnitType(configuration.UnitTypeId);
			Upgrades = new List<UnitUpgrade>();
			int points = Type.Points;
			HashSet<int> SlotsOccupied = new HashSet<int>();
			foreach (var upgradeId in configuration.Upgrades)
			{
				UnitUpgrade upgrade = Type.GetUpgrade(upgradeId);
				if (SlotsOccupied.Contains(upgrade.Slot))
					throw new ClientException("An upgrade slot was already occupied");
				SlotsOccupied.Add(upgrade.Slot);
				Upgrades.Add(upgrade);
				points += upgrade.Points;
			}
			Points = points;

			Hex = null;
			Strength = 1.0;

			Entrenched = false;

			AttritionDuration = 0;

			UpdateStats();
			ResetUnitForNewTurn();

			// Only air units are automatically "deployed" since they don't need to be placed on the map
			Deployed = Stats.Flags.Contains(UnitFlag.Air);
		}

		public void ResetUnitForNewTurn()
		{
			MovementPoints = Stats.Movement.Value;
			CanPerformAction = true;
		}

		public void MoveToHex(Hex hex)
		{
			Hex = hex;
			hex.Unit = this;
			// Just for convenience on the first move
			Deployed = true;
			// Moving a unit breaks entrenchment
			Entrenched = false;
			// Need to update the stats because of the new terrain
			UpdateStats();
		}

		public double GetDamage(Unit target, bool attacking)
		{
			double hardness = target.Type.Hardness.Value;
			double softness = 1 - hardness;
			if (attacking)
				return Stats.SoftAttack.Value * softness + Stats.HardAttack.Value * hardness;
			else
			{
				int bonus = 0;
				int hexOffsetIndex = Map.GetHexOffsetIndex(target.Hex, Hex);
				RiverEdge riverEdge = Hex.RiverEdges[hexOffsetIndex];
				if (riverEdge != null)
				{
					// Ground attacks across rivers cause defenders to receive a bonus
					bonus = 1;
				}
				return (Stats.SoftDefence.Value + bonus) * softness + (Stats.HardDefence.Value + bonus) * hardness;
			}
		}

		public bool IsArtillery()
		{
			return Stats.Flags.Contains(UnitFlag.Artillery);
		}

		public bool IsAirUnit()
		{
			return Stats.Flags.Contains(UnitFlag.Air);
		}

		public bool IsAntiAirUnit()
		{
			return Stats.Flags.Contains(UnitFlag.AntiAir);
		}

		public bool IsAlive()
		{
			return Strength > 0.0;
		}

		public bool CanEntrench()
		{
			return Stats.Flags.Contains(UnitFlag.Infantry) && MovementPoints == Stats.Movement && CanPerformAction;
		}

		public void Entrench()
		{
			Entrenched = true;
			UpdateStats();
		}

		public void BreakEntrenchment()
		{
			if (Entrenched)
			{
				Entrenched = false;
				UpdateStats();
			}
		}

		public void TakeAttritionDamage()
		{
			if (Stats.Flags.Contains(UnitFlag.Infantry))
				Strength -= GameConstants.InfantryAttritionDamage;
			else
				Strength -= GameConstants.MotorisedAttritionDamage;
			Strength = Math.Max(Strength, GameConstants.MinimumStrength);
			// Units that have run out of supply are unable to perform actions
			CanPerformAction = false;
		}

		void UpdateStats()
		{
			Stats = Type.Stats.Clone();
			foreach (var upgrade in Upgrades)
				Stats.Combine(upgrade.Effect);
			if (Deployed)
			{
				UnitStats terrainBonus = Combat.GetTerrainBonus(Hex.Terrain);
				Stats.Combine(terrainBonus);

				if (Entrenched)
				{
					UnitStats entrenchmentBonus = new UnitStats();
					entrenchmentBonus.SoftDefence = 1;
					entrenchmentBonus.HardDefence = 1;
					entrenchmentBonus.BombardmentDefence = 1;
					Stats.Combine(entrenchmentBonus);
				}
			}
		}
	}
}
