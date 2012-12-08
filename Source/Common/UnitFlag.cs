namespace PanzerKontrol
{
	enum UnitFlag
	{
		// Air units don't occupy hexes on the map like regular units.
		// They are capable of attacking any ground target in every round.
		// They can only be targetted automatically by anti-air units.
		Air,
		// Amphibious units are able to cross rivers using only one movement point.
		// Amphibious units also ignore the enemy river defence bonus when attacking enemy units across a river.
		Amphibious,
		// Anti-air units possess the ability to automatically fire at air units attacking nearby targets.
		AntiAir,
		// Artillery units are unable to perform attacks after having moved in a round.
		Artillery,
		// Engineers ignore entrenchment defence bonuses when attacking entrenched enemy units.
		Engineer,
	}
}
