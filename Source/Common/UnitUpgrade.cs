namespace PanzerKontrol
{
	public class UnitUpgrade
	{
		// The name of the upgrade.
		public string Name { get; set; }

		// A description of what the upgrade does.
		public string Description { get; set; }

		// The number of points this upgrade costs.
		public int Price { get; set; }

		// This numerically identifies the virtual slot occupied by the upgrade.
		// This is used to mark upgrades that are incompatible with each other.
		public int Slot { get; set; }

		// Additive bonuses to the unit that is upgraded.
		// They don't have to be positive!
		public UnitStats Bonus { get; set; }
	}
}
