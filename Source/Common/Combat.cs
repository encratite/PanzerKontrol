using System;
using System.Collections.Generic;
using System.Linq;

namespace PanzerKontrol
{
	enum AttackType
	{
		GroundAttack,
		ArtilleryAttack,
		AirAttack,
	}

	class Combat
	{
		// These are bonuses for defenders on certain terrain
		static Dictionary<TerrainType, UnitStats> TerrainBonuses;

		bool UseRandomisedCombatEfficiency;
		NormalDistribution Generator;

		UnitCombatState Attacker;
		UnitCombatState Defender;

		List<UnitCombatState> AntiAirUnits;

		AttackType AttackType;

		public double AttackerStrength
		{
			get
			{
				return Attacker.Strength;
			}
		}

		public double DefenderStrength
		{
			get
			{
				return Defender.Strength;
			}
		}

		#region Constructors

		public Combat(Unit attacker, Unit defender, bool useRandomisedCombatEfficiency, List<Unit> antiAirUnits = null)
		{
			UseRandomisedCombatEfficiency = useRandomisedCombatEfficiency;
			Generator = new NormalDistribution(GameConstants.CombatEfficiencyMean, GameConstants.CombatEfficiencyDeviation);

			Attacker = new UnitCombatState(attacker, this);
			Defender = new UnitCombatState(defender, this);

			if (attacker.IsAirUnit())
			{
				AttackType = AttackType.AirAttack;
				AntiAirUnits = antiAirUnits.Select((Unit x) => new UnitCombatState(x, this)).ToList();
				foreach (var unit in AntiAirUnits)
					unit.SetDamage(unit.Unit.Type.Stats.AirAttack.Value);
			}
			else if (attacker.IsArtillery())
				AttackType = AttackType.ArtilleryAttack;
			else
				AttackType = AttackType.GroundAttack;

			// Calculate the outcome right away
			Attack();
		}

		#endregion

		#region Public utility functions

		public double GetCombatEfficiency()
		{
			if (UseRandomisedCombatEfficiency)
				return Generator.Get();
			else
				return GameConstants.CombatEfficiencyMean;
		}

		public static UnitStats GetTerrainBonus(TerrainType terrain)
		{
			UnitStats output;
			if (!TerrainBonuses.TryGetValue(terrain, out output))
				throw new Exception("Unknown terrain");
			return output;
		}

		#endregion

		#region Generic internal functions

		void Attack()
		{
			double attackerDamage = Attacker.Unit.GetDamage(Defender.Unit, true);
			Attacker.SetDamage(attackerDamage);
			switch (AttackType)
			{
				case AttackType.GroundAttack:
					double defenderDamage = Defender.Unit.GetDamage(Attacker.Unit, false);
					Defender.SetDamage(defenderDamage);
					GroundAttack();
					break;
				case AttackType.ArtilleryAttack:
					Defender.SetDamageReduction(Defender.Unit.Stats.BombardmentDefence.Value);
					Bombard();
					break;
				case AttackType.AirAttack:
					Attacker.SetDamageReduction(Attacker.Unit.Stats.AntiAirDefence.Value);
					Defender.SetDamageReduction(Defender.Unit.Stats.BombardmentDefence.Value);
					Bombard();
					break;
				default:
					throw new Exception("Unknown attack type specified");
			}
		}

		bool TargetDamageReached()
		{
			double[] damage = { Attacker.DamageDealt, Defender.DamageDealt };
			Array.Sort(damage);
			double lowerDamage = damage[0];
			double higherDamage = damage[1];
			return lowerDamage > GameConstants.LowerTargetDamage && higherDamage > GameConstants.HigherTargetDamage;
		}

		// This is used to calculate the outcome of regular infantry/armour ground attacks
		void GroundAttack()
		{
			int round;
			for (round = 0; Attacker.IsAlive() && Defender.IsAlive() && !TargetDamageReached(); round++)
			{
				double attackerDamage = Attacker.GetDamage();
				double defenderDamage = Defender.GetDamage();
				Attacker.TakeDamage(defenderDamage);
				Defender.TakeDamage(attackerDamage);
			}
		}

		double GetStrengthAdjustedDamageFactor(UnitCombatState target, double minimumEfficiency)
		{
			double efficiency = minimumEfficiency + target.Strength * (1 - minimumEfficiency);
			return efficiency;
		}

		// This is used to calculate the outcome of artillery strikes and attacks of air units on ground units (including casualties to anti-air fire)
		void Bombard()
		{
			int round;
			for (round = 0; Attacker.IsAlive() && Defender.IsAlive() && round < GameConstants.BombardmentAttacks; round++)
			{
				double bombardmentEfficiency = GetStrengthAdjustedDamageFactor(Defender, GameConstants.BombardmentEfficiencyMinimum);
				double attackerDamage = bombardmentEfficiency * Attacker.GetDamage();
				Defender.TakeDamage(attackerDamage);
				if (AttackType == AttackType.AirAttack)
				{
					// Calculate air casualties to anti-air fire
					foreach (var unit in AntiAirUnits)
					{
						double antiAirEfficiency = GetStrengthAdjustedDamageFactor(Attacker, GameConstants.AntiAirEfficiencyMinimum);
						double antiAirDamage = unit.GetDamage();
						Attacker.TakeDamage(antiAirDamage);
					}
				}
			}
		}

		static void InitialiseTerrainBonuses()
		{
			if (TerrainBonuses == null)
			{
				UnitStats clear = new UnitStats();
				UnitStats forest = new UnitStats();
				forest.SoftDefence = 1;
				forest.HardDefence = 1;
				forest.BombardmentDefence = 2;
				UnitStats mountain = new UnitStats();
				mountain.SoftDefence = 2;
				mountain.HardDefence = 2;
				mountain.BombardmentDefence = 2;
				UnitStats swamp = new UnitStats();
				swamp.SoftDefence = 1;
				swamp.HardDefence = 2;
				UnitStats hill = new UnitStats();
				forest.SoftDefence = 1;
				forest.HardDefence = 1;

				TerrainBonuses = new Dictionary<TerrainType, UnitStats>();
				TerrainBonuses[TerrainType.Clear] = clear;
				TerrainBonuses[TerrainType.Forest] = forest;
				TerrainBonuses[TerrainType.Mountain] = mountain;
				TerrainBonuses[TerrainType.Swamp] = swamp;
				TerrainBonuses[TerrainType.Hill] = hill;
			}
		}

		#endregion
	}
}
