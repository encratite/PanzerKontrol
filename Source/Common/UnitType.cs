using System.Collections.Generic;
using System.Xml.Serialization;

namespace PanzerKontrol
{
	[XmlType("Unit")]
	public class UnitType
	{
		// This is the faction the unit is from, for convenience
		[XmlIgnore]
		public Faction Faction;

		// The numeric identifier of this unit.
		// This value is generated automatically by the server based on the order of units in the configuration file.
		[XmlIgnore]
		public int? Id;

		// The name of this unit.
		public string Name { get; set; }

		// A brief description of the purpose/type of this unit.
		public string Description { get; set; }

		// How many points this unit costs.
		public int Points { get; set; }

		// Particularly powerful units might have a limit per army.
		public int? Limit { get; set; }

		// Air units have no hardness.
		public double? Hardness { get; set; }

		// The stats of this unit.
		// They are separated in this way because they are also used for upgrades.
		public UnitStats Stats { get; set; }

		// Upgrades available for this type of unit.
		public List<UnitUpgrade> Upgrades { get; set; }

		public UnitType()
		{
			Stats = new UnitStats();
			Upgrades = new List<UnitUpgrade>();
		}

		public void SetIds()
		{
			int id = 0;
			foreach (var upgrade in Upgrades)
			{
				upgrade.Id = id;
				id++;
			}
		}

		public UnitUpgrade GetUpgrade(int upgradeId)
		{
			if (upgradeId < 0 || upgradeId >= Upgrades.Count)
				throw new GameException("Invalid upgrade ID");
			return Upgrades[upgradeId];
		}
	}
}
