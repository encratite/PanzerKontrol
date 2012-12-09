using System.Collections.Generic;

namespace PanzerKontrol
{
	public class UnitType
	{
		// The name of this unit.
		public string Name { get; set; }

		// The cost of points of this unit during the picking phase.
		public int Price { get; set; }

		// Particularly powerful units might have a limit per army.
		public int? Limit { get; set; }

		// Air units have no hardness
		public double? Hardness { get; set; }

		// The stats of this unit.
		// They are separated in this way because they are also used for upgrades.
		public UnitStats Stats { get; set; }

		// The flags of this unit describe special properties/rules.
		public List<UnitFlag> Flags { get; set; }

		// Upgrades available for this type of unit.
		public List<UnitUpgrade> UpgradesAvailable { get; set; }

		// The maximum number of upgrades that may be purchased.
		public int UpgradeLimit { get; set; }

		public UnitType()
		{
			Stats = new UnitStats();
			Flags = new List<UnitFlag>();
			UpgradesAvailable = new List<UnitUpgrade>();
		}
	}
}
