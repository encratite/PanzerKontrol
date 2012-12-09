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

		// Additive bonuses to the unit that is upgraded.
		public UnitStats Bonus { get; set; }
	}
}
