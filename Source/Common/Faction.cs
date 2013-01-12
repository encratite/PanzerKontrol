using System.Collections.Generic;
using System.Xml.Serialization;

namespace PanzerKontrol
{
	public class Faction
	{
		// The numeric identifier of this faction.
		// This value is generated automatically by the server based on the order of factions in the configurationfile.
		[XmlIgnore]
		public int? Id;

		// The name of the faction.
		public string Name { get; set; }

		// The lore.
		public string Description { get; set; }

		// Units that may be purchased when playing this faction.
		public List<UnitType> Units { get; set; }

		public Faction()
		{
			Units = new List<UnitType>();
		}

		public void SetIds()
		{
			int id = 0;
			foreach (UnitType unit in Units)
			{
				unit.Faction = this;
				unit.Id = id;
				unit.SetIds();
				id++;
			}
		}

		public UnitType GetUnitType(int unitTypeId)
		{
			if (unitTypeId < 0 || unitTypeId >= Units.Count)
				throw new ServerClientException("Invalid unit type specified");
			return Units[unitTypeId];
		}
	}
}
