namespace PanzerKontrol
{
	// These are important constants used in the rules of the game
	class GameConstants
	{
		public const double CombatEfficiencyMean = 1.0;
		public const double CombatEfficiencyDeviation = 0.1;
		public const double LowerTargetDamage = 0.1;
		public const double HigherTargetDamage = 0.3;
		public const double DamageMitigation = 1.0 / 200;
		public const double StrengthExponent = 0.95;
		public const double MinimumStrength = 0.05;
		public const double BombardmentEfficiencyMinimum = 0.3;
		public const double BombardmentAttacks = 25;
		public const double AntiAirEfficiencyMinimum = 0.5;
	}
}
