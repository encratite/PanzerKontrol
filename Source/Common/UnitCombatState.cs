using System;

namespace PanzerKontrol
{
	class UnitCombatState
	{
		public readonly Unit Unit;

		double Strength;
		double? BaseDamage;
		double CombatEfficiency;
		double TotalDamageDealt;
		int DamageDivisor;

		public double DamageDealt
		{
			get
			{
				return TotalDamageDealt;
			}
		}

		public UnitCombatState(Unit unit, UnitCombat combat)
		{
			Unit = unit;
			Strength = unit.Strength;
			BaseDamage = null;
			CombatEfficiency = combat.GetCombatEfficiency();
			TotalDamageDealt = 0.0;
			DamageDivisor = 1;
		}

		public void SetDamage(double baseDamage)
		{
			BaseDamage = baseDamage;
		}

		public void SetDamageReduction(int damageDivisor)
		{
			DamageDivisor = damageDivisor;
		}

		public double GetDamage()
		{
			double strengthFactor = Math.Pow(Strength, GameConstants.StrengthExponent);
			double damageDealt = strengthFactor * BaseDamage.Value * CombatEfficiency * GameConstants.DamageMitigation / DamageDivisor;
			TotalDamageDealt += damageDealt;
			return damageDealt;
		}

		public void TakeDamage(double damage)
		{
			Strength -= damage;
			if (Strength < GameConstants.MinimumStrength)
				Strength = 0.0;
		}

		public bool IsAlive()
		{
			return Strength > 0.0;
		}
	}
}
