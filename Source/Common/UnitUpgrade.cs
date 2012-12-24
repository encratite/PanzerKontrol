using System.Xml.Serialization;

namespace PanzerKontrol
{
	[XmlType("Upgrade")]
	public class UnitUpgrade
	{
		[XmlIgnore]
		public int? Id { get; set; }

		// The name of the upgrade.
		public string Name { get; set; }

		// The number of points this upgrade costs.
		public int Points { get; set; }

		// This numerically identifies the virtual slot occupied by the upgrade.
		// This is used to mark upgrades that are incompatible with each other.
		public int Slot { get; set; }

		// Additive bonuses to the unit that is upgraded.
		// They don't have to be positive!
		public UnitStats Effect { get; set; }
	}
}
