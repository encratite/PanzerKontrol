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

	class UnitCombat
	{
		bool UseRandomisedCombatEfficiency;
		NormalDistribution Generator;

		UnitCombatState Attacker;
		UnitCombatState Defender;

		List<UnitCombatState> AntiAirUnits;

		AttackType AttackType;

		#region Static constructors

		public static UnitCombat GroundAttack(Unit attacker, Unit defender, bool useRandomisedCombatEfficiency)
		{
			return new UnitCombat(attacker, defender, null, AttackType.GroundAttack, useRandomisedCombatEfficiency);
		}

		public static UnitCombat ArtilleryAttack(Unit attacker, Unit target, bool useRandomisedCombatEfficiency)
		{
			return new UnitCombat(attacker, target, null, AttackType.ArtilleryAttack, useRandomisedCombatEfficiency);
		}

		public static UnitCombat AirAttack(Unit attacker, Unit target, List<Unit> antiAirUnits, bool useRandomisedCombatEfficiency)
		{
			return new UnitCombat(attacker, target, antiAirUnits, AttackType.AirAttack, useRandomisedCombatEfficiency);
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

		#endregion

		#region Generic internal functions

		UnitCombat(Unit attacker, Unit defender, List<Unit> antiAirUnits, AttackType attackType, bool useRandomisedCombatEfficiency)
		{
			UseRandomisedCombatEfficiency = useRandomisedCombatEfficiency;
			Generator = new NormalDistribution(GameConstants.CombatEfficiencyMean, GameConstants.CombatEfficiencyDeviation);

			Attacker = new UnitCombatState(attacker, this);
			Defender = new UnitCombatState(defender, this);

			if (antiAirUnits != null)
			{
				AntiAirUnits = antiAirUnits.Select((Unit x) => new UnitCombatState(x, this)).ToList();
				foreach (var unit in AntiAirUnits)
					unit.SetDamage(unit.Unit.Type.Stats.AirAttack.Value);
			}

			AttackType = attackType;

			// Calculate the outcome right away
			Attack();
		}

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

		// This is used to calculate the outcome of artillery strikes and attacks of air units on ground units (including casualties to anti-air fire)
		void Bombard()
		{
			int round;
			for (round = 0; Attacker.IsAlive() && Defender.IsAlive() && round < GameConstants.BombardmentAttacks; round++)
			{
				double attackerDamage = Attacker.GetDamage();
				Defender.TakeDamage(attackerDamage);
				if (AttackType == AttackType.AirAttack)
				{
					// Calculate air casualties to anti-air fire
					foreach (var unit in AntiAirUnits)
					{
						double antiAirDamage = unit.GetDamage();
						Attacker.TakeDamage(antiAirDamage);
					}
				}
			}
		}

		#endregion
	}
}
