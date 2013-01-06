namespace PanzerKontrol
{
	// These are important constants used in the rules of the game
	class GameConstants
	{
		// In the multiplayer mode of this game each player is given the same amount of points they may use to purchase their initial army
		// A certain fraction (50%) of the unspent points are added to the reinforcement points of a player
		// These points may be used to reinforce existing units after a game has already started
		// This fraction is the reinforcement points penalty factor
		public const double ReinforcementPointsPenaltyFactor = 0.5;

		// Even without having unspent points after the initial deployment players are given a certain ratio (30%) of the base points they were given initially as reinforcement points for later use
		public const double ReinforcementPointsBaseRatio = 0.3;

		// These are two constants used by the normally distributed pseudo-random number generator used in the combat system of this game
		// The combat efficiency is a factor applied to the damage a unit deals for an entire fight
		// It is randomly chosen in the beginning of a fight
		// They are the mean and standard deviation parameters of the normal distribution used
		public const double CombatEfficiencyMean = 1.0;
		public const double CombatEfficiencyDeviation = 0.1;

		// Ground attacks have certain requirements for for losses on both sides
		// Attacks continue until the winner has taken at least 10% casualties and the loser has taken 30% casualties
		// This is used to cause a moderate number of casualties on both sides while still using a Lanchester's Law style system
		public const double LowerTargetDamage = 0.1;
		public const double HigherTargetDamage = 0.3;

		// This is a damage mitigation factor used to limit the damage dealt per attack/defence point per unit strength in each iteration of the combat system
		public const double DamageMitigation = 1.0 / 200;

		// This exponent is an attempt at implementing a light Lanchester's Law style effect in the combat system where damaged units become disproportionally more weak than undamaged ones
		public const double StrengthExponent = 0.95;

		// This means that a unit whose strength has fallen below 5% has been destroyed and is removed from the battlefield
		// This limit is useful to prevent the existence of excessively useless ultra low strength units that can also cause excessive number of iterations in the ground attack system due to the target damage requirements in the combat system
		public const double MinimumStrength = 0.05;

		// Artillery fire and air attacks on ground targets become increasingly less effective as units lose strength
		// This means that it's generally best to use attacks of this type against targets that are still at full strength (1.0) as their area of effect damage reaches their full potential
		// It means that the damage mitigation factor cannot fall below 30%
		public const double BombardmentEfficiencyMinimum = 0.3;

		// This is the number of combat iterations used to simulate the damage of artillery fire and attacks by aircraft
		// This is necessary because these attacks do not rely on the "target damage" requirements of ground attacks
		public const double BombardmentAttacks = 25;

		// This is basically the equivalent of BombardmentEfficiencyMinimum but for anti-air fire
		// As bombers lose strength they also become less susceptible to anti-air fire and the efficiency may go down as far as 50%
		public const double AntiAirEfficiencyMinimum = 0.5;

		// As units move on the battlefield they capture not only the hex grids they passed but possibly also others which can be captured indirectly
		// The conditions for indirect captures are:
		// - the capturing unit must be entering a region (that means a connected group of hexes that are currently not owned by the player) that is currently not occupied by any other units
		// - the size of the region must not exceed the number of hex grids specified by this constant
		public const int IndirectCaptureLimit = 3;
	}
}
